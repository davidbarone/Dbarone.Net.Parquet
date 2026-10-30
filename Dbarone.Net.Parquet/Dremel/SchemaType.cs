namespace Dbarone.Net.Parquet.Dremel;

/// <summary>
/// The type of schema node. Can be group or primitive
/// - Group: represents structs, nested structures, lists, maps etc
/// - Primitive: represent raw, physical type (e.g. INT32, BYTE_ARRAY)
/// </summary>
public enum SchemaType
{
  Group,
  Primitive
}