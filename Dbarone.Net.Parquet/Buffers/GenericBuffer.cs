namespace Dbarone.Net.Parquet.Buffers;

using Dbarone.Net.Parquet.Encoding;

/// <summary>
/// Represents a generic memory buffer.
/// </summary>
public class GenericBuffer : IBuffer
{
    #region Constructors

    /// <summary>
    /// Creates a non-resizeable buffer.
    /// </summary>
    /// <param name="buffer"></param>
    public GenericBuffer(byte[] buffer)
    {
        // MemoryStream with fixed capacity buffer
        this.buffer = buffer;
        this.Stream = new MemoryStream(buffer);
        this.Resizeable = false;
    }

    /// <summary>
    /// Creates an expandable buffer.
    /// </summary>
    public GenericBuffer()
    {
        this.buffer = new byte[] { };
        this.Stream = new MemoryStream();
        this.Resizeable = true;
    }

    #endregion

    private byte[] buffer;

    public MemoryStream Stream { get; private set; }

    /// <summary>
    /// Returns true if the current buffer can grow.
    /// </summary>
    public bool Resizeable { get; private set; }

    /// <summary>
    /// Gets / sets the position in the buffer.
    /// </summary>
    public long Position
    {
        get { return this.Stream.Position; }
        set { this.Stream.Position = value; }
    }

    /// <summary>
    /// Returns the length of the underlying buffer in bytes.
    /// </summary>
    public long Length
    {
        get { return this.Stream.Length; }
    }

    /// <summary>
    /// The internal byte array used for read and write operations. For resizeable buffers
    /// the buffer returned is the underlying MemoryStream buffer, and may return more
    /// bytes than actually populated. You will need to use the MemoryStream.Length property
    /// to get the actual size of the buffer.
    /// </summary>
    protected virtual byte[] InternalBuffer
    {
        get
        {
            return this.Resizeable ? this.Stream.GetBuffer() : this.buffer;
        }
    }

    public virtual byte this[int index]
    {
        get => this.InternalBuffer[index];
        set => this.InternalBuffer[index] = value;
    }

    public void Clear(int index, int length)
    {
        System.Array.Clear(InternalBuffer, index, length);
    }

    public void Fill(int index, int length, byte value)
    {
        for (var i = 0; i < length; i++)
        {
            InternalBuffer[index + i] = value;
        }
    }

    public virtual byte[] ToArray()
    {
        // copy existing buffer
        var buffer = new byte[this.Length];
        Buffer.BlockCopy(InternalBuffer, 0, buffer, 0, (int)this.Length);
        return buffer;
    }

    public virtual byte[] Slice(long index, long length)
    {
        // copy existing buffer
        var buffer = new byte[length];
        Buffer.BlockCopy(InternalBuffer, (int)index, buffer, 0, (int)length);
        return buffer;
    }

    public IBitPackedBuffer GetBitPackedBuffer(BitOrder bitOrder)
    {
        return new BitPackedBuffer(this, bitOrder);
    }

    #region Read methods

    public bool ReadBool()
    {
        var index = (int)this.Stream.Position;
        var result = InternalBuffer[index] != 0;
        this.Position += sizeof(Boolean);
        return result;
    }

    public Int32 ReadInt32(Endianness endianness = Endianness.DEFAULT)
    {
        var bytes = ReadBytes(4);
        ReverseByteArrayForEndianness(bytes, endianness);
        return BitConverter.ToInt32(bytes, 0);
    }

    public UInt32 ReadUInt32(Endianness endianness = Endianness.DEFAULT)
    {
        var bytes = ReadBytes(4);
        ReverseByteArrayForEndianness(bytes, endianness);
        return BitConverter.ToUInt32(bytes, 0);
    }

    public Int64 ReadInt64(Endianness endianness = Endianness.DEFAULT)
    {
        var bytes = ReadBytes(8);
        ReverseByteArrayForEndianness(bytes, endianness);
        return BitConverter.ToInt64(bytes, 0);
    }

    public UInt64 ReadUInt64(Endianness endianness = Endianness.DEFAULT)
    {
        var bytes = ReadBytes(8);
        ReverseByteArrayForEndianness(bytes, endianness);
        return BitConverter.ToUInt64(bytes, 0);
    }

    public float ReadFloat(Endianness endianness = Endianness.DEFAULT)
    {
        var bytes = ReadBytes(4);
        ReverseByteArrayForEndianness(bytes, endianness);
        return BitConverter.ToSingle(bytes, 0);
    }

    public Double ReadDouble(Endianness endianness = Endianness.DEFAULT)
    {
        var bytes = ReadBytes(8);
        ReverseByteArrayForEndianness(bytes, endianness);
        return BitConverter.ToDouble(bytes, 0);
    }

    public byte[] ReadBytes(int length)
    {
        var index = (int)this.Stream.Position;
        var bytes = new byte[length];
        Buffer.BlockCopy(InternalBuffer, index, bytes, 0, length);
        this.Position += length;
        return bytes;
    }

    public ULEB128 ReadULEB128()
    {
        // create copy of buffer starting at varint to read the ULEB128 value
        var slice = this.Slice(this.Position, this.Length - this.Position);
        var uleb = new ULEB128(slice);
        // set the original buffer position to end of read varint
        this.Position += uleb.Size;
        return uleb;
    }

