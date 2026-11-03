using Dbarone.Net.Parquet.Dremel;
using Dbarone.Net.Parquet.Thrift;

/// <summary>
/// Represents a single element in the logical path of the data object graph. A logical path node has 1:1 mapping to a node
/// in the final logical data output - no synthetic nodes exist. Logical path nodes are used for dremel assembly instead
/// of schema nodes and contain all the metadata required for assembly.
/// </summary>
public class LogicalPathNode
{
  public string Name { get; set; }

  // Computed per column path
  public int DefinitionLevel { get; set; }
  public int RepetitionLevel { get; set; }

  // For LIST + MAP, the outer container efinition level
  public int? ContainerDefinitionLevel { get; set; }
  public LogicalNodeType LogicalNodeType { get; set; }

  public LogicalPathNode(string name, LogicalNodeType logicalNodeType, int definitionLevel, int repetitionLevel, int? containerDefinitionLevel)
  {
    this.Name = name;
    this.LogicalNodeType = logicalNodeType;
    this.DefinitionLevel = definitionLevel;
    this.RepetitionLevel = repetitionLevel;
    this.ContainerDefinitionLevel = containerDefinitionLevel;
  }
}