using Dbarone.Net.Parquet.Dremel;
using Dbarone.Net.Parquet.Thrift;

/// <summary>
/// Defines chunk serialization operations.
/// A single chunk contains a subset of data for a single column.
/// </summary>
public interface IChunkSerializer
{
  /// <summary>
  /// Gets the data in a chunk.
  /// </summary>
  /// <returns>The chunk data returned as an array.</returns>
  ColumnBuffer GetData(SchemaNode node, ColumnChunk chunk);
}
