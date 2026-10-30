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
  public List<SchemaNode>? UnprunedPath { get; set; }
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

  public bool IsRoot()
  {
    return this.Parent is null;
  }

  public List<SchemaNode> GetPathFromAncestor(SchemaNode ancestor, bool collapsePath = false)
  {
    var path = new List<SchemaNode>();
    if (Find(ancestor, this, path))
      return collapsePath ? CollapsePath(path) : path;
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

  /// <summary>
  /// Returns the schema group type, if the schema type == SchemaType.GROUP
  /// </summary>
  public SchemaGroupType SchemaGroupType
  {
    get
    {
      if (this.SchemaType == SchemaType.Primitive)
      {
        throw new Exception("Node is not a group type.");
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
          throw new Exception("Unspecified GROUP node type");
        }
      }
    }
  }

  /// <summary>
  /// The schema type is computed from the Parquet type schema:
  /// - if Type is set -> primitive
  /// - if Type is NOT set and num_children > 0 -> group
  /// </summary>
  public SchemaType SchemaType
  {
    get
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
  }

  /// <summary>
  /// Parquet defines a special synthetic structure for handling LIST objects.
  /// The standard 3-node structure is used to support null lists, empty lists
  /// and null elements within a list.
  /// 
  /// 
  /// </summary>
  /// <returns></returns>
  public bool IsSyntheticList()
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

  public bool IsSyntheticMap()
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
  public bool IsSyntheticGroup()
  {
    if (this.SchemaElement is null)
    {
      throw new Exception("Cannot calculate IsSynthetic - SchemaElement is null");
    }
    else
    {
      return this.SchemaType == SchemaType.Group
        && this.SchemaElement.LogicalType is null
        && this.SchemaElement.NumChildren == 1
        && this.SchemaElement.RepetitionType != RepetitionType.REPEATED;
    }
  }

  /// <summary>
  /// Collapses a Parquet physical path.
  /// 
  /// The physical Parquet/Dremel schema contains
  /// additional encoding which is mapped in the
  /// final assembly of the logical data. The physical schema contains
  /// additional synthetic elements which must be removed / rewritten to
  /// produce a schema which maps 1:1 with the assembled data.
  /// 
  /// Extra schema nodes are used to encode things like:
  /// - repetition levels
  /// - definition levels
  /// - list element boundaries
  /// - optionality
  /// 
  /// The CollapsePath algorithm has following properties:
  /// - Input: A Parquet SchemaElement[] path (depth-first)
  /// - Output: A logical schema tree containing only:
  ///   - structs
  ///   - lists
  ///   - maps
  ///   - primitive fields
  /// 
  /// Algorithm (details)
  /// - Remove synthetic root
  /// - Collapse LIST structures
  /// - Collapse MAP structures 
  /// - Collapse synthetic groups with no meaning 
  /// - Keep only required nodes
  /// 
  /// Extra 
  /// </summary>
  /// <param name="physicalPath"></param>
  /// <returns></returns>
  public List<SchemaNode> CollapsePath(List<SchemaNode> physicalPath)
  {
    var path = physicalPath;
    int index = 0;
    while (true)
    {
      index = GetListIndex(path, index);
      if (index > -1)
      {
        path = PruneList(path, index);
        index++;
        continue;
      }
      // if we get here, no more work to do
      break;
    }
    return path;
  }

  private int GetListIndex(List<SchemaNode> path, int startAt = 0)
  {
    var element = path.FirstOrDefault(p => path.IndexOf(p) >= startAt && p.SchemaElement?.LogicalType?.LIST is not null);
    if (element is null)
    { return -1; }
    else
    {
      return path.IndexOf(element);
    }
  }

  /// <summary>
  /// Lists are pruned as follows:
  /// 
  /// Before (physical model):
  /// optional group mylist (LIST)
  ///   repeated group list
  ///     required <element-type> element
  /// 
  /// After (logical model):
  /// mylist: list<element_type>
  /// 
  /// Prune rule:
  /// - drop outer group (LIST wrapper)
  /// - drop inner list group (the repeated wrapper)
  /// - replace innner 'element' node name with name of top-level node
  /// - promote the element type as the list's item type
  /// 
  /// </summary>
  /// <param name="path"></param>
  /// <param name="index"></param>
  /// <returns></returns>
  private List<SchemaNode> PruneList(List<SchemaNode> path, int index)
  {
    var listNode = path[index];
    var repeatedNode = path[index + 1];
    var elementNode = path[index + 2];

    // Create new logical node for list:
    var elementSchemaElement = elementNode.SchemaElement;
    var listNodeElement = listNode.SchemaElement;
    if (elementSchemaElement is null)
    {
      throw new Exception("elementSchemaElement is null");
    }
    if (listNodeElement is null || listNodeElement.LogicalType is null)
    {
      throw new Exception("listNodeElement.LogicalType is null");
    }
    elementSchemaElement.LogicalType = listNodeElement.LogicalType;
    var logicalList = new SchemaNode(listNode.Name, listNode.Repetition, elementNode.SchemaIndex, elementNode.ChunkIndex, elementSchemaElement);

    // Get copy of the original logical/synthetic elements representing LIST
    // and add to new element
    logicalList.UnprunedPath = path.GetRange(index, 3);

    // Remove synthetic nodes and insert new logical node:
    path.RemoveRange(index, 3);
    path.Insert(index, logicalList);
    return path;
  }

  /// <summary>
  /// A schema node is considered 'pruned' if the original schema had additional
  /// nodes (e.g. synthetic nodes) used to store things like definition rules or
  /// special structures like LIST and MAP. The original schema path for a pruned
  /// node is stored in the unpruned path property. 
  /// </summary>
  public bool IsPrunedNode => this.UnprunedPath is not null;
}