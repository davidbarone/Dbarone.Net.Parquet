namespace Dbarone.Net.Parquet.Tests.PyArrow;

using System.Reflection;
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

    List<Dictionary<string, object?>> expected = [
      new() { ["int32"]=1, ["int64"]=10L, ["float"]=1.5f, ["double"]=1.1, ["bool"]=true,  ["string"]="a" },
      new() { ["int32"]=2, ["int64"]=20L, ["float"]=2.1f, ["double"]=2.2, ["bool"]=false, ["string"]="bb" },
      new() { ["int32"]=3, ["int64"]=30L, ["float"]=3.14f,["double"]=3.3, ["bool"]=false, ["string"]="ccc" },
      new() { ["int32"]=4, ["int64"]=40L, ["float"]=2.71f,["double"]=4.4, ["bool"]=true,  ["string"]="dddd" }
    ];

    Assert.Equal(expected, parquet.Data.ToDictionaryEnumerable(), new DictionaryComparer());
  }

  [Fact]
  public void alltypes_dictionary()
  {
    var bytes = GetFile("alltypes_dictionary.parquet");
    var parquet = new ParquetSerializer().Read(bytes);

    List<Dictionary<string, object?>> expected = [
      new() { ["dict_col"]="a" },
      new() { ["dict_col"]="b" },
      new() { ["dict_col"]="a" },
      new() { ["dict_col"]="c" },
      new() { ["dict_col"]="b" }
    ];

    Assert.Equal(expected, parquet.Data.ToDictionaryEnumerable(), new DictionaryComparer());
  }
}