namespace Dbarone.Net.Buffers.Encoding.Tests;

using Dbarone.Net.Parquet.Encoding;

public class ULEB128Tests
{
    [Theory]
    [InlineData(0, 1, new byte[] { 0 })]
    [InlineData(127, 1, new byte[] { 0x7F })]
    [InlineData(128, 2, new byte[] { 0x80, 0x01 })]
    [InlineData(8192, 2, new byte[] { 0x80, 0x40 })]
    [InlineData(16383, 2, new byte[] { 0xFF, 0x7F })]
    [InlineData(16384, 3, new byte[] { 0x80, 0x80, 0x01 })]
    [InlineData(2097151, 3, new byte[] { 0xFF, 0xFF, 0x7F })]
    [InlineData(2097152, 4, new byte[] { 0x80, 0x80, 0x80, 0x01 })]
    [InlineData(134217728, 4, new byte[] { 0x80, 0x80, 0x80, 0x40 })]
    [InlineData(268435455, 4, new byte[] { 0xFF, 0xFF, 0xFF, 0x7F })]
    [InlineData(long.MaxValue, 9, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F })]
    [InlineData(ulong.MaxValue, 10, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01 })]
    public void TestULEB128_WithLongConstructor(ulong value, int expectedLength, byte[] expectedBytes)
    {
        ULEB128 uleb128 = value;
        Assert.Equal(expectedLength, uleb128.Size);
        Assert.Equal(expectedBytes, uleb128.Bytes);
        Assert.Equal(uleb128.Value, value);
        Assert.Equal(uleb128.Size, uleb128.Bytes.Length);
    }

    [Theory]
    [InlineData(new byte[] { 0 }, 0, 1)]
    [InlineData(new byte[] { 0x7F }, 127, 1)]
    [InlineData(new byte[] { 0x80, 0x01 }, 128, 2)]
    [InlineData(new byte[] { 0x80, 0x40 }, 8192, 2)]
    [InlineData(new byte[] { 0xFF, 0x7F }, 16383, 2)]
    [InlineData(new byte[] { 0x80, 0x80, 0x01 }, 16384, 3)]
    [InlineData(new byte[] { 0xFF, 0xFF, 0x7F }, 2097151, 3)]
    [InlineData(new byte[] { 0x80, 0x80, 0x80, 0x01 }, 2097152, 4)]
    [InlineData(new byte[] { 0x80, 0x80, 0x80, 0x40 }, 134217728, 4)]
    [InlineData(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, 268435455, 4)]
    [InlineData(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, long.MaxValue, 9)]
    [InlineData(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01 }, ulong.MaxValue, 10)]
    public void TestULEB128_WithByteArrayConstructor(byte[] bytes, ulong expectedValue, int expectedLength)
    {
        var uleb128 = new ULEB128(bytes);
        Assert.Equal(expectedLength, uleb128.Size);
        Assert.Equal(expectedValue, uleb128.Value);
        Assert.Equal(uleb128.Bytes, bytes);
    }
}