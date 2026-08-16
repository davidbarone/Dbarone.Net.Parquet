namespace Dbarone.Net.Parquet.Thrift;

/// <summary>
/// Description for column metadata
/// </summary>
[ParquetThriftMetaData()]
public sealed class ColumnMetaData
{
  /// <summary>
  /// Type of this column
  /// </summary>
  [FieldId(1)]
  public Dbarone.Net.Parquet.Thrift.Type Type { get; set; }

  /// <summary>
  /// Set of all encodings used for this column. The purpose is to validate
  /// whether we can decode those pages. **/
  /// </summary>
  [FieldId(2)]
  public List<Encoding> Encodings { get; set; } = default!;

  /// <summary>
  /// Path in schema
  /// </summary>
  [FieldId(3)]
  public List<string> PathInSchema { get; set; } = default!;

  /// <summary>
  /// Compression codec
  /// </summary>
  [FieldId(4)]
  public CompressionCodec Codec { get; set; }

  /// <summary>
  /// Number of values in this column
  /// </summary>
  [FieldId(5)]
  public long NumValues { get; set; }

  /// <summary>
  /// total byte size of all uncompressed pages in this column chunk (including the headers)
  /// </summary>
  [FieldId(6)]
  public long TotalUncompressedSize { get; set; }

  /// <summary>
  /// total byte size of all compressed, and potentially encrypted, pages
  /// in this column chunk(including the headers)
  /// </summary>
  [FieldId(7)]
  public long TotalCompressedSize { get; set; }

  /// <summary>
  /// Optional key/value metadata
  /// </summary>
  [FieldId(8)]
  public List<KeyValue>? KeyValueMetaData { get; set; }

  /// <summary>
  /// Byte offset from beginning of file to first data page
  /// </summary>
  [FieldId(9)]
  public long DataPageOffset { get; set; }

  /// <summary>
  /// Byte offset from beginning of file to root index page
  /// </summary>
  [FieldId(10)]
  public long? IndexPageOffset { get; set; }

  /// <summary>
  /// Byte offset from the beginning of file to first (only) dictionary page
  /// </summary>
  [FieldId(11)]
  public long? DictionaryPageOffset { get; set; }

  /// <summary>
  /// optional statistics for this column chunk
  /// </summary>
  [FieldId(12)]
  public Statistics? Statistics { get; set; }

  /// <summary>
  /// Set of all encodings used for pages in this column chunk.
  /// This information can be used to determine if all data pages are
  /// dictionary encoded for example
  /// </summary>
  [FieldId(13)]
  public List<PageEncodingStats>? EncodingStats { get; set; }

  /// <summary>
  /// Byte offset from beginning of file to Bloom filter data.
  /// </summary>
  [FieldId(14)]
  public long? BloomFilterOffset { get; set; }

  /// <summary>
  /// Size of Bloom filter data including the serialized header, in bytes.
  /// Added in 2.10 so readers may not read this field from old files and
  /// it can be obtained after the BloomFilterHeader has been deserialized.
  /// Writers should write this field so readers can read the bloom filter
  /// in a single I/O.
  /// </summary>
  [FieldId(15)]
  public int? BloomFilterLength { get; set; }

  /// <summary>
  /// Optional statistics to help estimate total memory when converted to in-memory
  /// representations.The histograms contained in these statistics can
  /// also be useful in some cases for more fine-grained nullability/list length
  /// filter pushdown.
  /// </summary>
  [FieldId(16)]
  public SizeStatistics? SizeStatistics { get; set; }

  /// <summary>
  /// Optional statistics specific for Geometry and Geography logical types
  /// </summary>
  [FieldId(17)]
  public GeospatialStatistics? GeospatialStatistics { get; set; }
}

