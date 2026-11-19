namespace Dbarone.Net.Parquet.Document;

/// <summary>
/// Represents data types allowed in Dbarone.Net.Database.
/// </summary>
public enum DocumentType : int
{
    /// <summary>
    /// Missing or unknown value.
    /// </summary>
    Null = 0,

    /// <summary>
    /// Boolean (true/false) data type.
    /// </summary>
    Boolean = 1,

    /// <summary>
    /// Whole numbers that can be positive or negative. Can vary from 1 to 8 bytes.
    /// </summary>
    Integer = 2,

    /// <summary>
    /// Floating point numbers, stored as 8-byte IEEE floating point number.
    /// </summary>
    Real = 3,

    /// <summary>
    /// Datetime data type
    /// </summary>
    DateTime = 4,

    /// <summary>
    /// A variable-length byte-array.
    /// </summary>
    Blob = 6,

    /// <summary>
    /// Text strings using a database encoding (UTF-8, UTF-16 etc)
    /// </summary>
    Text = 7,
}