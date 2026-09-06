namespace Dbarone.Net.Parquet.Serialization;

using Dbarone.Net.Buffers;
using Dbarone.Net.Parquet.Thrift;
using Dbarone.Net.Parquet.Encoding;
using Dbarone.Net.Buffers.Document;
using Dbarone.Net.Parquet.Extensions;

/// <summary>
/// Base class to serialize and deserialize a page within a column chunk.
/// </summary>
public class PageSerializer
{
  private IBuffer Buffer { get; set; }
  private FileMetaData FileMetaData { get; set; }
  private ThriftMetaDataSerializer ThriftMetaDataSerializer { get; set; }
  private string[] PathInSchema { get; set; }
  private SchemaElement SchemaElement { get; set; }

  public PageSerializer(IBuffer buffer, FileMetaData fileMetaData, ThriftMetaDataSerializer thriftMetaDataSerializer, string[] pathInSchema)
  {
    this.Buffer = buffer;
    this.FileMetaData = fileMetaData;
    this.ThriftMetaDataSerializer = thriftMetaDataSerializer;
    this.PathInSchema = pathInSchema;
    this.SchemaElement = fileMetaData.GetSchemaElement(pathInSchema);
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

  private object[] GetDataPage(PageHeader pageHeader, int numValues)
  {
    Dbarone.Net.Parquet.Encoding.Encoding encoding = default!;

    var dataPageHeader = pageHeader.DataPageHeader;
    if (dataPageHeader is null)
    {
      throw new Exception("dataPageHeader is null");
    }

    // Get the encoding in the page:
    switch (dataPageHeader.Encoding)
    {
      case Thrift.Encoding.PLAIN:
        encoding = new PlainEncoding(Buffer);
        return encoding.Read(SchemaElement, numValues);
      case Thrift.Encoding.DELTA_BINARY_PACKED:
        // for int32 and int64
        encoding = new DeltaBinaryPackedEncoding(Buffer);
        return encoding.Read(SchemaElement, numValues);
      default:
        throw new Exception($"Encoding {dataPageHeader.Encoding} not supported.");
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
    if (definitionLevels is null)
    {
      return pageHeader.PageType == PageType.DICTIONARY_PAGE ? pageHeader.DictionaryPageHeader.NumValues : pageHeader.DataPageHeader.NumValues;
    }
    else
    {
      return definitionLevels.Count(l => l == maxDefinitionLevel);
    }
  }

  public object[] GetDictionaryPage(PageHeader pageHeader)
  {
    // First page is the dictionary page
    var dict = GetDictionary(pageHeader);

    // Next page is the RLE encoding using the dictionary
    var dataPageHeader = GetPageHeader();
    if (dataPageHeader.PageType != PageType.DATA_PAGE)
    {
      throw new Exception("Expecting page type DATA_PAGE here");
    }

    // Next get the indexes - this is always done as RLE encoding
    var indexes = new RLEEncoding(Buffer, RLEEncodingDataKind.DATA_PAGE_V1_DICTIONARY_INDICES).ReadInt32(dataPageHeader.DataPageHeader.NumValues);

    object[] results = new object[indexes.Length];
    for (int i = 0; i < indexes.Length; i++)
    {
      results[i] = dict[indexes[i]];
    }

    return results;
  }

  /// <summary>
  /// Gets the data in the page
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
    var offset = chunk.Metadata.DataPageOffset;
    this.Buffer.Position = offset;

    // Get the page page:
    var pageHeader = GetPageHeader();

    // Get DefinitionLevels
    var mdl = this.FileMetaData.GetMaxDefinitionLevel(this.SchemaElement);
    var definitionLevels = GetDefinitionLevels(pageHeader);

    // Get number of values to read from data stream
    var numValues = GetDataStreamNumValues(definitionLevels, mdl, pageHeader);

    object[] results = default!;
    // Check the type of page
    if (pageHeader.PageType == PageType.DATA_PAGE)
    {
      results = GetDataPage(pageHeader, numValues);
    }
    else if (pageHeader.PageType == PageType.DICTIONARY_PAGE)
    {
      results = GetDictionaryPage(pageHeader);
    }

    // Merge definition / Repetition levels with data
    results = MergeResults(results, definitionLevels);

    return results;
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

  private object[] GetDictionary(PageHeader pageHeader)
  {
    var dictionaryPageHeader = pageHeader.DictionaryPageHeader;
    if (dictionaryPageHeader is null)
    {
      throw new Exception("Expected dictionary page header here!");
    }

    // get the encoding
    var enc = dictionaryPageHeader.Encoding;

    if (enc == Dbarone.Net.Parquet.Thrift.Encoding.PLAIN_DICTIONARY)
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

}