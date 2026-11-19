namespace Dbarone.Net.Parquet.Buffers;

using Dbarone.Net.Parquet.Encoding;

/// <summary>
/// Describes operations that can be performed on a buffer.
/// </summary>
public interface IBuffer : IDisposable
{
    /// <summary>
    /// Clears bytes in the buffer
    /// </summary>
    /// <param name="index"></param>
    /// <param name="length"></param>
    public void Clear(int index, int length);

    /// <summary>
    /// Fills the buffer with a byte.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="length"></param>
    /// <param name="value"></param>
    public void Fill(int index, int length, byte value);

    /// <summary>
    /// Returns a byte array representation of the buffer.
    /// </summary>
    /// <returns></returns>
    public byte[] ToArray();

    /// <summary>
    /// Returns a slice of the byte array.
    /// </summary>
    /// <param name="index">The start of the byte array to return.</param>
    /// <param name="length">The length of the byte array to return.</param>
    /// <returns></returns>
    public byte[] Slice(long index, long length);

    /// <summary>
    /// Gets the memory stream used by the buffer.
    /// </summary>
    public MemoryStream Stream { get; }

    /// <summary>
    /// Gets the size of the buffer.
    /// </summary>
    public long Length { get; }

    /// <summary>
    /// Gets / sets the current position in the stream for next read or write.
    /// </summary>
    public long Position { get; set; }

    /// <summary>
    /// Gets a bit-packed buffer using the current buffer.
    /// </summary>
    /// <param name="bitOrder">The order to read bits.</param>
    /// <returns>Returns a bit-packed buffer based on the current buffer.</returns>
    public IBitPackedBuffer GetBitPackedBuffer(BitOrder bitOrder);

    #region Read methods

    public bool ReadBool();
    public Int32 ReadInt32(Endianness endianness = Endianness.DEFAULT);
    public UInt32 ReadUInt32(Endianness endianness = Endianness.DEFAULT);
    public Int64 ReadInt64(Endianness endianness = Endianness.DEFAULT);
    public UInt64 ReadUInt64(Endianness endianness = Endianness.DEFAULT);
    public float ReadFloat(Endianness endianness = Endianness.DEFAULT);
    public Double ReadDouble(Endianness endianness = Endianness.DEFAULT);
    public byte[] ReadBytes(int length);
    public ULEB128 ReadULEB128();
    public ZigZag ReadZigZag();
    public object Read(PhysicalDataType dataType, int length = 0, Endianness endianness = Endianness.DEFAULT);

    #endregion

    #region Write methods

    public int Write(bool value);
    public int Write(Int32 value, Endianness endianness = Endianness.DEFAULT);
    public int Write(UInt32 value, Endianness endianness = Endianness.DEFAULT);
    public int Write(Int64 value, Endianness endianness = Endianness.DEFAULT);
    public int Write(UInt64 value, Endianness endianness = Endianness.DEFAULT);
    public int Write(float value, Endianness endianness = Endianness.DEFAULT);
    public int Write(Double value, Endianness endianness = Endianness.DEFAULT);
    public int Write(byte[] value);
    public int Write(ULEB128 value);
    public int Write(ZigZag value);
    public int Write(object value, Endianness endianness = Endianness.DEFAULT);

    #endregion
}