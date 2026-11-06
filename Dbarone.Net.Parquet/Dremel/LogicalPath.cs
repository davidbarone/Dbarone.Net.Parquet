namespace Dbarone.Net.Parquet.Dremel;

/// <summary>
/// Represents a logical path from root to leaf node used by Dremel shredding / assembly.
/// The Parquet schema includes additional synthetic nodes that to not map to physical
/// levels in the final logical data model (for example the 3-level LIST structure). The
/// logical path represents the actual path in the final data model, and contains no
/// additional sythetic nodes. The logical path is used by the dremel encoding process.
/// 
/// The logical path removes additional nodes to annotate things like:
/// - repetition levels
/// - definition levels
/// - list element boundaries
/// - optionality
/// 
/// The general algorithm to map Parquet schema nodes to logical nodes is:
/// - Remove synthetic root
/// - Prune LIST structures
/// - Prune MAP structures 
/// - Prune synthetic groups with no meaning 
/// - Keep only required nodes
/// </summary>
public class LogicalPath
{
  public List<LogicalPathNode> Path { get; } = default!;
  public int MaxDefinitionLevel { get; }
  public int MaxRepetitionLevel { get; }
  public int Count => this.Path.Count();

  /// <summary>
  /// Constructs a logical path from a Parquet schema path.
  /// </summary>
  /// <param name="path">The Parquet schema path.</param>
  /// <returns>Returns a LogicalPath object.</returns>
  public LogicalPath(List<SchemaNode> path)
  {
    // Create structure to hold pruned list
    List<LogicalPathNode> pruned = new List<LogicalPathNode>();

    // Create the logical path, dealing with other synthetic modes
    int index = 0;
    while (true)
    {
      index = GetStructIndex(path);
      if (index > -1)
      {
        AddStruct(path, pruned, index);
      }

      index = GetListIndex(path);
      if (index > -1)
      {
        PruneList(path, pruned, index);
        continue;
      }

      index = GetLeafIndex(path);
      if (index > -1)
      {
        AddLeaf(path, pruned, index);
      }

      // if we get here, no more work to do
      break;
    }

    // Remove the root node from the path - this does not get added to the logical path.
    path.RemoveAt(0);

    this.Path = pruned;
    this.MaxDefinitionLevel = this.Path.Single(n => n.LogicalNodeType == LogicalNodeType.LEAF).DefinitionLevel;
    this.MaxRepetitionLevel = this.Path.Single(n => n.LogicalNodeType == LogicalNodeType.LEAF).RepetitionLevel;
  }

  /// <summary>
  /// Lists are pruned as follows:
  /// 
  /// Before (physical model):
  /// optional group mylist (LIST)
  ///   repeated group list
  ///     optional <element-type> element
  /// 
  /// After (logical model):
  /// repeated group mylist
  ///   optional <element-type> element
  /// 
  /// Prune rule:
  /// - drop L1 / outer group (LIST wrapper)
  /// - rename L2 node to be the original L1 name (i.e. 'list' -> 'mylist'). Change the logical type on L2 to 'list'
  /// - Add L1 node to the L2.Annotations property. This allows the list to differentiate between null list value and empty list
  /// </summary>
  /// <param name="path"></param>
  /// <param name="index"></param>
  /// <returns></returns>
  private void PruneList(List<SchemaNode> path, List<LogicalPathNode> pruned, int index)
  {
    var listNode = path[index];
    var repeatedNode = path[index + 1];
    var elementNode = path[index + 2];

    if (listNode.SchemaElement is null)
    {
      throw new Exception("List schema element cannot be null");
    }
    if (repeatedNode.SchemaElement is null)
    {
      throw new Exception("Repeated node schema element cannot be null");
    }

    var logicalList = new LogicalPathNode(
      listNode.Name,
      LogicalNodeType.LIST,
      repeatedNode.MaxDefinitionLevel,
      repeatedNode.MaxRepetitionLevel,
      listNode.MaxDefinitionLevel);

    var logicalElement = new LogicalPathNode(
      elementNode.Name,
      LogicalNodeType.LEAF,
      elementNode.MaxDefinitionLevel,
      elementNode.MaxRepetitionLevel,
      null
    );

    // Remove L1-L3 list nodes
    path.RemoveRange(index, 3);

    // Add new logical nodes
    pruned.Add(logicalList);
    pruned.Add(logicalElement);
  }

  private void AddStruct(List<SchemaNode> path, List<LogicalPathNode> pruned, int index)
  {
    var structNode = path[index];

    var logicalStruct = new LogicalPathNode(
      structNode.Name,
      LogicalNodeType.STRUCT,
      structNode.MaxDefinitionLevel,
      structNode.MaxRepetitionLevel,
      null);

    path.RemoveRange(index, 1);

    // Add new logical nodes
    pruned.Add(logicalStruct);
  }

  private void AddLeaf(List<SchemaNode> path, List<LogicalPathNode> pruned, int index)
  {
    var leafNode = path[index];

    var logicalLeaf = new LogicalPathNode(
      leafNode.Name,
      LogicalNodeType.LEAF,
      leafNode.MaxDefinitionLevel,
      leafNode.MaxRepetitionLevel,
      null);

    path.RemoveRange(index, 1);

    // Add new logical nodes
    pruned.Add(logicalLeaf);
  }

  private int GetStructIndex(List<SchemaNode> path)
  {
    var element = path.FirstOrDefault(p =>
      p.GetSchemaType() == SchemaType.Group &&
      p.GetSchemaGroupType() == SchemaGroupType.STRUCT);
    return element is null ? -1 : path.IndexOf(element);
  }

  private int GetListIndex(List<SchemaNode> path)
  {
    var element = path.FirstOrDefault(p =>
      p.GetSchemaType() == SchemaType.Group &&
      p.GetSchemaGroupType() == SchemaGroupType.LIST);
    return element is null ? -1 : path.IndexOf(element);
  }

  private int GetLeafIndex(List<SchemaNode> path)
  {
    var element = path.FirstOrDefault(p =>
      p.GetSchemaType() == SchemaType.Primitive);
    return element is null ? -1 : path.IndexOf(element);
  }
}