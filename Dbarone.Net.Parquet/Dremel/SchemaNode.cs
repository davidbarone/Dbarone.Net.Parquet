using Dbarone.Net.Parquet.Thrift;

namespace Dbarone.Net.Parquet.Dremel;

public sealed class SchemaNode
{
  public string Name { get; }
  public RepetitionKind Repetition { get; }
  public bool IsLeaf { get; set; } = true;
  public List<SchemaNode> Children { get; } = new();
  public SchemaNode? Parent { get; set; }

  // Computed per column path
  public int MaxDefinitionLevel { get; set; }
  public int MaxRepetitionLevel { get; set; }

  public SchemaNode(string name, RepetitionKind repetition, bool isLeaf = true)
  {
    Name = name;
    Repetition = repetition;
    IsLeaf = isLeaf;
  }

  public SchemaNode AddChild(SchemaNode child)
  {
    Children.Add(child);
    child.Parent = this;
    this.IsLeaf = false;
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
  public static SchemaNode BuildFromThriftSchema(List<SchemaElement> thriftSchema, SchemaNode? parent = null, int currentThriftIndex = 0)
  {
    // Get the current element
    if (parent is null)
    {
      currentThriftIndex = 0;
    }

    var element = thriftSchema[currentThriftIndex];

    // Repetition Kind
    RepetitionKind repetitionKind = RepetitionKind.Required;
    if (element.RepetitionType == RepetitionType.OPTIONAL)
    {
      repetitionKind = RepetitionKind.Optional;
    }
    if (element.RepetitionType == RepetitionType.REPEATED)
    {
      repetitionKind = RepetitionKind.Optional;
    }

    // Calculate MDL and MRL if root
    int MDL = 0;
    int MRL = 0;
    if (currentThriftIndex == 0)
    {
      if (element.RepetitionType == RepetitionType.OPTIONAL)
      {
        MDL = MDL + 1;
      }
      else if (element.RepetitionType == RepetitionType.REPEATED)
      {
        MRL = MRL + 1;
      }
    }

    // Is leaf?
    var isLeaf = element.NumChildren == 0;

    // Create new SchemaNode
    SchemaNode node = new SchemaNode(element.Name, repetitionKind, isLeaf);

    // Process children
    for (int i = 1; i <= element.NumChildren; i++)
    {
      BuildFromThriftSchema(thriftSchema, node, currentThriftIndex + i);
    }

    // Add to parent if applicable
    if (parent is not null)
    {
      parent.AddChild(node);
    }

    return node;
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