    public ZigZag ReadZigZag()
    {
        var uleb128 = ReadULEB128();
        return new ZigZag(uleb128);
    }

    public object Read(PhysicalDataType dataType, int length = 0, Endianness endianness = Endianness.DEFAULT)
    {
        switch (dataType)
        {
            case PhysicalDataType.BOOL:
                return ReadBool();
            case PhysicalDataType.INT32:
                return ReadInt32(endianness);
            case PhysicalDataType.UINT32:
                return ReadUInt32(endianness);
            case PhysicalDataType.INT64:
                return ReadInt64(endianness);
            case PhysicalDataType.UINT64:
                return ReadUInt64(endianness);
            case PhysicalDataType.FLOAT:
                return ReadFloat(endianness);
            case PhysicalDataType.DOUBLE:
                return ReadDouble(endianness);
            case PhysicalDataType.ULEB128:
                return ReadULEB128();
            case PhysicalDataType.ZIGZAG:
                return ReadZigZag();
            case PhysicalDataType.BYTE_ARRAY:
                return ReadBytes(length);
        }
        throw new Exception($"Invalid data type.");
    }

    #endregion

    #region Write methods

    public int Write(bool value)
    {
        var bytes = BitConverter.GetBytes(value);
        this.Stream.Write(bytes, 0, bytes.Length);
        return bytes.Length;
    }

    public int Write(Int32 value, Endianness endianness = Endianness.DEFAULT)
    {
        var bytes = BitConverter.GetBytes(value);
        ReverseByteArrayForEndianness(bytes, endianness);
        this.Stream.Write(bytes, 0, bytes.Length);
        return bytes.Length;
    }

    public int Write(UInt32 value, Endianness endianness = Endianness.DEFAULT)
    {
        var bytes = BitConverter.GetBytes(value);
        ReverseByteArrayForEndianness(bytes, endianness);
        this.Stream.Write(bytes, 0, bytes.Length);
        return bytes.Length;
    }

    public int Write(Int64 value, Endianness endianness = Endianness.DEFAULT)
    {
        var bytes = BitConverter.GetBytes(value);
        ReverseByteArrayForEndianness(bytes, endianness);
        this.Stream.Write(bytes, 0, bytes.Length);
        return bytes.Length;
    }

    public int Write(UInt64 value, Endianness endianness = Endianness.DEFAULT)
    {
        var bytes = BitConverter.GetBytes(value);
        ReverseByteArrayForEndianness(bytes, endianness);
        this.Stream.Write(bytes, 0, bytes.Length);
        return bytes.Length;
    }

    public int Write(float value, Endianness endianness = Endianness.DEFAULT)
    {
        var bytes = BitConverter.GetBytes(value);
        ReverseByteArrayForEndianness(bytes, endianness);
        this.Stream.Write(bytes, 0, bytes.Length);
        return bytes.Length;
    }

    public int Write(Double value, Endianness endianness = Endianness.DEFAULT)
    {
        var bytes = BitConverter.GetBytes(value);
        ReverseByteArrayForEndianness(bytes, endianness);
        this.Stream.Write(bytes, 0, bytes.Length);
        return bytes.Length;
    }

    public int Write(byte[] value)
    {
        //var index = (int)this.Stream.Position;
        //Buffer.BlockCopy(value, 0, this.InternalBuffer, index, value.Length);
        this.Stream.Write(value, 0, value.Length);
        return value.Length;
    }

    public int Write(ULEB128 value)
    {
        throw new NotImplementedException();
    }

    public int Write(ZigZag value)
    {
        throw new NotImplementedException();
    }

    public int Write(object value, Endianness endianness = Endianness.DEFAULT)
    {
        var type = value.GetType();
        if (type.IsEnum)
        {
            type = Enum.GetUnderlyingType(type);
        }

        if (type == typeof(bool))
        {
            return Write((bool)value);
        }
        else if (type == typeof(Int32))
        {
            return Write((Int32)value, endianness);
        }
        else if (type == typeof(UInt32))
        {
            return Write((UInt32)value, endianness);
        }
        else if (type == typeof(Int64))
        {
            return Write((Int64)value, endianness);
        }
        else if (type == typeof(UInt64))
        {
            return Write((UInt64)value, endianness);
        }
        else if (type == typeof(float))
        {
            return Write((float)value, endianness);
        }
        else if (type == typeof(double))
        {
            return Write((double)value, endianness);
        }
        else if (type == typeof(byte[]))
        {
            return Write((byte[])value);
        }
        throw new Exception("Shouldn't get here!");
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Checks the endianness required against the current
    /// computer architecture, and reverses the bytes if
    /// necessary.
    /// </summary>
    /// <param name="bytes">The byte array.</param>
    /// <param name="endianness">The assumes endianess required.</param>
    private void ReverseByteArrayForEndianness(byte[] bytes, Endianness endianness = Endianness.LITTLE_ENDIAN)
    {
        if (
            BitConverter.IsLittleEndian && endianness == Endianness.BIG_ENDIAN ||
            !BitConverter.IsLittleEndian && endianness == Endianness.LITTLE_ENDIAN
        )
        {
            Array.Reverse(bytes);
        }
    }

    #endregion Private Methods

    public void Dispose()
    {
        this.Stream?.Dispose();
    }
}