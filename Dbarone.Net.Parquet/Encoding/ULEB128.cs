namespace Dbarone.Net.Parquet.Encoding;

/// <summary>
/// Represents a variable-length unsigned integer using ULEB-128
/// encoding. ULEB128 is used to compress unsigned integers.
/// Internally, ULEB-128 uses little-endian ordering of bytes.
/// To compress signed integers, use another encoding like
/// ZigZag.
/// https://en.wikipedia.org/wiki/LEB128
/// </summary>
public struct ULEB128
{
  /// <summary>
  /// The unsigned value to encode. 
  /// </summary>
  public ulong Value { get; set; } = 0;

  /// <summary>
  /// The byte[] array representation of the unsigned value.
  /// </summary>
  public byte[] Bytes { get; set; } = new byte[0];

  /// <summary>
  /// The length in bytes that the ULEB128 compression results in.
  /// </summary>
  public int Size { get; set; } = 0;

  #region Ctors

  public ULEB128(ulong value)
  {
    Value = value;
    Bytes = this.ULongToByteArray(value);
    Size = Bytes.Length;
  }

  public ULEB128(byte[] bytes)
  {
    Bytes = bytes;
    int index = 0;
    ulong value = 0;
    byte b;
    do
    {
      value = (value) | (ulong)(((b = bytes[index]) & (ulong)0x7F) << (7 * index));
      index++;
    } while ((b & 0x80) != 0);

    Size = index;
    Value = value;
    Bytes = new byte[Size];
    Array.Copy(bytes, 0, Bytes, 0, Size);
  }

  #endregion

  #region Implicit Ctor

  // Int8 / sbyte
  public static implicit operator sbyte(ULEB128 value)
  {
    return (sbyte)value.Value;
  }

  // Int8 / sbyte
  public static implicit operator ULEB128(sbyte value)
  {
    return new ULEB128((ulong)value);
  }

  // UInt8 / byte
  public static implicit operator byte(ULEB128 value)
  {
    return (byte)value.Value;
  }

  // UInt8 / byte
  public static implicit operator ULEB128(byte value)
  {
    return new ULEB128(value);
  }

  // Int16
  public static implicit operator Int16(ULEB128 value)
  {
    return (Int16)value.Value;
  }

  // Int16
  public static implicit operator ULEB128(Int16 value)
  {
    return new ULEB128((ulong)value);
  }

  // UInt16
  public static implicit operator UInt16(ULEB128 value)
  {
    return (UInt16)value.Value;
  }

  // UInt16
  public static implicit operator ULEB128(UInt16 value)
  {
    return new ULEB128(value);
  }

  // Int32
  public static implicit operator Int32(ULEB128 value)
  {
    return (Int32)value.Value;
  }

  // Int32
  public static implicit operator ULEB128(Int32 value)
  {
    return new ULEB128((ulong)value);
  }

  // Int32
  public static implicit operator UInt32(ULEB128 value)
  {
    return (UInt32)value.Value;
  }

  // Int32
  public static implicit operator ULEB128(UInt32 value)
  {
    return new ULEB128(value);
  }

  // Int64
  public static implicit operator Int64(ULEB128 value)
  {
    return (Int64)value.Value;
  }

  // Int64
  public static implicit operator ULEB128(Int64 value)
  {
    return new ULEB128((ulong)value);
  }

  // UInt64
  public static implicit operator UInt64(ULEB128 value)
  {
    return (UInt64)value.Value;
  }

  // UInt64
  public static implicit operator ULEB128(UInt64 value)
  {
    return new ULEB128(value);
  }

  #endregion

  #region Operator Overloading

  public static VarInt operator +(ULEB128 left, ULEB128 right)
  {
    return new VarInt(left + right);
  }

  public static VarInt operator -(ULEB128 left, ULEB128 right)
  {
    return new VarInt(left - right);
  }

  public static VarInt operator *(ULEB128 left, ULEB128 right)
  {
    return new VarInt(left * right);
  }

  public static VarInt operator /(ULEB128 left, ULEB128 right)
  {
    return new VarInt(left / right);
  }

  #endregion

  #region Private members

  private byte[] ULongToByteArray(ulong value)
  {
    var bytes = new List<byte>();

    do
    {
      byte chunk = (byte)(value & 0x7F); // Take 7 bits
      value >>= 7; // Shift right by 7 bits

      if (value != 0)
      {
        chunk |= 0x80; // Set continuation bit
      }

      bytes.Add(chunk);
    }
    while (value != 0);

    return bytes.ToArray();
  }

  #endregion
}