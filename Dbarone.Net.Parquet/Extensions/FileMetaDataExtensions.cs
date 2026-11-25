namespace Dbarone.Net.Parquet.Extensions;

using Dbarone.Net.Parquet.Common;
using Dbarone.Net.Parquet.Dremel;
using Dbarone.Net.Parquet.Thrift;

public static class FileMetaDataExtensions
{
  /// <summary>
  /// Maps a schema (leaf) element to a column in a row group.
  ///
  /// The metadata schema defines all columns in a schema, including:
  /// [0]: This is always the synthetic 'root' element.
  /// [1..N]: The actual fields (including nested groups and leaf columns
  /// 
  /// RowGroup columns is a list of ColumnChunk objects.
  /// - One column chunk per leaf column
  /// - Ordered in schema leaf order
  /// 
  /// Column chunks are only required to store real physical data columns
  /// (i.e. the leaf columns). Group columns get reconstituted from leaf
  /// columns.
  /// 
  /// The schema list always starts with root at element [0], and the
  /// other elements follow in depth-first order.
  /// 
  /// To traverse the schema tree:
  /// - every SchemaElement with a physical type and num_children is
  ///   null or zero is a leaf element.
  /// - The RowGroup.Columns is in the same order as the leaf columns
  ///   exist in the schema.
  /// 
  /// https://deepwiki.com/apache/parquet-format/2.1-metadata-structures?utm_source=copilot.com
  /// https://deepwiki.com/apache/parquet-format/2.2-file-layout-and-organization?utm_source=copilot.com
  ///
  /// As a final check, ColumnChunk.ColumnMetaData.PathInSchema is the
  /// canonical mapping back to the schema element leaf.
  /// </summary>
  /// <param name="fileMetaData">The metadata.</param>
  /// <param name="pathInSchema">The path of the schema element</param>
  /// <returns>Returns the index in the column chunk array if a leaf / data column. Otherwise returns null.</returns>
  /// <exception cref="Exception"></exception>
  public static int? GetColumnChunkIndex(this FileMetaData fileMetaData, string[] pathInSchema)
  {
    // first, find the column name in the schema
    var paths = fileMetaData.GetSchemaPaths();
    var path = paths.First(p => p.SequenceEqual(pathInSchema));
    var idx = paths.ToList().IndexOf(path);
    var element = fileMetaData.Schema[idx];

    // if not a leaf / data element, return null
    if (element.NumChildren > 0 || element.Type is null)
    {
      // not a leaf / data column
      return null;
    }

    if (idx < 0)
    {
      throw new Exception($"Cannot find {pathInSchema} in schema");
    }

    int columnChunkIndex = -1;

    // loop from element 1 (ignore root element 0), counting the number of leaf
    // elements
    for (int i = 1; i <= idx; i++)
    {
      if (fileMetaData.Schema[i].Type is not null && (fileMetaData.Schema[i].NumChildren ?? 0) == 0)
      {
        // is a leaf node
        columnChunkIndex++;
      }
    }
    return columnChunkIndex;
  }

  public static int GetMaxDefinitionLevel(this FileMetaData fileMetaData, SchemaElement schemaElement)
  {
    // TO DO: Need to walk from root to this element, adding up the number of optional / repeated
    // levels. For now, just check the current element.
    return schemaElement.RepetitionType == RepetitionType.OPTIONAL ? 1 : 0;
  }

  public static int GetMaxRepetitionLevel(this FileMetaData fileMetaData, SchemaElement schemaElement)
  {
    // TO DO: Need to walk from root to this element, adding up the number of repeated
    // levels. For now, just check the current element.
    return schemaElement.RepetitionType == RepetitionType.REPEATED ? 1 : 0;
  }

  public static string[] GetSchemaPathForElement(this FileMetaData fileMetaData, SchemaElement schemaElement)
  {
    var idx = fileMetaData.Schema.IndexOf(schemaElement);
    return fileMetaData.GetSchemaPaths()[idx];
  }

