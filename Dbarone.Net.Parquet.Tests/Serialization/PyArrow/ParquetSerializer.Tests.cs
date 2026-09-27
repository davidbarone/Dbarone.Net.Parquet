namespace Dbarone.Net.Parquet.Tests.PyArrow;

using Dbarone.Net.Parquet.Serialization;

/// <summary>
/// Tests using a Parquet Test Pack generated from PyArrow
/// </summary>
public class ParquetSerializerTests
{
  public byte[] GetFile(string name)
  {
    string filePath = Path.Combine(AppContext.BaseDirectory, "TestData", "PyArrow", name);

    if (!File.Exists(filePath))
      throw new FileNotFoundException($"File not found: {filePath}");

    var bytes = File.ReadAllBytes(filePath);

    return bytes;
  }

  [Fact]
  public void alltypes_plain()
  {
    var bytes = GetFile("alltypes_plain.parquet");
    var parquet = new ParquetSerializer().Read(bytes);
  }

}