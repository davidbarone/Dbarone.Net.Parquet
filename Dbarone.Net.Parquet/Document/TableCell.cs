namespace Dbarone.Net.Parquet.Document;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Dbarone.Net.Parquet.I18n;

/// <summary>
/// Represent a simple value used in Document.
/// </summary>
public class TableCell : IComparable<TableCell>, IEquatable<TableCell>
{
    /// <summary>
    /// Represents a Null type.
    /// </summary>
    public static TableCell Null = new TableCell();

    /// <summary>
    /// Indicate DataType of this value.
    /// </summary>
    public DocumentType Type { get; }

    /// <summary>
    /// Get internal .NET value object.
    /// </summary>
    public virtual object? RawValue { get; }

    #region Constructors

    public TableCell()
    {
        this.Type = DocumentType.Null;
        this.RawValue = null;
    }

    public TableCell(bool value)
    {
        this.Type = DocumentType.Boolean;
        this.RawValue = value;
    }

    public TableCell(Int64 value)
    {
        this.Type = DocumentType.Integer;
        this.RawValue = value;
    }

    public TableCell(Double value)
    {
        this.Type = DocumentType.Real;
        this.RawValue = value;
    }

    public TableCell(DateTime value)
    {
        this.Type = DocumentType.DateTime;
        this.RawValue = value;
    }

    public TableCell(String value)
    {
        this.Type = value == null ? DocumentType.Null : DocumentType.Text;
        this.RawValue = value;
    }

    public TableCell(Byte[] value)
    {
        this.Type = value == null ? DocumentType.Null : DocumentType.Blob;
        this.RawValue = value;
    }

    public TableCell(object? value)
    {
        this.RawValue = value;

        if (value == null) this.Type = DocumentType.Null;
        else if (value is Boolean) this.Type = DocumentType.Boolean;
        else if (value is Byte) this.Type = DocumentType.Integer;
        else if (value is UInt16) this.Type = DocumentType.Integer;
        else if (value is UInt32) this.Type = DocumentType.Integer;
        else if (value is UInt64) this.Type = DocumentType.Integer;
        else if (value is SByte) this.Type = DocumentType.Integer;
        else if (value is Int16) this.Type = DocumentType.Integer;
        else if (value is Int32) this.Type = DocumentType.Integer;
        else if (value is Int64) this.Type = DocumentType.Integer;
        else if (value is Double) this.Type = DocumentType.Real;
        else if (value is Single) this.Type = DocumentType.Real;
        else if (value is decimal) this.Type = DocumentType.Real;
        else if (value is DateTime) this.Type = DocumentType.DateTime;
        else if (value is String) this.Type = DocumentType.Text;
        else if (value is Byte[]) this.Type = DocumentType.Blob;
    }

    #endregion

    #region Convert types

    public bool AsBoolean => (Boolean)this.RawValue!;
    public Int64 AsInteger => Convert.ToInt64(this.RawValue);
    public double AsReal => Convert.ToDouble(this.RawValue);
    public DateTime AsDateTime => Convert.ToDateTime(this.RawValue);
    public Byte[] AsBlob => this.RawValue as Byte[];
    public string AsText => (string)this.RawValue!;

    #endregion

    #region IsTypes

    public bool IsNull => this.Type == DocumentType.Null;

    public bool IsBoolean => this.Type == DocumentType.Boolean;

    public bool IsInteger => this.Type == DocumentType.Integer;

    public bool IsReal => this.Type == DocumentType.Real;

    public bool IsDateTime => this.Type == DocumentType.DateTime;

    public bool IsBlob => this.Type == DocumentType.Blob;

    public bool IsNumber => this.IsInteger || this.IsReal;

    public bool IsText => this.Type == DocumentType.Text;

    #endregion

    #region Implicit Ctor

    // Boolean
    public static implicit operator Boolean(TableCell value)
    {
        return value.AsBoolean;
    }

    // Boolean?
    public static implicit operator Boolean?(TableCell value)
    {
        return value.Type == DocumentType.Null ? null : value.AsBoolean;
    }

    // Boolean
    public static implicit operator TableCell(Boolean value)
    {
        return new TableCell(value);
    }

