namespace Dbarone.Net.Parquet.Dremel;

using Dbarone.Net.Parquet.Thrift;

/// <summary>
/// The SchemaNode class provides additional semantic information about a Parquet schema element.
/// SchemaNodes objects are the primary object used during Dremel splitting and assembly.
/// The SchemaElement type is a faithful reflection of the Thrift SchemaElement type.
/// The SchemaNode type was introduced to avoid polluting the Thrift SchemaElement type with additional
/// semantic information.
/// </summary>
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

  /// <summary>
  /// Creates a basic schema node with a specified name and repetition type.
  /// </summary>
  /// <param name="name">The name of the schema node.</param>
  /// <param name="repetition">The repetition kind.</param>
  public SchemaNode(string name, RepetitionKind repetition)
  {
    Name = name;
    Repetition = repetition;
  }

  /// <summary>
  /// Creates a schema node from a Parquet schema element. Note this is an internal constructor which is called
  /// from the BuildFromThriftSchema static method.
  /// </summary>
  /// <param name="name">The name of the node.</param>
  /// <param name="repetition">The repetition kind.</param>
  /// <param name="schemaIndex">The index of the node in the Parquet schema.</param>
  /// <param name="chunkIndex">The chunk index used (for leaf nodes only).</param>
  /// <param name="schemaElement">The Parquet schema element.</param>
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

  /// <summary>
  /// Gets the path from an ancestor node (typically the root node).
  /// </summary>
  /// <param name="ancestor">The ancestor node from which to create the path.</param>
  /// <param name="prunePath">If set to true, the path is pruned / modified to return a path representing the logical data path. Otherwise, the actual Parquet schema path is returned.</param>
  /// <returns>Returns a List of SchemaNode elements representing the path.</returns>
  /// <exception cref="InvalidOperationException"></exception>
  public List<SchemaNode> GetPathFromAncestor(SchemaNode ancestor)
  {
    var path = new List<SchemaNode>();
    if (Find(ancestor, this, path))
      return path;
    throw new InvalidOperationException("Current node not under ancestor.");

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

  public SchemaNode GetRoot()
  {
    var node = this;
    while (node.Parent is not null)
    {
      node = node.Parent;
    }
    return node;
  }

  #region Private methods on unpruned nodes

  private bool IsRoot()
  {
    return this.Parent is null;
  }

  /// <summary>
  /// Returns the schema group type, if the schema type == SchemaType.GROUP
  /// </summary>
  public SchemaGroupType GetSchemaGroupType()
  {
    if (this.GetSchemaType() == SchemaType.Primitive)
    {
      return SchemaGroupType.NOT_APPLICABLE;
    }
    else
    {
      if (this.IsRoot())
      {
        return SchemaGroupType.ROOT;
      }
      else if (this.IsSyntheticList())
      {
        return SchemaGroupType.LIST;
      }
      else if (this.IsSyntheticMap())
      {
        return SchemaGroupType.MAP;
      }
      else if (this.IsSyntheticGroup())
      {
        return SchemaGroupType.SYNTHETIC;
      }
      else
      {
        // Includes list L2 node (the repeating node)
        return SchemaGroupType.SYNTHETIC;
      }
    }
  }

  /// <summary>
  /// The schema type is computed from the Parquet type schema:
  /// - if Type is set -> primitive
  /// - if Type is NOT set and num_children > 0 -> group
  /// </summary>
  public SchemaType GetSchemaType()
  {
    if (this.SchemaElement is null)
    {
      throw new Exception("Cannot compute SchemaType if SchemaElement is null");
    }
    else
    {
      if (this.SchemaElement.Type is null && this.SchemaElement.NumChildren > 0)
      {
        return SchemaType.Group;
      }
      else if (this.SchemaElement.Type is not null)
      {
        return SchemaType.Primitive;
      }
      else
      {
        throw new Exception("Cannot compute SchemaType.");
      }
    }
  }

  /// <summary>
  /// Parquet defines a special synthetic structure for handling LIST objects.
  /// The standard 3-node structure is used to support null lists, empty lists
  /// and null elements within a list.
  /// 
  /// 
  /// </summary>
  /// <returns></returns>
  private bool IsSyntheticList()
  {
    if (this.SchemaElement is null)
    {
      throw new Exception("Cannot calculate IsSyntheticList - Parquet SchemaElement is null.");
    }
    else
    {
      if (this.SchemaElement.LogicalType?.LIST != null)
        return true;

      if (this.SchemaElement.ConvertedType == ConvertedType.LIST)
        return true;

      return false;
    }
  }

  private bool IsSyntheticMap()
  {
    if (this.SchemaElement is null)
    {
      throw new Exception("Cannot calculate IsSyntheticMap - Parquet SchemaElement is null.");
    }
    else
    {
      if (this.SchemaElement.LogicalType?.MAP != null)
        return true;

      if (this.SchemaElement.ConvertedType == ConvertedType.MAP)
        return true;

      return false;
    }
  }

  /// <summary>
  /// A synthetic group is:
  /// - a group (i.e. not a primitive node / physical type)
  /// - exactly 1 child
  /// - no logical type
  /// - not repeated
  /// 
  /// Notes:
  /// - Other artificial structures like LIST and MAP
  ///   are not considered as synthetic groups.
  /// </summary>
  /// <returns></returns>
  private bool IsSyntheticGroup()
  {
    if (this.SchemaElement is null)
    {
      throw new Exception("Cannot calculate IsSynthetic - SchemaElement is null");
    }
    else
    {
      return this.GetSchemaType() == SchemaType.Group
        && this.SchemaElement.LogicalType is null
        && this.SchemaElement.NumChildren == 1
        && this.SchemaElement.RepetitionType != RepetitionType.REPEATED;
    }
  }

  #endregion
}