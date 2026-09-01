namespace Dbarone.Net.Parquet.Encoding;

using Dbarone.Net.Buffers;

public enum RLEEncodingDataKind
{
  DATA_PAGE_V1_DEFINITION_LEVEL,
  DATA_PAGE_V1_REPETITION_LEVEL,
  DATA_PAGE_V1_DICTIONARY_INDICES,
  DATA_PAGE_V1_BOOLEAN_VALUES,
  DATA_PAGE_V2_DEFINITION_LEVEL,
  DATA_PAGE_V2_REPETITION_LEVEL,
  DATA_PAGE_V2_DICTIONARY_INDICES,
  DATA_PAGE_V2_BOOLEAN_VALUES,
}

/// <ksummary>
/// Implements RLE/Bit-Packing hybrid encoding in Parquet.
/// 
/// See: https://parquet.apache.org/docs/file-format/data-pages/encodings/#RLE
/// 
/// The grammar is as follows:
/// 
/// rle-bit-packed-hybrid: <length> <encoded-data>
/// length is not always prepended, please check the table below for more detail
/// length := length of the<encoded-data> in bytes stored as 4 bytes little endian(unsigned int32)
/// encoded-data := <run>*
/// run := <bit-packed-run> | <rle-run>
/// bit-packed-run := <bit-packed-header> <bit-packed-values>
/// bit-packed-header := varint-encode(<bit-pack-scaled-run-len> << 1 | 1)
/// // we always bit-pack a multiple of 8 values at a time, so we only store the number of values / 8
/// bit-pack-scaled-run-len := (bit-packed-run-len) / 8
/// bit-packed-run-len := * see 3 below*
/// bit-packed-values := * see 1 below*
/// rle-run := <rle-header> <repeated-value>
/// rle-header := varint-encode((rle-run-len) << 1)
/// rle-run-len := * see 3 below*
/// repeated-value := value that is repeated, using a fixed-width of round - up - to - next - byte(bit - width)
/// 
/// Notes:
/// 1. The bit-packing here is done in a different order than the one in the deprecated
/// bit-packing encoding. The values are packed from the least significant bit of each
/// byte to the most significant bit, though the order of the bits in each value remains
/// in the usual order of most significant to least significant.
/// 2. varint-encode() is ULEB-128 encoding, see https://en.wikipedia.org/wiki/LEB128
/// 3. bit-packed-run-len and rle-run-len must be in the range [1, 231 - 1].
/// 
/// Use table below to decide whether to prepend 4-byte length to encoded-data:
/// 
/// +--------------+------------------------+-----------------+
/// | Page kind    | RLE-encoded data kind  | Prepend length? |
/// +--------------+------------------------+-----------------+
/// | Data page v1 | Definition levels      | Y               |
/// |              | Repetition levels      | Y               |
/// |              | Dictionary indices     | N               |
/// |              | Boolean values         | Y               |
/// +--------------+------------------------+-----------------+
/// | Data page v2 | Definition levels      | N               |
/// |              | Repetition levels      | N               |
/// |              | Dictionary indices     | N               |
/// |              | Boolean values         | Y               |
/// +--------------+------------------------+-----------------+
/// </summary>
public class RLEEncoding : Encoding
{
  private RLEEncodingDataKind DataKind { get; set; }

  /// <summary>
  /// Some variants of RLE=3 encoding read the bit width during
  /// decoding phase (e.g. Data Page V1 Dictionary Indices).
  /// Others (e.g. Definition Levels) have bit width passed in
  /// from caller.
  /// </summary>
  private int? BitWidth { get; set; }
  public RLEEncoding(IBuffer buffer, RLEEncodingDataKind dataKind, int? bitWidth = null) : base(buffer)
  {
    this.DataKind = dataKind;
    this.BitWidth = bitWidth;
  }

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
    int length = -1;
    // Read Length - not all variants of RLE=3 encoding require length
    if (
      this.DataKind == RLEEncodingDataKind.DATA_PAGE_V1_DEFINITION_LEVEL ||
      this.DataKind == RLEEncodingDataKind.DATA_PAGE_V1_REPETITION_LEVEL
    )
    {
      length = Buffer.ReadInt32(Endianness.LITTLE_ENDIAN);
    }

    int[] results = new int[numValues];

    if (this.DataKind == RLEEncodingDataKind.DATA_PAGE_V1_DICTIONARY_INDICES)
    {
      // 1st byte of dictionary-encoded data page is the bit width
      this.BitWidth = Buffer.ReadBytes(1)[0];
    }

    if (this.BitWidth is null)
    {
      throw new Exception("Cannot read RLE values. BitWidth is not set.");
    }

    // Get start pointer for reading - this is required if length is provided
    var startPosition = this.Buffer.Position;

    // run-length encoding defined here:
    // https://parquet.apache.org/docs/file-format/data-pages/encodings/
    // For dictionary indicies, no length is prepended
    long processed = 0;
    while (processed < numValues)
    {
      var currentPosition = this.Buffer.Position;

      if (processed >= numValues)
      {
        // stop when processed = numValues. However, if length
        // also provided, current pointer - start should = length here
        if (length >= 0 && currentPosition - startPosition != length)
        {
          throw new Exception($"Error decoding RLE data. NumValues reached {numValues}, but more bytes still need to be read.");
        }
        break;
      }

      if (length >= 0 && currentPosition - startPosition >= length)
      {
        // If length provided, should not get here, as reading
        // rows should end when processed = numValues
        throw new Exception($"Error decoding RLE data. Length: {length} of bytes to read is reached, but more rows need to be read.");
      }

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
      var byteSizePerValue = (this.BitWidth / 8) + 1;
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