    // Boolean?
    public static implicit operator TableCell(Boolean? value)
    {
        return (value is null) ? new TableCell() : new TableCell(value.Value);
    }

    // Byte
    public static implicit operator Byte(TableCell value)
    {
        return (Byte)value.AsInteger;
    }

    // Byte?
    public static implicit operator Byte?(TableCell value)
    {
        return value.Type == DocumentType.Null ? null : (Byte)value.AsInteger;
    }

    // Byte
    public static implicit operator TableCell(Byte value)
    {
        return new TableCell(value);
    }

    // Byte?
    public static implicit operator TableCell(Byte? value)
    {
        return new TableCell(value);
    }

    // SByte
    public static implicit operator SByte(TableCell value)
    {
        return (SByte)value.AsInteger;
    }

    // SByte?
    public static implicit operator SByte?(TableCell value)
    {
        return value.Type == DocumentType.Null ? null : (SByte)value.AsInteger;
    }

    // SByte
    public static implicit operator TableCell(SByte value)
    {
        return new TableCell(value);
    }

    // SByte?
    public static implicit operator TableCell(SByte? value)
    {
        return new TableCell(value);
    }

    // Char
    public static implicit operator Char(TableCell value)
    {
        return (Char)value.AsInteger;
    }

    // Char?
    public static implicit operator Char?(TableCell value)
    {
        return value.Type == DocumentType.Null ? null : (Char)value.AsInteger;
    }

    // Char
    public static implicit operator TableCell(Char value)
    {
        return new TableCell(value);
    }

    // Char?
    public static implicit operator TableCell(Char? value)
    {
        return new TableCell(value);
    }

    // Int16
    public static implicit operator Int16(TableCell value)
    {
        return (Int16)value.AsInteger;
    }

    // Int16?
    public static implicit operator Int16?(TableCell value)
    {
        return value.Type == DocumentType.Null ? null : (Int16)value.AsInteger;
    }

    // Int16
    public static implicit operator TableCell(Int16 value)
    {
        return new TableCell(value);
    }

    // Int16?
    public static implicit operator TableCell(Int16? value)
    {
        return new TableCell(value);
    }

    // UInt16
    public static implicit operator UInt16(TableCell value)
    {
        return (UInt16)value.AsInteger;
    }

    // UInt16?
    public static implicit operator UInt16?(TableCell value)
    {
        return value.Type == DocumentType.Null ? null : (UInt16)value.AsInteger;
    }

    // UInt16
    public static implicit operator TableCell(UInt16 value)
    {
        return new TableCell(value);
    }

    // UInt16?
    public static implicit operator TableCell(UInt16? value)
    {
        return new TableCell(value);
    }

    // Int32
    public static implicit operator Int32(TableCell value)
    {
        return (Int32)value.AsInteger;
    }

    // Int32?
    public static implicit operator Int32?(TableCell value)
    {
        return value.Type == DocumentType.Null ? null : (Int32)value.AsInteger;
    }

    // Int32
    public static implicit operator TableCell(Int32 value)
    {
        return new TableCell(value);
    }

    // Int32?
    public static implicit operator TableCell(Int32? value)
    {
        return new TableCell(value);
    }

    // UInt32
    public static implicit operator UInt32(TableCell value)
    {
        return (UInt32)value.AsInteger;
    }

    // UInt32?
    public static implicit operator UInt32?(TableCell value)
    {
        return value.Type == DocumentType.Null ? null : (UInt32)value.AsInteger;
    }

    // UInt32
    public static implicit operator TableCell(UInt32 value)
    {
        return new TableCell(value);
    }

    // UInt32?
    public static implicit operator TableCell(UInt32? value)
    {
        return new TableCell(value);
    }

    // Int64
    public static implicit operator Int64(TableCell value)
    {
        return (Int64)value.AsInteger;
    }

    // Int64?
    public static implicit operator Int64?(TableCell value)
    {
        return value.Type == DocumentType.Null ? null : (Int64)value.AsInteger;
    }

    // Int64
    public static implicit operator TableCell(Int64 value)
    {
        return new TableCell(value);
    }

    // Int64?
    public static implicit operator TableCell(Int64? value)
    {
        return new TableCell(value);
    }

    // UInt64
    public static implicit operator UInt64(TableCell value)
    {
        return (UInt64)value.AsInteger;
    }

