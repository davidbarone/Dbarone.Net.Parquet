namespace Dbarone.Net.Parquet.Tests;

using System.Collections.Generic;
using System.Data;
using System;
using System.Linq;
using Dbarone.Net.Extensions;

public class TestPack : Dictionary<string, TestPackTable>
{
  /// <summary>
  /// Generates the test pack. Note that this method can be modified to return
  /// only a single dataset by entering the name of the dataset in the parameter.
  /// </summary>
  /// <param name="selectedDataset">Set this to the key of an individual test pack item to run only 1 test.</param>
  /// <returns>Returns a test pack of datasets.</returns>
  public TestPack Generate(string? selected = null)
  {
    // Get test pack
    var results = new TestPack
    {
      {
        "Delta Binary Packed - Int32 1-5",
        new TestPackTable {{"foo", new TestPackColumn(typeof(Int32), () => Enumerable.Range(1, 5).Select(n=>(object)n))}}
      },
      {
        "Delta Binary Packed - Int64 1-5",
        new TestPackTable {{"foo", new TestPackColumn(typeof(Int64), () => Enumerable.Range(1, 5).Select(n=>(object)n))} }
      },
      {
        "Delta Binary Packed - Int64 Long.Max",
        new TestPackTable {{"foo", new TestPackColumn(typeof(Int64), () => new object[] { long.MaxValue })} }
      },
      {
        "Delta Binary Packed - Int64 Long.Min",
        new TestPackTable {{"foo", new TestPackColumn(typeof(Int64), () => new object[] { long.MinValue })} }
      },
      {
        "Dictionary/RLE - Simple Int32",
        new TestPackTable {{"foo", new TestPackColumn(typeof(Int32), () => new object[] { 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 3 }) } }
      },
      {
        "Dictionary/RLE - Long.MaxValue * 1,000,000",
        new TestPackTable {{"foo", new TestPackColumn(typeof(Int64), () => Enumerable.Repeat(long.MaxValue,1000000).Select(n => (object)n)) } }
      },
      {
        "Plain Encoding - String #1 No Definition/Repetition Levels",
        new TestPackTable {{"foo", new TestPackColumn(typeof(string), () => new object[] { "A", "B", "C", "D", "E", "F", "G" }, false) } }
      },
      {
        "Plain Encoding - String #2 No Definition/Repetition Levels",
        new TestPackTable {{"foo", new TestPackColumn(typeof(string), () => new object[] { "A", "B", "C", "A", "B", "C", "A", "B", "C" }, false) } }
      }
    };

    var filtered = results.Where(kvp => (selected is null || selected == "") || kvp.Key.Equals(selected)).ToDictionary();

    TestPack tp = new TestPack();
    foreach (var item in filtered)
      tp.Add(item.Key, item.Value);

    return tp;
  }
}
