namespace Dbarone.Net.Parquet.I18n;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using Dbarone.Net.Buffers.Document;

/// <summary>
/// Specifies collation comparisons for defined culture settings. Default CurrentCulture with IgnoreCase.
/// </summary>
public class Collation : IComparer<TableCell>, IComparer<string>, IEqualityComparer<TableCell>
{
    private readonly CompareInfo _compareInfo;

    public Collation(string collation)
    {
        var parts = collation.Split('/');
        var culture = parts[0];
        var sortOptions = parts.Length > 1 ?
            (CompareOptions)Enum.Parse(typeof(CompareOptions), parts[1]) :
            CompareOptions.None;

        this.LCID = Dbarone.Net.Document.LCID.GetLCID(culture);
        this.SortOptions = sortOptions;
        this.Culture = new CultureInfo(culture);

        _compareInfo = this.Culture.CompareInfo;
    }

    public Collation(int lcid, CompareOptions sortOptions)
    {
        this.LCID = lcid;
        this.SortOptions = sortOptions;
        this.Culture = Dbarone.Net.Document.LCID.GetCulture(lcid);

        _compareInfo = this.Culture.CompareInfo;
    }

    public static Collation Default = new Collation(Dbarone.Net.Document.LCID.Current, CompareOptions.IgnoreCase);

    public static Collation Binary = new Collation(127 /* Invariant */, CompareOptions.Ordinal);

    /// <summary>
    /// Get LCID code from culture
    /// </summary>
    public int LCID { get; }

    /// <summary>
    /// Get database language culture
    /// </summary>
    public CultureInfo Culture { get; }

    /// <summary>
    /// Get options to how string should be compared in sort
    /// </summary>
    public CompareOptions SortOptions { get; }

    /// <summary>
    /// Compare 2 string values using current culture/compare options
    /// </summary>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value</param>
    /// <returns>Returns 0 if the values are equal, <0 if left is less than right and >0 if left is greater than right.</returns>
    public int Compare(string left, string right)
    {
        var result = _compareInfo.Compare(left, right, this.SortOptions);

        return result < 0 ? -1 : result > 0 ? +1 : 0;
    }

    public int Compare(TableCell left, TableCell right)
    {
        return left.CompareTo(right, this);
    }

    public bool Equals(TableCell x, TableCell y)
    {
        return this.Compare(x, y) == 0;
    }

    public int GetHashCode(TableCell obj)
    {
        return obj.GetHashCode();
    }

    public override string ToString()
    {
        return this.Culture.Name + "/" + this.SortOptions.ToString();
    }
}
