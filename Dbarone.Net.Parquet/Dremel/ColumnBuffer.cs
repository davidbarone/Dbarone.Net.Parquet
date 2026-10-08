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

  public ColumnBuffer(SchemaNode schema, object?[] values, int[] repetitionLevels, int[] definitionLevels)
  {
    if (values is null)
    {
      throw new Exception("Values must not be null");
    }
    ColumnSchema = schema;
    Values = values.ToList();
    RepetitionLevels = repetitionLevels.ToList();
    DefinitionLevels = definitionLevels.ToList();

    // need to balance the 3 arrays.
    if (schema.MaxDefinitionLevel == 0)
    {
      DefinitionLevels = new List<int>(System.Linq.Enumerable.Repeat<int>(0, Values.Count()));
    }
    if (schema.MaxRepetitionLevel == 0)
    {
      RepetitionLevels = new List<int>(System.Linq.Enumerable.Repeat<int>(0, Values.Count()));
    }
  }

  public void Write(object? value, int repLevel, int defLevel)
  {
    Values.Add(value);
    RepetitionLevels.Add(repLevel);
    DefinitionLevels.Add(defLevel);
  }
}