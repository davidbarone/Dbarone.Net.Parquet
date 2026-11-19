namespace Dbarone.Net.Parquet.Buffers;

/// <summary>
/// Defines the physical data type of the data
/// read/written from/to a buffer.
/// </summary>
public enum PhysicalDataType
{
  BITS,
  BOOL,
  INT32,
  UINT32,
  INT64,
  UINT64,
  FLOAT,
  DOUBLE,
  BYTE_ARRAY,
  ULEB128,
  ZIGZAG
}