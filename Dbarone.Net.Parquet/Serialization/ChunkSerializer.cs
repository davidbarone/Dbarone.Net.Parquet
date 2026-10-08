namespace Dbarone.Net.Parquet.Serialization;

using Dbarone.Net.Buffers;
using Dbarone.Net.Parquet.Thrift;
using Dbarone.Net.Parquet.Encoding;
using Dbarone.Net.Buffers.Document;
using Dbarone.Net.Parquet.Extensions;
using Dbarone.Net.Parquet.Dremel;

/// <summary>
/// Base class to serialize and deserialize a column chunk.
/// </summary>
public class ChunkSerializer : IChunkSerializer
{
  private IBuffer Buffer { get; set; }
  private ThriftMetaDataSerializer ThriftMetaDataSerializer { get; set; }

  public ChunkSerializer(IBuffer buffer, ThriftMetaDataSerializer thriftMetaDataSerializer)
  {
    this.Buffer = buffer;
    this.ThriftMetaDataSerializer = thriftMetaDataSerializer;
  }

  #region Public Methods

  /// <summary>
  /// Gets the data in a chunk. A chunk can have:
  /// - 0/1 dictionary pages
  /// - 1 or more data pages
  /// - 0 or more index pages
  /// 
  /// The order of pages is not specified in the Parquet specification.
  /// A page is the smallest unit of encoding and compression.
  /// Pages can be in any order within a chunk.
  /// </summary>
  /// <returns></returns>
  public ColumnBuffer GetData(SchemaNode node, ColumnChunk chunk)
  {
    // Does chunk contain a dictionary page? (a column chunk can have 0/1 dictionary pages)
    // We define the presence of a ditionary page if Metadata.DictionaryPageOffset is set.
    object[]? dict = null;

    if (chunk?.Metadata?.DictionaryPageOffset is not null)
    {
      dict = GetDictionaryPage(node, chunk);
    }

    if (chunk?.Metadata?.DataPageOffset is null)
    {
      throw new Exception("Column chunk data page offset is null");
    }

    // Get the data (a column chunk has 1 or more data pages, so the DataPageOffset MUST be set)
    var data = GetDataPage(node, chunk);

    // If dictionary used, perform lookups:
    if (dict is not null)
    {
      object[] results = new object[data.Values.Length];
      for (int i = 0; i < data.Values.Length; i++)
      {
        int index = (int)data.Values[i];
        results[i] = dict[index];
      }
      data.Values = results;
    }

    ColumnBuffer cb = new ColumnBuffer(node, data.Values, data.RepetitionLevels, data.DefinitionLevels);
    return cb;
  }

  #endregion

  #region Private Methods

  private PageHeader GetPageHeader()
  {
    // Get the current position of the buffer
    var start = Buffer.Position;
    var size = Buffer.Length;

    // When reading header, read in 4K limited by size remaining
    var lengthToRead = (int)long.Min(4000, size - start);

    var bytes = Buffer.ReadBytes(lengthToRead);
    GenericBuffer pageHeaderBuffer = new GenericBuffer(bytes);
    var ph = ThriftMetaDataSerializer.GetPageHeader(pageHeaderBuffer);

    // Set the original buffer's position to the same point reached
    Buffer.Position = start + pageHeaderBuffer.Position;

    return ph;
  }

  private object[] GetDictionaryPage(SchemaNode node, ColumnChunk chunk)
  {
    // Get header
    if (chunk.Metadata.DictionaryPageOffset.HasValue)
    {
      var offset = chunk.Metadata.DictionaryPageOffset.Value;
      this.Buffer.Position = offset;
    }
    else
    {
      throw new Exception("Expecting DictionaryPageOffset to be set.");
    }

    // Get the page page:
    var pageHeader = GetPageHeader();

    // The page header MUST have a DictionaryPageHeader if a dictionary page.
    var dictionaryPageHeader = pageHeader.DictionaryPageHeader;
    if (dictionaryPageHeader is null)
    {
      throw new Exception("Expected dictionary page header here!");
    }

    // get the encoding
    var enc = dictionaryPageHeader.Encoding;

    if (enc == Dbarone.Net.Parquet.Thrift.Encoding.PLAIN)
    {
      var encoding = new PlainEncoding(Buffer);
      var dict = encoding.Read(node.SchemaElement, dictionaryPageHeader.NumValues);
      return dict;
    }
    else
    {
      // only PLAIN encoding currently supported for dictionaries
      throw new Exception("Only PLAIN encoding currently supported for dictionaries.");
    }
  }

