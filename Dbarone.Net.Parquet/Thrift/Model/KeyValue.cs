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
  public string Key { get; set; } = default!;

  /// <summary>
  /// Field: 2
  /// </summary>
  public string? Value { get; set; }
}
