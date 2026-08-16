namespace Dbarone.Net.Parquet.Thrift;

/// <summary>
/// LogicalType annotations to replace ConvertedType.
/// 
/// To maintain compatibility, implementations using LogicalType for a
/// SchemaElement must also set the corresponding ConvertedType(if any)
/// from the following table.
/// </summary>
[ParquetThriftMetaData()]
public sealed class LogicalType
{
  [FieldId(1)]
  public StringType STRING { get; set; } = default!; // use ConvertedType UTF8

  [FieldId(2)]
  public MapType MAP { get; set; } = default!; // use ConvertedType MAP

  [FieldId(3)]
  public ListType LIST { get; set; } = default!; // use ConvertedType LIST

  [FieldId(4)]
  public EnumType ENUM { get; set; } = default!; // use ConvertedType ENUM

  [FieldId(5)]
  public DecimalType DECIMAL { get; set; } = default!; // use ConvertedType DECIMAL + SchemaElement.{scale, precision}

  [FieldId(6)]
  public DateType DATE { get; set; } = default!; // use ConvertedType DATE

  // use ConvertedType TIME_MICROS for TIME(isAdjustedToUTC = *, unit = MICROS)
  // use ConvertedType TIME_MILLIS for TIME(isAdjustedToUTC = *, unit = MILLIS)
  [FieldId(7)]
  public TimeType TIME { get; set; } = default!;

  // use ConvertedType TIMESTAMP_MICROS for TIMESTAMP(isAdjustedToUTC = *, unit = MICROS)
  // use ConvertedType TIMESTAMP_MILLIS for TIMESTAMP(isAdjustedToUTC = *, unit = MILLIS)
  [FieldId(8)]
  public TimestampType TIMESTAMP { get; set; } = default!;

  // 9: reserved for INTERVAL

  [FieldId(10)]
  public IntType INTEGER { get; set; } = default!; // use ConvertedType INT_* or UINT_*

  [FieldId(11)]
  public NullType UNKNOWN { get; set; } = default!; // no compatible ConvertedType

  [FieldId(12)]
  public JsonType JSON { get; set; } = default!; // use ConvertedType JSON

  [FieldId(13)]
  public BsonType BSON { get; set; } = default!; // use ConvertedType BSON

  [FieldId(14)]
  public UUIDType UUID { get; set; } = default!; // no compatible ConvertedType

  [FieldId(15)]
  public Float16Type FLOAT16 { get; set; } = default!; // no compatible ConvertedType

  [FieldId(16)]
  public VariantType VARIANT { get; set; } = default!; // no compatible ConvertedType

  [FieldId(17)]
  public GeometryType GEOMETRY { get; set; } = default!; // no compatible ConvertedType

  [FieldId(18)]
  public GeographyType GEOGRAPHY { get; set; } = default!; // no compatible ConvertedType
}

#region Empty structs to use as logical type annotations

[ParquetThriftMetaData()]
public sealed class StringType { } // allowed for BYTE_ARRAY, must be encoded with UTF-8

[ParquetThriftMetaData()]
public sealed class MapType { } // see LogicalTypes.mded as raw UUID bytes

[ParquetThriftMetaData()]
public sealed class UUIDType { } // allowed for FIXED[16], must be encoded as raw UUID bytes

[ParquetThriftMetaData()]
public sealed class ListType { } // see LogicalTypes.md

[ParquetThriftMetaData()]
public sealed class EnumType { } // allowed for BYTE_ARRAY, must be encoded with UTF-8

[ParquetThriftMetaData()]
public sealed class DateType { }  // allowed for INT32

[ParquetThriftMetaData()]
public sealed class Float16Type { } // allowed for FIXED[2], must be encoded as raw FLOAT16 bytes (see LogicalTypes.md)

/// <summary>
/// Logical type to annotate a column that is always null.
/// 
/// Sometimes when discovering the schema of existing data, values are always
/// null and the physical type can't be determined. This annotation signals
/// the case where the physical type was guessed from all null values.
/// </summary>
[ParquetThriftMetaData()]
public sealed class NullType { } // allowed for any physical type, only null values stored

/// <summary>
/// Decimal logical type annotation
/// 
/// Scale must be zero or a positive integer less than or equal to the precision.
/// Precision must be a non-zero positive integer.
/// 
/// To maintain forward-compatibility in v1, implementations using this logical
/// type must also set scale and precision on the annotated SchemaElement.
/// 
/// Allowed for physical types: INT32, INT64, FIXED_LEN_BYTE_ARRAY, and BYTE_ARRAY.
/// </summary>
[ParquetThriftMetaData()]
public sealed class DecimalType
{
  [FieldId(1)]
  public int Scale { get; set; }