  private (object[] Values, int[] RepetitionLevels, int[] DefinitionLevels) GetDataPage(SchemaNode node, ColumnChunk chunk)
  {
    var offset = chunk.Metadata.DataPageOffset;
    this.Buffer.Position = offset;

    // Get the page header:
    var pageHeader = GetPageHeader();

    Dbarone.Net.Parquet.Encoding.Encoding encoding = default!;

    var dataPageHeader = pageHeader.DataPageHeader;
    if (dataPageHeader is null)
    {
      throw new Exception("GetDataPage requires a DataPageHeader to be set to non-null value");
    }

    // A data page can optionally have repetition levels set
    var repetitionLevels = GetRepetitionLevels(node, pageHeader);

    // A data page can optionally have definition levels set
    var definitionLevels = GetDefinitionLevels(node, pageHeader);

    // Get the number of data values to read. This depends on the definition levels
    // as only non-null values are stored in a data page.
    var numValues = GetDataStreamNumValues(definitionLevels, node.MaxDefinitionLevel, pageHeader);

    // Get the data in the page:
    object[]? data = null;
    switch (dataPageHeader.Encoding)
    {
      case Thrift.Encoding.PLAIN:
        encoding = new PlainEncoding(Buffer);
        data = encoding.Read(node.SchemaElement, numValues);
        break;
      case Thrift.Encoding.DELTA_BINARY_PACKED:
        // for int32 and int64
        encoding = new DeltaBinaryPackedEncoding(Buffer);
        data = encoding.Read(node.SchemaElement, numValues);
        break;
      case Thrift.Encoding.RLE_DICTIONARY:
        encoding = new RLEEncoding(Buffer, RLEEncodingDataKind.DATA_PAGE_V1_DICTIONARY_INDICES);
        data = encoding.ReadInt32(numValues).Select(i => (object)i).ToArray();
        break;
      default:
        throw new Exception($"Encoding {dataPageHeader.Encoding} not supported.");
    }

    return (data, repetitionLevels, definitionLevels);
  }

  private int[] GetDefinitionLevels(SchemaNode node, PageHeader pageHeader)
  {
    // Check / get Repetition Levels
    var mdl = node.MaxDefinitionLevel;
    var numValues = pageHeader.PageType == PageType.DICTIONARY_PAGE ? pageHeader.DictionaryPageHeader.NumValues : pageHeader.DataPageHeader.NumValues;
    int[] definitionLevels = new int[numValues];

    if (mdl > 0)
    {
      // Calculate the bit width: bitWidth = log2(MaxDefinitionLevel + 1)
      int bitWidth = (int)Math.Ceiling(Math.Log2(mdl + 1));

      // read definition levels
      definitionLevels = new RLEEncoding(this.Buffer, RLEEncodingDataKind.DATA_PAGE_V1_DEFINITION_LEVEL, bitWidth).ReadInt32(numValues);
      return definitionLevels;
    }
    else
    {
      return Array.Empty<int>();
    }
  }

  private int[]? GetRepetitionLevels(SchemaNode node, PageHeader pageHeader)
  {
    // Check / get Repetition Levels
    var mrl = node.MaxRepetitionLevel;
    var numValues = pageHeader.PageType == PageType.DICTIONARY_PAGE ? pageHeader.DictionaryPageHeader.NumValues : pageHeader.DataPageHeader.NumValues;
    int[] repetitionLevels = new int[numValues];

    if (mrl > 0)
    {
      // Calculate the bit width: bitWidth = log2(MaxDefinitionLevel + 1)
      int bitWidth = (int)Math.Ceiling(Math.Log2(mrl + 1));

      // read definition levels
      repetitionLevels = new RLEEncoding(this.Buffer, RLEEncodingDataKind.DATA_PAGE_V1_DEFINITION_LEVEL, bitWidth).ReadInt32(numValues);
      return repetitionLevels;
    }
    else
    {
      return Array.Empty<int>();
    }
  }

  /// <summary>
  /// Gets the number of values to read from the data stream.
  /// 
  /// The following rules are interpreted from the specification:
  /// - If no definition levels, then all values are non null.
  /// In this case, all data is decoded from data stream.
  /// - If definition levels, then the data stream only includes
  /// non-null values. To get the number of non-null values you
  /// need to count the number of records in the definitionLevels
  /// where value==MaxDefinitionLevel. Note you cannot use
  /// PageHeader.Statistics.NullCount - PageHeader.Statistics is
  /// optional per specification. Counting DL non-null is
  /// canonical way to go.
  /// </summary>
  /// <param name="definitionLevels">The definition levels</param>
  /// <param name="maxDefinitionLevel">The maximum definition level</param>
  /// <param name="pageHeader">The page header</param>
  /// <returns>Returns the number of values to read from the data stream.</returns>
  public int GetDataStreamNumValues(int[]? definitionLevels, int maxDefinitionLevel, PageHeader pageHeader)
  {
    if (definitionLevels is null || definitionLevels.Length == 0)
    {
      return pageHeader.PageType == PageType.DICTIONARY_PAGE ? pageHeader.DictionaryPageHeader.NumValues : pageHeader.DataPageHeader.NumValues;
    }
    else
    {
      return definitionLevels.Count(l => l == maxDefinitionLevel);
    }
  }

  #endregion
}