/// <summary>
/// Encodings supported by Parquet.Not all encodings are valid for all types.  These
/// enums are also used to specify the encoding of definition and repetition levels.
/// See the accompanying doc for the details of the more complicated encodings.
/// </summary>
public enum Encoding : byte
{
  /// <summary>
  /// Default encoding.
  /// BOOLEAN - 1 bit per value. 0 is false; 1 is true.
  /// INT32 - 4 bytes per value.  Stored as little-endian.
  /// INT64 - 8 bytes per value.  Stored as little-endian.
  /// FLOAT - 4 bytes per value.  IEEE. Stored as little-endian.
  /// DOUBLE - 8 bytes per value.  IEEE. Stored as little-endian.
  /// BYTE_ARRAY - 4 byte length stored as little endian, followed by bytes.
  /// FIXED_LEN_BYTE_ARRAY - Just the bytes.
  /// </summary>
  PLAIN = 0,

  /// <summary>
  /// Group VarInt encoding for INT32/INT64.
  /// This encoding is deprecated. It was never used.
  /// </summary>
  [Deprecated()]
  GROUP_VAR_INT = 1,

  /// <summary>
  /// DEPRECATED: Dictionary encoding. The values in the dictionary are encoded in the
  /// plain type.
  /// For a data page use RLE_DICTIONARY instead.
  /// For a Dictionary page use PLAIN instead.
  /// </summary>
  [Deprecated()]
  PLAIN_DICTIONARY = 2,

  /// <summary>
  /// Group packed run length encoding. Usable for definition/repetition levels
  /// encoding and Booleans (on one bit: 0 is false; 1 is true.)
  /// </summary>
  RLE = 3,

  /// <summary>
  /// DEPRECATED: Bit packed encoding.  This can only be used if the data has a known max
  /// width.  Usable for definition/repetition levels encoding.
  /// Superseded by RLE (which is a hybrid of RLE and bit packing); see Encodings.md.
  /// </summary>
  [Deprecated()]
  BIT_PACKED = 4,

  /// <summary>
  /// Delta encoding for integers. This can be used for int columns and works best
  /// on sorted data
  /// </summary>
  DELTA_BINARY_PACKED = 5,

  /// <summary>
  /// Encoding for byte arrays to separate the length values and the data. The lengths
  /// are encoded using DELTA_BINARY_PACKED
  /// </summary>
  DELTA_LENGTH_BYTE_ARRAY = 6,

  /// <summary>
  /// Incremental-encoded byte array. Prefix lengths are encoded using DELTA_BINARY_PACKED.
  /// Suffixes are stored as delta length byte arrays.
  /// </summary>
  DELTA_BYTE_ARRAY = 7,

  /// <summary>
  /// Dictionary encoding: the ids are encoded using the RLE encoding
  /// </summary>
  RLE_DICTIONARY = 8,

  /// <summary>
  /// Encoding for fixed-width data (FLOAT, DOUBLE, INT32, INT64, FIXED_LEN_BYTE_ARRAY).
  /// K byte-streams are created where K is the size in bytes of the data type.
  /// The individual bytes of a value are scattered to the corresponding stream and
  /// the streams are concatenated.
  /// This itself does not reduce the size of the data but can lead to better compression
  /// afterwards.
  /// 
  /// Added in 2.8 for FLOAT and DOUBLE.
  /// Support for INT32, INT64 and FIXED_LEN_BYTE_ARRAY added in 2.11.
  /// </summary>
  BYTE_STREAM_SPLIT = 9
}

/// <summary>
/// Supported compression algorithms.
/// 
/// Codecs added in format version X.Y can be read by readers based on X.Y and later.
/// Codec support may vary between readers based on the format version and
/// libraries available at runtime.
/// 
/// See Compression.md for a detailed specification of these algorithms.
/// </summary>
public enum CompressionCodec : byte
{
  UNCOMPRESSED = 0,
  SNAPPY = 1,
  GZIP = 2,
  LZO = 3,
  BROTLI = 4,  // Added in 2.4
  LZ4 = 5,     // DEPRECATED (Added in 2.4)
  ZSTD = 6,    // Added in 2.4
  LZ4_RAW = 7 // Added in 2.9
}

