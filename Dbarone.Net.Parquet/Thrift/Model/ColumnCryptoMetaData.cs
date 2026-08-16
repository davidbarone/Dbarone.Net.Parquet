namespace Dbarone.Net.Parquet.Thrift;

[ParquetThriftMetaData()]
public sealed class ColumnCryptoMetaData
{
  [FieldId(1)]
  public EncryptionWithFooterKey ENCRYPTION_WITH_FOOTER_KEY { get; set; }

  [FieldId(2)]
  public EncryptionWithColumnKey ENCRYPTION_WITH_COLUMN_KEY { get; set; }
}

[ParquetThriftMetaData()]
public sealed class EncryptionWithFooterKey
{
}

[ParquetThriftMetaData()]
public sealed class EncryptionWithColumnKey
{
  /// <summary>
  /// Column path in schema
  /// </summary>
  [FieldId(1)]
  public List<string> PathInSchema { get; set; } = default!;

  /// <summary>
  /// Retrieval metadata of column encryption key
  /// </summary>
  [FieldId(2)]
  public byte[]? KeyMetaData { get; set; }
}