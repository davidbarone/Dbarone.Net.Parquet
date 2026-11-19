namespace Dbarone.Net.Parquet.Dremel;

/// <summary>
/// The group type of a SchemaElement object.
/// </summary>
public enum SchemaElementGroupType
{
  /// <summary>
  /// Struct node. Implemented as a dictionary in the logical data model.
  /// </summary>
  STRUCT,

  /// <summary>
  /// List node. Implemented as a list in the logical data model.
  /// </summary>
  LIST,

  /// <summary>
  /// Map node.
  /// </summary>
  MAP,

  /// <summary>
  /// Leaf / primitive node (i.e. not a group)
  /// </summary>
  PRIMITIVE
}