namespace Dbarone.Net.Parquet;

using Dbarone.Net.Parquet.Thrift;
using Dbarone.Net.Buffers.Document;

public class ParquetResult
{
  public FileMetaData MetaData { get; set; } = default!;
  public Table Data { get; set; } = default!;
}