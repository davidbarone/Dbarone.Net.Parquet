namespace Dbarone.Net.Parquet.Tests;

/// <summary>
/// Generates a column for a test pack table.
/// </summary>
public class TestPackColumn
{
  /// <summary>
  /// Library of the value generators.
  /// </summary>
  private Dictionary<string, Func<IEnumerable<object>>> ValueGeneratorMapping => new Dictionary<string, Func<IEnumerable<object>>>
  {
    {"INT_12345", () => Enumerable.Range(1, 5).Select(n=>(object)n) },
    {"INT8_MIN_MAX", () => new object[] { sbyte.MinValue, sbyte.MaxValue } },
    {"UINT8_MIN_MAX", () => new object[] { byte.MinValue, byte.MaxValue } },
    {"INT16_MIN_MAX", () => new object[] { short.MinValue, short.MaxValue } },
    {"UINT16_MIN_MAX", () => new object[] { ushort.MinValue, ushort.MaxValue } },
    {"INT32_MIN_MAX", () => new object[] { int.MinValue, int.MaxValue } },
    {"UINT32_MIN_MAX", () => new object[] { uint.MinValue, uint.MaxValue } },
    {"INT64_MIN_MAX", () => new object[] { long.MinValue, long.MaxValue } },
    {"UINT64_MIN_MAX", () => new object[] { ulong.MinValue, ulong.MaxValue } },
    {"FLOAT_MIN_MAX", () => new object[] { float.MinValue, float.MaxValue } },
    {"DOUBLE_MIN_MAX", () => new object[] { double.MinValue, double.MaxValue } },
    { "INT_111222233333",  () => new object[] { 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 3 }},
    {"LONG_MAX_REPEAT_1000000", () => Enumerable.Repeat(long.MaxValue,1000000).Select(n => (object)n)},
    {"STR_ABCDEFG", () => new object[] { "A", "B", "C", "D", "E", "F", "G" }},
    {"STR_ABCABCABC", () => new object[] { "A", "B", "C", "A", "B", "C", "A", "B", "C" }}
  };

  private Dictionary<string, Type> TypeMapping => new Dictionary<string, Type>()
  {
    {"BOOLEAN", typeof(bool)},
    {"INT8", typeof(sbyte)},
    {"UINT8", typeof(byte)},
    {"INT16", typeof(short)},
    {"UINT16", typeof(ushort)},
    {"INT32", typeof(int)},
    {"UINT32", typeof(uint)},
    {"INT64", typeof(long)},
    {"UINT64", typeof(ulong)},
    {"FLOAT", typeof(float)},
    {"DOUBLE", typeof(double)},
    {"BYTE_ARRAY", typeof(byte[])},
    {"STRING", typeof(string)},
    {"DECIMAL", typeof(decimal)},
    {"DATE", typeof(DateOnly)},
    {"TIME", typeof(TimeOnly)},
    {"TIMESTAMP", typeof(DateTime)},
    {"UUID", typeof(Guid)}
  };

  public TestPackColumn(string typeName, string valueGeneratorName, string encodingName)
  {
    // check if nullable type name
    bool nullable = false;
    if (typeName.Last() == '?')
    {
      nullable = true;
      typeName = typeName.Substring(0, typeName.Length - 1);
    }

    // get type
    if (!TypeMapping.ContainsKey(typeName))
    {
      throw new Exception($"Invalid type name: {typeName}");
    }
    var type = TypeMapping[typeName];

    // get value generator
    if (!ValueGeneratorMapping.ContainsKey(valueGeneratorName))
    {
      throw new Exception($"Invalid value generator name: {valueGeneratorName}");
    }
    var generator = ValueGeneratorMapping[valueGeneratorName];

    // get encoding
    Parquet.Thrift.Encoding encoding = (Parquet.Thrift.Encoding)Enum.Parse(typeof(Parquet.Thrift.Encoding), encodingName, ignoreCase: false);

    this.DataType = type;
    this.Generator = generator;
    this.Nullable = nullable;
    this.Encoding = encoding;
  }

  public Type DataType { get; set; } = default!;
  public bool? Nullable { get; set; } = null;
  public Func<IEnumerable<object>> Generator { get; set; } = default!;
  public Parquet.Thrift.Encoding Encoding { get; set; } = Thrift.Encoding.PLAIN;
}