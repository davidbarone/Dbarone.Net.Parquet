namespace Dbarone.Net.Parquet.Encoding;

/// <summary>
/// Enum to define the endianness (order of bytes within a word data type).
/// </summary>
public enum Endianness
{
  /// <summary>
  /// Little-endian encoding.
  /// </summary>
  LITTLE_ENDIAN,

  /// <summary>
  /// Big-endian encoding.
  /// </summary>
  BIG_ENDIAN,

  /// <summary>
  /// Default system encoding.
  /// </summary>
  DEFAULT
}