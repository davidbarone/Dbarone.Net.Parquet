namespace Dbarone.Net.Parquet.Serialization;

using Dbarone.Net.Buffers;
using Dbarone.Net.Parquet.Thrift;
using Dbarone.Net.Parquet.Encoding;
using Dbarone.Net.Buffers.Document;
using Dbarone.Net.Parquet.Extensions;

/// <summary>
/// Base class to serialize and deserialize a column chunk.
/// </summary>
public class ChunkSerializer : IChunkSerializer
{
  private IBuffer Buffer { get; set; }
  private FileMetaData FileMetaData { get; set; }
  private ThriftMetaDataSerializer ThriftMetaDataSerializer { get; set; }
  private string[] PathInSchema { get; set; }
  private SchemaElement SchemaElement { get; set; }

  public ChunkSerializer(IBuffer buffer, FileMetaData fileMetaData, ThriftMetaDataSerializer thriftMetaDataSerializer, string[] pathInSchema)
  {
    this.Buffer = buffer;
    this.FileMetaData = fileMetaData;
    this.ThriftMetaDataSerializer = thriftMetaDataSerializer;
    this.PathInSchema = pathInSchema;
    this.SchemaElement = fileMetaData.GetSchemaElement(pathInSchema);
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
  public object[] GetData()
  {
    // Get the chunk index + chunk for the column
    var chunk_idx = this.FileMetaData.GetColumnChunkIndex(PathInSchema);
    if (chunk_idx is null)
    {
      throw new Exception("Cannot get data for non-leaf column.");
    }

    // Set the buffer position to start of the column chunk.
    var chunk = this.FileMetaData.RowGroups[0].Columns[chunk_idx.Value];

    // Does chunk contain a dictionary page? (a column chunk can have 0/1 dictionary pages)
    // We define the presence of a ditionary page if Metadata.DictionaryPageOffset is set.
    object[]? dict = null;
    if (chunk.Metadata.DictionaryPageOffset is not null)
    {
      dict = GetDictionaryPage(chunk.Metadata);
    }

    // Get the data (a column chunk has 1 or more data pages, so the DataPageOffset MUST be set)
    object[] data = GetDataPage(chunk.Metadata);

    // If dictionary used, perform lookups:
    if (dict is not null)
    {
      object[] results = new object[data.Length];
      for (int i = 0; i < data.Length; i++)
      {
        int index = (int)data[i];
        results[i] = dict[index];
      }
      data = results;
    }

    return data;
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

  private int[]? GetDefinitionLevels(PageHeader pageHeader)
  {
    // Check / get Repetition Levels
    var mdl = this.FileMetaData.GetMaxDefinitionLevel(this.SchemaElement);
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
      return null;
    }
  }

  private int[]? GetRepetitionLevels(PageHeader pageHeader)
  {
    // Check / get Repetition Levels
    var mdl = this.FileMetaData.GetMaxRepetitionLevel(this.SchemaElement);
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
      return null;
    }
  }

  private object[] GetDictionaryPage(ColumnMetaData metadata)
  {
    // Get header
    if (metadata.DictionaryPageOffset.HasValue)
    {
      var offset = metadata.DictionaryPageOffset.Value;
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
      var dict = encoding.Read(SchemaElement, dictionaryPageHeader.NumValues);
      return dict;
    }
    else
    {
      // only PLAIN encoding currently supported for dictionaries
      throw new Exception("Only PLAIN encoding currently supported for dictionaries.");
    }
  }

  private object[] GetDataPage(ColumnMetaData metadata)
  {
    var offset = metadata.DataPageOffset;
    this.Buffer.Position = offset;

    // Get the page header:
    var pageHeader = GetPageHeader();

    Dbarone.Net.Parquet.Encoding.Encoding encoding = default!;

    var dataPageHeader = pageHeader.DataPageHeader;
    if (dataPageHeader is null)
    {
      throw new Exception("GetDataPage requires a DataPageHeader to be set to non-null value");
    }

    // A data page can optionally have definition levels set
    // Get DefinitionLevels
    var mdl = this.FileMetaData.GetMaxDefinitionLevel(this.SchemaElement);
    var definitionLevels = GetDefinitionLevels(pageHeader);

    // Get the number of data values to read. This depends on the definition levels
    // as only non-null values are stored in a data page.
    var numValues = GetDataStreamNumValues(definitionLevels, mdl, pageHeader);

    // Get the data in the page:
    object[]? data = null;
    switch (dataPageHeader.Encoding)
    {
      case Thrift.Encoding.PLAIN:
        encoding = new PlainEncoding(Buffer);
        data = encoding.Read(SchemaElement, numValues);
        break;
      case Thrift.Encoding.DELTA_BINARY_PACKED:
        // for int32 and int64
        encoding = new DeltaBinaryPackedEncoding(Buffer);
        data = encoding.Read(SchemaElement, numValues);
        break;
      case Thrift.Encoding.RLE_DICTIONARY:
        encoding = new RLEEncoding(Buffer, RLEEncodingDataKind.DATA_PAGE_V1_DICTIONARY_INDICES);
        data = encoding.ReadInt32(numValues).Select(i => (object)i).ToArray();
        break;
      default:
        throw new Exception($"Encoding {dataPageHeader.Encoding} not supported.");
    }

    // Merge definition / Repetition levels with data
    data = MergeResults(data, definitionLevels);

    return data;
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
    if (definitionLevels is null)
    {
      return pageHeader.PageType == PageType.DICTIONARY_PAGE ? pageHeader.DictionaryPageHeader.NumValues : pageHeader.DataPageHeader.NumValues;
    }
    else
    {
      return definitionLevels.Count(l => l == maxDefinitionLevel);
    }
  }

  /// <summary>
  /// Merges data with repetition and definition levels.
  /// 
  /// TODO: This is only working for basic scenarios
  /// 
  /// </summary>
  /// <param name="data"></param>
  /// <param name="definitionLevels"></param>
  /// <returns></returns>
  /// <exception cref="Exception"></exception>
  private object[] MergeResults(object[] data, int[]? definitionLevels)
  {
    int numValues = 0;
    object[] results = new object[numValues];

    if (definitionLevels is null)
    {
      return data;
    }
    else
    {
      numValues = definitionLevels.Length;
      results = new object[numValues];
    }

    int currentDataIdx = 0;
    for (int i = 0; i < definitionLevels.Length; i++)
    {
      // TODO: This is approximation for now - need to handle definition levels 0..n
      // not just 0..1
      if (definitionLevels[i] == 1)
      {
        // Current object is not null
        results[i] = data[currentDataIdx];
        currentDataIdx++;
      }
      else
      {
        results[i] = System.DBNull.Value;
      }
    }

    if (currentDataIdx != data.Length)
    {
      throw new Exception("Error merging data with definition levels");
    }
    return results;
  }

  #endregion
}