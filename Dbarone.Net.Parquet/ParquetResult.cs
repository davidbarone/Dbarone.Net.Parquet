namespace Dbarone.Net.Parquet;

using Dbarone.Net.Parquet.Thrift;
using Dbarone.Net.Parquet.Document;
using Dbarone.Net.Parquet.Dremel;
using Dbarone.Net.Parquet.Common;

public class ParquetResult
{
  public FileMetaData MetaData { get; set; } = default!;
  public Table Data { get; set; } = new Table();
  public List<Dictionary<Node<SchemaElement>, ColumnBuffer>> RowGroupBuffers { get; set; } = new List<Dictionary<Node<SchemaElement>, ColumnBuffer>>();
}