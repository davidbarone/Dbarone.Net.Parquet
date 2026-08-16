namespace Dbarone.Net.Parquet.Thrift;

/// <summary>
/// Union to specify the order used for the min_value and max_value fields for a
/// column.This union takes the role of an enhanced enum that allows rich
/// elements(which will be needed for a collation-based ordering in the future).
/// 
/// Possible values are:
/// * TypeDefinedOrder -the column uses the order defined by its logical or
/// physical type(if there is no logical type).
/// * IEEE754TotalOrder -the floating point column uses IEEE 754 total order.
/// 
/// If the reader does not support the value of this union, min and max stats
/// for this column should be ignored.
/// </summary>
[ParquetThriftMetaData()]
public sealed class ColumnOrder
{
  /**
   * The sort orders for logical types are:
   *   UTF8 - unsigned byte-wise comparison
   *   INT8 - signed comparison
   *   INT16 - signed comparison
   *   INT32 - signed comparison
   *   INT64 - signed comparison
   *   UINT8 - unsigned comparison
   *   UINT16 - unsigned comparison
   *   UINT32 - unsigned comparison
   *   UINT64 - unsigned comparison
   *   DECIMAL - signed comparison of the represented value
   *   DATE - signed comparison
   *   FLOAT16 - signed comparison of the represented value (*)
   *   TIME_MILLIS - signed comparison
   *   TIME_MICROS - signed comparison
   *   TIMESTAMP_MILLIS - signed comparison
   *   TIMESTAMP_MICROS - signed comparison
   *   INTERVAL - undefined
   *   JSON - unsigned byte-wise comparison
   *   BSON - unsigned byte-wise comparison
   *   ENUM - unsigned byte-wise comparison
   *   LIST - undefined
   *   MAP - undefined
   *   VARIANT - undefined
   *   GEOMETRY - undefined
   *   GEOGRAPHY - undefined
   *
   * In the absence of logical types, the sort order is determined by the physical type:
   *   BOOLEAN - false, true
   *   INT32 - signed comparison
   *   INT64 - signed comparison
   *   INT96 (only used for legacy timestamps) - undefined(+)
   *   FLOAT - signed comparison of the represented value (*)
   *   DOUBLE - signed comparison of the represented value (*)
   *   BYTE_ARRAY - unsigned byte-wise comparison
   *   FIXED_LEN_BYTE_ARRAY - unsigned byte-wise comparison
   *
   * (+) While the INT96 type has been deprecated, at the time of writing it is
   *    still used in many legacy systems. If a Parquet implementation chooses
   *    to write statistics for INT96 columns, it is recommended to order them
   *    according to the legacy rules:
   *    - compare the last 4 bytes (days) as a little-endian 32-bit signed integer
   *    - if equal last 4 bytes, compare the first 8 bytes as a little-endian
   *      64-bit signed integer (nanos)
   *    See https://github.com/apache/parquet-format/issues/502 for more details
   *
   * (*) Because TYPE_ORDER is ambiguous for floating point types due to
   *     underspecified handling of NaN and -0/+0, it is recommended that writers
   *     use IEEE_754_TOTAL_ORDER for these types.
   *
   *     If TYPE_ORDER is used for floating point types, then the following
   *     compatibility rules should be applied when reading statistics:
   *     - If the min is a NaN, it should be ignored.
   *     - If the max is a NaN, it should be ignored.
   *     - If the nan_count field is set, a reader can compute
   *       nan_count + null_count == num_values to deduce whether all non-null
   *       values are NaN.
   *     - If the min is +0, the row group may contain -0 values as well.
   *     - If the max is -0, the row group may contain +0 values as well.
   *     - When looking for NaN values, min and max should be ignored.
   *       If the nan_count field is set, it can be used to check whether
   *       NaNs are present.
   *
   *     When writing page or column chunk statistics for columns with
   *     TYPE_ORDER order, the following rules must be followed:
   *     - The nan_count field must be set for floating point types, even if
   *       it is zero.
   *     - If the nan_count field is set, min and max statistics fields, when
   *       present, must not contain NaN values and must be computed from
   *       non-NaN values only. This signals to readers that the min and max
   *       statistics are reliable for non-NaN values.
   *     - If all non-null values are NaN, min and max statistics must not be
   *       written.
   *     - If the computed max value is zero (whether negative or positive),
   *       `+0.0` should be written into the max statistics field.
   *     - If the computed min value is zero (whether negative or positive),
   *       `-0.0` should be written into the min statistics field.
   *
   *     When writing column indexes for columns with TYPE_ORDER order, the
   *     following rules must be followed:
   *     - NaNs must not be written to min_values or max_values.
   *     - If all non-null values of a page are NaN, a column index must not
   *       be written for this column chunk because min_values and max_values
   *       are required.
   *     - If the computed max value is zero (whether negative or positive),
   *       `+0.0` should be written into the corresponding max_values entry.
   *     - If the computed min value is zero (whether negative or positive),
   *       `-0.0` should be written into the corresponding min_values entry.
   */
  [FieldId(1)]
  public TypeDefinedOrder TYPE_ORDER { get; set; } = default!;

  /*
   * The floating point type is ordered according to the totalOrder predicate,
   * as defined in section 5.10 of IEEE-754 (2008 revision). Only columns of
   * physical type FLOAT or DOUBLE, or logical type FLOAT16 may use this ordering.
   *
   * Intuitively, this orders floats mathematically, but defines -0 to be less
   * than +0, -NaN to be less than anything else, and +NaN to be greater than
   * anything else. It also defines an order between different bit representations
   * of the same value.
   *
   * When writing statistics for columns with IEEE_754_TOTAL_ORDER order, then
   * following rules must be followed:
   * - Writing the nan_count field is mandatory when using this ordering.
   * - Min and max statistics must contain the smallest and largest non-NaN
   *   values respectively, or if all non-null values are NaN, the smallest and
   *   largest NaN values as defined by IEEE 754 total order.
   *
   * When reading statistics for columns with this order, the following rules
   * should be followed:
   * - Readers should consult the nan_count field to determine whether NaNs
   *   are present.
   * - A reader can compute nan_count + null_count == num_values to deduce
   *   whether all non-null values are NaN. In the page index, which does not
   *   have a num_values field, the presence of a NaN value in min_values
   *   or max_values indicates that all non-null values are NaN.
   */
  [FieldId(2)]
  public IEEE754TotalOrder IEEE_754_TOTAL_ORDER { get; set; } = default!;
}

/** Empty struct to signal the order defined by the physical or logical type */
[ParquetThriftMetaData()]
public sealed class TypeDefinedOrder { }

/** Empty struct to signal IEEE 754 total order for floating point types */
[ParquetThriftMetaData()]
public sealed class IEEE754TotalOrder { }