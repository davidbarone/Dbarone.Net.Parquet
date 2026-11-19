namespace Dbarone.Net.Parquet.Encoding;

/// <summary>
/// Represents a variable-length unsigned integer.
/// This encoding is used in Sqlite3.
/// This encoding uses big-endian byte order, with
/// a maximum length of 9 bytes for 64-bit integers.
/// This differs from ULEB128 which uses little-endian
/// byte order.
/// Algorithm taken from sqlite3 source:
/// https://github.com/sqlite/sqlite/blob/master/src/util.c
/// 
/// Per sqlite source:
/// 
/// The variable - length integer encoding is as follows:
/// 
/// KEY:
/// A = 0xxxxxxx    7 bits of data and one flag bit
/// B = 1xxxxxxx    7 bits of data and one flag bit
/// C = xxxxxxxx    8 bits of data
/// 
///  7 bits - A
/// 14 bits - BA
/// 21 bits - BBA
/// 28 bits - BBBA
/// 35 bits - BBBBA
/// 42 bits - BBBBBA
/// 49 bits - BBBBBBA
/// 56 bits - BBBBBBBA
/// 64 bits - BBBBBBBBC
/// 
/// Write a 64-bit variable-length integer to memory starting at p[0].
/// The length of data write will be between 1 and 9 bytes.  The number
/// of bytes written is returned.
/// 
/// A variable-length integer consists of the lower 7 bits of each byte
/// for all bytes that have the 8th bit set and one byte with the 8th
/// bit clear.  Except, if we get to the 9th byte, it stores the full
/// 8 bits and is the last byte.
/// </summary>
public struct VarInt
{
    /// <summary>
    /// The integer value of the VarInt. 
    /// </summary>
    public ulong Value { get; set; } = 0;

    /// <summary>
    /// The byte[] array representation of the VarInt value.
    /// </summary>
    public byte[] Bytes { get; set; } = new byte[0];

    /// <summary>
    /// The length in bytes that the VarInt uses to store the integer value.
    /// </summary>
    public int Size { get; set; } = 0;

    #region Ctors

    public VarInt(ulong value)
    {
        Value = value;
        Bytes = this.ULongToByteArray(value);
        Size = Bytes.Length;
    }

    public VarInt(byte[] bytes)
    {
        Bytes = bytes;
        int index = 0;
        ulong value = 0;
        byte b;
        do
        {
            if (index == 8)
            {
                // special case for 9-byte - no continuation bit, and exit
                value = (value << 8) | (ulong)((b = bytes[index]));
                index++;
                break;
            }
            else
            {
                value = (value << 7) | (ulong)((b = bytes[index]) & (ulong)0x7F);
                index++;
            }
        } while ((b & 0x80) != 0);

        Size = index;
        Value = value;
        Bytes = new byte[Size];
        Array.Copy(bytes, 0, Bytes, 0, Size);
    }

    #endregion

    #region Implicit Ctor

    // Int8 / sbyte
    public static implicit operator sbyte(VarInt value)
    {
        return (sbyte)value.Value;
    }

    // Int8 / sbyte
    public static implicit operator VarInt(sbyte value)
    {
        return new VarInt((ulong)value);
    }

    // UInt8 / byte
    public static implicit operator byte(VarInt value)
    {
        return (byte)value.Value;
    }

    // UInt8 / byte
    public static implicit operator VarInt(byte value)
    {
        return new VarInt(value);
    }

    // Int16
    public static implicit operator Int16(VarInt value)
    {
        return (Int16)value.Value;
    }

    // Int16
    public static implicit operator VarInt(Int16 value)
    {
        return new VarInt((ulong)value);
    }

    // UInt16
    public static implicit operator UInt16(VarInt value)
    {
        return (UInt16)value.Value;
    }

    // UInt16
    public static implicit operator VarInt(UInt16 value)
    {
        return new VarInt(value);
    }

    // Int32
    public static implicit operator Int32(VarInt value)
    {
        return (Int32)value.Value;
    }

    // Int32
    public static implicit operator VarInt(Int32 value)
    {
        return new VarInt((ulong)value);
    }

    // Int32
    public static implicit operator UInt32(VarInt value)
    {
        return (UInt32)value.Value;
    }

    // Int32
    public static implicit operator VarInt(UInt32 value)
    {
        return new VarInt(value);
    }

    // Int64
    public static implicit operator Int64(VarInt value)
    {
        return (Int64)value.Value;
    }

    // Int64
    public static implicit operator VarInt(Int64 value)
    {
        return new VarInt((ulong)value);
    }

    // UInt64
    public static implicit operator UInt64(VarInt value)
    {
        return (UInt64)value.Value;
    }

    // UInt64
    public static implicit operator VarInt(UInt64 value)
    {
        return new VarInt(value);
    }

    #endregion

    #region Operator Overloading

    public static VarInt operator +(VarInt left, VarInt right)
    {
        return new VarInt(left + right);
    }

    public static VarInt operator -(VarInt left, VarInt right)
    {
        return new VarInt(left - right);
    }

    public static VarInt operator *(VarInt left, VarInt right)
    {
        return new VarInt(left * right);
    }

    public static VarInt operator /(VarInt left, VarInt right)
    {
        return new VarInt(left / right);
    }

    #endregion

    #region Private members

    private byte[] ULongToByteArray(ulong value)
    {
        byte[] bytes = new byte[9];

        // Values < 128 encode in 1 byte
        if (value <= 0x7F)
        {
            bytes[0] = (byte)value;
            Array.Resize(ref bytes, 1);
            return bytes;
        }
        else
        {
            // Special 9‑byte rule
            if ((value & (((ulong)0xff000000) << 32)) != 0)
            {
                bytes[8] = (byte)value;
                value >>= 8;

                for (int i = 7; i >= 0; i--)
                {
                    bytes[i] = (byte)((value & 0x7f) | 0x80);
                    value >>= 7;
                }

                return bytes;
            }
            else
            {
                // Normal varint path
                int n = 0;

                do
                {
                    bytes[n++] = (byte)((value & 0x7f) | 0x80);
                    value >>= 7;
                }
                while (value != 0);

                bytes[0] &= 0x7f; // Clear continuation bit on first byte
                Array.Resize(ref bytes, n);   // resize array

                // Reverse into output
                Array.Reverse(bytes);

                return bytes;
            }
        }
    }

    #endregion
}