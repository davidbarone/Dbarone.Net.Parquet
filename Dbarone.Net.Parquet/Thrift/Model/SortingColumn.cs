namespace Dbarone.Net.Parquet.Thrift;

/// <summary>
/// Sort order within a RowGroup of a leaf column
/// </summary>
[ParquetThriftMetaData()]
public sealed class SortingColumn
{
  /// <summary>
  /// The ordinal position of the column (in this row group)
  /// </summary>
  [FieldId(1)]
  public int ColumnIdx { get; set; }

  /// <summary>
  /// If true, indicates this column is sorted in descending order.
  /// </summary>
  [FieldId(2)]
  public bool Descending { get; set; }

  /// <summary>
  /// If true, nulls will come before non-null values, otherwise,
  /// nulls go at the end.
  /// </summary>
  [FieldId(3)]
  public bool NullsFirst { get; set; }
}