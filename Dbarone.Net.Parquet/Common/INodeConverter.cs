namespace Dbarone.Net.Parquet.Common;

public interface INodeConverter<T, U>
{
  static Node<U> Convert(Node<T> node) { return default!; }
  static Node<T> Convert(Node<U> node) { return default!; }
}