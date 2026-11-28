namespace Dbarone.Net.Parquet.Buffers.Tests;

using Dbarone.Net.Parquet.Buffers;

public class GenericBufferTests
{
    [Fact]
    public void TestCreateBufferSize()
    {
        var buffer = new byte[] { 10, 20, 30 };
        var pb = new GenericBuffer(buffer);
        Assert.Equal(3, pb.Length);
    }

    [Fact]
    public void TestCreateBufferIndex()
    {
        var buffer = new byte[] { 10, 20, 30 };
        var pb = new GenericBuffer(buffer);
        Assert.Equal(10, pb[0]);
    }


    [Fact]
    public void TestBufferFill()
    {
        var buffer = new byte[] { 10, 20, 30 };
        var pb = new GenericBuffer(buffer);
        pb.Fill(0, 3, (byte)100);
        Assert.Equal(3, pb.Length);
        Assert.Equal(100, pb[0]);
        Assert.Equal(100, pb[1]);
        Assert.Equal(100, pb[2]);
    }

    [Fact]
    public void TestBufferClear()
    {
        var buffer = new byte[] { 10, 20, 30 };
        var pb = new GenericBuffer(buffer);
        pb.Clear(0, 3);
        Assert.Equal(3, pb.Length);
        Assert.Equal(0, pb[0]);
        Assert.Equal(0, pb[1]);
        Assert.Equal(0, pb[2]);
    }

    [Fact]
    public void TestBufferSlice()
    {
        var buffer = new byte[] { 10, 20, 30, 40, 50, 60, 70, 80 };
        var pb = new GenericBuffer(buffer);
        var slice = pb.Slice(2, 3);
        Assert.Equal(8, pb.Length);
        Assert.Equal(30, slice[0]);
        Assert.Equal(40, slice[1]);
        Assert.Equal(50, slice[2]);
    }

    [Fact]
    public void TestBuffer_Fill()
    {
        byte testValue = 0b10101010;
        int pageSize = 8192;
        byte[] buffer = new byte[pageSize];
        var pb = new GenericBuffer(buffer);
        pb.Fill(0, pageSize, testValue);
        Assert.Equal(pageSize, pb.Length);
        Assert.All(pb.ToArray(), (b) => Assert.Equal(testValue, b));
    }

    [Fact]
    public void TestCreateResizableBuffer()
    {
        var buf = new GenericBuffer();
        Assert.Equal(0, buf.Position);
        Assert.Equal(0, buf.Length);
    }

    [Fact]
    public void TestResizableBuffer_Write()
    {
        var buf = new GenericBuffer();
        var length = buf.Write((long)123);
        Assert.Equal(length, buf.Position);
        Assert.Equal(length, buf.Length);
    }

    [Fact]
    public void TestResizableBuffer_SetPosition()
    {
        var buf = new GenericBuffer();
        //buf.Write((long)123);
        buf.Position = 100;
        Assert.Equal(100, buf.Position);
        var length = buf.Write((long)123);
        Assert.Equal(100 + length, buf.Position);
        Assert.Equal(100 + length, buf.Length);
    }

    [Fact]
    public void TestResizableBuffer_WriteRead()
    {
        var buf = new GenericBuffer();
        var length = buf.Write((long)123);
        Assert.Equal(length, buf.Position);
        Assert.Equal(length, buf.Length);

        // reset position + read int
        buf.Position = 0;
        var actual = buf.ReadInt64();
        Assert.Equal(123, actual);
    }

    [Theory]
    [InlineData(PhysicalDataType.BOOL, true)]
    [InlineData(PhysicalDataType.INT64, long.MaxValue)]
    [InlineData(PhysicalDataType.DOUBLE, double.MaxValue)]
    [InlineData(PhysicalDataType.BYTE_ARRAY, new byte[] { 1, 2, 3, 4, 5 })]
    public void WriteTests(PhysicalDataType dataType, object expectedValue)
    {
        var buf = new GenericBuffer();
        var length = buf.Write(expectedValue);
        buf.Position = 0;
        var actualValue = buf.Read(dataType, length);   // length required for byte array. Ignored for other data types.
        Assert.Equal(expectedValue, actualValue);
    }
}