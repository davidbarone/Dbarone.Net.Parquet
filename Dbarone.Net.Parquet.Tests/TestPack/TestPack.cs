namespace Dbarone.Net.Parquet.Tests;

using System.Collections.Generic;
using System.Data;
using System;
using System.Linq;
using Dbarone.Net.Extensions;
using System.Text.RegularExpressions;

public class TestPack : Dictionary<string, TestPackTable>
{


  /// <summary>
  /// Generates a test pack table from a spec string.
  /// 
  /// The format of the spec string is as follows:
  /// Compression:{compression},...[{column1},{column2},...]
  /// 
  /// The format of a column specification is:
  /// {column name}:{column type}:{value generator}:{encoding}
  /// 
  /// Column Type:
  /// - Must be 
  /// </summary>
  /// <param name="spec"></param>
  /// <returns></returns>
  private TestPackTable SpecToTable(string spec)
  {
    TestPackTable table = new TestPackTable();

    string pattern = @"^(?<props>.*)\[(?<columns>.*?)\]";
    Match match = Regex.Match(spec, pattern);
    if (!match.Success)
    {
      throw new Exception("Not a valid test spec string.");
    }
    var props = match.Groups["props"].Value;
    var columns = match.Groups["columns"].Value;

    // Parse the props
    Dictionary<string, string> propsDict = new Dictionary<string, string>();
    var propsArray = props.Split(",");
    foreach (var propsItem in propsArray)
    {
      var keyValue = propsItem.Split(":");
      propsDict.Add(keyValue[0], keyValue[1]);
    }
    // Parse the columns and add to table
    var columnsArray = columns.Split(",");
    foreach (var columnItem in columnsArray)
    {
      var columnSpec = columnItem.Split(":");
      var column = new TestPackColumn(columnSpec[1], columnSpec[2], columnSpec[3]);
      table[columnSpec[0]] = column;
    }

    return table;
  }

  /// <summary>
  /// Generates the test pack. Note that this method can be modified to return
  /// only a single dataset by entering the name of the dataset in the parameter.
  /// </summary>
  /// <param name="selectedDataset">Set this to the key of an individual test pack item to run only 1 test.</param>
  /// <returns>Returns a test pack of datasets.</returns>
  public TestPack Generate(string? selected = null)
  {
    string[] testPack = new string[]
    {
      "Compression:None[foo:INT32:INT_12345:PLAIN]",
      "Compression:None[foo:INT32:INT_12345:DELTA_BINARY_PACKED]",
      "Compression:None[foo:INT64:INT_12345:PLAIN]",
      "Compression:None[foo:INT64:INT_12345:DELTA_BINARY_PACKED]",
      "Compression:None[foo:INT64:LONG_MAX:PLAIN]",
      "Compression:None[foo:INT64:LONG_MIN:PLAIN]",
      "Compression:None[foo:INT64:INT_111222233333:RLE_DICTIONARY]",
      "Compression:None[foo:INT64:LONG_MAX_REPEAT_1000000:RLE_DICTIONARY]",
      "Compression:None[foo:STRING:STR_ABCDEFG:PLAIN]",
      "Compression:None[foo:STRING:STR_ABCABCABC:PLAIN]",
      "Compression:None[foo:STRING:STR_ABCDEFG:RLE_DICTIONARY]",
      "Compression:None[foo:STRING:STR_ABCABCABC:RLE_DICTIONARY]"
    };

    var filtered = testPack.Where(t => (selected is null || selected == "") || t.Equals(selected)).ToArray();

    TestPack tp = new TestPack();
    foreach (var item in filtered)
      tp.Add(item, SpecToTable(item));

    return tp;
  }
}
