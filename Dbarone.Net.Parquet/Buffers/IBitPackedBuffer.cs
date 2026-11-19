namespace Dbarone.Net.Parquet.Buffers;

/// <summary>
/// Reads and writes bits from a stream.
/// </summary>
public interface IBitPackedBuffer : IDisposable
{
  /// <summary>
  /// Reads a number of bits (1-32) from a buffer.
  /// </summary>
  /// <param name="bitWidth">The number of bits to read.</param>
  /// <returns>Returns a uint representing the value of the bits read.</returns>
  uint ReadBits(int bitWidth);

  /// <summary>
  /// Clears the bits in the buffer.
  /// </summary>
  void ClearBits();
}