  /// <summary>
  /// Generates a 'path-in-schema' property for each of the elements in the Schema array.
  /// No explicit identifier or path exists on the SchemaElement type. However, the path
  /// in the schema can be calculated as the list of schema elements is stored strictly
  /// depth-first. The num_children property can be used to determine when a leaf
  /// column is reached.
  /// </summary>
  /// <param name="fileMetaData"></param>
  /// <returns></returns>
  public static string[][] GetSchemaPaths(this FileMetaData fileMetaData, int i = 0, Stack<string>? paths = null, string[][]? results = null)
  {
    // Initialise variables
    if (results is null && paths is null)
    {
      var count = fileMetaData.Schema.Count();
      results = new string[count][];
      paths = new Stack<string>();

      // Walk schema (depth first)
      fileMetaData.GetSchemaPaths(0, paths, results);
    }
    else
    {
      // recursive bit
      var schema = fileMetaData.Schema;

      // get current element and push on stack
      var item = schema[i];
      var name = item.Name;
      paths.Push(name);

      // update results
      results[i] = paths.Reverse().ToArray();
      i++;

      // Get the children for current element
      var numChildren = item.NumChildren;
      for (int j = 0; j < numChildren; j++)
      {
        fileMetaData.GetSchemaPaths(i + j, paths, results);
      }
      paths.Pop();
      return results;
    }

    // Return results
    return results;
  }

  public static SchemaElement GetSchemaElement(this FileMetaData fileMetaData, string[] pathInSchema)
  {
    var paths = fileMetaData.GetSchemaPaths().ToList();
    var path = paths.First(p => p.SequenceEqual(pathInSchema));
    var idx = paths.IndexOf(path);
    return fileMetaData.Schema[idx];
  }

  public static bool IsLeafColumn(this FileMetaData fileMetaData, string[] pathInSchema)
  {
    var schema = fileMetaData.Schema;
    var paths = fileMetaData.GetSchemaPaths();
    var path = paths.ToList().FirstOrDefault(p => p.SequenceEqual(pathInSchema));
    if (path is null)
    {
      throw new Exception("Cannot find column");
    }
    var idx = paths.ToList().IndexOf(path);
    // get element
    var element = schema[idx];
    if ((element.NumChildren is null || element.NumChildren == 0) && element.Type is not null)
    {
      return true;
    }
    else
    {
      return false;
    }
  }

  #region Node extensions

  /// <summary>
  /// Gets the nested nullability for LIST and MAP types
  /// </summary>
  /// <param name="logicalSchemaElement"></param>
  /// <param name="dl"></param>
  /// <returns></returns>
  public static GroupNullability GetGroupNullabilityForRow(this Node<LogicalSchemaElement> node, int dl)
  {
    var mdl = node.MaxDefinitionLevel();
    if (node.Value.DremelKind.HasFlag(DremelKind.IsOptional) && dl <= mdl - node.Value.MaxDefinitionLevelIncr)
    {
      // container is null
      return GroupNullability.NULL;
    }
    else if (node.Value.DremelKind.HasFlag(DremelKind.IsEmptyable) && dl <= mdl - 1)
    {
      return GroupNullability.EMPTY;
    }
    else
    {
      return GroupNullability.VALUES;
    }
  }

  /// <summary>
  /// Gets the max definition level for a schema element node.
  /// </summary>
  /// <param name="schemaElement"></param>
  /// <returns></returns>
  public static int MaxDefinitionLevel(this Node<LogicalSchemaElement> logicalSchemaElement)
  {
    if (logicalSchemaElement.IsRoot)
    {
      return logicalSchemaElement.Value.MaxDefinitionLevelIncr;
    }
    else
    {
      return logicalSchemaElement.Parent!.MaxDefinitionLevel() + logicalSchemaElement.Value.MaxDefinitionLevelIncr;
    }
  }

  public static int MaxRepetitionLevel(this Node<LogicalSchemaElement> logicalSchemaElement)
  {
    if (logicalSchemaElement.IsRoot)
    {
      return logicalSchemaElement.Value.MaxRepetitionLevelIncr;
    }
    else
    {
      return logicalSchemaElement.Parent!.MaxRepetitionLevel() + logicalSchemaElement.Value.MaxRepetitionLevelIncr;
    }
  }

