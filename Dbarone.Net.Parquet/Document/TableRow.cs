namespace Dbarone.Net.Parquet.Document;

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dbarone.Net.Extensions;

/// <summary>
/// Represents a document as a dictionary of string / <see cref="TableCell"/> pairs.
/// </summary>
public class TableRow : IDictionary<string, TableCell>, IComparable<TableRow>, IEquatable<TableRow>
{
    public IDictionary<string, TableCell> RawValue = new Dictionary<string, TableCell>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Creates an empty table row.
    /// </summary>
    public TableRow() { }

    /// <summary>
    /// Creates a table row with a single column cell.
    /// </summary>
    /// <param name="columnName"></param>
    /// <param name="cell"></param>
    public TableRow(string columnName, TableCell cell)
    {
        this.Add(columnName, cell);
    }

    /// <summary>
    /// Creates a table row with a single column value.
    /// </summary>
    /// <param name="columnName"></param>
    /// <param name="value"></param>
    public TableRow(string columnName, object value) : this(columnName, new TableCell(value))
    { }

    /// <summary>
    /// Creates a new document using a dictionary of values.
    /// </summary>
    /// <param name="dict">The dictionary containing the values.</param>
    /// <exception cref="ArgumentNullException">Throws an error if a null dictionary value is passed in.</exception>
    public TableRow(ConcurrentDictionary<string, TableCell> dict)
        : this()
    {
        if (dict == null) throw new ArgumentNullException(nameof(dict));

        foreach (var element in dict)
        {
            this.Add(element);
        }
    }

    public TableRow(IDictionary<string, object> dict) : this()
    {
        if (dict == null) throw new ArgumentNullException(nameof(dict));

        foreach (var element in dict)
        {
            if (element.Value is null)
            {
                this[element.Key] = new TableCell();
            }
            else
            {
                this[element.Key] = new TableCell(element.Value);
            }
        }
    }

    /// <summary>
    /// Creates a new document using a dictionary of values.
    /// </summary>
    /// <param name="dict">The dictionary containing the values.</param>
    /// <exception cref="ArgumentNullException">Throws an error if a null dictionary value is passed in.</exception>
    public TableRow(IDictionary<string, TableCell> dict)
        : this()
    {
        if (dict == null) throw new ArgumentNullException(nameof(dict));

        foreach (var element in dict)
        {
            this.Add(element);
        }
    }

    /// <summary>
    /// Get / set a field for document. Fields are case sensitive
    /// </summary>
    public TableCell this[string key]
    {
        get
        {
            return this.RawValue.GetOrDefault(key, TableCell.Null);
        }
        set
        {
            this.RawValue[key] = value ?? TableCell.Null;
        }
    }

    #region IDictionary

    public ICollection<string> Keys => this.RawValue.Keys;

    public ICollection<TableCell> Values => this.RawValue.Values;

    public int Count => this.RawValue.Count;

    public bool IsReadOnly => false;

    public bool ContainsKey(string key) => this.RawValue.ContainsKey(key);

    /// <summary>
    /// Get all document elements.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<KeyValuePair<string, TableCell>> GetElements()
    {
        foreach (var item in this.RawValue)
        {
            yield return item;
        }
    }

    /// <summary>
    /// Adds a new member to the document.
    /// </summary>
    /// <param name="key">The new member key.</param>
    /// <param name="value">The new member value.</param>
    public void Add(string key, TableCell value) => this.RawValue.Add(key, value ?? TableCell.Null);

    /// <summary>
    /// Removes a member from the document.
    /// </summary>
    /// <param name="key">The member key to remove.</param>
    public bool Remove(string key) => this.RawValue.Remove(key);

    public void Clear() => this.RawValue.Clear();

    public bool TryGetValue(string key, out TableCell value) => this.RawValue.TryGetValue(key, out value);

    public void Add(KeyValuePair<string, TableCell> item) => this.Add(item.Key, item.Value);

    public bool Contains(KeyValuePair<string, TableCell> item) => this.RawValue.Contains(item);

    public bool Remove(KeyValuePair<string, TableCell> item) => this.Remove(item.Key);

    public IEnumerator<KeyValuePair<string, TableCell>> GetEnumerator() => this.RawValue.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.RawValue.GetEnumerator();

    public void CopyTo(KeyValuePair<string, TableCell>[] array, int arrayIndex)
    {
        ((ICollection<KeyValuePair<string, TableCell>>)this.RawValue).CopyTo(array, arrayIndex);
    }

    public void CopyTo(TableRow other)
    {
        foreach (var element in this)
        {
            other[element.Key] = element.Value;
        }
    }

    public bool Equals(TableRow? other)
    {
        throw new NotImplementedException();
    }

    public int CompareTo(TableRow? other)
    {
        if (this is null || other is null)
        {
            throw new NotImplementedException();
        }

        var thisKeys = this.Keys.ToArray();
        var thisLength = thisKeys.Length;

        var otherKeys = other.Keys.ToArray();
        var otherLength = otherKeys.Length;

        var result = 0;
        var i = 0;
        var stop = Math.Min(thisLength, otherLength);

        for (; 0 == result && i < stop; i++)
            result = this[thisKeys[i]].CompareTo(other[thisKeys[i]]);

        // are different
        if (result != 0) return result;

        // test keys length to check which is bigger
        if (i == thisLength) return i == otherLength ? 0 : -1;

        return 1;
    }

    #endregion
}
