using System.Diagnostics;
using Dbarone.Net.Parquet.Common;
using Dbarone.Net.Parquet.Dremel;
using Dbarone.Net.Parquet.Thrift;

public interface INodeConverter<T, U>
{
  static Node<U> Convert(Node<T> node) { return default!; }
  static Node<T> Convert(Node<U> node) { return default!; }

}