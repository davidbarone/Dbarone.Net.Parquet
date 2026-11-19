public class NodeTraverseAccumulator<TAcc>
{
  public bool Cancel { get; }
  public TAcc Accumulator { get; }

  public NodeTraverseAccumulator(bool cancel, TAcc accumulator)
  {
    Cancel = cancel;
    Accumulator = accumulator;
  }
}