/// <summary>
/// Statistics per row group and per page
/// All fields are optional.
/// </summary>
[ParquetThriftMetaData()]
public sealed class Statistics
{
  /// <summary>
  /// DEPRECATED: min and max value of the column.Use min_value and max_value.
  /// 
  /// Values are encoded using PLAIN encoding, except that variable-length byte
  /// arrays do not include a length prefix.
  /// 
  /// These fields encode min and max values determined by signed comparison
  /// only. New files should use the correct order for a column's logical type
  /// and store the values in the min_value and max_value fields.
  /// 
  /// To support older readers, these may be set when the column order is
  /// signed.
  /// </summary>
  [Deprecated()]
  [FieldId(1)]
  public byte[]? Max { get; set; }

  /// <summary>
  /// DEPRECATED: min and max value of the column.Use min_value and max_value.
  /// 
  /// Values are encoded using PLAIN encoding, except that variable-length byte
  /// arrays do not include a length prefix.
  /// 
  /// These fields encode min and max values determined by signed comparison
  /// only. New files should use the correct order for a column's logical type
  /// and store the values in the min_value and max_value fields.
  /// 
  /// To support older readers, these may be set when the column order is
  /// signed.
  /// </summary>
  [Deprecated()]
  [FieldId(2)]
  public byte[]? Min { get; set; }

  /// <summary>
  /// Count of null values in the column.
  /// 
  /// Writers SHOULD always write this field even if it is zero (i.e.no null value)
  /// or the column is not nullable.
  /// Readers MUST distinguish between null_count not being present and null_count == 0.
  /// If null_count is not present, readers MUST NOT assume null_count == 0.
  /// </summary>
  [FieldId(3)]
  public long? NullCount { get; set; }

  /// <summary>
  /// count of distinct values occurring
  /// </summary>
  [FieldId(4)]
  public long? DistinctCount { get; set; }

  /// <summary>
  /// Lower and upper bound values for the column, determined by its ColumnOrder.
  /// 
  /// These may be the actual minimum and maximum values found on a page or column
  /// chunk, but can also be(more compact) values that do not exist on a page or
  /// column chunk.For example, instead of storing "Blart Versenwald III", a writer
  /// may set min_value = "B", max_value = "C".Such more compact values must still be
  /// valid values within the column's logical type.
  /// 
  /// Values are encoded using PLAIN encoding, except that variable-length byte
  /// arrays do not include a length prefix.
  /// </summary>
  [FieldId(5)]
  public byte[]? MaxValue { get; set; }

  /// <summary>
  /// Lower and upper bound values for the column, determined by its ColumnOrder.
  /// 
  /// These may be the actual minimum and maximum values found on a page or column
  /// chunk, but can also be(more compact) values that do not exist on a page or
  /// column chunk.For example, instead of storing "Blart Versenwald III", a writer
  /// may set min_value = "B", max_value = "C".Such more compact values must still be
  /// valid values within the column's logical type.
  /// 
  /// Values are encoded using PLAIN encoding, except that variable-length byte
  /// arrays do not include a length prefix.
  /// </summary>
  [FieldId(6)]
  public byte[]? MinValue { get; set; }

  /// <summary>
  /// If true, max_value is the actual maximum value for a column
  /// </summary>
  [FieldId(7)]
  public bool? IsMaxValueExact { get; set; }

  /// <summary>
  /// If true, min_value is the actual minimum value for a column
  /// </summary>
  [FieldId(8)]
  public bool? IsMinValueExact { get; set; }

  /// <summary>
  /// Count of NaN values in the column; only present if physical type is FLOAT
  /// or DOUBLE, or logical type is FLOAT16.
  /// If this field is not present, readers MUST assume NaNs may be present
  /// (i.e.MUST assume nan_count > 0 and MAY NOT assume nan_count == 0).
  /// </summary>
  [FieldId(9)]
  public long? NanCount { get; set; }
}

