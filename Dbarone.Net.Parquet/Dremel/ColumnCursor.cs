namespace Dbarone.Net.Parquet.Dremel;

/// <summary>
/// Acts as a cursor in a ColumnBuffer object. Enables reading of the
/// definition level, repetition level, and data value.
/// </summary>
public sealed class ColumnCursor
{
  public ColumnBuffer Buffer { get; }
  public int Index { get; private set; }

  public ColumnCursor(ColumnBuffer buffer)
  {
    Buffer = buffer;
    Index = 0;
  }

  public bool HasNext => Index < Buffer.Values.Count;

  public (object? value, int def, int rep) Peek()
  {
    return (Buffer.Values[Index],
            Buffer.DefinitionLevels[Index],
            Buffer.RepetitionLevels[Index]);
  }

  public void Advance() => Index++;
}