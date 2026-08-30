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

  private object[] GetDataPage(PageHeader pageHeader)
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
        return encoding.Read(SchemaElement, dataPageHeader.NumValues);
      case Thrift.Encoding.DELTA_BINARY_PACKED:
        // for int32 and int64
        encoding = new DeltaBinaryPackedEncoding(Buffer);
        return encoding.Read(SchemaElement, dataPageHeader.NumValues);
      default:
        throw new Exception($"Encoding {dataPageHeader.Encoding} not supported.");
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
    var indexes = new RLEEncoding(Buffer).ReadInt32(dataPageHeader.DataPageHeader.NumValues);

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

    object[] results = default!;
    // Check the type of page
    if (pageHeader.PageType == PageType.DATA_PAGE)
    {
      results = GetDataPage(pageHeader);
    }
    else if (pageHeader.PageType == PageType.DICTIONARY_PAGE)
    {
      results = GetDictionaryPage(pageHeader);
    }
    return results;
  }

  private void GetRepetitionLevels()
  {

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