namespace Dbarone.Net.Parquet.Thrift;

using Dbarone.Net.Parquet.Buffers;

/// <summary>
/// Codes / decodes using the Thrift compact binary protocol.
/// 
/// Useful links:
/// - https://github.com/apache/thrift/blob/master/doc/specs/thrift-compact-protocol.md
/// - https://thrift.apache.org/
/// - https://thrift.apache.org/static/files/thrift-20070401.pdf
/// - https://simonkjohnston.life/thrift-specs/protocol-compact.html
/// - https://issues.apache.org/jira/browse/THRIFT-110
/// </summary>
public class ThriftCompactSerializer
{
  /// <summary>
  /// Returns the low 4 bits of a byte.
  /// </summary>
  /// <param name="input">Input byte.</param>
  /// <returns>Returns low 4 bits.</returns>
  private int GetLowNibble(byte input)
  {
    return input & 0xF;
  }

  /// <summary>
  /// Returns the high 4 bits of a byte.
  /// </summary>
  /// <param name="input">Input byte.</param>
  /// <returns>Returns high 4 bits.</returns>
  private int GetHighNibble(byte input)
  {
    return (input & ~0xF) >> 4;
  }

  /// <summary>
  /// Reads the next field from a buffer.
  /// </summary>
  /// <param name="buffer">The input buffer.</param>
  /// <returns>Returns an object representing the field</returns>
  private (FieldType FieldType, int FieldID, object? Value) ReadField(IBuffer buffer, int currentFieldID)
  {
    var (fieldType, fieldIDDelta) = ReadFieldHeader(buffer);
    if (fieldType == FieldType.CT_STOP)
    {
      return (fieldType, currentFieldID, null);
    }
    else
    {
      var newfieldID = currentFieldID + fieldIDDelta;
      var value = ReadValue(buffer, fieldType);
      return (fieldType, newfieldID, value);
    }
  }

  private Dictionary<int, object?> ReadStruct(IBuffer buffer)
  {
    Dictionary<int, object?> dict = new Dictionary<int, object?>();
    var field = ReadField(buffer, 0);
    while (field.FieldType != FieldType.CT_STOP)
    {
      dict.Add(field.FieldID, field.Value);
      field = ReadField(buffer, field.FieldID);
    }
    return dict;
  }

  private object? ReadValue(IBuffer buffer, FieldType type)
  {
    switch (type)
    {
      case FieldType.CT_BOOLEAN_TRUE:
        return true;
      case FieldType.CT_BOOLEAN_FALSE:
        return false;
      case FieldType.CT_I16:
        // Integer encoing uses https://en.wikipedia.org/wiki/LEB128
        var zz = buffer.ReadZigZag();
        return zz.Decoded;
      case FieldType.CT_I32:
        // Integer encoing uses https://en.wikipedia.org/wiki/LEB128
        zz = buffer.ReadZigZag();
        return zz.Decoded;
      case FieldType.CT_I64:
        // Integer encoing uses https://en.wikipedia.org/wiki/LEB128
        zz = buffer.ReadZigZag();
        return zz.Decoded;
      case FieldType.CT_LIST:
        var list = ReadList(buffer);
        return list;
      case FieldType.CT_STRUCT:
        var st = ReadStruct(buffer);
        return st;
      case FieldType.CT_BINARY:
        // for byte[] and string types
        var length = buffer.ReadULEB128();
        var bytes = buffer.ReadBytes(length);
        return bytes;
      case FieldType.CT_BYTE:
        var b = buffer.ReadBytes(1)[0];
        return b;
      case FieldType.CT_STOP:
        return null;
      default:
        throw new Exception("whoops");
    }
  }

  private List<object?> ReadList(IBuffer buffer)
  {
    List<object?> arr = new List<object?>();
    var listHeader = ReadListHeader(buffer);
    for (ulong i = 0; i < listHeader.Size; i++)
    {
      arr.Add(ReadValue(buffer, listHeader.ElementType));
    }
    return arr;
  }

  /// <summary>
  /// Gets a field header.
  /// </summary>
  /// <param name="buffer">The input buffer to read.</param>
  /// <returns>Returns the field type and field ID delta.</returns>
  private (FieldType FieldType, int FieldIDDelta) ReadFieldHeader(IBuffer buffer)
  {
    var b = buffer.ReadBytes(1)[0];
    var low = GetLowNibble(b);
    var high = GetHighNibble(b);
    return (
      FieldType: (FieldType)low,
      FieldIDDelta: high
      );
  }

  /// <summary>
  /// Within a LIST, read the list header first.
  /// - high 4 bits: size (if < 15), otherwise, 0xF, then read varint for size
  /// - low 4 bits: element type
  /// </summary>
  /// <param name="buffer">The input buffer.</param>
  /// <returns>Returns list header</returns>
  private (FieldType ElementType, ulong Size) ReadListHeader(IBuffer buffer)
  {
    var b = buffer.ReadBytes(1)[0];
    var low = GetLowNibble(b);
    var high = GetHighNibble(b);
    ulong size = (ulong)high;
    if (size == 15)
    {
      // read next VarInt to get size
      var vi = buffer.ReadULEB128();
      size = vi.Value;
    }
    return (
      ElementType: (FieldType)low,
      Size: size
    );
  }

  /// <summary>
  /// Deserializes a buffer using Thrift Compact Protocol,
  /// outputting a dictionary.
  /// </summary>
  /// <param name="buffer">The input buffer to read.</param>
  /// <returns>Data is returned in a dictionary structure.</returns>
  public Dictionary<int, object?> DeserializeStruct(IBuffer buffer)
  {
    return ReadStruct(buffer);
  }
}