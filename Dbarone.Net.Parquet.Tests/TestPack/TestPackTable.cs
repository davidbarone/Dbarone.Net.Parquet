using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Parquet;
using Parquet.Schema;
using Xunit;
using System;
using Dbarone.Net.Database;
using Dbarone.Net.Csv;
using System.Linq;
using Dbarone.Net.Database.Tests;
using Dbarone.Net.Extensions;


public class TestPackTable : Dictionary<string, TestPackColumn>
{
  /// <summary>
  /// Generates an enumerable dictionary object from a test pack table.
  /// </summary>
  /// <returns>Returns the test pack table as an enumerable dictionary.</returns>
  public List<Dictionary<string, object?>> GenerateEnumerableDictionary()
  {
    var results = new List<Dictionary<string, object?>>();
    var columns = this.Keys.ToList();
    var dataTypes = this.Values.Select(v => v.DataType).ToList();
    var data = this.Values.Select(v => v.Generator().ToArray()).ToList();
    var rowCount = data[0].Count();
    var columnCount = columns.Count;

    if (columnCount != dataTypes.Count || data.Count != columns.Count)
    {
      throw new Exception("GenerateDataTable - invalid data schema");
    }

    for (int i = 0; i < columnCount; i++)
    {
      if (data[i].Count() != rowCount)
      {
        throw new Exception($"Column {i} has invalid row count");
      }
    }

    // Now create the dataset
    for (var i = 0; i < rowCount; i++)
    {
      Dictionary<string, object?> row = new Dictionary<string, object?>();
      for (var j = 0; j < columnCount; j++)
      {
        row[columns[j]] = data[j][i];
      }
      results.Add(row);
    }
    return results;
  }
}