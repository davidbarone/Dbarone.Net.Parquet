/// <summary>
/// The type of logical node.
/// </summary>
public enum LogicalNodeType
{
  /// <summary>
  /// Struct node. Implemented as a dictionary in the logical data model.
  /// </summary>
  STRUCT,

  /// <summary>
  /// List node. Implemented as a list in the logical data model.
  /// </summary>
  LIST,

  /// <summary>
  /// Map node.
  /// </summary>
  MAP,

  /// <summary>
  /// Leaf node
  /// </summary>
  LEAF
}