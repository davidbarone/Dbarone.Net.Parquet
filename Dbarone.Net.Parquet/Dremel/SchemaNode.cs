using Dbarone.Net.Parquet.Thrift;

namespace Dbarone.Net.Parquet.Dremel;

public sealed class SchemaNode
{
  public bool CreatedFromThrift { init; get; } = false;
  public string Name { get; }
  public RepetitionKind Repetition { get; }
  public bool IsLeaf { get; set; } = true;
  public List<SchemaNode> Children { get; } = new();
  public SchemaNode? Parent { get; set; }

  // Computed per column path
  public int MaxDefinitionLevel { get; set; }
  public int MaxRepetitionLevel { get; set; }

  // Thrift information
  public SchemaElement? SchemaElement { get; set; }
  public int SchemaIndex { get; set; }
  public int? ChunkIndex { get; set; }

  public SchemaNode(string name, RepetitionKind repetition)
  {
    Name = name;
    Repetition = repetition;
  }

  internal SchemaNode(string name, RepetitionKind repetition, int schemaIndex, int? chunkIndex, SchemaElement? schemaElement = null)
  {
    Name = name;
    Repetition = repetition;

    // Calculate MDL and MRL for the node in isolation of its parent.
    MaxDefinitionLevel = repetition == RepetitionKind.Required ? 0 : 1; // In dremel specification, repeated fields are nullable AND repeated
    MaxRepetitionLevel = repetition == RepetitionKind.Repeated ? 1 : 0;

    SchemaIndex = schemaIndex;
    ChunkIndex = chunkIndex;
    SchemaElement = schemaElement;

    CreatedFromThrift = true;
  }

  public SchemaNode AddChild(SchemaNode child)
  {
    Children.Add(child);
    child.Parent = this;
    this.IsLeaf = false;

    // When adding child to parent, add the parent's MDL/MRL to the child.
    child.MaxDefinitionLevel = this.MaxDefinitionLevel + child.MaxDefinitionLevel;
    child.MaxRepetitionLevel = this.MaxRepetitionLevel + child.MaxRepetitionLevel;

    return this;
  }

  public SchemaNode GetRoot()
  {
    var node = this;
    while (node.Parent is not null)
    {
      node = node.Parent;
    }
    return node;
  }

  public List<SchemaNode> GetPathFromAncestor(SchemaNode ancestor)
  {
    var path = new List<SchemaNode>();
    if (Find(ancestor, this, path))
      return path;
    throw new InvalidOperationException("Node not under root.");

    bool Find(SchemaNode node, SchemaNode target, List<SchemaNode> acc)
    {
      acc.Add(node);
      if (node == target)
        return true;

      foreach (var child in node.Children)
      {
        if (Find(child, target, acc))
          return true;
      }

      acc.RemoveAt(acc.Count - 1);
      return false;
    }
  }

  /// <summary>
  /// Parses the Thrift schema which is ordered in depth-first
  /// order, and returns a SchemaNode object.
  /// </summary>
  /// <param name="thriftSchema">The list of SchemaElement objects in the Thrift schema.</param>
  /// <returns>Returns a root SchemaNode object</returns>
  public static (SchemaNode node, int CurrentChunkIndex) BuildFromThriftSchema(List<SchemaElement> thriftSchema, SchemaNode? parent = null, int currentThriftIndex = 0, int currentChunkIndex = -1)
  {
    var element = thriftSchema[currentThriftIndex];

    // Repetition Kind
    RepetitionKind repetitionKind = RepetitionKind.Required;
    if (element.RepetitionType == RepetitionType.OPTIONAL)
    {
      repetitionKind = RepetitionKind.Optional;
    }
    if (element.RepetitionType == RepetitionType.REPEATED)
    {
      repetitionKind = RepetitionKind.Repeated;
    }

    // Is leaf?
    var isLeaf = (element.NumChildren ?? 0) == 0;
    if (isLeaf)
    {
      currentChunkIndex++;
    }

    // Create new SchemaNode
    SchemaNode node = new SchemaNode(element.Name, repetitionKind, currentThriftIndex, currentChunkIndex, element);

    // Add to parent if applicable
    if (parent is not null)
    {
      parent.AddChild(node);
    }

    // Process children
    for (int i = 1; i <= element.NumChildren; i++)
    {
      var childThriftIndex = currentThriftIndex + i;
      (_, currentChunkIndex) = BuildFromThriftSchema(thriftSchema, node, childThriftIndex, currentChunkIndex);
    }

    return (node, currentChunkIndex);
  }

  /// <summary>
  /// Gets all leaf nodes/columns from the root node.
  /// </summary>
  /// <returns>A list of all leaf nodes.</returns>
  public static IReadOnlyList<SchemaNode> GetLeafNodes(SchemaNode root)
  {
    var leaves = new List<SchemaNode>();
    Traverse(root, leaves);
    return leaves;

    void Traverse(SchemaNode node, List<SchemaNode> leaves)
    {
      if (node.IsLeaf)
      {
        leaves.Add(node);
      }
      else
      {
        foreach (var child in node.Children)
          Traverse(child, leaves);
      }
    }
  }
}