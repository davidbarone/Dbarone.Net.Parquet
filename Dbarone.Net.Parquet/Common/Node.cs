namespace Dbarone.Net.Parquet.Common;

/// <summary>
/// A generic tree node class.
/// </summary>
/// <typeparam name="T">The node type.</typeparam>
public class Node<T>
{
  public T Value { get; }
  public List<Node<T>> Children { get; } = new();
  public Node<T>? Parent { get; private set; }

  /// <summary>
  /// Creates a new node.
  /// </summary>
  /// <param name="value">The node value.</param>
  public Node(T value)
  {
    Value = value;
  }

  /// <summary>
  /// Returns true if the current node is the root node of a tree.
  /// </summary>
  /// <returns>Returns true if the node is a root node.</returns>
  public bool IsRoot => this.Parent is null;

  /// <summary>
  /// Returns true if the current node is a leaf node of the tree.
  /// </summary>
  public bool IsLeaf => this.Children is null || this.Children.Count() == 0;

  /// <summary>
  /// Number of edges from the root node to the given node.
  /// </summary>
  public int depth
  {
    get
    {
      int depth = 0;
      var current = this;
      while (current.Parent != null)
      {
        depth++;
        current = current.Parent;
      }
      return depth;
    }
  }

  /// <summary>
  /// Number of edges on the longest path from the node down to a leaf.
  /// </summary>
  public int Height
  {
    get
    {
      if (Children.Count == 0) return 0; // Leaf node
      int maxChildHeight = 0;
      foreach (var child in Children)
      {
        maxChildHeight = Math.Max(maxChildHeight, child.Height);
      }
      return maxChildHeight + 1;
    }
  }

  /// <summary>
  /// Adds a child to the current node.
  /// </summary>
  /// <param name="child">The child to add.</param>
  public void AddChild(T child)
  {
    var childNode = new Node<T>(child);
    AddChild(childNode);
  }

  /// <summary>
  /// Adds a child node to the current node.
  /// </summary>
  /// <param name="child">The child node to add.</param>
  public void AddChild(Node<T> childNode)
  {
    Children.Add(childNode);
    childNode.Parent = this;
  }

  /// <summary>
  /// Removes a child node from the current node.
  /// </summary>
  /// <param name="child"></param>
  public void RemoveChild(Node<T> child)
  {
    if (Children.Remove(child))
    {
      child.Parent = null;
    }
  }

  /// <summary>
  /// Performs depth-first traversal of tree, using a custom child selector and visit function.
  /// </summary>
  /// <typeparam name="TAcc">The type of accumulator object to use in the visit function.</typeparam>
  /// <param name="start">The start node.</param>
  /// <param name="acc">The current value of the accumulator.</param>
  /// <param name="childrenSelector">The child selector function.</param>
  /// <param name="visitor">The visit function.</param>
  /// <returns>Returns the accumulator.</returns>
  public NodeTraverseAccumulator<TAcc> DepthFirstTraverse<TAcc>(Node<T> start, NodeTraverseAccumulator<TAcc> acc, Func<Node<T>, IEnumerable<Node<T>>> childrenSelector, Func<Node<T>, NodeTraverseAccumulator<TAcc>, NodeTraverseAccumulator<TAcc>> visitor)
  {
    // visit current node
    acc = visitor(start, acc);

    if (!acc.Cancel)
    {
      // Traverse children
      foreach (var child in childrenSelector(start))
      {
        acc = DepthFirstTraverse(child, acc, childrenSelector, visitor);
      }
    }
    return acc;
  }

  /// <summary>
  /// Performs a breadth-first traversal of tree, using a custom child selector and visit function.
  /// </summary>
  /// <typeparam name="TAcc">The type of accumulator object to use in the visit function.</typeparam>
  /// <param name="start">The start node.</param>
  /// <param name="acc">The current value of the accumulator.</param>
  /// <param name="childrenSelector">The child selector function.</param>
  /// <param name="visitor">The visit function.</param>
  /// <returns>Returns the accumulator.</returns>
  public NodeTraverseAccumulator<TAcc> BreadthFirstTraverse<TAcc>(Node<T> start, NodeTraverseAccumulator<TAcc> acc, Func<Node<T>, IEnumerable<Node<T>>> childrenSelector, Func<Node<T>, NodeTraverseAccumulator<TAcc>, NodeTraverseAccumulator<TAcc>> visitor)
  {
    var queue = new Queue<Node<T>>();
    queue.Enqueue(start);

    while (queue.Count > 0)
    {
      var node = queue.Dequeue();
      acc = visitor(node, acc);

      foreach (var child in childrenSelector(node))
        queue.Enqueue(child);
    }

    return acc;
  }

