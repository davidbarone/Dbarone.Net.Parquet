namespace Dbarone.Net.Parquet.Thrift;

[ParquetThriftMetaData()]
public sealed class PageHeader
{
  /// <summary>
  /// the type of the page: indicates which of the *_header fields is set
  /// </summary>
  [FieldId(1)]
  public PageType PageType { get; set; }

  /// <summary>
  /// Uncompressed page size in bytes (not including this header)
  /// </summary>
  [FieldId(2)]
  public int UncompressedPageSize { get; set; }

  /// <summary>
  /// Compressed (and potentially encrypted) page size in bytes, not including this header
  /// </summary>
  [FieldId(3)]
  public int CompressedPageSize { get; set; }

  /// <summary>
  /// The 32-bit CRC checksum for the page, to be calculated as follows:
  /// 
  /// - The standard CRC32 algorithm is used(with polynomial 0x04C11DB7,
  /// the same as in e.g.GZIP).
  /// - All page types can have a CRC(v1 and v2 data pages, dictionary pages,
  /// etc.).
  /// - The CRC is computed on the serialization binary representation of the page
  /// (as written to disk), excluding the page header.For example, for v1
  /// data pages, the CRC is computed on the concatenation of repetition levels,
  /// definition levels and column values (optionally compressed, optionally
  /// encrypted).
  /// - The CRC computation therefore takes place after any compression
  /// and encryption steps, if any.
  /// 
  /// If enabled, this allows for disabling checksumming in HDFS if only a few
  /// pages need to be read.
  /// </summary>
  [FieldId(4)]
  public int? Crc { get; set; }

  /// <summary>
  /// Headers for page specific data.  One only will be set.
  /// </summary>
  [FieldId(5)]
  public DataPageHeader? DataPageHeader { get; set; }

  /// <summary>
  /// Headers for page specific data.  One only will be set.
  /// </summary>
  [FieldId(6)]
  public IndexPageHeader? IndexPageHeader { get; set; }

  /// <summary>
  /// Headers for page specific data.  One only will be set.
  /// </summary>
  [FieldId(7)]
  public DictionaryPageHeader? DictionaryPageHeader { get; set; }

  /// <summary>
  /// Headers for page specific data.  One only will be set.
  /// </summary>
  [FieldId(8)]
  public DataPageHeaderV2? DataPageHeaderV2 { get; set; }
}

public enum PageType
{
  DATA_PAGE = 0,
  INDEX_PAGE = 1,
  DICTIONARY_PAGE = 2,
  DATA_PAGE_V2 = 3
}

/// <summary>
/// Data page header.
/// </summary>
[ParquetThriftMetaData()]
public sealed class DataPageHeader
{
  /// <summary>
  /// Number of values, including NULLs, in this data page.
  /// 
  /// If an OffsetIndex is present, a page must begin at a row
  /// boundary (repetition_level = 0). Otherwise, pages may begin
  /// within a row(repetition_level > 0).
  /// </summary>
  [FieldId(1)]
  public int NumValues { get; set; }

  /// <summary>
  /// Encoding used for this data page.
  /// </summary>
  [FieldId(2)]
  public Encoding Encoding { get; set; }

  /// <summary>
  /// Encoding used for definition levels.
  /// </summary>
  [FieldId(3)]
  public Encoding DefinitionLevelEncoding { get; set; }

  /// <summary>
  /// Encoding used for repetition levels.
  /// </summary>
  [FieldId(4)]
  public Encoding RepetitionLevelEncoding { get; set; }

  /// <summary>
  /// Optional statistics for the data in this page.
  /// </summary>
  [FieldId(5)]
  public Statistics? Statistics { get; set; }
}

/// <summary>
/// To do.
/// </summary>
[ParquetThriftMetaData()]
public sealed class IndexPageHeader
{

}

/// <summary>
/// The dictionary page must be placed at the first position of the column chunk
/// if it is partly or completely dictionary encoded.At most one dictionary page
/// can be placed in a column chunk.
/// </summary>
[ParquetThriftMetaData()]
public sealed class DictionaryPageHeader
{
  /// <summary>
  /// Number of values in the dictionary
  /// </summary>
  [FieldId(1)]
  public int NumValues { get; set; }

  /// <summary>
  /// Encoding using this dictionary page
  /// </summary>
  [FieldId(2)]
  public Encoding Encoding { get; set; }

  /// <summary>
  /// If true, the entries in the dictionary are sorted in ascending order
  /// </summary>
  [FieldId(3)]
  public bool? IsSorted { get; set; }
}

/// <summary>
/// Alternate page format allowing reading levels without decompressing the data
/// Repetition and definition levels are uncompressed
/// The remaining section containing the data is compressed if is_compressed is true
/// 
/// Implementation note - this header is not necessarily a strict improvement over
/// `DataPageHeader` (in particular the original header might provide better compression
/// in some scenarios). Page indexes require pages to start and end at row boundaries,
/// regardless of which page header is used.
/// </summary>
[ParquetThriftMetaData()]
public sealed class DataPageHeaderV2
{
  /// <summary>
  /// Number of values, including NULLs, in this data page.
  /// </summary>
  [FieldId(1)]
  public int NumValues { get; set; }

  /// <summary>
  /// Number of NULL values, in this data page.
  /// Number of non - null = num_values - num_nulls which is also the number of values in the data section
  /// </summary>
  [FieldId(2)]
  public int NumNulls { get; set; }

  /// <summary>
  /// Number of rows in this data page.Every page must begin at a
  /// row boundary (repetition_level = 0): rows must **not * *be
  /// split across page boundaries when using V2 data pages.
  /// </summary>
  [FieldId(3)]
  public int NumRows { get; set; }

  /// <summary>
  /// Encoding used for data in this page
  /// </summary>
  [FieldId(4)]
  public Encoding Encoding { get; set; }

  /// <summary>
  /// Length of the definition levels
  /// repetition levels and definition levels are always using RLE (without size in it)
  /// </summary>
  [FieldId(5)]
  public int DefinitionLevelsByteLength { get; set; }

  /// <summary>
  /// Length of the repetition levels
  /// repetition levels and definition levels are always using RLE (without size in it)
  /// </summary>
  [FieldId(6)]
  public int RepetitionLevelsByteLength { get; set; }

  /// <summary>
  /// Whether the values are compressed.
  /// Which means the section of the page between
  /// definition_levels_byte_length + repetition_levels_byte_length and compressed_page_size(included)
  /// is compressed with the compression_codec.
  /// If missing it is considered compressed
  /// </summary>
  [FieldId(7)]
  public bool? IsCompressed { get; set; } = true;

  /// <summary>
  /// Optional statistics for the data in this page
  /// </summary>
  [FieldId(8)]
  public Statistics? Statistics { get; set; }
}