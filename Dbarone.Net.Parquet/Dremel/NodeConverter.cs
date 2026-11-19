using Dbarone.Net.Parquet.Common;
using Dbarone.Net.Parquet.Dremel;
using Dbarone.Net.Parquet.Thrift;

public abstract class NodeConverter<T, U>
{
  public abstract Node<U> Convert(Node<T> node);
}