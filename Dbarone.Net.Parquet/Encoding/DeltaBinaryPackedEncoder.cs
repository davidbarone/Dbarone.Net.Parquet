namespace Dbarone.Net.Parquet.Encoding;

using Dbarone.Net.Buffers;

/// <summary>
/// Implementation of delta binary packed encoding used in Parquet
/// 
/// Used to compress INT32 and INT64
/// https://parquet.apache.org/docs/file-format/data-pages/encodings/#DELTAENC
/// Parquet adaption from: https://arxiv.org/pdf/1209.2137v5
/// 
/// Delta encoding consists of a header followed by blocks of delta encoded
/// values, binary packed. Each block is made up of mini blocks, each of
/// them packed with its own bit width.
/// 
/// Header is defined as:
/// <block size in values> <number of miniblocks in a block> <total value count> <first value>
/// where:
/// - the block size is a multiple of 128; it is stored as a ULEB128 int
/// - the miniblock count per block is a divisor of the block size such that their quotient, the number of values in a miniblock, is a multiple of 32; it is stored as a ULEB128 int
/// - the total value count is stored as a ULEB128 int
/// - the first value is stored as a zigzag ULEB128 int
/// 
/// Each block contains:
/// <min delta> <list of bitwidths of miniblocks> <miniblocks>
/// where:
/// the min delta is a zigzag ULEB128 int (we compute a minimum as we need positive integers for bit packing)
/// the bitwidth of each miniblock is stored as a byte
/// each miniblock is a list of bit-packed ints according to the bit width stored at the beginning of the block
/// </summary>
public class DeltaBinaryPackedEncoder
{
  /// <summary>
  /// Decodes to an sequence of long integers.
  /// </summary>
  /// <param name="buffer"></param>
  /// <returns></returns>
  public IEnumerable<long> Decode(IBuffer buffer)
  {
    // Block size (ULEB128)
    var blockSize = buffer.ReadULEB128().Value;
    // Number of mini blocks (ULEB128)
    var miniblockCount = buffer.ReadULEB128().Value;
    // Total values (ULEB128)
    var totalValues = buffer.ReadULEB128().Value;
    // First value (zigzag ULEB128)
    var firstValue = buffer.ReadZigZag().Decoded;
    // valuesInMiniBlock (calculated: must be multiple of 32)
    var valuesInMiniBlock = blockSize / miniblockCount;

    ulong processed = 0;
    var prevValue = firstValue;

    if (totalValues > 0)
    {
      // yield first value
      processed++;
      yield return prevValue;
    }

    if (processed < totalValues)
    {
      var blockCount = totalValues / blockSize + 1;
      for (ulong i = 0; i < blockCount; i++)
      {
        // calculate how many miniblocks in this block:
        var miniBlocksInBlock = (totalValues - (i * blockSize)) / valuesInMiniBlock + 1;
        if (miniBlocksInBlock <= 0 || miniBlocksInBlock > miniblockCount)
        {
          throw new Exception("Invalid miniBlocksInBlock!");
        }

        // process each block
        // Min Delta (zigzag ULEB128)
        var minDelta = buffer.ReadZigZag().Decoded;

        // Read in the bit-width (byte) for EACH mini block in block
        List<byte> bitWidths = new List<byte>();
        for (ulong j = 0; j < miniBlocksInBlock; j++)
        {
          bitWidths.Add(buffer.ReadBytes(1)[0]);
        }

        // read each miniblock
        // data from this point is bit-packed
        BitPackedBuffer bpb = new BitPackedBuffer(buffer);

        for (int j = 0; j < (int)miniBlocksInBlock && processed < totalValues; j++)
        {
          var bitWidth = bitWidths[j];
          for (int k = 0; k < (int)valuesInMiniBlock && processed < totalValues; k++)
          {
            if (bitWidth == 0)
            {
              // no need to read data for bit width = 0
              prevValue = prevValue + (0 + minDelta);
              processed++;
              yield return prevValue;
            }
            else
            {
              // read next bit-packed value
              var value = bpb.Read(bitWidth);
              // calculate actual value
              prevValue = prevValue + (value + minDelta);
              processed++;
              yield return prevValue;
            }
          }
        }
      }
    }
  }
}