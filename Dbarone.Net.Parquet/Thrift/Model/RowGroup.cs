namespace Dbarone.Net.Parquet.Thrift;

[ParquetThriftMetaData()]
public sealed class RowGroup
{
  /// <summary>
  /// Metadata for each column chunk in this row group.
  /// This list must have the same order as the SchemaElement list in FileMetaData.
  /// </summary>
  [FieldId(1)]
  public List<ColumnChunk> Columns { get; set; } = default!;

  /// <summary>
  /// Total byte size of all the uncompressed column data in this row group
  /// </summary>
  [FieldId(2)]
  public long TotalByteSize { get; set; }

  /// <summary>
  /// Number of rows in this row group
  /// </summary>
  [FieldId(3)]
  public long NumRows { get; set; }

  /// <summary>
  /// If set, specifies a sort ordering of the rows in this RowGroup.
  /// The sorting columns can be a subset of all the columns.
  /// </summary>
  [FieldId(4)]
  public List<SortingColumn>? SortingColumns { get; set; }

  /// <summary>
  /// Byte offset from beginning of file to first page (data or dictionary)
  /// in this row group
  /// </summary>
  [FieldId(5)]
  public long? FileOffset { get; set; }

  /// <summary>
  /// Total byte size of all compressed (and potentially encrypted) column data
  /// in this row group
  /// </summary>
  [FieldId(6)]
  public long? TotalCompressedSize { get; set; }

  /// <summary>
  /// Row group ordinal in the file
  /// </summary>
  [FieldId(7)]
  public short? Ordinal { get; set; }
}