/// <summary>
/// statistics of a given page type and encoding
/// </summary>
[ParquetThriftMetaData()]
public sealed class PageEncodingStats
{
  /// <summary>
  /// the page type (data/dic/...)
  /// </summary>
  [FieldId(1)]
  public PageType PageType { get; set; }

  /// <summary>
  /// encoding of the page
  /// </summary>
  [FieldId(2)]
  public Encoding Encoding { get; set; }

  /// <summary>
  /// number of pages of this type with this encoding
  /// </summary>
  [FieldId(3)]
  public long Count { get; set; }
}

/// <summary>
/// A structure for capturing metadata for estimating the unencoded,
/// uncompressed size of data written. This is useful for readers to estimate
/// how much memory is needed to reconstruct data in their memory model and for
/// fine grained filter pushdown on nested structures (the histograms contained
/// in this structure can help determine the number of nulls at a particular
/// nesting level and maximum length of lists).
/// </summary>
[ParquetThriftMetaData()]
public sealed class SizeStatistics
{
  /// <summary>
  /// The number of physical bytes stored for BYTE_ARRAY data values assuming
  /// no encoding.This is exclusive of the bytes needed to store the length of
  /// each byte array.In other words, this field is equivalent to the `(size
  /// of PLAIN-ENCODING the byte array values) - (4 bytes* number of values
  /// written)`. To determine unencoded sizes of other types readers can use
  /// schema information multiplied by the number of non - null and null values.
  /// The number of null / non - null values can be inferred from the histograms
  /// below.
  /// 
  /// For example, if a column chunk is dictionary-encoded with dictionary
  /// ["a", "bc", "cde"], and a data page contains the indices[0, 0, 1, 2],
  /// then this value for that data page should be 7 (1 + 1 + 2 + 3).
  /// 
  /// This field should only be set for types that use BYTE_ARRAY as their
  /// physical type.
  /// </summary>
  [FieldId(1)]
  public long? UnencodedByteArrayDataBytes { get; set; }

  /// <summary>
  /// When present, there is expected to be one element corresponding to each
  /// repetition(i.e.size= max repetition_level+1) where each element
  /// represents the number of times the repetition level was observed in the
  /// data.
  /// 
  /// This field may be omitted if max_repetition_level is 0 without loss
  /// of information.
  /// </summary>
  [FieldId(2)]
  public List<long>? RepetitionLevelHistogram { get; set; }

  /// <summary>
  /// Same as repetition_level_histogram except for definition levels.
  /// 
  /// This field may be omitted if max_definition_level is 0 or 1 without
  /// loss of information.
  /// </summary>
  [FieldId(3)]
  public List<long>? DefinitionLevelHistogram { get; set; }
}

/// <summary>
/// Bounding box for GEOMETRY or GEOGRAPHY type in the representation of min/max
/// value pair of coordinates from each axis.
/// </summary>
[ParquetThriftMetaData()]
public sealed class BoundingBox
{
  [FieldId(1)]
  public double xmin { get; set; }

  [FieldId(2)]
  public double xmax { get; set; }

  [FieldId(3)]
  public double ymin { get; set; }

  [FieldId(4)]
  public double ymax { get; set; }

  [FieldId(5)]
  public double? zmin { get; set; }

  [FieldId(6)]
  public double? zmax { get; set; }

  [FieldId(7)]
  public double? mmin { get; set; }

  [FieldId(8)]
  public double? mmax { get; set; }
}

/// <summary>
/// Statistics specific to Geometry and Geography logical types
/// </summary>
[ParquetThriftMetaData()]
public sealed class GeospatialStatistics
{
  /// <summary>
  /// A bounding box of geospatial instances
  /// </summary>
  [FieldId(1)]
  public BoundingBox? Bbox { get; set; }

  /// <summary>
  /// Geospatial type codes of all instances, or an empty list if not known
  /// </summary>
  [FieldId(2)]
  public List<int>? GeospatialTypes { get; set; }
}