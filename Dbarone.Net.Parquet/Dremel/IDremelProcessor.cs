namespace Dbarone.Net.Parquet.Dremel;

using Dbarone.Net.Parquet.Common;
using Dbarone.Net.Parquet.Thrift;

public interface IDremelProcessor
{
  public Dictionary<Node<SchemaElement>, ColumnBuffer> Shred(Node<SchemaElement> root, IEnumerable<IDictionary<string, object?>> rows);

  public IList<IDictionary<string, object?>> Assemble(Dictionary<Node<SchemaElement>, ColumnBuffer> buffers);
}