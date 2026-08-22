namespace Dbarone.Net.Parquet;

using Dbarone.Net.Parquet.Thrift;
using Dbarone.Net.Buffers;
using Dbarone.Net.Buffers.Document;
using Dbarone.Net.Parquet.Encoding;
using System.Text;

/// <summary>
/// Parquet is an open source, column-oriented data file format designed for
/// efficient data storage and retrieval.
/// 
/// The Parquet format document can be found here: https://parquet.apache.org/
/// 
/// Parquet files use Parquet.Thrift:
/// (https://github.com/apache/parquet-format/blob/master/src/main/thrift/parquet.thrift)
/// To store metadata in Parquet files.
/// 
/// Parquet.Thrift is encoded using the Thrift Compact Protocol encoding:
/// https://github.com/apache/thrift/blob/master/doc/specs/thrift-compact-protocol.md
/// </summary>
public class ParquetSerializer
{
  /// <summary>
  /// To read/write metadata.
  /// </summary>
  public ThriftMetaDataSerializer ThriftMetaDataSerialiser { get; set; } = new ThriftMetaDataSerializer();

  public ParquetResult Read(byte[] bytes, TextEncoding textEncoding = TextEncoding.UTF8)
  {
    IBuffer buffer = new GenericBuffer(bytes);
    return Read(buffer, textEncoding);
  }


  /// <summary>
  /// Deserializes a buffer contains parquet-formatted data, into a table.
  /// </summary>
  /// <param name="buffer"></param>
  /// <param name="textEncoding"></param>
  /// <returns></returns>
  public ParquetResult Read(IBuffer buffer, TextEncoding textEncoding = TextEncoding.UTF8)
  {
    // Create return object
    ParquetResult model = new ParquetResult();

    // Magic header
    buffer.Position = 0;
    var magicHeader = System.Text.Encoding.UTF8.GetString(buffer.ReadBytes(4));
    if (!magicHeader.Equals("PAR1"))
    {
      throw new Exception("Invalid magic header");
    }

    // Magic footer
    buffer.Position = buffer.Length - 4;
    var magicFooter = System.Text.Encoding.UTF8.GetString(buffer.ReadBytes(4));
    if (!magicFooter.Equals("PAR1"))
    {
      throw new Exception("Invalid magic footer");
    }

    // Get file metadata length - 4 bytes immediately prior to magic footer - 4 bytes in little-endian format
    model.MetaData = GetFileMetaData(buffer);

    // Having got the metadata, we can now read the actual data
    // Order is: RowGroup -> ColumnChunk -> PageHeader -> DataPage
    // 1 parquet file can only have 1 column schema - all rows must have same colums + types

    // To store the results
    List<Dictionary<string, object?>> results = new List<Dictionary<string, object?>>();

    // Get the schema
    // Note that schema[0] is 'root'.
    var schema = model.MetaData.Schema;

    // Loop through each row group
    // row groups are unioned at the end
    foreach (var rowGroup in model.MetaData.RowGroups)
    {
      // loop through each column chunk in the columns.
      // each column chunk has same number of rows - the rows in the row group
      var numRows = rowGroup.NumRows;
      for (int i = 1; i < schema.Count; i++)  // ignore the 'root' schema element.
      {
        var columnName = schema[i].Name;  // column name
        var chunk = rowGroup.Columns[i - 1];

        // each column chunk in a row group is divided into pages.
        // get start and length of 1st page header for chunk
        var start = chunk.FileOffset;
        buffer.Position = start;
        var ph = GetPageHeader(buffer);

        // Check the type of page
        if (ph.PageType == PageType.DICTIONARY_PAGE)
        {
          var dict = GetDictionary(ph.DictionaryPageHeader!, chunk.Metadata!.Type, buffer);
          // Now we get the data for the dictionary
          var dataPageHeader = GetPageHeader(buffer);
          if (dataPageHeader.PageType != PageType.DATA_PAGE)
          {
            throw new Exception("whoops!");
          }

          List<TableRow> rows = new List<TableRow>();
          foreach (var item in new RLEEncoder().Decode(buffer, chunk.Metadata.NumValues, dict))
          {
            TableRow tr = new TableRow(columnName, item);
            rows.Add(tr);
          }
          model.Data = new Table(rows);
        }
        else if (ph.PageType == PageType.DATA_PAGE)
        {
          List<TableRow> rows = new List<TableRow>();
          var raw = GetDataPage(chunk.Metadata.Type, ph.DataPageHeader, buffer);
          foreach (var item in raw)
          {
            TableRow tr = new TableRow(columnName, item);
            rows.Add(tr);
          }
          model.Data = new Table(rows);
        }
      }
    }
    return model;
  }

