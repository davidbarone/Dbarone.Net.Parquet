using Dbarone.Net.Parquet.Thrift;

public class FileMetaDataHelper
{
  public FileMetaData MetaData { get; set; }
  public FileMetaDataHelper(FileMetaData metaData)
  {
    this.MetaData = metaData;
  }

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
  /// <param name="schemaElement"></param>
  /// <returns></returns>
  public int SchemaElementToColumnChunkIndex(string columnName)
  {
    // first, find the column name in the schema
    var element = this.MetaData.Schema.FirstOrDefault(e => e.Name.Equals(columnName));

    if (element is null)
    {
      throw new Exception($"Cannot find {columnName} in schema");
    }

    var i = this.MetaData.Schema.IndexOf(element);
    int columnChunkIndex = -1;

    // loop from element 1 (ignore root element 0), counting the number of leaf
    // elements
    for (int j = 1; j <= i; j++)
    {
      if (this.MetaData.Schema[j].Type is not null && (this.MetaData.Schema[j].NumChildren ?? 0) == 0)
      {
        // is a leaf node
        columnChunkIndex++;
      }
    }
    return columnChunkIndex;
  }

  public int GetMaxDefinitionLevel(string columnName)
  {
    var columns = this.MetaData.Schema;
    var column = columns.FirstOrDefault(c => c.Name.Equals(columnName));

    column.RepetitionType
  }
}