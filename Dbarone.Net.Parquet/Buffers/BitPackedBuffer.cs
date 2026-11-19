namespace Dbarone.Net.Parquet.Buffers;

using Dbarone.Net.Parquet.Encoding;

/// <summary>
/// Streams in .NET are generally byte-aligned.
/// The BitPackedBuffer class allows for reading
/// and writing of arbitrary numbers of bits in a stream.
/// </summary>
public class BitPackedBuffer : IBitPackedBuffer, IDisposable
{
  private readonly Stream _stream;
  private byte _bitBuffer;       // Holds bits read from the stream
  private int _bitsInBuffer;    // Number of bits currently in the buffer
  private BitOrder _bitOrder;   // The order of bits packed/read within a byte. MSB is usual order.

  #region #ctor

  public BitPackedBuffer(IBuffer buffer, BitOrder bitOrder = BitOrder.MSB) : this(buffer.Stream, bitOrder) { }

  public BitPackedBuffer(Stream stream, BitOrder bitOrder = BitOrder.MSB)
  {
    _stream = stream ?? throw new ArgumentNullException(nameof(stream));
    if (!stream.CanRead)
      throw new ArgumentException("Stream must be readable.", nameof(stream));

    this._bitOrder = bitOrder;
  }

  #endregion

  #region Public Methods

  /// <summary>
  /// Reads an unsigned integer value from the stream using
  /// the specified number of bits.
  /// </summary>
  public uint ReadBits(int bitWidth)
  {
    if (bitWidth <= 0 || bitWidth > 32)
      throw new ArgumentOutOfRangeException(nameof(bitWidth), "Bit width must be between 1 and 32.");

    uint result = 0;
    int bitsNeeded = bitWidth;

    while (bitsNeeded > 0)
    {
      // If buffer is empty, read the next byte
      if (_bitsInBuffer == 0)
      {
        int nextByte = _stream.ReadByte();

        if (nextByte == -1)
          throw new EndOfStreamException("Not enough bits in stream.");

        _bitBuffer = (byte)nextByte;
        _bitsInBuffer = 8;

        /*
                // Reverse bit order if LSB:
                if (this._bitOrder == BitOrder.LSB)
                {
                  _bitBuffer = ReverseBits(_bitBuffer);
                }
        */
      }

      // Take as many bits as possible from the buffer
      if (this._bitOrder == BitOrder.MSB)
      {
        // MSB
        int bitsToTake = Math.Min(bitsNeeded, _bitsInBuffer);
        int shift = _bitsInBuffer - bitsToTake;
        int extractedBits = (_bitBuffer >> shift) & ((1 << bitsToTake) - 1);

        result = (result << bitsToTake) | (uint)extractedBits;

        _bitsInBuffer -= bitsToTake;
        _bitBuffer &= (byte)((1 << _bitsInBuffer) - 1); // Mask remaining bits
        bitsNeeded -= bitsToTake;
      }
      else
      {
        // LSB
        int bitsToTake = Math.Min(bitsNeeded, _bitsInBuffer);
        int shift = _bitsInBuffer - bitsToTake;
        int extractedBits = (_bitBuffer) & ((1 << bitsToTake) - 1);

        result = (result) | (uint)extractedBits << (bitWidth - bitsNeeded);

        _bitsInBuffer -= bitsToTake;
        _bitBuffer = (byte)((_bitBuffer >> bitsToTake) & ((1 << _bitsInBuffer) - 1)); // Mask remaining bits
        bitsNeeded -= bitsToTake;
      }
    }
    return result;
  }

  /// <summary>
  /// Clears remaining bits in the currently held byte.
  /// </summary>
  public void ClearBits()
  {
    _bitBuffer = 0;
    _bitsInBuffer = 0;
  }

  public void Dispose()
  {
    _stream?.Dispose();
  }

  #endregion

  #region Private Methods

  private byte ReverseBits(byte b)
  {
    b = (byte)((b * 0x0202020202 & 0x010884422010) % 1023);
    return b;
  }

  #endregion
}