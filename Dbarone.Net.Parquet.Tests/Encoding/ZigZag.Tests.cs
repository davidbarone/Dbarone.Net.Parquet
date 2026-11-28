namespace Dbarone.Net.Buffers.Encoding.Tests;

using Dbarone.Net.Parquet.Encoding;

public class ZigZagTests
{
    [Theory]
    [InlineData(-1, 18446744073709551615)]
    public void TestLongToUlongCast(long input, ulong output)
    {
        // note that casting a small negative number produces a large unsigned number
        // This is what standard .NET casting does - however, does not produce small
        // number which we need for ZigZag compression.
        Assert.Equal(output, (ulong)input);
    }

    [Theory]
    [InlineData(-10, 19)]
    [InlineData(-9, 17)]
    [InlineData(-8, 15)]
    [InlineData(-7, 13)]
    [InlineData(-6, 11)]
    [InlineData(-5, 9)]
    [InlineData(-4, 7)]
    [InlineData(-3, 5)]
    [InlineData(-2, 3)]
    [InlineData(-1, 1)]
    [InlineData(0, 0)]
    [InlineData(1, 2)]
    [InlineData(2, 4)]
    [InlineData(3, 6)]
    [InlineData(4, 8)]
    [InlineData(5, 10)]
    [InlineData(6, 12)]
    [InlineData(7, 14)]
    [InlineData(8, 16)]
    [InlineData(9, 18)]
    [InlineData(10, 20)]
    [InlineData(long.MaxValue, ulong.MaxValue - 1)]
    [InlineData(long.MinValue, ulong.MaxValue)]
    public void TestZigZagEncoding(long actualInput, ulong expectedOutput)
    {
        // all positive integers should become even integers twice as big
        // all negative integers should become positive odd integers, 1 less than double
        ZigZag zz = new ZigZag(actualInput);
        Assert.Equal(expectedOutput, zz.Encoded);
    }

    [Theory]
    [InlineData(19, -10)]
    [InlineData(17, -9)]
    [InlineData(15, -8)]
    [InlineData(13, -7)]
    [InlineData(11, -6)]
    [InlineData(9, -5)]
    [InlineData(7, -4)]
    [InlineData(5, -3)]
    [InlineData(3, -2)]
    [InlineData(1, -1)]
    [InlineData(0, 0)]
    [InlineData(2, 1)]
    [InlineData(4, 2)]
    [InlineData(6, 3)]
    [InlineData(8, 4)]
    [InlineData(10, 5)]
    [InlineData(12, 6)]
    [InlineData(14, 7)]
    [InlineData(16, 8)]
    [InlineData(18, 9)]
    [InlineData(20, 10)]
    [InlineData(ulong.MaxValue - 1, long.MaxValue)]
    [InlineData(ulong.MaxValue, long.MinValue)]
    public void TestZigZagDecoding(ulong encoded, long expectedDecode)
    {
        ZigZag zz = new ZigZag(new ULEB128(encoded));
        Assert.Equal(expectedDecode, zz.Decoded);
    }
}