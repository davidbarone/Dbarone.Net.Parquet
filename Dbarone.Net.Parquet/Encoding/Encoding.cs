using Dbarone.Net.Buffers;

namespace Dbarone.Net.Parquet.Encoding;

public class Encoding : IEncoding
{
  protected IBuffer Buffer { get; set; } = default!;

  public Encoding(IBuffer buffer)
  {
    this.Buffer = buffer;
  }

  public virtual bool[] ReadBool(int numValues)
  {
    throw new NotSupportedException();
  }

  public virtual byte[][] ReadByteArray(int numValues)
  {
    throw new NotSupportedException();
  }

  public virtual double[] ReadDouble(int numValues)
  {
    throw new NotSupportedException();
  }

  public virtual byte[][] ReadFixedLengthByteArray(int numValues, int length)
  {
    throw new NotSupportedException();
  }

  public virtual float[] ReadFloat(int numValues)
  {
    throw new NotSupportedException();
  }

  public virtual int[] ReadInt32(int numValues)
  {
    throw new NotSupportedException();
  }

  public virtual long[] ReadInt64(int numValues)
  {
    throw new NotSupportedException();
  }

  public object[] Read(Thrift.Type type, int numValues)
  {
    if (type == Thrift.Type.INT32)
    {
      return this.ReadInt32(numValues).Cast<object>().ToArray();
    }
    else if (type == Thrift.Type.INT64)
    {
      return this.ReadInt64(numValues).Cast<object>().ToArray(); ;
    }
    else if (type == Thrift.Type.BYTE_ARRAY)
    {
      return this.ReadByteArray(numValues).Cast<object>().ToArray(); ;
    }
    else
    {
      throw new NotSupportedException();
    }
  }
}