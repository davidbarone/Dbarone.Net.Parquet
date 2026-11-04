
using Dbarone.Net.Buffers.Document;

namespace Dbarone.Net.Parquet.Dremel;

/// <summary>
/// Dremel Implementation
/// 
/// Assembly
/// 1. Create IEnumerable Dictionary to store results
/// 2. For each row:
/// 
/// </summary>
public class DremelProcessor : IDremelProcessor
{
  #region Shred Public Methods
  public Dictionary<SchemaNode, ColumnBuffer> Shred(SchemaNode root, IEnumerable<IDictionary<string, object?>> rows)
  {
    var leafColumns = SchemaNode.GetLeafNodes(root);
    var buffers = leafColumns.ToDictionary(c => c, c => new ColumnBuffer(c));

    int docRepLevel = 0; // root repeated level (usually 0)

    foreach (var row in rows)
    {
      // shred 1 row at a time, writing to each of the column buffers.
      ShredMessage(root, row, buffers, defLevel: 0, repLevel: docRepLevel);
    }

    return buffers;
  }

  #endregion

  #region Shred Private Methods

  private static void ShredMessage(
          SchemaNode node,
          IDictionary<string, object?> message,
          Dictionary<SchemaNode, ColumnBuffer> buffers,
          int defLevel,
          int repLevel)
  {
    foreach (var child in node.Children)
    {
      message.TryGetValue(child.Name, out var value);

      bool isNull = value is null;
      bool isRepeated = child.Repetition == RepetitionKind.Repeated;

      int childDefInc = (child.Repetition == RepetitionKind.Optional ||
                         child.Repetition == RepetitionKind.Repeated) ? 1 : 0;
      int childDef = defLevel + (isNull ? 0 : childDefInc);

      if (isNull)
      {
        if (child.IsLeaf)
        {
          buffers[child].Write(null, repLevel, childDef);
        }
        else
        {
          EmitNullsForStruct(child, buffers, childDef, repLevel);
        }
        continue;
      }

      if (isRepeated)
      {
        var list = (IList<object?>)value!;

        if (list.Count == 0)
        {
          EmitNullsForStruct(child, buffers, childDef, repLevel);
          continue;
        }

        for (int i = 0; i < list.Count; i++)
        {
          var item = list[i];
          bool itemIsNull = item is null;

          int itemDef = childDef + (itemIsNull ? 0 : 1);
          int itemRep = (i == 0) ? repLevel : child.MaxRepetitionLevel;

          if (child.IsLeaf)
          {
            buffers[child].Write(item, itemRep, itemDef);
          }
          else
          {
            var subMsg = (IDictionary<string, object?>)item!;
            ShredMessage(child, subMsg, buffers, itemDef, itemRep);
          }
        }
      }
      else
      {
        if (child.IsLeaf)
        {
          buffers[child].Write(value, repLevel, childDef);
        }
        else
        {
          var subMsg = (IDictionary<string, object?>)value!;
          ShredMessage(child, subMsg, buffers, childDef, repLevel);
        }
      }
    }
  }

  private static void EmitNullsForStruct(
          SchemaNode structNode,
          Dictionary<SchemaNode, ColumnBuffer> buffers,
          int defLevel,
          int repLevel)
  {
    foreach (var leaf in GetLeaves(structNode))
    {
      buffers[leaf].Write(null, repLevel, defLevel);
    }

    static IEnumerable<SchemaNode> GetLeaves(SchemaNode node)
    {
      if (node.IsLeaf)
      {
        yield return node;
        yield break;
      }

      foreach (var child in node.Children)
      {
        foreach (var leaf in GetLeaves(child))
          yield return leaf;
      }
    }
  }

  #endregion

  #region Assemble Public Methods

