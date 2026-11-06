namespace Dbarone.Net.Parquet.Dremel;

using Dbarone.Net.Buffers.Document;

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
    // Get the logical node path + length
    var path = logicalPath.Path;
    var pathLength = path.Count();

    // Define structure to hold object fragment currently active
    // at each level of the path. This object is later collapsed
    // into final object graph for the row.
    // Entry exists for each item in the path. The entry depends
    // on the logical node type at each level in path:
    //  - STRUCT: Dictionary<string, object> representing the struct
    //  - LIST: List<object> representing the list
    //  - LEAF: object representing the leaf value
    object?[] objectList = new object[pathLength];

    // Create structure to keep track of which element within a list we are at for each repetition level
    int[] listIndexes = new int[pathLength];

    // Get mdl/mrl for the leaf
    var mdl = logicalPath.MaxDefinitionLevel;
    var mrl = logicalPath.MaxRepetitionLevel;

    // Outer loop - each dl, rl, value for row
    for (int i = 0; i < defs.Count; i++)
    {
      var dl = defs[i];
      var rl = reps[i];
      var value = values[i];

      // Test for new repetition level and reset listIndexes if a new repetition list encountered.
      // if rl < mrl and rl > 0, we clear all objectstack records after node where
      // node.MaxRepetitionLevel = rl so that new list can be created.
      if (mrl > 0 && rl < mrl)
      {
        var pathIndex = path.FindIndex(p => p.RepetitionLevel >= rl);
        for (int j = pathIndex; j < pathLength; j++)
        {
          if (j == pathIndex && path[j].LogicalNodeType == LogicalNodeType.LIST)
          {
            listIndexes[j]++;
          }
          else
          {
            listIndexes[j] = 0;
            objectList[j] = null;
          }
        }
      }
      else if (rl > 0 && rl == mrl)
      {
        // add repeated value at leaf (current list)
        var pathIndex = path.FindIndex(p => p.RepetitionLevel == mrl);
        for (int j = pathIndex; j < pathLength; j++)
        {
          if (j == pathIndex)
          {
            listIndexes[j]++;
          }
          else
          {
            listIndexes[j] = 0;
            objectList[j] = null;
          }
        }
      }

      // Inner loop - walk through each node in the path
      for (int j = 0; j < path.Count; j++)
      {
        var currentNode = path[j];
        var parentNode = j == 0 ? null : path[j - 1];
        var parentNodeType = j == 0 ? LogicalNodeType.STRUCT : parentNode!.LogicalNodeType;
        var parentObject = j == 0 ? row : objectList[j - 1];
        var parentDict = parentObject as Dictionary<string, object?>;
        var parentList = parentObject as List<object?>;

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
          // group node - make sure exists at each level of the path.
          bool currentExists = true;
          if (parentNodeType == LogicalNodeType.STRUCT && (!parentDict.TryGetValue(currentNode.Name, out var childObj) || childObj is null))
          {
            // Parent is struct, but current object doesn't exists - create
            currentExists = false;
          }
          else if (parentNodeType == LogicalNodeType.LIST && (parentList.Count() < listIndexes[j]))
          {
            // Parent is LIST, but current list index doesn't exists - create
            currentExists = false;
          }

          if (!currentExists)
          {
            // need to create current object + add to parent
            if (currentNode.LogicalNodeType == LogicalNodeType.LIST && dl < currentNode.ContainerDefinitionLevel)
            {
              if (parentNodeType == LogicalNodeType.LIST)
              {
                // add new null element and break from inner path loop
                parentList[listIndexes[j - 1]] = DBNull.Value;
              }
              else if (parentNodeType == LogicalNodeType.STRUCT)
              {
                parentDict[currentNode.Name] = DBNull.Value;
              }
              objectList[j] = DBNull.Value;
              break;
            }
            else if (currentNode.LogicalNodeType == LogicalNodeType.LIST && dl < currentNode.DefinitionLevel)
            {
              if (parentNodeType == LogicalNodeType.LIST)
              {
                // add new empty list and break from inner path loop
                parentList[listIndexes[j - 1]] = new List<object?>();
              }
              else if (parentNodeType == LogicalNodeType.STRUCT)
              {
                parentDict[currentNode.Name] = new List<object?>();
              }
              objectList[j] = new List<object?>();
              break;
            }
            else if (currentNode.LogicalNodeType == LogicalNodeType.STRUCT && dl < currentNode.DefinitionLevel)
            {
              // null struct
              if (parentNodeType == LogicalNodeType.LIST)
              {
                // add new null element and break from inner path loop
                parentList[listIndexes[j - 1]] = DBNull.Value;
              }
              else if (parentNodeType == LogicalNodeType.STRUCT)
              {
                parentDict[currentNode.Name] = DBNull.Value;
              }
              objectList[j] = DBNull.Value;
              break;
            }
            else
            {
              // Current value is not null - add group object, and do not break from inner loop
              object? currentObject = null;
              if (currentNode.LogicalNodeType == LogicalNodeType.STRUCT)
              {
                currentObject = new Dictionary<string, object?>();
              }
              else if (currentNode.LogicalNodeType == LogicalNodeType.LIST)
              {
                currentObject = new List<object?>();
              }

              if (parentNodeType == LogicalNodeType.LIST)
              {
                // add new null element and break from inner path loop
                parentList[listIndexes[j - 1]] = currentObject;
              }
              else if (parentNodeType == LogicalNodeType.STRUCT)
              {
                parentDict[currentNode.Name] = currentObject;
              }
              objectList[j] = currentObject;
            }
          }
        }
      }
    }
  }

  #endregion

}