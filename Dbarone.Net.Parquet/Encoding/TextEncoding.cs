namespace Dbarone.Net.Buffers.Parquet.Encoding;

/// <summary>
/// Specifies the text encoding. Currently only UTF8 is supported.
/// </summary>
public enum TextEncoding : Byte
{
    /// <summary>
    /// Unicode UTF-8
    /// </summary>
    UTF8 = 0,

    /// <summary>
    /// Unicode UTF-16
    /// </summary>
    UTF16 = 1,

    /// <summary>
    /// ISO-8859-1
    /// </summary>
    Latin1 = 2
}