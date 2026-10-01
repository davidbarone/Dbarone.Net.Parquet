namespace Dbarone.Net.Parquet.Tests;

using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using System;
using System.Linq;
using Dbarone.Net.Csv;
using Dbarone.Net.Buffers.Document;
using Dbarone.Net.Parquet.Thrift;
using Dbarone.Net.Parquet.Serialization;
using Dbarone.Net.Parquet.Extensions;

/// <summary>
/// To test Parquet serialization module, we use the Parquet.NET
/// library (https://www.nuget.org/packages/Parquet.Net) as a
// validation tool.
/// </summary>
public class ParquetSerializerTests
{
  /// <summary>
  /// Gets the data for tests. Used to drive a theory-based test pack.
  /// </summary>
  /// <returns></returns>
  public static IEnumerable<object[]> GetData(string? selected = null)
  {
    TestPack testPack = new TestPack().Generate(selected);
    List<object[]> results = new List<object[]>();
    foreach (var kvp in testPack)
    {
      results.Add(new object[]
      {
        (
          name: kvp.Key,
          table: kvp.Value
        )
      });
    }
    return results;
  }

  private void VerifyColumnEncoding(TestPackTable table, Dbarone.Net.Parquet.Thrift.FileMetaData metadata)
  {
    // checks columns in test pack table and Parquet.NET match with encodings
    foreach (var key in table.Keys)
    {
      var schemaElement = metadata.Schema.SingleOrDefault(e => e.Name.Equals(key));
      if (schemaElement is null)
      {
        throw new Exception($"Column: {key} does not exist in Parquet.Net.");
      }
      var i = metadata.Schema.IndexOf(schemaElement);
      var pathInSchema = metadata.GetSchemaPaths()[i];
      // Get the column chunk for the schema element
      var chunk_idx = metadata.GetColumnChunkIndex(pathInSchema);
      var parquetNetEncodings = metadata.RowGroups[0].Columns[chunk_idx.Value].Metadata.Encodings;
      if (parquetNetEncodings.Contains(table[key].Encoding))
      {
        // nop
      }
      else if (parquetNetEncodings.Contains(Encoding.PLAIN_DICTIONARY) && table[key].Encoding == Encoding.RLE_DICTIONARY)
      {
        // Note that Parquet.NET still uses PLAIN_DICTIONARY which is deprecated.
        // Dbarone.Net.Parquet uses RLE_DICTIONARY which is the correct encoding for new files.
        // nop
      }
      else
      {
        throw new Exception("Encoding: {table[key].Encoding} not in parquet file.");
      }
    }
  }

  /// <summary>
  /// Tests reading of parquet files.
  /// </summary>
  /// <param name="name">The name of the method that generates test</param>
  /// <param name="data"></param>
  /// <returns></returns>
  [Theory]
  [MemberData(nameof(GetData), "")]
  public async Task ParquetReadTest((string name, TestPackTable table) testCase)
  {
    var (name, table) = testCase;

    // Create an in-memory parquet file from the test pack item:
    // for each table in the test pack, we first create an in memory parquet file
    // using Parquet.NET.
    var parquetBytes = await ParquetNETHelper.CreateFromTestPackTable(table);

    // Read the parquet ms using both Parquet.NET and Dbarone.Net.Database
    var parquetNet = await ParquetNETHelper.Read(parquetBytes);
    var parquetDbarone = new ParquetSerializer().Read(parquetBytes);

    if (parquetNet is null)
    {
      Assert.Fail("parquetNet should not be null!");
    }
    else
    {
      var md = parquetNet.Metadata!;
      // Assertions / tests
      Assert.Equal(md.CreatedBy, parquetDbarone.MetaData.CreatedBy);
      Assert.Equal(md.NumRows, parquetDbarone.MetaData.NumRows);
      Assert.Equal(md.RowGroups.Count, parquetDbarone.MetaData.RowGroups.Count);
      Assert.Equal(md.RowGroups[0].TotalByteSize, parquetDbarone.MetaData.RowGroups[0].TotalByteSize);
      Assert.Equal(md.Schema.Count, parquetDbarone.MetaData.Schema.Count);
      Assert.Equivalent(md.Schema.Select(s => s.Name), parquetDbarone.MetaData.Schema.Select(s => s.Name));

      // Check metadata
      VerifyColumnEncoding(table, parquetDbarone.MetaData);

      // Test that the original dataset, and the dataset read by Dbarone.Net.Database are the same:
      var parquetNETData = await ParquetNETHelper.ToEnumerableDictionary(parquetNet);
      var parquetDbaroneData = parquetDbarone.Data.ToDictionaryEnumerable();
      Assert.Equal(parquetNETData, parquetDbaroneData, new DictionaryComparer());
    }
  }


  #region Private helper methods

  /// <summary>
  /// Reads a CSV string, where the data type information is included in the header and
  /// returns a dictionary list.
  /// </summary>
  /// <param name="csvData"></param>
  /// <returns></returns>
  private static List<Dictionary<string, object?>> GetDataset(string csvData)
  {
    var encoding = System.Text.Encoding.UTF8;
    byte[] byteArray = encoding.GetBytes(csvData ?? string.Empty);
    var ms = new MemoryStream(byteArray);
    CsvReader reader = new CsvReader(ms);

    // The column names have the data types. Cast here
    List<Dictionary<string, object?>> results = new List<Dictionary<string, object?>>();
    foreach (var row in reader.Read().ToList())
    {
      Dictionary<string, object?> dict = new Dictionary<string, object?>();
      foreach (var key in row.Keys)
      {
        var name_type = key.Split(":");
        var column_name = name_type[0];
        var dataType = name_type[1];
        switch (dataType.ToLower())
        {
          case "int":
            dict[column_name] = Convert.ToInt32(row[key]);
            break;
          default:
            dict[column_name] = null;
            break;
        }
      }
      results.Add(dict);
    }
    return results;
  }

  #endregion

}
