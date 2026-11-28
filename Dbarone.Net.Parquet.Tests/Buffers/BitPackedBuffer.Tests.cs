namespace Dbarone.Net.Parquet.Buffers.Tests;

using Dbarone.Net.Parquet.Buffers;
using Dbarone.Net.Parquet.Encoding;

public class BitPackedBufferTests
{
  [Fact]
  public void TestRead2FromLSB()
  {
    byte[] bytes = [0B10000100, 0B00000001];
    GenericBuffer buf = new GenericBuffer(bytes);
    var bpb = new BitPackedBuffer(buf, BitOrder.LSB);

    // Read 8 lots of 2 bits at a time:
    byte[] results = new byte[8];
    for (int i = 0; i < 8; i++)
    {
      results[i] = (byte)bpb.ReadBits(2);
    }

    // Assert
    Assert.Equal(new byte[] { 0x00, 0x01, 0x00, 0x02, 0x01, 0x00, 0x00, 0x00 }, results);
  }

  [Fact]
  public void TestRead2FromMSB()
  {
    byte[] bytes = [0B10000100, 0B00000001];
    GenericBuffer buf = new GenericBuffer(bytes);
    var bpb = new BitPackedBuffer(buf, BitOrder.MSB);

    // Read 8 lots of 2 bits at a time:
    byte[] results = new byte[8];
    for (int i = 0; i < 8; i++)
    {
      results[i] = (byte)bpb.ReadBits(2);
    }

    // Assert
    Assert.Equal(new byte[] { 0x02, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x01 }, results);
  }

  [Fact]
  public void TestRead3FromLSB()
  {
    // Example taken from: https://parquet.apache.org/docs/file-format/data-pages/encodings/
    byte[] bytes = [0B10001000, 0B11000110, 0B11111010];
    GenericBuffer buf = new GenericBuffer(bytes);
    var bpb = new BitPackedBuffer(buf, BitOrder.LSB);

    // Read 8 lots of 3 bits at a time:
    byte[] results = new byte[8];
    for (int i = 0; i < 8; i++)
    {
      results[i] = (byte)bpb.ReadBits(3);
    }

    // Assert
    Assert.Equal(new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 }, results);
  }

  [Fact]
  public void TestRead3FromMSB()
  {
    byte[] bytes = [0B10001000, 0B11000110, 0B11111010];
    GenericBuffer buf = new GenericBuffer(bytes);
    var bpb = new BitPackedBuffer(buf, BitOrder.MSB);

    // Read 8 lots of 2 bits at a time:
    byte[] results = new byte[8];
    for (int i = 0; i < 8; i++)
    {
      results[i] = (byte)bpb.ReadBits(3);
    }

    // Assert
    Assert.Equal(new byte[] { 0x04, 0x02, 0x01, 0x04, 0x03, 0x03, 0x07, 0x02 }, results);
  }
}