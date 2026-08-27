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
      var encoding = table[item].Encoding;
      switch (dataType)
      {
        case Type _ when dataType == typeof(byte):
          fields.Add(new ParquetNetSchema.DataField<byte>(name, nullable));
          break;
        case Type _ when dataType == typeof(sbyte):
          fields.Add(new ParquetNetSchema.DataField<sbyte>(name, nullable));
          break;
        case Type _ when dataType == typeof(Int16):
          fields.Add(new ParquetNetSchema.DataField<Int16>(name, nullable));
          break;
        case Type _ when dataType == typeof(UInt16):
          fields.Add(new ParquetNetSchema.DataField<UInt16>(name, nullable));
          break;
        case Type _ when dataType == typeof(Int32):
          fields.Add(new ParquetNetSchema.DataField<Int32>(name, nullable));
          break;
        case Type _ when dataType == typeof(UInt32):
          fields.Add(new ParquetNetSchema.DataField<UInt32>(name, nullable));
          break;
        case Type _ when dataType == typeof(Int64):
          fields.Add(new ParquetNetSchema.DataField<Int64>(name, nullable));
          break;
        case Type _ when dataType == typeof(UInt64):
          fields.Add(new ParquetNetSchema.DataField<UInt64>(name, nullable));
          break;
        case Type _ when dataType == typeof(float):
          fields.Add(new ParquetNetSchema.DataField<float>(name, nullable));
          break;
        case Type _ when dataType == typeof(double):
          fields.Add(new ParquetNetSchema.DataField<double>(name, nullable));
          break;
        case Type _ when dataType == typeof(string):
          fields.Add(new ParquetNetSchema.DataField<string>(name, nullable));
          break;
        default:
          throw new Exception($"Error in CreateFromTestPackTable(). Cannot create column for type: {dataType}.");
      }
    }
    var schema = new ParquetNetSchema.ParquetSchema(fields);

    // default compression method = snappy
    var options = new ParquetNet.ParquetOptions
    {
      CompressionMethod = ParquetNet.CompressionMethod.None
    };

    // Set column encoding hints - note that PLAIN cannot be set - it is the default
    foreach (var key in table.Keys)
    {
      if (table[key].Encoding == Thrift.Encoding.DELTA_BINARY_PACKED)
      {
        options.ColumnEncodingHints[key] = ParquetNet.EncodingHint.DeltaBinaryPacked;
      }
      else if (table[key].Encoding == Thrift.Encoding.RLE_DICTIONARY)
      {
        // Note that Parquet.NET still uses PLAIN_DICTIONARY which is deprecated
        options.ColumnEncodingHints[key] = ParquetNet.EncodingHint.Dictionary;
      }
    }

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
              case Type _ when dataField.ClrType == typeof(byte):
                await groupWriter
                  .WriteAsync<byte>(
                    (ParquetNetSchema.DataField)field,
                    rows.Select(r => Convert.ToByte(r[field.Name])).ToArray());
                break;
              case Type _ when dataField.ClrType == typeof(sbyte):
                await groupWriter
                  .WriteAsync<sbyte>(
                    (ParquetNetSchema.DataField)field,
                    rows.Select(r => Convert.ToSByte(r[field.Name])).ToArray());
                break;
              case Type _ when dataField.ClrType == typeof(Int16):
                await groupWriter
                  .WriteAsync<Int16>(
                    (ParquetNetSchema.DataField)field,
                    rows.Select(r => Convert.ToInt16(r[field.Name])).ToArray());
                break;
              case Type _ when dataField.ClrType == typeof(UInt16):
                await groupWriter
                  .WriteAsync<UInt16>(
                    (ParquetNetSchema.DataField)field,
                    rows.Select(r => Convert.ToUInt16(r[field.Name])).ToArray());
                break;
              case Type _ when dataField.ClrType == typeof(Int32):
                await groupWriter
                  .WriteAsync<Int32>(
                    (ParquetNetSchema.DataField)field,
                    rows.Select(r => Convert.ToInt32(r[field.Name])).ToArray());
                break;
              case Type _ when dataField.ClrType == typeof(UInt32):
                await groupWriter
                  .WriteAsync<UInt32>(
                    (ParquetNetSchema.DataField)field,
                    rows.Select(r => Convert.ToUInt32(r[field.Name])).ToArray());
                break;
              case Type _ when dataField.ClrType == typeof(Int64):
                await groupWriter
                  .WriteAsync<Int64>(
                    (ParquetNetSchema.DataField)field,
                    rows.Select(r => Convert.ToInt64(r[field.Name])).ToArray());
                break;
              case Type _ when dataField.ClrType == typeof(UInt64):
                await groupWriter
                  .WriteAsync<UInt64>(
                    (ParquetNetSchema.DataField)field,
                    rows.Select(r => Convert.ToUInt64(r[field.Name])).ToArray());
                break;
              case Type _ when dataField.ClrType == typeof(float):
                await groupWriter
                  .WriteAsync<float>(
                    (ParquetNetSchema.DataField)field,
                    rows.Select(r => Convert.ToSingle(r[field.Name])).ToArray());
                break;
              case Type _ when dataField.ClrType == typeof(double):
                await groupWriter
                  .WriteAsync<double>(
                    (ParquetNetSchema.DataField)field,
                    rows.Select(r => Convert.ToDouble(r[field.Name])).ToArray());
                break;
              case Type _ when dataField.ClrType == typeof(ReadOnlyMemory<char>): // Parquet >6.1 strings
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
            case Type byteType when byteType == typeof(byte):
              byte[] byteValues = new byte[groupReader.RowCount];
              await groupReader.ReadAsync<byte>(field, byteValues);
              dataAsList.Add(byteValues.Cast<object>().ToList());
              break;
            case Type sByteType when sByteType == typeof(sbyte):
              sbyte[] sByteValues = new sbyte[groupReader.RowCount];
              await groupReader.ReadAsync<sbyte>(field, sByteValues);
              dataAsList.Add(sByteValues.Cast<object>().ToList());
              break;
            case Type shortType when shortType == typeof(short):
              short[] shortValues = new short[groupReader.RowCount];
              await groupReader.ReadAsync<short>(field, shortValues);
              dataAsList.Add(shortValues.Cast<object>().ToList());
              break;
            case Type uShortType when uShortType == typeof(ushort):
              ushort[] uShortValues = new ushort[groupReader.RowCount];
              await groupReader.ReadAsync<ushort>(field, uShortValues);
              dataAsList.Add(uShortValues.Cast<object>().ToList());
              break;
            case Type intType when intType == typeof(Int32):
              int[] intValues = new int[groupReader.RowCount];
              await groupReader.ReadAsync<int>(field, intValues);
              dataAsList.Add(intValues.Cast<object>().ToList());
              break;
            case Type uIntType when uIntType == typeof(UInt32):
              uint[] uIntValues = new uint[groupReader.RowCount];
              await groupReader.ReadAsync<uint>(field, uIntValues);
              dataAsList.Add(uIntValues.Cast<object>().ToList());
              break;
            case Type longType when longType == typeof(Int64):
              long[] longValues = new long[groupReader.RowCount];
              await groupReader.ReadAsync<long>(field, longValues);
              dataAsList.Add(longValues.Cast<object>().ToList());
              break;
            case Type uLongType when uLongType == typeof(UInt64):
              ulong[] uLongValues = new ulong[groupReader.RowCount];
              await groupReader.ReadAsync<ulong>(field, uLongValues);
              dataAsList.Add(uLongValues.Cast<object>().ToList());
              break;
            case Type floatType when floatType == typeof(float):
              float[] floatValues = new float[groupReader.RowCount];
              await groupReader.ReadAsync<float>(field, floatValues);
              dataAsList.Add(floatValues.Cast<object>().ToList());
              break;
            case Type doubleType when doubleType == typeof(double):
              double[] doubleValues = new double[groupReader.RowCount];
              await groupReader.ReadAsync<double>(field, doubleValues);
              dataAsList.Add(doubleValues.Cast<object>().ToList());
              break;
            case Type stringType when stringType == typeof(ReadOnlyMemory<char>): // Parquet >6.1 string
              string[] stringValues = new string[groupReader.RowCount];
              await groupReader.ReadAsync(field, stringValues);
              dataAsList.Add(stringValues.Cast<object>().ToList());
              break;
            default:
              throw new Exception($"Error in ToEnumerableDictionary(). Cannot write type: {field.ClrType}.");
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