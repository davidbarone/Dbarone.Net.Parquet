namespace Dbarone.Net.Parquet.Dremel;

/// <summary>
/// A special variant of the Parquet RepetitionKind. This class is able to logically describe all necessary
/// null / empty permutations in a single value, as opposed to the Parquet RepetitionKind, where each schema
/// node can ONLY be one of Required, Optional or Repeated -> The Parquet schema therefore requires additional
/// annotation only / synthetic nodes to be able to define all valid nullability / empty states of LIST and
/// MAP types. This enum is defined as a FLAG field.
/// </summary>
[Flags]
public enum DremelKind : Byte
{
  None = 0,

  Required = 1,

  /// <summary>
  /// The node allows a null value. Note that repeated nodes are also optional.
  /// </summary>
  Optional = 2,

  /// <summary>
  /// The node allows an empty state. Only applicable to LIST and MAP nodes.
  /// With the Parquet implementation of Dremel, LIST types are repeated and
  /// are also implicitly optional. However, additional synthetic nodes are
  /// required to distinguish between null list and empty list. This member
  /// is provided so a single logical node can distinguish between null list
  /// and empty list. 
  /// </summary>
  Emptyable = 4,

  /// <summary>
  /// The node allows repeated values. If set, the child node describes the
  /// element type. This setting ONLY applicable to LIST nodes.
  /// </summary>
  Repeated = 8
}