/// This file defines the Parquet Thrift interface (Thrift IDL)
/// https://github.com/apache/parquet-format/blob/master/src/main/thrift/parquet.thrift
namespace Dbarone.Net.Parquet.Thrift;

/// <summary>
/// Types supported by Parquet.  These types are intended to be used in combination
/// with the encodings to control the on disk storage format.
/// For example INT16 is not included as a type since a good encoding of INT32
/// would handle this.
/// </summary>
public enum Type : byte
{
  BOOLEAN = 0,
  INT32 = 1,
  INT64 = 2,
  INT96 = 3,  // deprecated, new Parquet writers should not write data in INT96
  FLOAT = 4,
  DOUBLE = 5,
  BYTE_ARRAY = 6,
  FIXED_LEN_BYTE_ARRAY = 7
}