  #endregion

  #region SchemaElement extensions

  /// <summary>
  /// Converts a flattened parquet schema to a node tree.
  /// </summary>
  /// <param name="schema">Flattened Parquet schema</param>
  /// <returns>Returns a node tree.</returns>
  public static Node<SchemaElement> ToNode(this List<SchemaElement> schema)
  {
    // nested function to process elements
    Node<SchemaElement> ProcessThriftSchema(List<SchemaElement> thriftSchema, Node<SchemaElement>? parent = null, int currentThriftIndex = 0)
    {
      // Get current element
      var element = thriftSchema[currentThriftIndex];
      var node = new Node<SchemaElement>(element);

      // Add to parent if applicable
      if (parent is not null)
      {
        parent.AddChild(node);
      }

      // Process children
      for (int i = 1; i <= element.NumChildren; i++)
      {
        var childThriftIndex = currentThriftIndex + i;
        ProcessThriftSchema(thriftSchema, node, childThriftIndex);
      }

      return node;
    }
    return ProcessThriftSchema(schema);
  }

  private static int MaxDefinitionLevelIncr(this SchemaElement schemaElement)
  {
    // In dremel specification, repeated fields are nullable AND repeated    
    return schemaElement.RepetitionType == RepetitionType.REQUIRED ? 0 : 1;
  }

  private static int MaxRepetitionLevelIncr(this SchemaElement schemaElement)
  {
    return schemaElement.RepetitionType == RepetitionType.REPEATED ? 1 : 0;
  }

  /// <summary>
  /// Gets the max definition level for a schema element node.
  /// </summary>
  /// <param name="schemaElement"></param>
  /// <returns></returns>
  public static int MaxDefinitionLevel(this Node<SchemaElement> schemaElement)
  {
    if (schemaElement.IsRoot)
    {
      return schemaElement.Value.MaxDefinitionLevelIncr();
    }
    else
    {
      return schemaElement.Parent!.MaxDefinitionLevel() + schemaElement.Value.MaxDefinitionLevelIncr();
    }
  }

  public static int MaxRepetitionLevel(this Node<SchemaElement> schemaElement)
  {
    if (schemaElement.IsRoot)
    {
      return schemaElement.Value.MaxRepetitionLevelIncr();
    }
    else
    {
      return schemaElement.Parent!.MaxRepetitionLevel() + schemaElement.Value.MaxRepetitionLevelIncr();
    }
  }

  /// <summary>
  /// Calculates the element group type. 
  /// </summary>
  /// <param name="element">The element</param>
  /// <returns>Returns the group type of the element.</returns>
  /// <exception cref="Exception">Throws an exception if the group types cannot be determined.</exception>
  public static SchemaElementGroupType GetSchemaElementGroupType(this SchemaElement element)
  {
    if (element.Type is not null)
    {
      return SchemaElementGroupType.PRIMITIVE;
    }
    else if (element.NumChildren > 0 && element.Type is null && (element.LogicalType?.LIST is not null || element.ConvertedType == ConvertedType.LIST))
    {
      return SchemaElementGroupType.LIST;
    }
    else if (element.NumChildren > 0 && element.Type is null && (element.LogicalType?.MAP is not null || element.ConvertedType == ConvertedType.MAP))
    {
      return SchemaElementGroupType.MAP;
    }
    else if (element.NumChildren > 0 && element.Type is null)
    {
      return SchemaElementGroupType.STRUCT;
    }
    else
    {
      throw new Exception($"Unable to determine SchemaElementGroupType for {element.Name}.");
    }
  }
  public static Node<LogicalSchemaElement> ToLogicalSchemaElementNode(this Node<SchemaElement> schemaElementNode)
  {
    return schemaElementNode.ConvertTo(new LogicalSchemaElementConverter());
  }

  #endregion
}