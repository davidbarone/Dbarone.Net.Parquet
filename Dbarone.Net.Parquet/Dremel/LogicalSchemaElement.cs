namespace Dbarone.Net.Parquet.Dremel;

using Dbarone.Net.Parquet.Thrift;

/// <summary>
/// Represents a single element in the logical path of the data object graph. A logical schema element has 1:1 mapping
/// to an element in the logical data output - no synthetic nodes exist. Logical nodes are used for dremel assembly
/// instead Parquet SchemaElement objects, and contain all the metadata required for assembly.
/// </summary>
public class LogicalSchemaElement
{
  public string Name { get; set; }
  public DremelKind DremelKind { get; set; }
  public Thrift.Type? PhysicalType { get; set; }
  public LogicalType? LogicalType { get; set; }
  public SchemaElementGroupType SchemaElementGroupType { get; set; }

  public LogicalSchemaElement(string name, DremelKind dremelKind)
  {
    this.Name = name;
    this.DremelKind = dremelKind;
  }

  public LogicalSchemaElement(string name, DremelKind dremelKind, Thrift.Type? physicalType, LogicalType? logicalType, SchemaElementGroupType schemaElementGroupType)
  {
    this.Name = name;
    this.DremelKind = dremelKind;
    this.PhysicalType = physicalType;
    this.LogicalType = logicalType;
    this.SchemaElementGroupType = schemaElementGroupType;
  }

  /// <summary>
  /// Calculates the max definition level increment for the current node.
  /// </summary>
  public int MaxDefinitionLevelIncr => (this.DremelKind.HasFlag(DremelKind.Optional) ? 1 : 0) + (this.DremelKind.HasFlag(DremelKind.Emptyable) ? 1 : 0);

  /// <summary>
  /// Calculates the repetition level incrementfor the current node.
  /// </summary>
  public int MaxRepetitionLevelIncr => this.DremelKind.HasFlag(DremelKind.Repeated) ? 1 : 0;
}