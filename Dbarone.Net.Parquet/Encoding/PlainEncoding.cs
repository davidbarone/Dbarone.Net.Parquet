namespace Dbarone.Net.Parquet.Encoding;

using Dbarone.Net.Buffers;

/// <summary>
/// Implements PLAIN encoding in Parquet.
/// 
/// This is the plain encoding that must be supported for types.  It is
/// intended to be the simplest encoding.  Values are encoded back to back.
/// 
/// The plain encoding is used whenever a more efficient encoding can not be used. It
/// stores the data in the following format:
///  - BOOLEAN: [Bit Packed] (#BITPACKED), LSB first
///  - INT32: 4 bytes little endian
///  - INT64: 8 bytes little endian
///  - INT96: 12 bytes little endian(deprecated)
///  - FLOAT: 4 bytes IEEE little endian
///  - DOUBLE: 8 bytes IEEE little endian
///  - BYTE_ARRAY: length in 4 bytes little endian followed by the bytes contained in the array
///  - FIXED_LEN_BYTE_ARRAY: the bytes contained in the array
/// For native types, this outputs the data as little endian. Floating
///     point types are encoded in IEEE.
/// 
/// For the byte array type, it encodes the length as a 4 byte little
/// endian, followed by the bytes.
/// </summary>
public class PlainEncoding : Encoding
{
  public PlainEncoding(IBuffer buffer) : base(buffer) { }

  public override int[] ReadInt32(int numValues)
  {
    int[] results = new int[numValues];
    for (int i = 0; i < numValues; i++)
    {
      results[i] = Buffer.ReadInt32(Endianness.LITTLE_ENDIAN);
    }
    return results;
  }

  public override long[] ReadInt64(int numValues)
  {
    long[] results = new long[numValues];
    for (int i = 0; i < numValues; i++)
    {
      results[i] = Buffer.ReadInt64(Endianness.LITTLE_ENDIAN);
    }
    return results;
  }

  public override float[] ReadFloat(int numValues)
  {
    float[] results = new float[numValues];
    for (int i = 0; i < numValues; i++)
    {
      results[i] = Buffer.ReadFloat(Endianness.LITTLE_ENDIAN);
    }
    return results;
  }

  public override double[] ReadDouble(int numValues)
  {
    double[] results = new double[numValues];
    for (int i = 0; i < numValues; i++)
    {
      results[i] = Buffer.ReadDouble(Endianness.LITTLE_ENDIAN);
    }
    return results;
  }

  public override byte[][] ReadByteArray(int numValues)
  {
    byte[][] results = new byte[numValues][];
    for (int i = 0; i < numValues; i++)
    {
      // first 4 bytes are length (little-endian)
      var length = Buffer.ReadInt32(Endianness.LITTLE_ENDIAN);
      byte[] bytes = Buffer.ReadBytes(length);
      results[i] = bytes;
    }
    return results;
  }
}
