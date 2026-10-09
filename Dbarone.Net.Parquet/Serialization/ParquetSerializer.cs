namespace Dbarone.Net.Parquet.Serialization;

using Dbarone.Net.Parquet.Thrift;
using Dbarone.Net.Buffers;
using Dbarone.Net.Buffers.Document;
using Dbarone.Net.Parquet.Encoding;
using Dbarone.Net.Parquet.Extensions;
using Dbarone.Net.Parquet.Dremel;

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
    IList<Dictionary<string, object?>> results = new List<Dictionary<string, object?>>();

    // Loop through each row group
    // and get all corresponding column chunks, assembling them into a
    // resultset using Dremel, and unioning all row chunks at the end.

    // Convert schema to dremel node hierarchy
    DremelProcessor dremel = new DremelProcessor();
    (var root, _) = SchemaNode.BuildFromThriftSchema(model.MetaData.Schema);

    for (int i = 0; i < model.MetaData.RowGroups.Count(); i++)
    {
      var rowGroup = model.MetaData.RowGroups[i];
      var rowGroupBuffers = AssembleRowGroup(buffer, root, rowGroup.Columns);
      model.RowGroupBuffers.Add(rowGroupBuffers);
      var result = dremel.Assemble(root, rowGroupBuffers);
      var rows = result.Select(r => new TableRow(r));
      model.Data.AddRange(rows);
    }

    // Assemble data using Dremel encoding
    return model;
  }

  private Dictionary<SchemaNode, ColumnBuffer> AssembleRowGroup(IBuffer buffer, SchemaNode root, List<ColumnChunk> chunks)
  {
    // Get leaf nodes
    var leaves = SchemaNode.GetLeafNodes(root);

    // Get data for each leaf column
    Dictionary<SchemaNode, ColumnBuffer> results = new Dictionary<SchemaNode, ColumnBuffer>();
    IChunkSerializer ser = new ChunkSerializer(buffer, ThriftMetaDataSerialiser);
    foreach (var leaf in leaves)
    {
      var data = ser.GetData(leaf, chunks[leaf.ChunkIndex!.Value]);
      results[leaf] = data;
    }
    return results;
  }

  /// <summary>
  /// Converts a SchemaElement + array of values into a single-column table,
  /// or appends the column to an existing Table object.
  /// </summary>
  /// <param name="results"></param>
  /// <param name="schemaElement"></param>
  /// <param name="existingTable"></param>
  /// <returns></returns>
  private Table ResultsToTable(object[] results, SchemaElement schemaElement, Table existingTable)
  {
    List<TableRow> rows = new List<TableRow>();
    var rowCount = results.Count();

    // if appending column to existing table, make sure row counts are the same
    if (existingTable is not null && existingTable.Count() != results.Count())
    {
      throw new Exception("Error in ResultsToTable. Row mismatch.");
    }

    for (int i = 0; i < rowCount; i++)
    {
      if (existingTable is null)
      {
        // First column being added to table, just add all rows as single-column rows
        TableRow tr = new TableRow(schemaElement.Name, results[i]);
        rows.Add(tr);
      }
      else
      {
        // update existing row
        var existingRow = existingTable[i];
        existingRow[schemaElement.Name] = new TableCell(results[i]);

      }
    }
    if (existingTable is null)
    {
      return new Table(rows);
    }
    else
    {
      return existingTable;
    }
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