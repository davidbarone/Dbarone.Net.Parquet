namespace Dbarone.Net.Parquet.Dremel;

using Dbarone.Net.Parquet.Thrift;
using Dbarone.Net.Parquet.Common;
using Dbarone.Net.Parquet.Extensions;

/// <summary>
/// Represents a logical path from root to leaf node used by Dremel shredding / assembly.
/// The Parquet schema includes additional synthetic nodes that to not map to physical
/// levels in the final logical data model (for example the 3-level LIST structure). The
/// logical path represents the actual path in the final data model, and contains no
/// additional sythetic nodes. The logical path is used by the dremel encoding process.
/// 
/// The logical path removes additional nodes to annotate things like:
/// - repetition levels
/// - definition levels
/// - list element boundaries
/// - optionality
/// 
/// The general algorithm to map Parquet schema nodes to logical nodes is:
/// - Remove synthetic root
/// - Prune LIST structures
/// - Prune MAP structures 
/// - Prune synthetic groups with no meaning 
/// - Keep only required nodes
/// </summary>



/// <summary>
/// Converts a Parquet SchemaElement node to a LogicalSchemaElement node.
/// 
/// The Node&lt;SchemaElement%gt; class represents a tree specified by the Parquet
/// schema. This includes additional synthetic nodes that do not map to physical
/// levels in the final logical data model (for deample the 3-level LIST structure).
/// 
/// The Node&lt;LogicalSchemaElement&gt; class represents a logical tree that can
/// be used by Dremel shredding / assembly. The logical tree contains no additional
/// synthetic nodes.
/// 
/// This converter removes additional nodes which annotate things like:
/// - repetition levels
/// - definition levels
/// - list element boundaries
/// - optionality
/// 
/// The general algorithm to map Parquet schema nodes to logical nodes is:
/// - Remove synthetic root
/// - Prune LIST structures
/// - Prune MAP structures 
/// - Prune synthetic groups with no meaning 
/// - Keep only required nodes
/// </summary>
public class LogicalSchemaElementConverter : NodeConverter<SchemaElement, LogicalSchemaElement>
{
  public override Node<LogicalSchemaElement> Convert(Node<SchemaElement> node)
  {
    SchemaElement element = node.Value;

    if (element.GetSchemaElementGroupType() == SchemaElementGroupType.PRIMITIVE)
    {
      return ConvertPrimitive(node);
    }
    else if (element.GetSchemaElementGroupType() == SchemaElementGroupType.LIST)
    {
      return ConvertList(node);
    }
    else if (element.GetSchemaElementGroupType() == SchemaElementGroupType.MAP)
    {
      return ConvertMap(node);
    }
    else if (element.GetSchemaElementGroupType() == SchemaElementGroupType.STRUCT)
    {
      return ConvertStruct(node);
    }
    else
    {
      throw new Exception($"Cannot convert element: {element.Name}.");
    }
  }

  /// <summary>
  /// Lists are pruned as follows:
  /// 
  /// Before (physical model):
  /// optional group mylist (LIST)
  ///   repeated group list
  ///     optional <element-type> element
  /// 
  /// After (logical model):
  /// optional/repeated group mylist
  ///   optional <element-type> element
  /// 
  /// Prune rule:
  /// - drop L1 / outer group (LIST wrapper)
  /// - rename L2 node to be the original L1 name (i.e. 'list' -> 'mylist'). Change the logical type on L2 to 'list'
  /// - Add L1 node to the L2.Annotations property. This allows the list to differentiate between null list value and empty list
  /// </summary>
  /// <param name="element">The SchemaElement to convert.</param>
  /// <returns>Returns a LogicalSchemaElement</returns>
  private Node<LogicalSchemaElement> ConvertList(Node<SchemaElement> node)
  {
    var listNode = node;
    var repeatedNode = node.Children.First();
    var elementNode = node.Children.First().Children.First();

    // Dremel kind is aggregated from L1+L2
    DremelKind kind = DremelKind.None;
    kind |= repeatedNode.Value.RepetitionType == RepetitionType.OPTIONAL ? DremelKind.IsOptional : DremelKind.None;
    kind |= DremelKind.IsEmptyable;
    kind |= DremelKind.IsRepeated;

    var newList = new LogicalSchemaElement(
      listNode.Value.Name,
      kind,
      listNode.Value.Type,
      listNode.Value.LogicalType,
      listNode.Value.GetSchemaElementGroupType()
    );
    var newNode = new Node<LogicalSchemaElement>(newList);

    var newElement = elementNode.ConvertTo(new LogicalSchemaElementConverter());
    newNode.AddChild(newElement);

    return new Node<LogicalSchemaElement>(newList);
  }

  private Node<LogicalSchemaElement> ConvertMap(Node<SchemaElement> node)
  {
    throw new NotImplementedException();
  }

  private Node<LogicalSchemaElement> ConvertStruct(Node<SchemaElement> node)
  {
    // Struct element
    var element = node.Value;
    DremelKind kind = DremelKind.None;
    if (element.RepetitionType == RepetitionType.OPTIONAL)
    {
      kind |= DremelKind.IsOptional;
    }

    // convert the current node
    LogicalSchemaElement logical = new LogicalSchemaElement(
      element.Name,
      kind,
      element.Type,
      element.LogicalType,
      element.GetSchemaElementGroupType()
    );
    var structNode = new Node<LogicalSchemaElement>(logical);

    // Convert children
    foreach (var child in node.Children)
    {
      structNode.AddChild(child.ConvertTo<LogicalSchemaElement>(new LogicalSchemaElementConverter()));
    }

    return structNode;
  }

  private Node<LogicalSchemaElement> ConvertPrimitive(Node<SchemaElement> node)
  {
    var element = node.Value;
    DremelKind kind = DremelKind.None;
    if (element.RepetitionType == RepetitionType.OPTIONAL)
    {
      kind |= DremelKind.IsOptional;
    }
    if (element.RepetitionType == RepetitionType.REPEATED)
    {
      kind |= DremelKind.IsRepeated;
    }

    // convert the current node
    LogicalSchemaElement logical = new LogicalSchemaElement(
      element.Name,
      kind,
      element.Type,
      element.LogicalType,
      element.GetSchemaElementGroupType()
    );

    return new Node<LogicalSchemaElement>(logical);
  }
}