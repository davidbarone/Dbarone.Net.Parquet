namespace Dbarone.Net.Parquet.Encoding;

/// <summary>
/// Provides compression for signed integers, using
/// VarInt encoding internally.
/// </summary>
/// <remarks>
/// ZigZag compression works by converting signed
/// integers to unsigned integers then using VarInt
/// encoding to compress.
/// - https://lemire.me/blog/2022/11/25/making-all-your-integers-positive-with-zigzag-encoding/
/// - https://curioloop.com/en/posts/variable-length-numeric-compression
/// </remarks>
public class ZigZag
{
    public static int SizeOf(long value)
    {
        ZigZag zz = new ZigZag(value);
        return zz.ULEB128.Size;
    }

    public ZigZag(long value)
    {
        int sizeLong = sizeof(long);
        Decoded = value;
        Encoded = (ulong)((Decoded << 1) ^ (Decoded >> (sizeLong * 8 - 1)));
        ULEB128 = Encoded;
    }

    public ZigZag(ULEB128 uleb128)
    {
        this.ULEB128 = uleb128;
        Encoded = this.ULEB128.Value;
        Decoded = (long)((Encoded >> 1) ^ (ulong)-(long)(Encoded & 1));
    }

    public ulong Encoded { get; set; }

    public long Decoded { get; set; }

    public ULEB128 ULEB128 { get; set; }
}