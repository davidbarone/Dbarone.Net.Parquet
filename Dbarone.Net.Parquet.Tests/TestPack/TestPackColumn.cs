namespace Dbarone.Net.Parquet.Tests;

/// <summary>
/// Generates a column for a test pack table.
/// </summary>
public class TestPackColumn
{
  public TestPackColumn(Type dataType, Func<IEnumerable<object>> generator, bool? nullable = null)
  {
    this.DataType = dataType;
    this.Generator = generator;
    this.Nullable = nullable;
  }

  public Type DataType { get; set; } = default!;
  public bool? Nullable { get; set; } = null;
  public Func<IEnumerable<object>> Generator { get; set; } = default!;
}