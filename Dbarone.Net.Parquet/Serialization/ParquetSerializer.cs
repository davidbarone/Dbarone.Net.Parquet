namespace Dbarone.Net.Parquet.Serialization;

using Dbarone.Net.Parquet.Thrift;
using Dbarone.Net.Buffers;
using Dbarone.Net.Buffers.Document;
using Dbarone.Net.Parquet.Encoding;
using Dbarone.Net.Parquet.Extensions;

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
    var paths = model.MetaData.GetSchemaPaths();

    // Loop through each row group
    // row groups are unioned at the end
    foreach (var rowGroup in model.MetaData.RowGroups)
    {
      // loop through each column chunk in the columns.
      // each column chunk has same number of rows - the rows in the row group
      var numRows = rowGroup.NumRows;
      for (int i = 1; i < schema.Count; i++)  // ignore the 'root' schema element.
      {
        var schemaElement = schema[i];    // schema element
        var columnName = schema[i].Name;  // column name
        var pathInSchema = paths[i];      // paths in schema

        // Check if a leaf column
        if (model.MetaData.IsLeafColumn(pathInSchema))
        {
          // Get Data Page HERE
          PageSerializer pageSer = new PageSerializer(buffer, model.MetaData, ThriftMetaDataSerialiser, paths[i]);
          var data = pageSer.GetData();

          model.Data = ResultsToTable(data, schemaElement);
        }
      }
    }
    return model;
  }

  private Table ResultsToTable(object[] results, SchemaElement schemaElement)
  {
    List<TableRow> rows = new List<TableRow>();
    foreach (var item in results)
    {
      TableRow tr = new TableRow(schemaElement.Name, item);
      rows.Add(tr);
    }
    return new Table(rows);
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
}