namespace Dbarone.Net.Parquet.Dremel;

using Dbarone.Net.Parquet.Common;
using Dbarone.Net.Parquet.Dremel;

public interface IDremelProcessor
{
  public Dictionary<Node<LogicalSchemaElement>, ColumnBuffer> Shred(Node<LogicalSchemaElement> root, IEnumerable<IDictionary<string, object?>> rows);

  public IList<IDictionary<string, object?>> Assemble(Node<LogicalSchemaElement> root, Dictionary<Node<LogicalSchemaElement>, ColumnBuffer> buffers);
}