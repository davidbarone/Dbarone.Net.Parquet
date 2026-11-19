namespace Dbarone.Net.Parquet.Thrift;

/// <summary>
/// Thrift compact protocol field type enum. Defines the field
/// types allowed in Thrift Compact Protocol encoding.
/// </summary>
public enum FieldType
{
  /// <summary>
  /// End of struct.
  /// </summary>
  CT_STOP = 0x00,

  /// <summary>
  /// Boolean true in header.
  /// </summary>
  CT_BOOLEAN_TRUE = 0x01,

  /// <summary>
  /// Boolean false in header.
  /// </summary>
  CT_BOOLEAN_FALSE = 0x02,

  /// <summary>
  /// 8-bit signed integer.
  /// </summary>
  CT_BYTE = 0x03,

  /// <summary>
  /// 16-bit signed integer (zig-zag + varint).
  /// </summary>
  CT_I16 = 0x04,

  /// <summary>
  /// 32-bit signed integer (zig-zag + varint).
  /// </summary>
  CT_I32 = 0x05,

  /// <summary>
  /// 64-bit signed integer (zig-zag + varint).
  /// </summary>
  CT_I64 = 0x06,

  /// <summary>
  /// 64-bit IEEE float.
  /// </summary>
  CT_DOUBLE = 0x07,

  /// <summary>
  /// Length‑prefixed binary/string.
  /// </summary>
  CT_BINARY = 0x08,

  /// <summary>
  /// List container.
  /// </summary>
  CT_LIST = 0x09,

  /// <summary>
  /// Set container.
  /// </summary>
  CT_SET = 0x0A,

  /// <summary>
  /// Map container.
  /// </summary>
  CT_MAP = 0x0B,

  /// <summary>
  /// Nested struct.
  /// </summary>
  CT_STRUCT = 0x0C
}