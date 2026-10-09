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


  /// <summary>
  /// Creates a complete set of column buffers in one hit:
  /// - repetition levels
  /// - definition levels
  /// - physical data values
  /// 
  /// notes: for internal storage, all 3 are required to have same number of rows.
  /// However, these values are read in from Parquet file, with the following
  /// properties:
  /// - RL can be empty or populated
  /// - DL can be empty or populated
  /// - if RL and DL populated, then must have same number of rows (DL/RL pair)
  /// - number of DL/RL pairs = total number of logical values (null/non null)
  /// - number of physical/encoded values = number of non null (where DL=MDL)
  /// 
  /// This constructor checks the provided data arrays, and creates a column buffer
  /// with the following properties:
  /// - total DL = total RL = total values
  /// - values are padded with nulls where required
  /// - DL + RL are padded with 0's where required
  /// </summary>
  /// <param name="schema"></param>
  /// <param name="values"></param>
  /// <param name="repetitionLevels"></param>
  /// <param name="definitionLevels"></param>
  /// <exception cref="Exception"></exception>
  internal ColumnBuffer(SchemaNode schema, object?[] values, int[] repetitionLevels, int[] definitionLevels)
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
    if (DefinitionLevels.Count() == 0 && RepetitionLevels.Count() == 0 && Values.Count() == 0)
    {
      // Genuinly empty chunk
    }
    else if (DefinitionLevels.Count() == 0 && RepetitionLevels.Count() == 0)
    {
      // pad out RL + DL with 0's
      DefinitionLevels = new List<int>(System.Linq.Enumerable.Repeat<int>(0, Values.Count()));
      RepetitionLevels = new List<int>(System.Linq.Enumerable.Repeat<int>(0, Values.Count()));
    }
    else if (DefinitionLevels.Count() > 0 && RepetitionLevels.Count() == 0)
    {
      // pad out RL with MRL?
      RepetitionLevels = new List<int>(System.Linq.Enumerable.Repeat<int>(schema.MaxRepetitionLevel, DefinitionLevels.Count()));
      // pad out values with nulls
      for (int i = 0; i < DefinitionLevels.Count(); i++)
      {
        if (DefinitionLevels[i] < schema.MaxDefinitionLevel)
        {
          // add a null
          Values.Insert(i, null);
        }
      }
    }
    else if (RepetitionLevels.Count() > 0 && DefinitionLevels.Count() == 0)
    {
      // pad out DL with MDL?
      DefinitionLevels = new List<int>(System.Linq.Enumerable.Repeat<int>(schema.MaxDefinitionLevel, DefinitionLevels.Count()));
    }
    else
    {
      // repetition + definition values set
      // pad out values with nulls
      for (int i = 0; i < DefinitionLevels.Count(); i++)
      {
        if (DefinitionLevels[i] < schema.MaxDefinitionLevel)
        {
          // add a null
          Values.Insert(i, null);
        }
      }
    }
  }

  public void Write(object? value, int repLevel, int defLevel)
  {
    Values.Add(value);
    RepetitionLevels.Add(repLevel);
    DefinitionLevels.Add(defLevel);
  }
}