  private FileMetaData GetFileMetaData(IBuffer buffer)
  {
    // Get file metadata length - 4 bytes immediately prior to magic footer - 4 bytes in little-endian format
    buffer.Position = buffer.Length - 4 - 4;
    var bytes = buffer.ReadBytes(4);
    // reverse byte order for big-endian systems:
    if (!BitConverter.IsLittleEndian)
    {
      Array.Reverse(bytes);
    }
    int length = BitConverter.ToInt32(bytes, 0);

    // Get metadata
    // Encoded in Apache Thrift compact/binary protocol (FileMetaData struct)
    // https://thrift.apache.org/
    buffer.Position = buffer.Length - 4 - 4 - length;
    var metadataBytes = buffer.ReadBytes(length);
    GenericBuffer metadataBuffer = new GenericBuffer(metadataBytes);
    return ThriftMetaDataSerialiser.GetFileMetaData(metadataBuffer);
  }

  private PageHeader GetPageHeader(IBuffer buffer)
  {
    // Get the current position of the buffer
    var start = buffer.Position;
    var size = buffer.Length;

    // When reading header, read in 4K limited by size remaining
    var lengthToRead = (int)long.Min(4000, size - start);

    var bytes = buffer.ReadBytes(lengthToRead);
    GenericBuffer pageHeaderBuffer = new GenericBuffer(bytes);
    var ph = ThriftMetaDataSerialiser.GetPageHeader(pageHeaderBuffer);

    // Set the original buffer's position to the same point reached
    buffer.Position = start + pageHeaderBuffer.Position;

    return ph;
  }

  /// <summary>
  /// Gets a dictionary page.
  /// </summary>
  /// <param name="buffer">The parquet buffer.</param>
  /// <returns>Returns a dictionary page.</returns>
  private IList<object> GetDictionary(DictionaryPageHeader header, Dbarone.Net.Parquet.Thrift.Type type, IBuffer buffer)
  {
    if (header is null)
    {
      throw new Exception("Dictionary page header is null!");
    }

    // get the encoding
    var enc = header.Encoding;

    if (enc == Dbarone.Net.Parquet.Thrift.Encoding.PLAIN_DICTIONARY)
    {
      var dict = new PlainEncoder().Decode(buffer, header.NumValues, type).ToList();
      return dict;
    }
    else
    {
      // only PLAIN encoding currently supported for dictionaries
      throw new Exception("Only PLAIN encoding currently supported for dictionaries.");
    }
  }

  private IEnumerable<object> GetDataPage(Dbarone.Net.Parquet.Thrift.Type type, DataPageHeader dataPageHeader, IBuffer buffer)
  {
    // Get the encoding in the page:
    switch (dataPageHeader.Encoding)
    {
      case Thrift.Encoding.PLAIN:
        PlainEncoder encoder2 = new PlainEncoder();
        foreach (var item in encoder2.Decode(buffer, dataPageHeader.NumValues, type))
        {
          yield return item;
        }
        break;
      case Thrift.Encoding.DELTA_BINARY_PACKED:
        // for int32 and int64
        DeltaBinaryPackedEncoder encoder = new DeltaBinaryPackedEncoder();
        var result = encoder.Decode(buffer);
        foreach (var item in result)
        {
          if (type == Thrift.Type.INT32)
          {
            yield return (int)item;
          }
          else if (type == Thrift.Type.INT64)
          {
            yield return item;
          }
          else
          {
            throw new Exception($"Invalid type: {type}");
          }
        }
        break;
      default:
        throw new Exception($"Encoding {dataPageHeader.Encoding} not supported.");
    }
  }
}