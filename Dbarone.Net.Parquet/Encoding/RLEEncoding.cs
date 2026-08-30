namespace Dbarone.Net.Parquet.Encoding;

using Dbarone.Net.Buffers;

/// <ksummary>
/// Implements RLE/Bit-Packing hybrid encoding in Parquet
/// </summary>
public class RLEEncoding : Encoding
{
  public RLEEncoding(IBuffer buffer) : base(buffer) { }

  /// <summary>
  /// Reads ints using RLE encoding.
  /// 
  /// TBD: Note - check that signed Int32 is always the data type returned?
  /// https://arrow.apache.org/rust/src/parquet/encodings/rle.rs.html?utm_source=copilot.com
  /// </summary>
  /// <param name="numValues">The number of values to return.</param>
  /// <returns>An array of signed Int32 values.</returns>
  public override int[] ReadInt32(int numValues)
  {
    int[] results = new int[numValues];

    // 1st byte of dictionary-encoded data page is the bit width
    var width = Buffer.ReadBytes(1)[0];

    // run-length encoding defined here:
    // https://parquet.apache.org/docs/file-format/data-pages/encodings/
    // For dictionary indicies, no length is prepended
    long processed = 0;
    while (processed < numValues)
    {
      // grammar for RLE:
      // rle-run := <rle-header> <repeated-value>
      // rle-header := varint-encode((rle-run-len) << 1)

      // Get header, and shift 1 by one:
      var runLength = Buffer.ReadULEB128().Value;

      bool isBitPackedRun = (runLength & 1) == 1;

      if (isBitPackedRun)
      {
        throw new Exception("Bit-packed-runs not currently supported");
      }

      // alternative is rle-run.
      runLength = runLength >> 1;

      // Get value:
      var byteSizePerValue = (width / 8) + 1;
      int index = 0;
      var j = 0;
      while (j < byteSizePerValue)
      {
        index = index + (Buffer.ReadBytes(1)[0] * (1 >> (8 * j)));
        j++;
      }
      // return the element
      while (runLength > 0)
      {
        results[processed] = index;
        processed++;
        runLength--;
      }
    }
    return results;
  }
}