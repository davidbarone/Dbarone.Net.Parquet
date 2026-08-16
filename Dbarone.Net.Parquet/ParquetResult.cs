namespace Dbarone.Net.Parquet;

using Dbarone.Net.Parquet.Thrift;

public class ParquetResult
{
  public FileMetaData MetaData { get; set; } = default!;
  public Table Data { get; set; } = default!;
}