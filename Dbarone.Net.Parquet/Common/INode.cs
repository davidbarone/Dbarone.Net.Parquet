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
  Node<T> AddChild(T child);

  /// <summary>
  /// Adds a child node to the current node.
  /// </summary>
  /// <param name="child">The child node to add.</param>
  Node<T> AddChild(Node<T> childNode);

  /// <summary>
  /// Removes a child node from the current node.
  /// </summary>
  /// <param name="child"></param>
  void RemoveChild(Node<T> child);

  /// <summary>
  /// Performs depth-first traversal of tree, using a custom child selector and visit function.
  /// </summary>
  /// <typeparam name="TAcc">The type of accumulator object to use in the visit function.</typeparam>
  /// <param name="start">The start node.</param>
  /// <param name="acc">The current value of the accumulator.</param>
  /// <param name="childrenSelector">The child selector function.</param>
  /// <param name="visitor">The visit function.</param>
  /// <returns>Returns the accumulator.</returns>
  NodeTraverseAccumulator<TAcc> DepthFirstTraverse<TAcc>(NodeTraverseAccumulator<TAcc> acc, Func<Node<T>, IEnumerable<Node<T>>> childrenSelector, Func<Node<T>, NodeTraverseAccumulator<TAcc>, NodeTraverseAccumulator<TAcc>> visitor);

  /// <summary>
  /// Performs a breadth-first traversal of tree, using a custom child selector and visit function.
  /// </summary>
  /// <typeparam name="TAcc">The type of accumulator object to use in the visit function.</typeparam>
  /// <param name="start">The start node.</param>
  /// <param name="acc">The current value of the accumulator.</param>
  /// <param name="childrenSelector">The child selector function.</param>
  /// <param name="visitor">The visit function.</param>
  /// <returns>Returns the accumulator.</returns>
  NodeTraverseAccumulator<TAcc> BreadthFirstTraverse<TAcc>(Node<T> start, NodeTraverseAccumulator<TAcc> acc, Func<Node<T>, IEnumerable<Node<T>>> childrenSelector, Func<Node<T>, NodeTraverseAccumulator<TAcc>, NodeTraverseAccumulator<TAcc>> visitor);

  /// <summary>
  /// Gets the index of the node using a depth-first approach.
  /// </summary>
  /// <param name="leafOnly">Set to true to get the index of leaf nodes only.</param>
  /// <returns>Returns the index of the node within the tree, or null if leafOnly set, and the node is not a leaf.</returns>
  int? GetDepthFirstIndex(bool leafOnly = false);

  /// <summary>
  /// Converts a node of one type to another type using a converter class.
  /// </summary>
  /// <typeparam name="U">The node type to convert to.</typeparam>
  /// <param name="converter">The convert class to be used.</param>
  /// <returns>Returns a corresponding node of type U.</returns>
  Node<U> ConvertTo<U>(INodeConverter<T, U> converter);

  /// <summary>
  /// Gets the path from an ancestor node (typically the root node).
  /// </summary>
  /// <param name="ancestor">The ancestor node from which to create the path.</param>
  /// <param name="prunePath">If set to true, the path is pruned / modified to return a path representing the logical data path. Otherwise, the actual Parquet schema path is returned.</param>
  /// <returns>Returns a List of SchemaNode elements representing the path.</returns>
  List<Node<T>> GetPathFromAncestor(Node<T> ancestor);

  /// <summary>
  /// Returns the current node, where the children are filtered based on a predicate.
  /// </summary>
  /// <param name="predicate">Predicate to filter children.</param>
  /// <returns>Returns a new Node object with filtered children.</returns>
  Node<T> Filter(Func<Node<T>, bool> predicate);

  /// <summary>
  /// Performs depth-first traversal of tree, outputting each node to a list.
  /// </summary>
  /// <returns></returns>
  List<Node<T>> ToList();


  /// <summary>
  /// Returns a flat list of nodes starting with current node, based on a predicate.
  /// </summary>
  /// <param name="predicate">The predicate to select nodes.</param>
  /// <returns>Returns a list of nodes.</returns>
  List<Node<T>> Find(Func<Node<T>, bool> predicate);

  /// <summary>
  /// Gets the leaf nodes from current node using a depth-first traverse.
  /// </summary>
  /// <returns>Returns a list of leaf nodes from the current node.</returns>
  List<Node<T>> GetLeafNodes();

  /// <summary>
  /// Gets the root node.
  /// </summary>
  /// <returns>Returns the root node.</returns>
  Node<T> GetRoot();

  /// <summary>
  /// Takes a current node, applies a filter predicate, and converts the node elements to
  /// a dictionary where the node elements are the keys, and the values are provided by a
  /// custom transformation.
  /// </summary>
  /// <typeparam name="TVal"></typeparam>
  /// <param name="valueTransformation"></param>
  Dictionary<Node<T>, TVal> ToDictionary<TVal>(Func<Node<T>, bool> predicate, Func<Node<T>, TVal> valueTransformation);
}