    // UInt64?
    public static implicit operator UInt64?(TableCell value)
    {
        return value.Type == DocumentType.Null ? null : (UInt64)value.AsInteger;
    }

    // UInt64
    public static implicit operator TableCell(UInt64 value)
    {
        return new TableCell(value);
    }

    // UInt64?
    public static implicit operator TableCell(UInt64? value)
    {
        return new TableCell(value);
    }

    // Single
    public static implicit operator Single(TableCell value)
    {
        return (Single)value.AsReal;
    }

    // Single?
    public static implicit operator Single?(TableCell value)
    {
        return value.Type == DocumentType.Null ? null : (Single)value.AsReal;
    }

    // Single
    public static implicit operator TableCell(Single value)
    {
        return new TableCell(value);
    }

    // Single?
    public static implicit operator TableCell(Single? value)
    {
        return new TableCell(value);
    }

    // DateTime
    public static implicit operator DateTime(TableCell value)
    {
        return DateTime.FromBinary(value.AsInteger);
    }

    // DateTime?
    public static implicit operator DateTime?(TableCell value)
    {
        return value.Type == DocumentType.Null ? null : value.AsDateTime;
    }

    // DateTime
    public static implicit operator TableCell(DateTime value)
    {
        return new TableCell(value);
    }

    // DateTime?
    public static implicit operator TableCell(DateTime? value)
    {
        return new TableCell(value);
    }

    // Double
    public static implicit operator Double(TableCell value)
    {
        return (Double)value.AsReal;
    }

    // Double?
    public static implicit operator Double?(TableCell value)
    {
        return value.Type == DocumentType.Null ? null : (Double)value.AsReal;
    }

    // Double
    public static implicit operator TableCell(Double value)
    {
        return new TableCell(value);
    }

    // Double?
    public static implicit operator TableCell(Double? value)
    {
        return new TableCell(value);
    }

    // Decimal
    public static implicit operator Decimal(TableCell value)
    {
        return (Decimal)value.AsReal;
    }

    // Decimal?
    public static implicit operator Decimal?(TableCell value)
    {
        return value.Type == DocumentType.Null ? null : (Decimal)value.AsReal;
    }

    // Decimal
    public static implicit operator TableCell(Decimal value)
    {
        return new TableCell(value);
    }

    // Decimal?
    public static implicit operator TableCell(Decimal? value)
    {
        return new TableCell(value);
    }

    // Guid
    public static implicit operator Guid(TableCell value)
    {
        return new Guid(value.AsBlob);
    }

    // Guid?
    public static implicit operator Guid?(TableCell value)
    {
        return value.Type == DocumentType.Null ? null : new Guid(value.AsBlob);
    }

    // Guid
    public static implicit operator TableCell(Guid value)
    {
        return new TableCell(value);
    }

    // Guid?
    public static implicit operator TableCell(Guid? value)
    {
        return new TableCell(value);
    }

    // String
    public static implicit operator String(TableCell value)
    {
        return value.AsText;
    }

    // String
    public static implicit operator TableCell(String value)
    {
        return new TableCell(value);
    }

    // Binary
    public static implicit operator Byte[](TableCell value)
    {
        return value.AsBlob;
    }

    // Binary
    public static implicit operator TableCell(Byte[] value)
    {
        return new TableCell(value);
    }

    // +
    public static TableCell operator +(TableCell left, TableCell right)
    {
        if (!left.IsNumber || !right.IsNumber) return TableCell.Null;
        if (left.IsInteger && right.IsNull) return left.AsInteger + right.AsInteger;
        else return left.AsReal + right.AsReal;
    }

    // -
    public static TableCell operator -(TableCell left, TableCell right)
    {
        if (!left.IsNumber || !right.IsNumber) return TableCell.Null;
        if (left.IsInteger && right.IsNull) return left.AsInteger - right.AsInteger;
        else return left.AsReal - right.AsReal;
    }

    // *
    public static TableCell operator *(TableCell left, TableCell right)
    {
        if (!left.IsNumber || !right.IsNumber) return TableCell.Null;
        if (left.IsInteger && right.IsNull) return left.AsInteger * right.AsInteger;
        else return left.AsReal * right.AsReal;
    }

