namespace Dbarone.Net.Parquet.Dremel;

/// <summary>
/// Stores the definition levels, repetition levels and data values for a single leaf column.
/// </summary>
public sealed class ColumnBuffer
{
  public SchemaNode ColumnSchema { get; }
  public List<object?> Values { get; } = new();
  public List<int> DefinitionLevels { get; } = new();
  public List<int> RepetitionLevels { get; } = new();

  public ColumnBuffer(SchemaNode schema)
  {
    ColumnSchema = schema;
  }

  public void Write(object? value, int defLevel, int repLevel)
  {
    Values.Add(value);
    DefinitionLevels.Add(defLevel);
    RepetitionLevels.Add(repLevel);
  }
}