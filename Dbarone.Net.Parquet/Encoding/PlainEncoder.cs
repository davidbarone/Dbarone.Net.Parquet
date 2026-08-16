using System.Text.Unicode;
using Dbarone.Net.Database;

/// <ksummary>
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
public class PlainEncoder
{
  public IEnumerable<object> Decode(IBuffer buffer, long numValues, Dbarone.Net.Database.Parquet.Type type)
  {
    while (numValues > 0)
    {
      numValues = numValues - 1;
      switch (type)
      {
        case Dbarone.Net.Database.Parquet.Type.INT32:
          // INT32 always stored in little-endian
          var bytesInt32 = buffer.ReadBytes(4);
          if (!BitConverter.IsLittleEndian)
          {
            // reverse bytes on big-endian systems (most x86 systems are little-endian)
            Array.Reverse(bytesInt32);
          }
          yield return BitConverter.ToInt32(bytesInt32, 0);
          break;
        case Dbarone.Net.Database.Parquet.Type.INT64:
          // INT64 always stored in little-endian
          var bytesInt64 = buffer.ReadBytes(8);
          if (!BitConverter.IsLittleEndian)
          {
            // reverse bytes on big-endian systems (most x86 systems are little-endian)
            Array.Reverse(bytesInt64);
          }
          yield return BitConverter.ToInt64(bytesInt64, 0);
          break;
        case Dbarone.Net.Database.Parquet.Type.BYTE_ARRAY:
          // read length (4 bytes little-endian)
          var bytes = buffer.ReadBytes(4);
          if (!BitConverter.IsLittleEndian)
          {
            Array.Reverse(bytes);
          }
          var length = BitConverter.ToInt32(bytes, 0);
          var strBytes = buffer.ReadBytes(length);
          yield return System.Text.Encoding.UTF8.GetString(strBytes);
          break;
        default:
          throw new Exception($"Type: {type} not currently supported for PLAIN encoding");
      }
    }
  }
}
