namespace Dbarone.Net.Parquet.Document;

/// <summary>
/// Represents an attribute node in a document schema definition.
/// </summary>
public class SchemaAttribute
{
    /// <summary>
    /// The attribute id. Attribute ids must NOT be changed on a type, as they are used to encode objects during the serialisation process.
    /// </summary>
    public short AttributeId { get; set; } = default!;

    /// <summary>
    /// The attribute name.
    /// </summary>
    public string AttributeName { get; set; } = default!;

    public DocumentType DocumentType { get; set; }

    public Boolean AllowNull { get; set; }

    public Boolean IsKey { get; set; }

    public SchemaAttribute(short attributeId, string attributeName, DocumentType documentType, bool allowNull, bool isKey)
    {
        this.AttributeId = attributeId;
        this.AttributeName = attributeName;
        this.DocumentType = documentType;
        this.AllowNull = allowNull;
        this.IsKey = isKey;
    }

    /// <summary>
    /// Creates a new SchemaAttribute from a <see cref="DictionaryDocument"/> instance.
    /// </summary>
    /// <param name="dictionaryDocument"></param>
    public static SchemaAttribute FromTableRow(TableRow row)
    {
        var sa = new SchemaAttribute(
            row["attributeId"],
            row["attributeName"],
            (DocumentType)(int)row["documentType"],
            row["allowNull"],
            row["isKey"]
        );
        return sa;
    }

    /// <summary>
    /// Converts the current SchemaAttribute to a <see cref="DictionaryDocument"/> instance.
    /// </summary>
    /// <returns></returns>
    public TableRow ToTableRow()
    {
        TableRow tr = new TableRow();
        tr.Add("attributeId", this.AttributeId);
        tr.Add("attributeName", this.AttributeName);
        tr.Add("documentType", (int)this.DocumentType);
        tr.Add("allowNull", this.AllowNull);
        tr.Add("isKey", this.IsKey);
        return tr;
    }
}