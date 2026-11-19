namespace Dbarone.Net.Parquet.Document;

/// <summary>
/// Represents an element in a document schema definition.
/// </summary>
public class TableSchema
{
    /// <summary>
    /// Specifies the attributes.
    /// </summary>
    public IEnumerable<SchemaAttribute> Attributes { get; set; } = new List<SchemaAttribute>();

    public void AddAttribute(string attributeName, DocumentType documentType, bool allowNull, bool isKey)
    {
        var attributeId = (short)this.Attributes.Count();
        var schemaAttribute = new SchemaAttribute(
            attributeId,
            attributeName,
            documentType,
            allowNull,
            isKey);

        this.Attributes = this.Attributes.Union(new SchemaAttribute[] { schemaAttribute });
    }


    /// <summary>
    /// Converts the schema to a table object.
    /// </summary>
    /// <returns></returns>
    public Table ToTable()
    {
        Table t = new Table();
        foreach (var attribute in Attributes)
        {
            t.Add(attribute.ToTableRow());
        }
        return t;
    }

    /// <summary>
    /// Creates a schema from a table.
    /// </summary>
    /// <param name="table">Table structure to create schema from.</param>
    /// <returns></returns>
    public static TableSchema FromTable(Table table)
    {
        TableSchema schema = new TableSchema();
        schema.Attributes = table.Select(row => SchemaAttribute.FromTableRow(row));
        return schema;
    }

    public bool ValidateRow(TableRow row)
    {
        if (this.Attributes == null)
        {
            throw new Exception("Document validation: Attributes are null.");
        }

        var schemaAttributes = this.Attributes.Select(a => a.AttributeName);
        var notNullableSchemaAttributes = this.Attributes.Where(a => !a.AllowNull).Select(a => a.AttributeName);

        var dict = row.RawValue;
        var documentAttributes = dict.Keys;

        // attributes can be missing in document if the schema AllowNull is set.
        var attributesMissingInDocument = notNullableSchemaAttributes.Except(documentAttributes);
        if (attributesMissingInDocument.Any())
        {
            throw new Exception($"Attribute: {attributesMissingInDocument.First()} is not defined in the document.");
        }

        var attributesMissingInSchema = documentAttributes.Except(schemaAttributes);
        if (attributesMissingInSchema.Any())
        {
            throw new Exception($"Attribute: {attributesMissingInSchema.First()} is not defined in the schema.");
        }

        var validAttributes = schemaAttributes.Intersect(documentAttributes);

        // validate cells
        foreach (var attribute in validAttributes)
        {
            var cell = dict[attribute];
            var attr = this.Attributes.First(a => a.AttributeName.Equals(attribute, StringComparison.Ordinal));
            if (attr.AllowNull && cell.Type == DocumentType.Null)
            {
                // valid null
            }
            else if (attr.DocumentType != cell.Type)
            {
                throw new Exception($"Attribute: {attr.AttributeName} contains value: {cell.ToString()} which is an invalid data type.");
            }
        }
        return true;
    }

    public bool ValidateTable(Table table)
    {
        foreach (var row in table)
        {
            ValidateRow(row);
        }
        return true;
    }

    /// <summary>
    /// Returns true if the schema describes a tabular document.
    /// </summary>
    /// <returns>Returns true if a tabular schema</returns>
    public bool IsTabularSchema() => this.Attributes.Count() > 1;

    /// <summary>
    /// Returns true if the schema contains 1 column only.
    /// </summary>
    /// <returns>Returns true if a list schema</returns>
    public bool IsListSchema() => this.Attributes.Count() == 1;
}
