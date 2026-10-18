
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

  private static IDictionary<string, object?> AssembleSingleRow(
          SchemaNode root,
          Dictionary<SchemaNode, ColumnCursor> cursors)
  {
    // intialise the root object for the row. This is a dictionary.
    var rootObj = new Dictionary<string, object?>();

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
      PlaceColumnValues(root, leaf, rootObj, values, defs, reps);
    }

    // and return the root object.
    return rootObj;
  }

  /// <summary>
  /// This places values for a single column across various levels of an
  /// object, where the root object represents 1 row of the final output.
  /// </summary>
  /// <param name="root">The root schema node.</param>
  /// <param name="leaf">The leaf / data node.</param>
  /// <param name="rootObj">The root object being constructed</param>
  /// <param name="values">The values for the single root row</param>
  /// <param name="defs">The definition levels for the single root row</param>
  /// <param name="reps">The repetition levels for the single roow row</param>
  private static void PlaceColumnValues(
          SchemaNode root,
          SchemaNode leaf,
          IDictionary<string, object?> rootObj,
          IList<object?> values,
          IList<int> defs,
          IList<int> reps)
  {
    // Get all nodes from root to leaf
    var path = leaf.GetPathFromAncestor(root);

    // Define structure to hold object fragment currently active
    // at each level of the path. This object is later collapsed
    // into final object graph for the row.
    // Entry exists for each item in the path. Each entry can be
    // - leaf node - fragment stored is the leaf value
    // - list node (if node repetition type = Repeated) - fragment stored is the list
    // - Dictionary node (<> leaf) - fragment stored is the dictionary
    // Same object can exist at multiple levels
    List<object> objectList = new List<object>();
    // The row's root object is 0th element:
    objectList.Add(rootObj);

    // Get mdl/mrl for the leaf
    var mdl = leaf.MaxDefinitionLevel;
    var mrl = leaf.MaxRepetitionLevel;

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
        var pathIndex = path.FindIndex(p => p.MaxRepetitionLevel == rl);
        objectList.RemoveRange(pathIndex, objectList.Count() - pathIndex);
      }

      // We walk through each node in the path
      // exclude root node
      for (int j = 1; j < path.Count(); j++)
      {
        var currentNode = path[j];
        var parentNode = path[j - 1];
        var parentObject = objectList[j - 1];

        // if parent is repeated + node mrl >= rl, then we add new element to parent
        if (parentNode.Repetition == RepetitionKind.Repeated && parentNode.MaxRepetitionLevel >= rl)
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

        // If leaf node, we write value
        if (currentNode.IsLeaf)
        {
          object? leafValue = dl == leaf.MaxDefinitionLevel ? value : null;

          // if leaf is repeated, add element to array
          if (currentNode.Repetition == RepetitionKind.Repeated)
          {
            (parentObject as List<object>).Add(leafValue);
          }
          else
          {
            parentDict[currentNode.Name] = leafValue;
          }
        }
        else
        {
          // Non leaf - we build out object graph
          // Check if we are at the last object with values. If so,
          // bubble down the same parent to each level of objectlist
          if (mdl > 0 && dl < mdl && dl > currentNode.MaxDefinitionLevel)
          {
            // bubble parent down
            objectList.Insert(j, parentDict);
          }
          else
          {
            // There should be value at this level
            // Check if child object is present?
            if (!parentDict.TryGetValue(currentNode.Name, out var childObj) || childObj is null)
            {
              if (currentNode.Repetition == RepetitionKind.Repeated)
              {
                // Create list list, add element, and add list to parent property.
                var list = new List<object?>();
                var dict = new Dictionary<string, object?>();
                list.Add(dict);
                parentDict[currentNode.Name] = list;
                objectList.Insert(j, list);
              }
              else if (!currentNode.IsLeaf)
              {
                // create dict, and add to parent
                var dict = new Dictionary<string, object?>();
                parentDict[currentNode.Name] = dict;
                objectList.Insert(j, dict);
              }
            }
          }
        }
      }
    }
  }

  #endregion

}