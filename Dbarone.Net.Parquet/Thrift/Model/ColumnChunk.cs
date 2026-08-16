namespace Dbarone.Net.Parquet.Thrift;

[ParquetThriftMetaData()]
public sealed class ColumnChunk
{
  /// <summary>
  /// File where column data is stored.  If not set, assumed to be same file as
  /// metadata.This path is relative to the current file.
  /// 
  /// As of December 2025, the only known use-case for this field is writing summary
  /// parquet files(i.e. "_metadata" files).  These files consolidate footers from
  /// multiple parquet files to allow for efficient reading of footers to avoid file
  /// listing costs and prune out files that do not need to be read based on statistics.
  /// 
  /// These files do not appear to have ever been formally specified in the specification.
  /// and are potentially problematic from a correctness perspective [1].
  /// 
  /// https://lists.apache.org/thread/ootf2kmyg3p01b1bvplpvp4ftd1bt72d
  /// 
  /// There is no other known usage of this field.Specifically, there are no known
  /// reference implementations that will read externally stored column data if this field is populated
  /// within a standard parquet file. Making use of the field for this purpose is
  /// not considered part of the Parquet specification.
  /// </summary>
  [FieldId(1)]
  public string? FilePath { get; set; }

  /// <summary>
  /// DEPRECATED: Byte offset in file_path to the ColumnMetaData
  /// 
  /// Past use of this field has been inconsistent, with some implementations
  /// using it to point to the ColumnMetaData and some using it to point to
  /// the first page in the column chunk.In many cases, the ColumnMetaData at this
  /// location is wrong.This field is now deprecated and should not be used.
  /// Writers should set this field to 0 if no ColumnMetaData has been written outside
  /// the footer.
  /// </summary>
  [FieldId(2)]
  [Deprecated()]
  public long FileOffset { get; set; }

  /// <summary>
  /// Column metadata for this chunk. Some writers may also replicate this at the
  /// location pointed to by file_path/file_offset.
  /// Note: while marked as optional, this field is in fact required by most major
  /// Parquet implementations.As such, writers MUST populate this field.
  /// </summary>
  [FieldId(3)]
  public ColumnMetaData? Metadata { get; set; }

  /// <summary>
  /// File offset of ColumnChunk's OffsetIndex
  /// </summary>
  [FieldId(4)]
  public long? OffsetIndexOffset { get; set; }

  /// <summary>
  /// Size of ColumnChunk's OffsetIndex, in bytes
  /// </summary>
  [FieldId(5)]
  public int? OffsetIndexLength { get; set; }

  /// <summary>
  /// File offset of ColumnChunk's ColumnIndex
  /// </summary>
  [FieldId(6)]
  public long? ColumnIndexOffset { get; set; }

  /// <summary>
  /// Size of ColumnChunk's ColumnIndex, in bytes
  /// </summary>
  [FieldId(7)]
  public int? ColumnIndexLength { get; set; }

  /// <summary>
  /// Crypto metadata of encrypted columns
  /// </summary>
  [FieldId(8)]
  public ColumnCryptoMetaData? CryptoMetaData { get; set; }

  /// <summary>
  /// Encrypted column metadata for this chunk
  /// </summary>
  [FieldId(9)]
  public byte[]? EncryptedColumnMetaData { get; set; }
}