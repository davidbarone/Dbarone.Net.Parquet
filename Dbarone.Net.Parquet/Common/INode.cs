namespace Dbarone.Net.Parquet.Common;

/// <summary>
/// Defines interface for a standard tree structure with nodes.
/// </summary>
public interface INode<T>
{
  /// <summary>
  /// The node value.
  /// </summary>
  T Value { get; set; }

  /// <summary>
  /// Gets the children of the current node. 
  /// </summary>
  List<INode<T>> Children { get; }

  /// <summary>
  /// Gets the parent of the current node.
  /// </summary>
  public INode<T>? Parent { get; }

  /// <summary>
  /// Returns true if the current node is the root of a tree (no parent).
  /// </summary>
  bool IsRoot { get; }

  /// <summary>
  /// Returns true if the current node is a leaf node of the tree.
  /// </summary>
  public bool IsLeaf { get; }

  /// <summary>
  /// Returns true if the current node is an ancestor of the provided node.
  /// </summary>
  /// <param name="node">The descendant node.</param>
  /// <returns>Returns true if the current node is an ancestor of the specified node.</returns>
  bool IsAncestorOf(Node<T> node);

  /// <summary>
  /// Number of edges from the root node to the given node.
  /// </summary>
  int Depth { get; }

  /// <summary>
  /// Number of edges on the longest path from the node down to a leaf.
  /// </summary>
  int Height { get; }

  /// <summary>
  /// Adds a child to the current node.
  /// </summary>
  /// <param name="child">The child to add.</param>
  void AddChild(T child);

  /// <summary>
  /// Adds a child node to the current node.
  /// </summary>
  /// <param name="child">The child node to add.</param>
  public void AddChild(Node<T> childNode);

  /// <summary>
  /// Removes a child node from the current node.
  /// </summary>
  /// <param name="child"></param>
  public void RemoveChild(Node<T> child);


}