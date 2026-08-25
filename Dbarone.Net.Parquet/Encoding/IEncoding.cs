
using Dbarone.Net.Buffers.Thrift;
using Dbarone.Net.Parquet.Thrift;

/// <summary>
/// Defines the operations for a Parquet encoding algorithm.
/// 
/// The supported encoding algorithms can be found here:
/// https://parquet.apache.org/docs/file-format/data-pages/encodings/
/// </summary>
public interface IEncoding
{
  public bool[] ReadBool(int numValues);
  public Int32[] ReadInt32(int numValues);
  public Int64[] ReadInt64(int numValues);
  public float[] ReadFloat(int numValues);
  public double[] ReadDouble(int numValues);
  public byte[][] ReadByteArray(int numValues);
  public byte[][] ReadFixedLengthByteArray(int numValues, int length);
  public object[] Read(SchemaElement element, int length);
}