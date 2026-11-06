namespace Dbarone.Net.Parquet.Dremel;

/// <summary>
/// Group nodes are non-primitive schema elements that contain child fields, and have no physical type.
/// 
/// There are 4 main types of GROUP node types:
/// - The 'root' message group (the top-level schema element - this group does not appear in the final logical data schema output).
/// - STRUCT like groups - these are general grouping structures with one or more child fields. They are used for nested objects e.g. address {street; city;}.
/// - LIST group - a special type of synthetic group structure annotated with the Parquet LIST logical type.
/// - MAP group - a special type of synthetic group structure annotated with the Parquet MAP logical type.
/// 
/// Additionally, this enum includes a SYNTHETIC enum. This is for other synthetic group nodes, as defined where node.IsSyntheticGroup = true 
/// </summary>
public enum SchemaGroupType
{
  /// <summary>
  /// This node is not a group type.
  /// </summary>
  NOT_APPLICABLE,

  /// <summary>
  /// The 'root' message group (the top-level schema element - this group does not appear in the final logical data schema output).
  /// - The top-level schema node.
  /// - Represented as a GROUP with repetition = None, meaning it is the schema root.
  /// - Contains all top-level fields.
  /// </summary>
  ROOT,

  /// <summary>
  /// A 'struct' like group:
  /// - A general nested structure (similar to a struct).
  /// - Represented as GroupType with child fields.
  /// - Used for nested objects, e.g. address { street; city; }.
  /// - Repetition can be required, optional, or repeated (0+ occurrences).
  /// </summary>
  STRUCT,

  /// <summary>
  /// LIST group - a special type of synthetic group structure annotated with the Parquet LIST logical type:
  /// - A GROUP annotated with the Parquet LIST logical type.
  /// - Has a specific internal structure (modern Parquet writer use 3-node structure):
  ///   - Outer group annotated as a LIST logical type
  ///   - Contains repeated group (normally called 'list')
  ///   - 3-level contains the element type (normally called 'element')
  /// </summary>
  LIST,

  /// <summary>
  /// MAP group - a special type of synthetic group structure annotated with the Parquet MAP logical type:
  /// - A GROUP annotated with the Parquet MAP logical type.
  /// - Has a required internal structure:
  ///   - Outer group annotated as MAP
  ///   - Contains a repeated key_value group. Inside, contains 2 nodes:
  ///     - key (required)
  ///     - value (optional or required)
  /// </summary>
  MAP,

  /// <summary>
  /// This covers other group nodes (often emitted by older writers) that have no logical meaning:
  /// - groups with no logical type
  /// - groups whose only child is another group
  /// - groups used only to carry repetition levels
  /// - groups named array, list, bag, tuple, item etc.
  /// </summary>
  SYNTHETIC
}