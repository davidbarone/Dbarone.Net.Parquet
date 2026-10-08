namespace Dbarone.Net.Parquet;

using Dbarone.Net.Parquet.Thrift;
using Dbarone.Net.Buffers.Document;
using Dbarone.Net.Parquet.Dremel;

public class ParquetResult
{
  public FileMetaData MetaData { get; set; } = default!;
  public Table Data { get; set; } = new Table();
  public List<Dictionary<SchemaNode, ColumnBuffer>> RowGroupBuffers { get; set; } = new List<Dictionary<SchemaNode, ColumnBuffer>>();
}