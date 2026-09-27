namespace Dbarone.Net.Parquet.Thrift;

/// <summary>
/// Wrapper struct to store key values
/// </summary>
[ParquetThriftMetaData()]
public sealed class KeyValue
{
  /// <summary>
  /// Field: 1
  /// </summary>
  [FieldId(1)]
  public string Key { get; set; } = default!;

  /// <summary>
  /// Field: 2
  /// </summary>
  [FieldId(2)]
  public string? Value { get; set; }
}
