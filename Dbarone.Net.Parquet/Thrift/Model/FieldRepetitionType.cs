/// This file defines the Parquet Thrift interface (Thrift IDL)
/// https://github.com/apache/parquet-format/blob/master/src/main/thrift/parquet.thrift
namespace Dbarone.Net.Parquet.Thrift;

/// <summary>
/// Representation of Schemas
/// </summary>
public enum RepetitionType : byte
{
  /** This field is required (can not be null) and each row has exactly 1 value. */
  REQUIRED = 0,

  /** The field is optional (can be null) and each row has 0 or 1 values. */
  OPTIONAL = 1,

  /** The field is repeated and can contain 0 or more values */
  REPEATED = 2,
}