  /// <summary>
  /// Gets the index of the node using a depth-first approach.
  /// </summary>
  /// <param name="leafOnly">Set to true to get the index of leaf nodes only.</param>
  /// <returns>Returns the index of the node within the tree, or null if leafOnly set, and the node is not a leaf.</returns>
  /// <exception cref="Exception"></exception>
  public int? GetDepthFirstIndex(bool leafOnly = false)
  {
    var root = this.GetRoot();
    NodeTraverseAccumulator<int?> result = new NodeTraverseAccumulator<int?>(false, null);
    Func<Node<T>, IEnumerable<Node<T>>> childSelector = (n) => n.Children;
    Func<Node<T>, NodeTraverseAccumulator<int?>, NodeTraverseAccumulator<int?>> visitor = (node, acc) =>
    {
      int index = 0;
      bool cancel = false;
      if (node == this)
      {
        cancel = true;
      }
      if (leafOnly == false || (leafOnly == true && node.IsLeaf))
      {
        if (acc.Accumulator.HasValue)
        {
          index++;
        }
        else
        {
          index = 0;
        }
      }
      return new NodeTraverseAccumulator<int?>(cancel, index);
    };

    var acc = DepthFirstTraverse(root, result, childSelector, visitor);
    return acc.Accumulator;
  }

  /// <summary>
  /// Converts a node of one type to another type using a converter class.
  /// </summary>
  /// <typeparam name="U">The node type to convert to.</typeparam>
  /// <param name="converter">The convert class to be used.</param>
  /// <returns>Returns a corresponding node of type U.</returns>
  public Node<U> ConvertTo<U>(NodeConverter<T, U> converter)
  {
    return converter.Convert(this);
  }

  /// <summary>
  /// Gets the path from an ancestor node (typically the root node).
  /// </summary>
  /// <param name="ancestor">The ancestor node from which to create the path.</param>
  /// <param name="prunePath">If set to true, the path is pruned / modified to return a path representing the logical data path. Otherwise, the actual Parquet schema path is returned.</param>
  /// <returns>Returns a List of SchemaNode elements representing the path.</returns>
  /// <exception cref="InvalidOperationException">Throws exception if the current node is not a descendent of the ancestor.</exception>
  public List<Node<T>> GetPathFromAncestor(Node<T> ancestor)
  {
    var path = new List<Node<T>>();
    if (Find(ancestor, this, path))
      return path;
    throw new InvalidOperationException("Current node not under ancestor.");

    bool Find(Node<T> node, Node<T> target, List<Node<T>> acc)
    {
      acc.Add(node);
      if (node == target)
        return true;

      foreach (var child in node.Children)
      {
        if (Find(child, target, acc))
          return true;
      }

      acc.RemoveAt(acc.Count - 1);
      return false;
    }
  }

  /// <summary>
  /// Returns a flat list of nodes starting with current node, based on a predicate.
  /// </summary>
  /// <param name="predicate">The predicate to select nodes.</param>
  /// <returns>Returns a list of nodes.</returns>
  public List<Node<T>> GetNodes(Func<Node<T>, bool> predicate)
  {
    NodeTraverseAccumulator<List<Node<T>>> result = new NodeTraverseAccumulator<List<Node<T>>>(false, new List<Node<T>>());
    Func<Node<T>, IEnumerable<Node<T>>> childSelector = (n) => n.Children;
    Func<Node<T>, NodeTraverseAccumulator<List<Node<T>>>, NodeTraverseAccumulator<List<Node<T>>>> visitor = (node, acc) =>
    {
      if (predicate(node))
      {
        acc.Accumulator.Add(node);
      }
      return acc;
    };
    var acc = DepthFirstTraverse(this, result, childSelector, visitor);
    return acc.Accumulator;
  }

  /// <summary>
  /// Gets the leaf nodes from current node using a depth-first traverse.
  /// </summary>
  /// <returns>Returns a list of leaf nodes from the current node.</returns>
  public List<Node<T>> GetLeafNodes()
  {
    return GetNodes((n) => n.IsLeaf);
  }

  /// <summary>
  /// Gets the root node.
  /// </summary>
  /// <returns>Returns the root node.</returns>
  public Node<T> GetRoot()
  {
    Node<T> node = this;
    while (node.Parent is not null)
    {
      node = node.Parent;
    }
    return node;
  }

  /// <summary>
  /// Takes a current node, applies a filter predicate, and converts the node elements to
  /// a dictionary where the node elements are the keys, and the values are provided by a
  /// custom transformation.
  /// </summary>
  /// <typeparam name="TVal"></typeparam>
  /// <param name="valueTransformation"></param>
  public Dictionary<Node<T>, TVal> ToDictionary<TVal>(Func<Node<T>, bool> predicate, Func<Node<T>, TVal> valueTransformation)
  {
    var list = GetNodes(predicate);
    Dictionary<Node<T>, TVal> results = new Dictionary<Node<T>, TVal>();
    foreach (var item in list)
    {
      results[item] = valueTransformation(item);
    }
    return results;
  }
}