using System.Collections.Generic;
using System.Linq;
using Dbarone.Net.Extensions;

// Custom comparer for Dictionary<string, object>
public class DictionaryComparer : IEqualityComparer<Dictionary<string, object?>>
{
  public bool Equals(Dictionary<string, object?>? x, Dictionary<string, object?>? y)
  {
    if (x == null || y == null) return x == y;
    if (x.Count != y.Count) return false;

    foreach (var kvp in x)
    {
      if (!y.TryGetValue(kvp.Key, out var value)) return false;

      // Handle nulls and value equality
      if (kvp.Value == null && value != null) return false;
      if (kvp.Value != null && kvp.Value.GetType() == typeof(byte[]) && ((byte[])kvp.Value).SequenceEqual((byte[])value)) return true;  // comparing byte[]
      // if values are themselves lists / IEnumerable, do sequence compare
      if (kvp.Value != null && kvp.Value is IEnumerable<object> && value != null && value is IEnumerable<object>)
      {
        var left = kvp.Value as IEnumerable<object>;
        var right = value as IEnumerable<object>;
        return left.SequenceEqual(right);
      }
      // finally, do reference equality (note this will return false if 2 equivalent objects with different address references)
      if (kvp.Value != null && !kvp.Value.Equals(value)) return false;
    }
    return true;
  }

  public int GetHashCode(Dictionary<string, object?> obj)
  {
    if (obj == null) return 0;
    // Combine hash codes of all key-value pairs
    int hash = 17;
    foreach (var kvp in obj.OrderBy(k => k.Key))
    {
      hash = hash * 31 + kvp.Key.GetHashCode();
      hash = hash * 31 + (kvp.Value?.GetHashCode() ?? 0);
    }
    return hash;
  }
}
