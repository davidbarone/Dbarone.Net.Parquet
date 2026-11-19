namespace Dbarone.Net.Parquet.Document;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Table : IList<TableRow>
{
    IList<TableRow> RawValue = new List<TableRow>();

    /// <summary>
    /// If set, the table has a fixed schema. Otherwise the table is schemaless.
    /// </summary>
    public TableSchema? Schema { get; set; }

    public Table() { }

    public Table(List<TableRow> array)
    {
        if (array == null) throw new ArgumentNullException(nameof(array));

        this.AddRange(array);
    }

    public Table(params TableRow[] array)
    {
        if (array == null) throw new ArgumentNullException(nameof(array));
        this.AddRange(array);
    }

    public Table(IEnumerable<TableRow> rows)
    {
        if (rows == null) throw new ArgumentNullException(nameof(rows));
        this.AddRange(rows);
    }

    public TableRow this[int index]
    {
        get
        {
            return this.RawValue[index];
        }
        set
        {
            this.RawValue[index] = value;
        }
    }

    public int Count => this.RawValue.Count;

    public bool IsReadOnly => false;

    public void Add(TableRow row) => this.RawValue.Add(row);

    public void AddRange<TCollection>(TCollection collection)
        where TCollection : ICollection<TableRow>
    {
        if (collection == null)
            throw new ArgumentNullException(nameof(collection));

        var list = (List<TableRow>)this.RawValue;

        var listEmptySpace = list.Capacity - list.Count;
        if (listEmptySpace < collection.Count)
        {
            list.Capacity += collection.Count;
        }

        foreach (var row in collection)
        {
            list.Add(row);
        }
    }

    public void AddRange(IEnumerable<TableRow> rows)
    {
        if (rows == null) throw new ArgumentNullException(nameof(rows));

        foreach (var row in rows)
        {
            this.Add(row);
        }
    }

    public void Clear() => this.RawValue.Clear();

    public bool Contains(TableRow row) => this.RawValue.Contains(row);

    public void CopyTo(TableRow[] array, int arrayIndex) => this.RawValue.CopyTo(array, arrayIndex);

    public IEnumerator<TableRow> GetEnumerator() => this.RawValue.GetEnumerator();

    public int IndexOf(TableRow row) => this.RawValue.IndexOf(row);

    public void Insert(int index, TableRow row) => this.RawValue.Insert(index, row);

    public bool Remove(TableRow row) => this.RawValue.Remove(row);

    public void RemoveAt(int index) => this.RawValue.RemoveAt(index);


    public void Slice(int start, int length)
    {
        this.RawValue = new List<TableRow>(RawValue.Skip(start).Take(length));
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        foreach (var value in this.RawValue)
        {
            yield return value;
        }
    }

    /// <summary>
    /// Converts the table object to a Enumerable sequence of dictionary objects.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Dictionary<string, object?>> ToDictionaryEnumerable()
    {
        if (this is null)
        {
            throw new Exception("whoops");
        }

        foreach (var row in this)
        {
            Dictionary<string, object?> dict = new Dictionary<string, object?>();
            foreach (var kvp in row)
            {
                dict[kvp.Key] = kvp.Value.RawValue ?? DBNull.Value;
            }
            yield return dict;
        }
    }

    /// <summary>
    /// Validates the table if a schema is set.
    /// </summary>
    /// <returns>Returns true if the data conforms to the schema. Otherwise, throws an exception.</returns>
    public bool IsValid => this.Schema is not null ? this.Schema.ValidateTable(this) : true;
}
