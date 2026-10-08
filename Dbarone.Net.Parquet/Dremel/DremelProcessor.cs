
namespace Dbarone.Net.Parquet.Dremel;

public class DremelProcessor : IDremelProcessor
{
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

  public IList<IDictionary<string, object?>> Assemble(
          SchemaNode root,
          Dictionary<SchemaNode, ColumnBuffer> buffers)
  {
    var leafColumns = SchemaNode.GetLeafNodes(root);
    var cursors = leafColumns.ToDictionary(c => c, c => new ColumnCursor(buffers[c]));

    var result = new List<IDictionary<string, object?>>();

    while (cursors.Values.All(c => c.HasNext))
    {
      var row = AssembleSingleRow(root, cursors);
      result.Add(row);
    }

    return result;
  }


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
        // For leaf: emit null with DL < maxDL
        if (child.IsLeaf)
        {
          buffers[child].Write(null, repLevel, childDef);
        }
        else
        {
          // For struct nulls, each leaf under it will be emitted
          EmitNullsForStruct(child, buffers, repLevel, childDef);
        }
        continue;
      }

      if (isRepeated)
      {
        IList<object> list = null;
        if (child.IsLeaf)
        {
          list = (IList<object?>)value!;
        }
        else
        {
          list = ((IList<Dictionary<string, object>>)value!).Select(i => (object)i).ToList();
        }
        for (int i = 0; i < list.Count; i++)
        {
          int childRep = (i == 0) ? repLevel : child.MaxRepetitionLevel;
          var item = list[i];

          if (child.IsLeaf)
          {
            buffers[child].Write(item, childRep, child.MaxDefinitionLevel);
          }
          else
          {
            var subMsg = (IDictionary<string, object?>)item!;
            ShredMessage(child, subMsg, buffers, childDef, childRep);
          }
        }

        if (list.Count == 0)
        {
          // Empty list: emit a null at DL = defLevel (no repeated element)
          EmitNullsForStruct(child, buffers, defLevel, repLevel);
        }
      }
      else
      {
        // Non-repeated field
        if (child.IsLeaf)
        {
          buffers[child].Write(value, repLevel, child.MaxDefinitionLevel);
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

  private static IDictionary<string, object?> AssembleSingleRow(
          SchemaNode root,
          Dictionary<SchemaNode, ColumnCursor> cursors)
  {
    var pathStack = new Stack<object>(); // holds current nested containers
    var rootObj = new Dictionary<string, object?>();
    pathStack.Push(rootObj);

    // For each leaf column, consume one value and place it
    foreach (var kvp in cursors)
    {
      var leaf = kvp.Key;
      var cursor = kvp.Value;

      var (value, def, rep) = cursor.Peek();

      // RL: how many repeated levels are reused
      // DL: how deep the value is defined (vs null)
      PlaceValue(root, leaf, pathStack, value, def, rep);

      cursor.Advance();
    }

    return (IDictionary<string, object?>)rootObj;
  }

  private static void PlaceValue(
          SchemaNode root,
          SchemaNode leaf,
          Stack<object> pathStack,
          object? value,
          int def,
          int rep)
  {
    // Build path from root to leaf
    var path = leaf.GetPathFromAncestor(leaf.GetRoot());

    // Ensure stack depth matches repetition reuse (simplified)
    while (pathStack.Count > rep + 1)
      pathStack.Pop();

    // Walk down the path, creating containers as needed
    object current = pathStack.Peek();

    for (int i = 1; i < path.Count; i++)
    {
      var node = path[i];

      if (node.Repetition == RepetitionKind.Repeated)
      {
        var parentDict = (IDictionary<string, object?>)current;

        if (!parentDict.TryGetValue(node.Name, out var listObj) || listObj is null)
        {
          listObj = new List<object?>();
          parentDict[node.Name] = listObj;
        }

        var list = (IList<object?>)listObj;

        object? element;

        if (node.IsLeaf)
        {
          // leaf repeated field → element is the value itself
          element = null;
        }
        else
        {
          // struct repeated field → element is a new dictionary
          element = new Dictionary<string, object?>();
        }

        list.Add(element);

        // Push the element (never the list)
        if (!node.IsLeaf)
          pathStack.Push(element);

        current = element ?? current;
      }
      else
      {
        var parentDict = (IDictionary<string, object?>)current;

        if (!parentDict.TryGetValue(node.Name, out var childObj) || childObj is null)
        {
          childObj = node.IsLeaf
              ? null
              : new Dictionary<string, object?>();

          parentDict[node.Name] = childObj;
        }

        current = childObj ?? parentDict;
      }
    }

    // Finally, set the leaf value if DL == maxDL (otherwise it’s null)
    var leafParent = (IDictionary<string, object?>)pathStack.Peek();
    if (def == leaf.MaxDefinitionLevel)
    {
      leafParent[leaf.Name] = value;
    }
    else
    {
      leafParent[leaf.Name] = null;
    }
  }
}