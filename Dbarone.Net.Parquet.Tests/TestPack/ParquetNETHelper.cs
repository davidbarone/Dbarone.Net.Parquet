namespace Dbarone.Net.Parquet.Tests;

extern alias ParquetNetAlias;
using ParquetNet = ParquetNetAlias.Parquet;
using ParquetNetSchema = ParquetNetAlias.Parquet.Schema;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Linq;

/// <summary>
/// Provides helper functions for reading / writing using Parquet.NET.
/// Parquet.NET is used as reference / baseline for testing.
/// </summary>
public class ParquetNETHelper
{
  /// <summary>
  /// Generates an in-memory Parquet file using a test pack table.
  /// </summary>
  /// <param name="table">The source test pack table.</param>
  /// <returns></returns>
  public static async Task<byte[]> CreateFromTestPackTable(TestPackTable table)
  {
    var rows = table.GenerateEnumerableDictionary();

    // create schema
    List<ParquetNetSchema.Field> fields = new List<ParquetNetSchema.Field>();
    foreach (var item in table.Keys)
    {
      var name = item;
      var dataType = table[item].DataType;
      var nullable = table[item].Nullable;
      switch (dataType)
      {
        case Type _ when dataType == typeof(Int32):
          fields.Add(new ParquetNetSchema.DataField<int>(name, nullable));
          break;
        case Type _ when dataType == typeof(Int64):
          fields.Add(new ParquetNetSchema.DataField<long>(name, nullable));
          break;
        case Type _ when dataType == typeof(string):
          fields.Add(new ParquetNetSchema.DataField<string>(name, nullable));
          break;
      }
    }
    var schema = new ParquetNetSchema.ParquetSchema(fields);

    // default compression method = snappy
    var options = new ParquetNet.ParquetOptions
    {
      CompressionMethod = ParquetNet.CompressionMethod.None
    };

    MemoryStream ms = new MemoryStream();
    await using (var parquetWriter = await ParquetNet.ParquetWriter.CreateAsync(schema, ms, options: options))
    {
      using (ParquetNet.ParquetRowGroupWriter groupWriter = parquetWriter.CreateRowGroup())
      {
        foreach (var field in schema.Fields)
        {
          var dataField = field as ParquetNet.Schema.DataField;
          if (dataField is not null)
          {
            switch (dataField.ClrType)
            {
              case Type _ when dataField.ClrType == typeof(Int32):
                await groupWriter
                  .WriteAsync<Int32>(
                    (ParquetNetSchema.DataField)field,
                    rows.Select(r => Convert.ToInt32(r[field.Name])).ToArray());
                break;
              case Type _ when dataField.ClrType == typeof(Int64):
                await groupWriter
                  .WriteAsync<Int64>(
                    (ParquetNetSchema.DataField)field,
                    rows.Select(r => Convert.ToInt64(r[field.Name])).ToArray());
                break;
              case Type _ when dataField.ClrType == typeof(string):
                await groupWriter
                  .WriteAsync(
                    (ParquetNetSchema.DataField)field,
                    rows.Select(r => Convert.ToString(r[field.Name])).ToArray()
                  );
                break;
              default:
                throw new Exception($"Cannot write {dataField.ClrType} type.");
            }
          }
        }
      }
    }
    using FileStream fs = new FileStream("test.parquet", FileMode.Create, FileAccess.Write);
    ms.WriteTo(fs);
    return MemoryStreamToByteArray(ms);
  }

  public static async Task<ParquetNet.ParquetReader> Read(byte[] bytes)
  {
    var ms = new MemoryStream(bytes);
    ParquetNet.ParquetReader reader = await ParquetNet.ParquetReader.CreateAsync(ms);
    return reader;
  }

  /// <summary>
  /// Reads data in Parquet.NET object and returns to dictionary list.
  /// </summary>
  /// <param name="reader"></param>
  /// <returns></returns>
  public static async Task<List<Dictionary<string, object?>>> ToEnumerableDictionary(ParquetNet.ParquetReader reader)
  {
    var result = new List<Dictionary<string, object?>>();

    for (int g = 0; g < reader.RowGroupCount; g++)
    {
      using (ParquetNet.ParquetRowGroupReader groupReader = reader.OpenRowGroupReader(g))
      {
        var fields = reader.Schema.GetDataFields();
        var dataAsList = new List<IList<object>>();

        foreach (var field in fields)
        {
          switch (field.ClrType)
          {
            case Type intType when intType == typeof(Int32):
              int[] intValues = new int[groupReader.RowCount];
              await groupReader.ReadAsync<int>(field, intValues);
              dataAsList.Add(intValues.Cast<object>().ToList());
              break;
            case Type longType when longType == typeof(Int64):
              long[] longValues = new long[groupReader.RowCount];
              await groupReader.ReadAsync<long>(field, longValues);
              dataAsList.Add(longValues.Cast<object>().ToList());
              break;
            case Type stringType when stringType == typeof(string):
              string[] stringValues = new string[groupReader.RowCount];
              await groupReader.ReadAsync(field, stringValues);
              dataAsList.Add(stringValues.Cast<object>().ToList());
              break;
          }
        }

        for (int row = 0; row < groupReader.RowCount; row++)
        {
          var dict = new Dictionary<string, object?>();
          for (int col = 0; col < fields.Length; col++)
          {
            dict[fields[col].Name] = dataAsList[col][row] ?? DBNull.Value;
          }
          result.Add(dict);
        }
      }
    }
    return result;
  }

  #region Private methods

  private static byte[] MemoryStreamToByteArray(MemoryStream ms)
  {
    if (ms == null)
      throw new Exception("MemoryStream cannot be null.");

    // Ensure the position is at the beginning
    if (ms.CanSeek)
      ms.Position = 0;

    return ms.ToArray(); // Creates a copy of the data    
  }

  #endregion

}