/// This file defines the Parquet Thrift interface (Thrift IDL)
/// https://github.com/apache/parquet-format/blob/master/src/main/thrift/parquet.thrift
namespace Dbarone.Net.Parquet.Thrift;

/// <summary>
/// Represents an element inside a schema definition.
///  - if it is a group (inner node) then type is undefined and num_children is defined
///  - if it is a primitive type (leaf) then type is defined and num_children is undefined
/// the nodes are listed in depth first traversal order.
/// </summary>
[ParquetThriftMetaData()]
public class SchemaElement
{
  /// <summary>
  /// Data type for this field. Not set if the current element is a non-leaf node
  /// </summary>
  [FieldId(1)]
  public Type? Type { get; set; }

  /// <summary>
  /// If type is FIXED_LEN_BYTE_ARRAY, this is the byte length of the values.
  /// Otherwise, if specified, this is the maximum bit length to store any of the values.
  /// (e.g.a low cardinality INT col could have this set to 3).  Note that this is
  /// in the schema, and therefore fixed for the entire file.
  /// </summary>
  [FieldId(2)]
  public int? TypeLength { get; set; }

  /// <summary>
  /// repetition of the field. The root of the schema does not have a repetition_type.
  /// All other nodes must have one
  /// </summary>
  [FieldId(3)]
  public RepetitionType? RepetitionType { get; set; }

  /// <summary>
  /// Name of the field in the schema
  /// </summary>
  [FieldId(4)]
  public string Name { get; set; } = default!;

  /// <summary>
  /// Nested fields.  Since thrift does not support nested fields,
  /// the nesting is flattened to a single list by a depth-first traversal.
  /// The children count is used to construct the nested relationship.
  /// This field is not set when the element is a primitive type
  /// </summary>
  [FieldId(5)]
  public int? NumChildren { get; set; }

  /// <summary>
  /// DEPRECATED: When the schema is the result of a conversion from another model.
  /// Used to record the original type to help with cross conversion.
  /// 
  /// This is superseded by logicalType.
  /// </summary>
  [Deprecated()]
  [FieldId(6)]
  public ConvertedType? ConvertedType { get; set; }

  /// <summary>
  /// DEPRECATED: Used when this column contains decimal data.
  /// See the DECIMAL converted type for more details.
  /// 
  /// This is superseded by using the DecimalType annotation in logicalType.
  /// </summary>
  [Deprecated()]
  [FieldId(7)]
  public int? Scale { get; set; }

  [Deprecated()]
  [FieldId(8)]
  public int? Precision { get; set; }

  /// <summary>
  /// When the original schema supports field ids, this will save the
  /// original field id in the parquet schema
  /// </summary>
  [FieldId(9)]
  public int? FieldId { get; set; }

  /// <summary>
  /// The logical type of this SchemaElement
  /// 
  /// LogicalType replaces ConvertedType, but ConvertedType is still required
  /// for some logical types to ensure forward-compatibility in format v1.
  /// </summary>
  [FieldId(10)]
  public LogicalType? LogicalType { get; set; }
}