    // /
    public static TableCell operator /(TableCell left, TableCell right)
    {
        if (!left.IsNumber || !right.IsNumber) return TableCell.Null;
        if (left.IsInteger && right.IsNull) return left.AsInteger / right.AsInteger;
        else return left.AsReal / right.AsReal;
    }

    /// <summary>
    /// TO DO: DELETE THIS?
    /// </summary>
    /// <returns></returns>
    public string ToStringJson()
    {
        return JsonSerializer.Serialize(this);
    }

    #endregion

    #region IComparable, IEquatable

    public virtual int CompareTo(TableCell other)
    {
        return this.CompareTo(other, Collation.Binary);
    }

    public virtual int CompareTo(TableCell other, Collation collation)
    {
        // first, test if types are different
        if (this.Type != other.Type)
        {
            // if both values are number, convert them to Decimal (128 bits) to compare
            // it's the slowest way, but more secure
            if (this.IsNumber && other.IsNumber)
            {
                return Convert.ToDecimal(this.RawValue).CompareTo(Convert.ToDecimal(other.RawValue));
            }
            // if not, order by sort type order
            else
            {
                var result = this.Type.CompareTo(other.Type);
                return result < 0 ? -1 : result > 0 ? +1 : 0;
            }
        }

        // for both values with same data type just compare
        switch (this.Type)
        {
            case DocumentType.Null:
                return 0;

            case DocumentType.Boolean: return this.AsBoolean.CompareTo(other.AsBoolean);
            case DocumentType.Integer: return this.AsInteger.CompareTo(other.AsInteger);
            case DocumentType.Real: return this.AsReal.CompareTo(other.AsReal);
            case DocumentType.DateTime: return this.AsDateTime.CompareTo(other.AsDateTime);
            case DocumentType.Text: return collation.Compare(this.AsText, other.AsText);
            case DocumentType.Blob: return this.BinaryCompare(this.AsBlob, other.AsBlob);
            default: throw new NotImplementedException();
        }
    }

    private int BinaryCompare(byte[] lh, byte[] rh)
    {
        if (lh == null) return rh == null ? 0 : -1;
        if (rh == null) return 1;

        var result = 0;
        var i = 0;
        var stop = Math.Min(lh.Length, rh.Length);

        for (; 0 == result && i < stop; i++)
            result = lh[i].CompareTo(rh[i]);

        if (result != 0) return result < 0 ? -1 : 1;
        if (i == lh.Length) return i == rh.Length ? 0 : -1;
        return 1;
    }

    public bool Equals(TableCell other)
    {
        return this.CompareTo(other) == 0;
    }

    #endregion

    #region Operators

    public static bool operator ==(TableCell lhs, TableCell rhs)
    {
        if (object.ReferenceEquals(lhs, null)) return object.ReferenceEquals(rhs, null);
        if (object.ReferenceEquals(rhs, null)) return false; // don't check type because sometimes different types can be ==

        return lhs.Equals(rhs);
    }

    public static bool operator !=(TableCell lhs, TableCell rhs)
    {
        return !(lhs == rhs);
    }

    public static bool operator >=(TableCell lhs, TableCell rhs)
    {
        return lhs.CompareTo(rhs) >= 0;
    }

    public static bool operator >(TableCell lhs, TableCell rhs)
    {
        return lhs.CompareTo(rhs) > 0;
    }

    public static bool operator <(TableCell lhs, TableCell rhs)
    {
        return lhs.CompareTo(rhs) < 0;
    }

    public static bool operator <=(TableCell lhs, TableCell rhs)
    {
        return lhs.CompareTo(rhs) <= 0;
    }

    public override bool Equals(object obj)
    {
        if (obj is TableCell other)
        {
            return this.Equals(other);
        }

        return false;
    }

    #endregion

    public override int GetHashCode()
    {
        var hash = 17;
        hash = 37 * hash + this.Type.GetHashCode();
        hash = 37 * hash + (this.RawValue?.GetHashCode() ?? 0);
        return hash;
    }

    public override string ToString()
    {
        var value = this.RawValue;
        if (value is not null)
        {
            return value.ToString()!;
        }
        else
        {
            return "null";
        }
    }
}
