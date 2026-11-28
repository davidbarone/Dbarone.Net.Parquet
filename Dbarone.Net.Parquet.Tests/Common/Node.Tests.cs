namespace Dbarone.Net.Parquet.Common.Tests;

using System.Security.Cryptography.X509Certificates;
using Dbarone.Net.Parquet.Common;

public class NodeTests
{
  Node<string> GetTestNode()
  {
    Node<string> node = "A";
    node
      .AddChild(
        new Node<string>("A.A")
          .AddChild("A.A.A")
          .AddChild("A.A.B"))
      .AddChild(
        new Node<string>("A.B")
          .AddChild("A.B.A")
          .AddChild("A.B.B")
          .AddChild("A.B.C"))
      .AddChild(
        new Node<string>("A.C")
          .AddChild("A.C.A"));
    return node;
  }

  [Fact]
  public void TestParentChild()
  {
    var root = GetTestNode();
    var AB = root.Find(n => n.Equals("A.B")).First();
    Assert.True(!AB.IsRoot);
    Assert.True(!AB.IsLeaf);
  }

  [Fact]
  public void TestRootDepthHeight()
  {
    var root = GetTestNode();
    Assert.Equal(0, root.Depth);
    Assert.Equal(3, root.Height);
  }

  [Fact]
  public void TestLeafDepthHeight()
  {
    var leaf = GetTestNode().GetLeafNodes().First();
    Assert.Equal(3, leaf.Depth);
    Assert.Equal(0, leaf.Height);
  }

  [Fact]
  public void TestNodeConstructor()
  {
    Node<string> node = "A";
    Assert.True(node.IsRoot);
    Assert.True(node.IsLeaf);
    Assert.Null(node.Parent);
    Assert.Empty(node.Children);
  }
}