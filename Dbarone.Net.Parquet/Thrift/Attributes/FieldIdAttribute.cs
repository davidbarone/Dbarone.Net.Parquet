namespace Dbarone.Net.Parquet.Thrift;

[System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
public sealed class FieldIdAttribute : System.Attribute
{
  // See the attribute guidelines at
  //  http://go.microsoft.com/fwlink/?LinkId=85236
  readonly int fieldId;

  // This is a positional argument
  public FieldIdAttribute(int fieldId)
  {
    this.fieldId = fieldId;
  }

  public int FieldId
  {
    get { return fieldId; }
  }
}