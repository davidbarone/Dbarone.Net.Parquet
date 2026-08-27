namespace Dbarone.Net.Parquet.Encoding;

using Dbarone.Net.Buffers;
using Dbarone.Net.Parquet.Thrift;
using System.Linq;

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

  public object[] Readxxx(Thrift.Type type, int numValues)
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

  /// <summary>
  /// Reads data for a schema element.
  /// 
  /// Not all types require logical types (for example if the physical
  /// type fully describes the Parquet type, e.g. DOUBLE).
  /// 
  /// Refer: https://parquet.apache.org/docs/file-format/types/logicaltypes/
  /// </summary>
  /// <param name="logicalType">Thrift logical type</param>
  /// <param name="length">Number of values to read.</param>
  /// <returns>Returns an array of objects.</returns>
  public object[] Read(SchemaElement element, int numValues)
  {
    var logicalType = element.LogicalType;
    var physicalType = element.Type;

    if (physicalType == Type.FLOAT)
    {
      // no need for logical type for FLOAT
      var values = ReadFloat(numValues);
      return values.Cast<object>().ToArray();
    }
    else if (physicalType == Type.DOUBLE)
    {
      // no need for logical type for DOUBLE
      var values = ReadDouble(numValues);
      return values.Cast<object>().ToArray();
    }
    else if (physicalType == Type.BOOLEAN)
    {
      var values = ReadBool(numValues);
      return values.Cast<object>().ToArray();
    }
    else if (physicalType == Type.BYTE_ARRAY && logicalType is null)
    {
      // BYTE_ARRAY
      var values = ReadByteArray(numValues);
      return values.Cast<object>().ToArray();
    }
    else if (logicalType.STRING is not null)
    {
      // strings stored in UTF8.
      var values = ReadByteArray(numValues).Select(v => System.Text.Encoding.UTF8.GetString((byte[])v)).ToArray();
      return values;
    }
    else if (logicalType.INTEGER is not null)
    {
      // covers all the signed/unsigned integers
      var lt = logicalType.INTEGER;
      if (lt.BitWidth == 8 && lt.IsSigned)
      {
        // sbyte
        var values = ReadInt32(numValues).Select(v => Convert.ToSByte(v)).Cast<object>().ToArray();
        return values;
      }
      else if (lt.BitWidth == 8 && !lt.IsSigned)
      {
        // byte
        var values = ReadInt32(numValues).Select(v => Convert.ToByte(v)).Cast<object>().ToArray();
        return values;
      }
      else if (lt.BitWidth == 16 && lt.IsSigned)
      {
        // short
        var values = ReadInt32(numValues).Select(v => Convert.ToInt16(v)).Cast<object>().ToArray();
        return values;
      }
      else if (lt.BitWidth == 16 && !lt.IsSigned)
      {
        // ushort
        var values = ReadInt32(numValues).Select(v => Convert.ToUInt16(v)).Cast<object>().ToArray();
        return values;
      }
      else if (lt.BitWidth == 32 && lt.IsSigned)
      {
        // Int32
        var values = ReadInt32(numValues);
        return values.Cast<object>().ToArray();
      }
      else if (lt.BitWidth == 32 && !lt.IsSigned)
      {
        // UInt32
        var values = ReadInt32(numValues);
        return values.Select(v => (UInt32)v).Cast<object>().ToArray();
      }
      else if (lt.BitWidth == 64 && lt.IsSigned)
      {
        // Int64
        var values = ReadInt64(numValues);
        return values.Cast<object>().ToArray();
      }
      else if (lt.BitWidth == 64 && !lt.IsSigned)
      {
        // UInt64
        var values = ReadInt64(numValues);
        return values.Select(v => (UInt64)v).Cast<object>().ToArray();
      }
      else
      {
        throw new Exception($"Unable to read logical type: {logicalType}");
      }
    }
    else
    {
      throw new Exception($"Unable to read logical type: {logicalType}");
    }
  }
}