  /// <summary>
  /// Uses Dremel assembly algorithm to assemble shredded leaf data into a single logical dataset.
  /// </summary>
  /// <param name="root">The root schema node object (which contains all child / leaf nodes too).</param>
  /// <param name="buffers">The buffers containing the leaf physical data.</param>
  /// <returns>The separate physical leaf datasets are assembled into a single logical dataset.</returns>
  public IList<IDictionary<string, object?>> Assemble(
      SchemaNode root,
      Dictionary<SchemaNode, ColumnBuffer> buffers)
  {
    // Get all the leaf columns in schema, and create
    // a cursor object for each
    var leafColumns = SchemaNode.GetLeafNodes(root).ToList();
    var cursors = leafColumns.ToDictionary(c => c, c => new ColumnCursor(buffers[c]));

    // To store the results
    var result = new List<IDictionary<string, object?>>();

    // while all columns have more values...
    while (cursors.Values.All(c => c.HasNext))
    {
      // process next row and add row to results
      var row = AssembleSingleRow(root, cursors);
      result.Add(row);
    }

    // return results
    return result;
  }

  #endregion

  #region Assemble Private Methods

  /// <summary>
  /// Assembles a single row.
  /// 
  /// This method consumes data from the cursors until a repetition value of 0 is encountered. This
  /// denotes a new list at the root (i.e. new record).
  /// </summary>
  /// <param name="root">The root schema node object (which contains all child / leaf nodes too).</param>
  /// <param name="cursors">Object containing all the leaf cursors.</param>
  /// <returns>Returns 1 row of the final logical data model.</returns>
  private static IDictionary<string, object?> AssembleSingleRow(
          SchemaNode root,
          Dictionary<SchemaNode, ColumnCursor> cursors)
  {
    // intialise the root object for the row. This is a dictionary.
    var row = new Dictionary<string, object?>();

    // process 1 column at a time
    foreach (var kvp in cursors)
    {
      // get the schema node, and the cursor object for the leaf
      var leaf = kvp.Key;
      var cursor = kvp.Value;

      // we read value, def and rep level as one logical slot
      var values = new List<object?>();
      var defs = new List<int>();
      var reps = new List<int>();

      while (cursor.HasNext)
      {
        // get next slot
        var (value, def, rep) = cursor.Peek();

        // we keep reading until rep==0.
        // rep==0 signifies a new root record
        // rep==1..MRL-1 signifies new lists
        // at a lower level for the same root. 
        if (rep == 0 && values.Count > 0)
          break; // next row

        values.Add(value);
        defs.Add(def);
        reps.Add(rep);

        cursor.Advance();
      }

      // finally place the data at the correct place in the object
      PlaceColumnValues(cursor.Buffer.LogicalPath, row, values, defs, reps);
    }

    // and return the row.
    return row;
  }

