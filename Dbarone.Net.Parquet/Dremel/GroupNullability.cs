namespace Dbarone.Net.Parquet.Dremel;

/// <summary>
/// Specifies the null/empty status of a LIST or MAP group container for a row.
/// </summary>
public enum GroupNullability
{
  /// <summary>
  /// The container is null.
  /// </summary>
  NULL,

  /// <summary>
  /// The container is not null but empty.
  /// </summary>
  EMPTY,

  /// <summary>
  /// The container has values.
  /// </summary>
  VALUES
}