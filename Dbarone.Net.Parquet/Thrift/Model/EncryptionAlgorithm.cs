namespace Dbarone.Net.Parquet.Thrift;

[ParquetThriftMetaData()]
public sealed class EncryptionAlgorithm
{
  [FieldId(1)]
  public AesGcmV1 AES_GCM_V1 { get; set; } = default!;

  [FieldId(2)]
  public AesGcmCtrV1 AES_GCM_CTR_V1 { get; set; } = default!;
}

[ParquetThriftMetaData()]
public sealed class AesGcmV1
{
  /// <summary>
  /// AAD prefix
  /// </summary>
  [FieldId(1)]
  public byte[]? AadPrefix { get; set; }

  /// <summary>
  /// Unique file identifier part of AAD suffix
  /// </summary>
  [FieldId(2)]
  public byte[]? AadFileUnique { get; set; }

  /// <summary>
  /// In files encrypted with AAD prefix without storing it,
  /// readers must supply the prefix**/
  /// </summary>
  [FieldId(3)]
  public bool? SupplyAadPrefix { get; set; }
}

[ParquetThriftMetaData()]
public sealed class AesGcmCtrV1
{
  /// <summary>
  /// AAD prefix
  /// </summary>
  [FieldId(1)]
  public byte[]? AadPrefix { get; set; }

  /// <summary>
  /// Unique file identifier part of AAD suffix
  /// </summary>
  [FieldId(2)]
  public byte[]? AadFileUnique { get; set; }

  /// <summary>
  /// In files encrypted with AAD prefix without storing it,
  /// readers must supply the prefix**/
  /// </summary>
  [FieldId(3)]
  public bool? SupplyAadPrefix { get; set; }
}