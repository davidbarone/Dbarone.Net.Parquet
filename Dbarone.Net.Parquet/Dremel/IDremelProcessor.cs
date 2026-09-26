using Dbarone.Net.Parquet.Dremel;

public interface IDremelProcessor
{
  public Dictionary<SchemaNode, ColumnBuffer> Shred(SchemaNode root, IEnumerable<IDictionary<string, object?>> rows);

  public IList<IDictionary<string, object?>> Assemble(SchemaNode root, Dictionary<SchemaNode, ColumnBuffer> buffers);
}