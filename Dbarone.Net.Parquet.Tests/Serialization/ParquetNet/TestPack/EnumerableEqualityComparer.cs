public class EnumerableEqualityComparer<T> : IEqualityComparer<IEnumerable<T>>
{
  private readonly IEqualityComparer<T> _elementComparer;

  public EnumerableEqualityComparer(IEqualityComparer<T>? elementComparer = null)
  {
    _elementComparer = elementComparer ?? EqualityComparer<T>.Default;
  }

  public bool Equals(IEnumerable<T>? x, IEnumerable<T>? y)
  {
    if (ReferenceEquals(x, y)) return true;
    if (x is null || y is null) return false;

    return x.SequenceEqual(y, _elementComparer);
  }

  public int GetHashCode(IEnumerable<T>? obj)
  {
    if (obj is null) return 0;

    unchecked
    {
      int hash = 17;
      foreach (var item in obj)
      {
        hash = hash * 31 + _elementComparer.GetHashCode(item);
      }
      return hash;
    }
  }
}