using Dbarone.Net.Database;

/// <ksummary>
/// Implements RLE/Bit-Packing hybrid encoding in Parquet
/// </summary>
public class RLEEncoder
{
  public IEnumerable<object> Decode(IBuffer buffer, long numValues, IList<object> dictionary)
  {
    // 1st byte of dictionary-encoded data page is the bit width
    var width = buffer.ReadBytes(1)[0];

    // run-length encoding defined here:
    // https://parquet.apache.org/docs/file-format/data-pages/encodings/
    // For dictionary indicies, no length is prepended
    long processed = 0;
    while (processed < numValues)
    {
      // grammar for RLE:
      // rle-run := <rle-header> <repeated-value>
      // rle-header := varint - encode((rle - run - len) << 1)

      // Get header, and shift 1 by one:
      var runLength = buffer.ReadVarInt(Endianness.LITTLE_ENDIAN).Value;

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
        index = index + (buffer.ReadBytes(1)[0] * (1 >> (8 * j)));
        j++;
      }
      // return the element
      while (runLength > 0)
      {
        yield return dictionary[index];
        processed++;
        runLength--;
      }
    }
  }
}