  [FieldId(2)]
  public int Precision { get; set; }
}

/// <summary>
/// Time units for logical types
/// </summary>
[ParquetThriftMetaData()]
public sealed class MilliSeconds { }

[ParquetThriftMetaData()]
public sealed class MicroSeconds { }

[ParquetThriftMetaData()]
public sealed class NanoSeconds { }

[ParquetThriftMetaData()]
public sealed class TimeUnit
{
  [FieldId(1)]
  public MilliSeconds MILLIS { get; set; } = default!;

  [FieldId(2)]
  public MicroSeconds MICROS { get; set; } = default!;

  [FieldId(3)]
  public NanoSeconds NANOS { get; set; } = default!;
}


/// <summary>
/// Timestamp logical type annotation
/// 
/// Allowed for physical types: INT64
/// </summary>
[ParquetThriftMetaData()]
public sealed class TimestampType
{
  [FieldId(1)]
  public bool IsAdjustedToUTC { get; set; }

  [FieldId(2)]
  public TimeUnit Unit { get; set; } = default!;
}

/// <summary>
/// Time logical type annotation
/// 
/// Allowed for physical types: INT32(millis), INT64(micros, nanos)
/// </summary>
[ParquetThriftMetaData()]
public sealed class TimeType
{

  [FieldId(1)]
  public bool IsAdjustedToUTC { get; set; }

  [FieldId(2)]
  public TimeUnit Unit { get; set; } = default!;
}

/// <summary>
/// Integer logical type annotation
/// 
/// bitWidth must be 8, 16, 32, or 64.
/// 
/// Allowed for physical types: INT32, INT64
/// </summary>
[ParquetThriftMetaData()]
public sealed class IntType
{
  [FieldId(1)]
  public Byte BitWidth { get; set; }

  [FieldId(2)]
  public bool IsSigned { get; set; }
}

/// <summary>
/// Embedded JSON logical type annotation
/// 
/// Allowed for physical types: BYTE_ARRAY
/// </summary>
[ParquetThriftMetaData()]
public sealed class JsonType
{
}

/// <summary>
/// Embedded BSON logical type annotation
/// 
/// Allowed for physical types: BYTE_ARRAY
/// </summary>
[ParquetThriftMetaData()]
public sealed class BsonType
{
}

/**
 * Embedded Variant logical type annotation
 */
[ParquetThriftMetaData()]
public sealed class VariantType
{
  // The version of the variant specification that the variant was
  // written with.
  [FieldId(1)]
  public byte? SpecificationVersion { get; set; }
}

/// <summary>
/// Edge interpolation algorithm for Geography logical type
/// </summary>
public enum EdgeInterpolationAlgorithm : byte
{
  SPHERICAL = 0,
  VINCENTY = 1,
  THOMAS = 2,
  ANDOYER = 3,
  KARNEY = 4,
}

/// <summary>
/// Embedded Geometry logical type annotation
/// 
/// Geospatial features in the Well-Known Binary(WKB) format and `edges` interpolation
/// is always linear/planar.
/// 
/// A custom CRS can be set by the crs field.If unset, it defaults to "OGC:CRS84",
/// which means that the geometries must be stored in longitude, latitude based on
/// the WGS84 datum.
/// 
/// Allowed for physical type: BYTE_ARRAY.
/// 
/// See Geospatial.md for details.
/// </summary>
[ParquetThriftMetaData()]
public sealed class GeometryType
{
  [FieldId(1)]
  public string? Crs { get; set; }
}

/// <summary>
/// Embedded Geography logical type annotation
/// 
/// Geospatial features in the WKB format with an explicit (non-linear/non-planar)
/// `edges` interpolation algorithm.
/// 
/// A custom geographic CRS can be set by the crs field, where longitudes are
/// bound by[-180, 180] and latitudes are bound by [-90, 90]. If unset, the CRS
/// defaults to "OGC:CRS84".
/// 
/// An optional algorithm can be set to correctly interpret `edges` interpolation
/// of the geometries. If unset, the algorithm defaults to SPHERICAL.
/// 
/// Allowed for physical type: BYTE_ARRAY.
/// 
/// See Geospatial.md for details.
/// </summary>
[ParquetThriftMetaData()]
public sealed class GeographyType
{
  [FieldId(1)]
  public string? Crs { get; set; }

  [FieldId(2)]
  public EdgeInterpolationAlgorithm? Algorithm { get; set; }
}

#endregion