  /// <summary>
  /// This places values for a single column across various levels of an
  /// object, where the row object represents 1 row of the final output.
  /// 
  /// Input:
  /// - logicalPath(LogicalPath) - contains all logical nodes / mdl / mrl values from root -> leaf
  /// - row(IDictionary<string, object?>) - the current row object - gets built in successive calls to this method
  /// 
  /// Summary
  /// - PlaceColumnValues is called multiple times - once for each leaf node
  /// - Objects are progressively built-out using the leaf data and repetition + definition levels
  /// - The row can contain nested objects, for example:
  ///   - Nested dictionary objects
  ///   - Nested list objects
  /// 
  /// Algorithm (for the current iteration of the PlaceColumnValues method - for one leaf node):
  /// - Get the logical path from root to current leaf / physical node
  /// - Create structures to keep tabs of current nested objects within the path:
  ///   - objectList: represents 'current' active object at each level of the logical path:
  ///     - For STRUCT nodes, this is a dictionary
  ///     - For LIST nodes, this is a list
  ///     - for LEAF nodes, this is the actual value
  ///   - listIndexes: keeps track of the current list index (only applies to LIST nodes)
  ///     - listIndex size = mrl
  ///     - listIndex[n] stores the current list index for the LIST node with rl = n + 1 
  /// - Calculate mdl (i.e. dl for the leaf node)
  /// - Calculate mrl (i.e. rl for the leaf node)
  /// - Loop for each value, dl, rl in the row:
  ///   - Test for new repetition level to start (if rl > 0 and rl < mrl):
  ///     - Find the logical path index, i where node type = LIST, and node.rl == rl
  ///     - Remove all items in objectList where index  > i
  ///     - Increment listIndex[rl-1]
  ///     - Set all elements in listIndex[rl]..listIndex[mrl-1] = 0
  ///   - Walk logical path from head -> leaf (path loop):
  ///     - Determine the parentObject for each current node (n):
  ///       - if parent node (n-1) is STRUCT, parent = ((objectList[n-1) as dict)
  ///       - if parent node (n-1) is LIST, parent = (objectList[n-1] as List)[listIndex[n-1]]
  ///       - Check if object at current level exists:
  ///         - if parent = STRUCT, check not null property using current object name
  ///         - if parent = LIST, check element exists at listIndex[n] position
  ///       - If object does not exist, create + add to parent (also test for nulls here)
  ///         - if node.type=LIST and mdl > 0 and dl < node.cdl:
  ///           - if listIndexes[node.rl-1] >= currentobject.length:
  ///             - add a new null value to the LIST at currentobject
  ///           - break
  ///         - if node.type=LIST and mdl > 0 and dl node.dl:
  ///           - if listIndexes[node.rl-1] >= currentobject.length:
  ///             - add new empty list to the list at currentobject
  ///           - break
  ///         - Otherwise, create a non null value and continue path walk
  ///           - If parent is STRUCT, set property
  ///           - If Parent is LIST, add element
  ///           - The object to create depends on current node type:
  ///             - current node = STRUCT: create dict
  ///             - current node = LIST: create list
  ///             - current node = LEAF: just set / append the actual value to the parent
  ///             - if current node.dl < mdl, write null
  ///         - Add the created element to objectList[n] too
  ///         
  /// Assumption
  /// - n(values)==n(defs)==n(reps)
  /// </summary>
  /// <param name="path">The logical levels for a single leaf node of an object.</param>
  /// <param name="row">The root object being constructed, representing a single row of the final output.</param>
  /// <param name="values">The values for the single root row.</param>
  /// <param name="defs">The definition levels for the single root row.</param>
  /// <param name="reps">The repetition levels for the single roow row.</param>
  private static void PlaceColumnValues(
          LogicalPath logicalPath,
          IDictionary<string, object?> row,
          IList<object?> values,
          IList<int> defs,
          IList<int> reps)
  {
    // Define structure to hold object fragment currently active
    // at each level of the path. This object is later collapsed
    // into final object graph for the row.
    // Entry exists for each item in the path. The entry depends
    // on the logical node type at each level in path:
    //  - STRUCT: Dictionary<string, object> representing the struct
    //  - LIST: List<object> representing the list
    //  - LEAF: object representing the leaf value
    List<object> objectList = new List<object>();

    // Get mdl/mrl for the leaf
    var mdl = logicalPath.MaxDefinitionLevel;
    var mrl = logicalPath.MaxRepetitionLevel;

    // Create structure to keep track of which element within a list we are at for each repetition level
    List<int> listIndexes = Enumerable.Repeat<int>(0, mrl).ToList();

    var path = logicalPath.Path;

    for (int i = 0; i < defs.Count; i++)
    {
      var dl = defs[i];
      var rl = reps[i];
      var value = values[i];

      // if rl < mrl and rl > 0, we clear all objectstack records
      // after node where node.MaxRepetitionLevel = rl
      // so that new list can be created (only where not root)
      if (rl > 0 && rl < mrl)
      {
        var pathIndex = path.FindIndex(p => p.RepetitionLevel == rl);
        objectList.RemoveRange(pathIndex, objectList.Count() - pathIndex);
      }

      // We walk through each node in the path
      for (int j = 0; j < path.Count; j++)
      {
        var currentNode = path[j];
        var parentNode = j == 0 ? null : path[j - 1];

        var parentObject = j == 0 ? row : objectList[j - 1];
        var parentDict = parentObject as Dictionary<string, object?>;
        var parentList = parentObject as List<object?>;

        // if parent is repeated (i.e. a LIST) + node mrl >= rl, then we add new element to parent
        if (parentNode is not null && parentNode.LogicalNodeType == LogicalNodeType.LIST && parentNode.RepetitionLevel >= rl)
        {
          var dict = new Dictionary<string, object?>();
          (parentObject as List<object>).Add(dict);
        }

        // ParentDict
        // - ParentObject if parentNode.Repetition <> REPEATED
        // - ParentObject.LastElement if parentNode.Repetition = REPEATED
        var parentDict = parentNode.Repetition ==
          RepetitionKind.Repeated ?
          ((parentObject as List<object>).Last() as Dictionary<string, object?>) :
          (parentObject as Dictionary<string, object?>);

        // Check dl to see whether nulls should be written. If so
        // We exit the path walk early.
        if (mdl > 0 && dl < currentNode.
        DefinitionLevel)
        {
          parentDict[currentNode.Name] = null;
          break;    // exit walk
        }

        // If leaf node, we write value
        if (currentNode.LogicalNodeType == LogicalNodeType.LEAF)
        {
          object? leafValue = dl == currentNode.DefinitionLevel ? value : DBNull.Value;

          // Add the leaf to parent (which can be STRUCT, LIST, MAP)
          if (parentNode.LogicalNodeType == LogicalNodeType.STRUCT && parentDict is not null)
          {
            parentDict[currentNode.Name] = leafValue;
          }
          else if (parentNode.LogicalNodeType == LogicalNodeType.LIST && parentList is not null)
          {
            parentList.Add(leafValue);
          }
          else
          {
            throw new Exception("Unable to write leaf value.");
          }
        }
        else
        {
          // Non leaf - we build out object graph
          // Check if we are at the last object with values. If so,
          // bubble down the same parent to each level of objectlist
          if (mdl > 0 && dl < mdl && dl < currentNode.DefinitionLevel)
          {
            // bubble parent down
            objectList.Insert(j, parentObject);
          }
          else
          {
            // There should be value at this level
            // Check if child object is present?
            if (parentNode is not null && parentNode.LogicalNodeType == LogicalNodeType.STRUCT && parentDict is not null && (!parentDict.TryGetValue(currentNode.Name, out var childObj) || childObj is null))
            {
              // parent is STRUCT, and no existing property for current node.
              // We need to set a property on parent, and add new object to object list
              if (currentNode.LogicalNodeType == LogicalNodeType.STRUCT)
              {
                var dict = new Dictionary<string, object?>();
                parentDict[currentNode.Name] = dict;
                objectList.Insert(j, dict);
              }
              else if (currentNode.LogicalNodeType == LogicalNodeType.LIST)
              {
                var list = new List<object?>();
                parentDict[currentNode.Name] = list;
                objectList.Insert(j, list);
              }
            }
            else if (parentNode is not null && parentNode.LogicalNodeType == LogicalNodeType.LIST && parentList is not null)
            {
              // parent is LIST - we need to add element to parent and add new object to object list
              if (currentNode.LogicalNodeType == LogicalNodeType.STRUCT)
              {
                var dict = new Dictionary<string, object?>();
                parentList.Add(dict);
                objectList.Insert(j, dict);
              }
              else if (currentNode.LogicalNodeType == LogicalNodeType.LIST)
              {
                var list = new List<object?>();
                parentList.Add(list);
                objectList.Insert(j, list);
              }
            }
          }
        }
      }
    }
  }

  #endregion

}