namespace Dbarone.Net.Parquet.Common;

/// <summary>
/// Object that stores state over a node walk algorithm.
/// </summary>
/// <typeparam name="TAcc">Type used to store the accumulated state.</typeparam>
public class NodeTraverseAccumulator<TAcc>
{
  /// <summary>
  /// Set to True to abort the traversal / walk.
  /// </summary>
  public bool Cancel { get; }

  /// <summary>
  /// The accumulated state.
  /// </summary>
  public TAcc Accumulator { get; }

  /// <summary>
  /// Constructor.
  /// </summary>
  /// <param name="cancel">Initial cancel value.</param>
  /// <param name="accumulator">Initial accumulated state value.</param>
  public NodeTraverseAccumulator(bool cancel, TAcc accumulator)
  {
    Cancel = cancel;
    Accumulator = accumulator;
  }
}