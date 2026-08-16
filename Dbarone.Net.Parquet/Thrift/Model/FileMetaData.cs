namespace Dbarone.Net.Parquet.Thrift;

/// <summary>
/// Description for file metadata
/// https://github.com/apache/parquet-format/blob/master/src/main/thrift/parquet.thrift
/// 
/// - FileMetaData
///   - SchemaElement
///     - e:Type
///     - e:FieldRepetitionType
///     - e:ConvertedType
///     - LogicalType
///       - StringType
///       - MapType
///       - ListType
///       - EnumType
///       - DecimalType
///       - DateType
///       - TimeType
///         - TimeUnit
///       - TimestampType
///         - TimeUnit
///       - IntType
///       - NullType
///       - JsonType
///       - BsonType
///       - UUIDType
///       - Float16Type
///       - VariantType
///       - GeometryType
///       - GeographyType
///         - EdgeInterpolationAlgorithm
///   - RowGroup
///     - ColumnChunk
///       - ColumnMetaData
///         - Type (see above)
///         - e:Encoding
///         - e:CompressionCodec
///         - KeyValue
///         - Statistics
///         - PageEncodingStats
///           - PageType
///           - Encoding (see above)
///         - SizeStatistics
///         - GeospatialStatistics
///           - BoundingBox
///       - ColumnCryptoMetaData
///         - EncryptionWithFooterKey
///         - EncryptionWithColumnKey
///     - SortingColumn
///   - KeyValue
///   - ColumnOrder
///     - TypeDefinedOrder
///     - IEEE754TotalOrder
///   - EncryptionAlgorithm
///     - AesGcmV1
///     - AesGcmCtrV1
/// 
/// </summary>
[ParquetThriftMetaData()]
public sealed class FileMetaData
{
  /// <summary>
  /// Version of this file
  /// 
  /// As of December 2025, there is no agreed upon consensus of what constitutes
  /// version 2 of the file.For maximum compatibility with readers, writers should
  /// always populate "1" for version.For maximum compatibility with writers,
  /// readers should accept "1" and "2" interchangeably.All other versions are
  /// reserved for potential future use-cases.
  /// </summary>
  [FieldId(1)]
  public int Version { get; set; }

  /// <summary>
  /// Parquet schema for this file.  This schema contains metadata for all the columns.
  /// The schema is represented as a tree with a single root.The nodes of the tree
  /// are flattened to a list by doing a depth-first traversal.
  /// The column metadata contains the path in the schema for that column which can be
  /// used to map columns to nodes in the schema.
  /// The first element is the root
  /// </summary>
  [FieldId(2)]
  public List<SchemaElement> Schema { get; set; } = default!;

  /// <summary>
  /// Number of rows in this file
  /// </summary>
  [FieldId(3)]
  public long NumRows { get; set; }

  /// <summary>
  /// Row groups in this file
  /// </summary>
  [FieldId(4)]
  public List<RowGroup> RowGroups { get; set; } = default!;

  /// <summary>
  /// Optional key/value metadata
  /// </summary>
  [FieldId(5)]
  public List<KeyValue>? KeyValueMetaData { get; set; }

  /// <summary>
  /// String for application that wrote this file.  This should be in the format
  /// <Application> version<App Version>(build<App Build Hash>).
  /// e.g.impala version 1.0 (build 6cf94d29b2b7115df4de2c06e2ab4326d721eb55)
  /// </summary>
  [FieldId(6)]
  public string? CreatedBy { get; set; }

  /// <summary>
  /// Sort order used for the min_value and max_value fields in the Statistics
  /// objects and the min_values and max_values fields in the ColumnIndex
  /// objects of each column in this file. Sort orders are listed in the order
  /// matching the columns in the schema.The indexes are not necessarily the same
  /// though, because only leaf nodes of the schema are represented in the list
  /// of sort orders.
  /// 
  /// Without column_orders, the meaning of the min_value and max_value fields
  /// in the Statistics object and the ColumnIndex object is undefined.To ensure
  /// well-defined behaviour, if these fields are written to a Parquet file,
  /// column_orders must be written as well.
  /// 
  /// The obsolete min and max fields in the Statistics object are always sorted
  /// by signed comparison regardless of column_orders.
  /// </summary>
  [FieldId(7)]
  public List<ColumnOrder>? ColumnOrders { get; set; }

  [FieldId(8)]
  public EncryptionAlgorithm? EncryptionAlgorithm { get; set; } = default!;

  [FieldId(9)]
  public byte[]? FooterSigningKeyMetadata { get; set; } = default!;
}