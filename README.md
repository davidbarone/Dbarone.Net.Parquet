# Dbarone.Net.Parquet
This project was started to understand more about the Apache Parquet format.

## Background
Apache Parquet is a free and open source column-oriented data storage format, used in the Apache Hadoop ecosystem. It was originally released in 2013. Parquet provides efficient compression, and is used where large data volumes are required, in particular for analytics.

Parquet files are column-oriented. In short, this means that when persisting data, which in the basic sense you can consider to be tabular data (although Parquet is not restricted to purely tabular data), instead of storing all columns of a single row of data contiguously on disk, all rows of a single column are stored contiguously on disk. Row-based storage is typically used for online transactional processing (OLTP) relational databases - for example the traditional databases that have been around for decades. Column-based storage is typically used for analytics (online analytics processing or OLAP).

Due the fundamentally different way that data is stored between row-oriented and column-oriented databases, there are some stark pros and cons between the two:

|                          | Row-Oriented                                             | Column-oriented                                                    |
| ------------------------ | -------------------------------------------------------- | ------------------------------------------------------------------ |
| Storage                  | Row(s) stored together, usually in pages (e.g. 8K pages) | Each column stored as 1 unit                                       |
| Optimised for            | OLTP                                                     | OLAP                                                               |
| Single row reads         | Very fast (using B-tree data structures)                 | slower                                                             |
| Single row updates       | Very fast O(log n) (using B-tree data structures)        | very slow - segments of file must be rewritten                     |
| Aggregations / Analytics | Very slow - must read all columns of all rows            | Extremely fast - only reads the columns required in query          |
| Compression              | Low                                                      | Very high - due to various encodings available on per-column basis |

## Where to Start
The following are good starting points for learning about Parquet:
- https://parquet.apache.org/docs/overview/

## High Level File Format
As discussed on this page: https://github.com/apache/parquet-format, the overall format of a Parquet file is as follows:
```
4-byte magic number "PAR1"
<Column 1 Chunk 1>
<Column 2 Chunk 1>
...
<Column N Chunk 1>
<Column 1 Chunk 2>
<Column 2 Chunk 2>
...
<Column N Chunk 2>
...
<Column 1 Chunk M>
<Column 2 Chunk M>
...
<Column N Chunk M>
File Metadata
4-byte length in bytes of file metadata (little endian)
4-byte magic number "PAR1"
```

## Metadata and Thrift
Parquet files can be conceptually thought of as non-human-readable csv files designed for storing large amounts of data efficiently. Csv files could be used for some tasks that Parquet is currently used for - however, Parquet files have at least 2 major advantages over csv:
- The Parquet file format offers huge compression - this is vital when massive data volumes are required (for example in data analytics)
- The Parquet file format is self-describing

Whilst csv files are extremely simple to use, they don't contain any additional metadata, for example:
- The data types of the columns
- The number of rows in a column
- Statistical information about values in the columns, allowing readers to quickly determine when a value is present in the file without having to read the entire file to find out.
- How blank fields should be treated / what null values look like

Parquet contains this information and much more in sections call 'metdata'. There are 2 types of metdata in a Parquet file:
- File Metadata
- Page Header Metadata

Both areas of metadata are encoded and serialised using a protocol called: Thrift Compact Protocol (or TCompactProtocol, or just 'Thrift').

### Thrift

### File Metadata

### Page Header Metadata

## Types
There are in fact 2 sets of types required when talking about Parquet files:
- Physical / primitive types
- Logical types

A good page to read up on this is here: https://deepwiki.com/apache/parquet-format/3.1-physical-and-logical-types

### Physical types
The physical types describe how data is physically written to disk. There are only a handful of these types, shown in the table below:
```
  - BOOLEAN: 1 bit boolean
  - INT32: 32 bit signed ints
  - INT64: 64 bit signed ints
  - INT96: 96 bit signed ints (deprecated; only used by legacy implementations)
  - FLOAT: IEEE 32-bit floating point values
  - DOUBLE: IEEE 64-bit floating point values
  - BYTE_ARRAY: arbitrarily long byte arrays
  - FIXED_LEN_BYTE_ARRAY: fixed length byte arrays
```
This list is intentially small to allow Parquet readers / writers to be simple. Other logical types as we will see are supported, but get 'converted' to one of the above types.

### Logical Types
A wider variety of logical types exist. The list of supported types is found in the `LogicalType` enum. Note that this replaces the deprecated `ConvertedType` enum. The following logical types, and their mappings to (physical) type, and CLR type is shown below:

| Logical Type      | Description                                                                        | Physical Type        | .NET CLR Type   |
| ----------------- | ---------------------------------------------------------------------------------- | -------------------- | --------------- |
| STRING            | Interpreted as UTF-8 encoded character string                                      | BYTE_ARRAY           | System.String   |
| ENUM              | TBD                                                                                |                      |                 |
| UUID              | 16-byte universally unique identifier                                              | FIXED_LEN_BYTE_ARRAY | System.Guid     |
| INTEGER(8,true)   | 8-bit signed integer                                                               | INT32                | System.Int8     |
| INTEGER(16,true)  | 16-bit signed integer                                                              | INT32                | System.Int16    |
| INTEGER(32,true)  | 32-bit signed integer                                                              | INT32                | System.Int32    |
| INTEGER(64,true)  | 64-bit signed integer                                                              | INT64                | System.Int64    |
| INTEGER(8,false)  | 8-bit unsigned integer                                                             | INT32                | System.UInt8    |
| INTEGER(16,false) | 16-bit unsigned integer                                                            | INT32                | System.UInt16   |
| INTEGER(32,false) | 32-bit unsigned integer                                                            | INT32                | System.UInt32   |
| INTEGER(64,false) | 64-bit unsigned integer                                                            | INT64                | System.UInt64   |
| DECIMAL           | arbitrary-precision signed decimal numbers of the form unscaledValue * 10^(-scale) | TBD                  | TBD             |
| FLOAT16           | half-precision floating-point numbers in the 2-byte IEEE little-endian format      | TBD                  | TBD             |
| DATE              | Date without a time. Equivalent to number of days from Unix epoch, 1 January 1970  | INT32                | System.DateOnly |
| TIME              |                                                                                    |                      | s               | k |

### Encodings
Parquet provides a number of encodings. Generally an encoding provides a different way to encode the values on disk. PLAIN encoding is the default encoder. Other encodings provide compression benefits. The table below shows which encodings are available for which physical types. The table also shows which encodings are currently supported in this project:

| Encoding                | Enum | BOOLEAN | INT32 | INT64 | INT96 | FLOAT | DOUBLE | BYTE_ARRAY | FIXED_LEN_BYTE_ARRAY |
| ----------------------- | ---- | ------- | ----- | ----- | ----- | ----- | ------ | ---------- | -------------------- |
| PLAIN                   | 0    | YES     | YES   | YES   | **    | YES   | YES    | YES        | YES                  |
| PLAIN_DICTIONARY        | 2    | **      | **    | **    |       | **    | **     | **         | **                   |
| RLE_DICTIONARY          | 8    |         | YES   | YES   |       | YES   | YES    | YES        | YES                  |
| RLE                     | 3    |         |       |       |       |       |        |            |                      |
| DELTA_BINARY_PACKED     | 5    |         | YES   | YES   |       |       |        |            |                      |
| DELTA_LENGTH_BYTE_ARRAY | 6    |         |       |       |       |       |        |            |                      |
| DELTA_STRINGS           | 7    |         |       |       |       |       |        |            |                      |
| BYTE_STREAM_SPLIT       | 9    |         |       |       |       |       |        |            |                      |

Key
- YES: Valid encoding for type, and supported in this project
- *: Valid encoding, but not yet supported in this project
- **: Deprecated encoding / type. Not implemented in this project

Refer:
- https://parquet.apache.org/docs/file-format/data-pages/encodings/
- 

### Dremel: Nullable Columns, Repeated Columns, Defition Levels and Repetition Levels
Parquet supports nested data structures. You are not limited to storing only primitive scalar types in columns - columns can be objects with nested objects within them. You can also include arrays or lists of values within columns. These complex values can be combined to arbitrary depth levels.

When it comes to storing this complex data, different database engines and formats choose different ways to physically store the nested data structures. Parquet's approach is to use an encoding described in a Google research paper known as Dremel, which can be read below:

https://research.google/pubs/pub36632/

The Dremel paper discusses a column-striped storage process. This allows for nested structures (including lists and arrays) to be decomposed to primitive types for storage. The decomposed data can be re-assembled back to the original complex data in a reverse operation. There are 2 main operations discussed in the Dremel paper:
- Dissecting / Shredding: Converting complex data structures to striped columns for secondary storage
- Assembly: Converting striped secondary storage data back into compex data structures

#### Data Model
In order to encode using Dremel, a dataset's structure or schema must be described with various properties:
- A field can be an atomic/primitive type or a record type (called a group).
- Record types can contain 1 or more fields.
- Each field has 3 attributes:
  - Name
  - Type
  - Repetition Type
- The repetition type can be one of the following:
  - Required: exactly 1 occurence
  - Optional: 0 or 1 occurence
  - Repeated: 0 or more occurences

In order to convert between a flattened / striped set of columns and a complex structure containing optional or null values, and lists at different levels, we need 2 pieces of additional information also discussed in the dremel paper: repetition levels and definition levels

#### Repetition Levels
Where a leaf column contains values for fields that are to be interpreted as lists, the values alone don't tell you which values are for which lists - all values are flattened into a single list when stored. We need to be reassemble the flatted list of values into indidividual lists at the correct place in the data object graph. Additional information is required to denote when to start new lists. The repetition level tells us where at what level a value is repeated.

To calculate the repetition level for a value in a field, we first need to calculate the max repetition level for the field. This is calculated based on the schema alone, not the values, by adding up all the repeated levels from the root down to the field. The max repetition level is therefore a number between 0 and n. interpreting repetition levels, the values mean the following:
- 0: We haven't seen any repeated fields yet for the record.
- 1: The value starts a new repetition list at the field 1 level down from root.
- 2: The value starts a new repetition list at the field 2 levels down from root.
- n: Only the current leaf field is repeating.

Additional complexities arise with missing / null values. Sometimes records have null values for a parent of the current field, so you need to 'skip' a record entirely. Additional information is required. This is encoded in the definition levels.

Essentially, the repetition level tells the reader at what level's list to add the newly viewed value:
- 0: We are creating a new list at the root level
- 1: We are creating a new list at level 1 from root
- n: The value is being added to the current level

#### Definition Levels
Definition levels are defined for each value of a leaf field with a particular path 'p' (including NULL values) - the definition level is specified as the number of fields in path 'p' that are defined as optional (so could be null), which are actually not null for the current leaf record.

#### General Encoding Rules
- Only leaf columns are stored. Higher-level complex objects are 'reassembled' from the leaf columns
- Each leaf column stores the following data:
  - Values (nulls are not stored)
  - Definition levels (which provide information about the nulls and where they occur in the hierarchy)
  - Repetition levels which describe how lists repeat and when new repetition lists start
  - If the defintion level for a record is less than the max definition / repetition level for the field the record is null
  - if a field is required (no nulls possible), definition levels are not stored
  - Repetition levels are also only stored if required. For example, if no definition levels are required, no repetition levels are required either.

Levels appear in the data page only if the schema requires them:
- Definition levels → appear when the field is optional OR the field is a list or repeated group
- Repetition levels → appear when the field is repeated (lists, repeated groups)

Note: groups are implicitly optional, so generate definition levels too.

#### Storage
Repetition and Definition Levels are stored in each data page, before the encoded values, using RLE encoding. The order is always:
```
[RLE repetition levels]
[RLE definition levels]
[encoded values]
```
Each block is self-contained:
- The RLE header tells you run type + run length.
- Bit width is determined from the schema’s max repetition/definition level.
- The values follow immediately after.

#### Step-by-Step Algorithm
TBD

- Get the leaf column's schema path, e.g:

```
optional group person {
    optional group address {
        optional int32 zip;
    }
}
```
produces:
`person → address → zip`

- For each node in the path, read `SchemaElement.RepetitionType` from the metadata.
- Count the OPTIONAL and REPEATED nodes from the root to the current element. The result is the maximum defintion level (MDL) for the current leaf column.

For a given Max Definition Level of 'n', the following values have the following meanings:
- 0: The root ancestor object (depth=0) has a null value
- 1: The ancestor at depth=1 has a null value
- 2: The ancestor at depth=2 has a null value
- n-1: All parent objects are not null, but the current object is null
- n: The current object is not null 

To calculate the number of bits per definition level entry before RLE/Bit-Packing encoding, use the formula where n = Max Definition Level: `ceil(log2(n+1))`.

Format of Definition Levels:
```
{RLE block length: 4 bytes}{run1}{run2}{run...}
Run1 = {bit width}{header: run length}{value}
Run2 = {bit width}{header: run length}{value}
```
## Reading a Parquet file


## Testing

The testing strategy is to feed a large variety of mainly small test parquet files to validate the reader library. I've gone with the following:
- Using Parquet.NET to generate parquet files, then validate reading using my library
- Using Python and PyArrow to generate many basic parquet files
- Using canonical test Parquet files from the official Parquet source repository

Other test sources I may add in the future include:

### Using Python + PyArrow to generate many basic test Parquet Files
Using the approach of creating many very small Parquet test data files is desireable for several reasons:
- Each test file can test one aspect of a Parquet file in isolation without worrying about other aspects
- Generated files are small enough to also inspect using hex editor, making byte-level debugging much easier
- Easy to isolate edge-case scenarios (e.g. specific encodings)
- Generally avoid noise of large datasets
- You can still set very low row group sizes to test page boundaries

Using Python and PyArrow has several advantages
- Python + PyArrow has huge level of support, so you can be assured that Parquet file generation will support all edge cases
- The PyArrow library is very expressive, and enables a variety of Parquet files to be generated with minimal lines of code
- By self-generating data files (opposed to using existing Parquet files from Apache Parquet project), any licencing restrictions are avoided

The Python environment was set up outside of this project, but involved the following on a Windows setup to run + debug Python files in VSCode:
- Go to https://www.python.org/downloads/
- Download the Python Install Manager 
- Run the Python Install Manager to install latest version of python (3.14.7)
- Go to cmd line, run py to launch python. Can use same parameters as python.exe
- Need to install the pyarrow library using: `py -m pip install pyarrow`
- Installed the Microsoft Python extension for VSCode

A python file like below can then be used to generate a number of basic test datasets:
``` python
import pyarrow as pa
import pyarrow.parquet as pq
import os

OUTPUT_DIR = "parquet_test_files"
os.makedirs(OUTPUT_DIR, exist_ok=True)

# 1. alltypes_plain.parquet
def gen_alltypes_plain():

    schema = pa.schema([
        pa.field("int32", pa.int32(), nullable=False),
        pa.field("int64", pa.int64(), nullable=False),
        pa.field("float", pa.float32(), nullable=False),
        pa.field("double", pa.float64(), nullable=False),
        pa.field("bool", pa.bool_(), nullable=False),
        pa.field("string", pa.string(), nullable=False)
    ])

    table = pa.Table.from_arrays([
        pa.array([1, 2, 3, 4], pa.int32()),
        pa.array([10, 20, 30, 40], pa.int64()),
        pa.array([1.5, 2.1, 3.14, 2.71], pa.float32()),
        pa.array([1.1, 2.2, 3.3, 4.4], pa.float64()),
        pa.array([True, False, False, True]),
        pa.array(["a", "bb", "ccc", "dddd"])
    ], schema = schema)

    pq.write_table(table, f"{OUTPUT_DIR}/alltypes_plain.parquet", use_dictionary=False, compression=None)


# 2. alltypes_dictionary.parquet
def gen_alltypes_dictionary():
    dict_arr = pa.array(
        ["a", "b", "a", "c", "b"],
        type=pa.dictionary(pa.int32(), pa.string())
    )
    table = pa.table({"dict_col": dict_arr})
    pq.write_table(table, f"{OUTPUT_DIR}/alltypes_dictionary.parquet")


# 3. binary.parquet
def gen_binary():
    table = pa.table({
        "bin": pa.array([b"a", b"", None, b"xyz"])
    })
    pq.write_table(table, f"{OUTPUT_DIR}/binary.parquet")


# 4. nulls.parquet
def gen_nulls():
    table = pa.table({
        "ints": pa.array([None, 1, None, 2, None, 3]),
        "strings": pa.array([None, "x", None, "y", None, "z"])
    })
    pq.write_table(table, f"{OUTPUT_DIR}/nulls.parquet")


# 5. nested_list.parquet
def gen_nested_list():
    list_type = pa.list_(pa.int32())
    table = pa.table({
        "list_col": pa.array([[1, 2], None, [3], []], type=list_type)
    })
    pq.write_table(table, f"{OUTPUT_DIR}/nested_list.parquet")


# 6. nested_struct.parquet
def gen_nested_struct():
    struct_type = pa.struct([
        ("a", pa.int32()),
        ("b", pa.string())
    ])
    table = pa.table({
        "struct_col": pa.array([
            {"a": 1, "b": "x"},
            None,
            {"a": 3, "b": None},
            {"a": None, "b": "y"}
        ], type=struct_type)
    })
    pq.write_table(table, f"{OUTPUT_DIR}/nested_struct.parquet")


# 7. map.parquet
def gen_map():
    map_type = pa.map_(pa.string(), pa.int64())
    table = pa.table({
        "map_col": pa.array([
            {"a": 1, "b": 2},
            None,
            {"x": 10},
            {}
        ], type=map_type)
    })
    pq.write_table(table, f"{OUTPUT_DIR}/map.parquet")


# 8. snappy.parquet
def gen_snappy():
    table = pa.table({
        "ints": pa.array([1, 2, 3, 4]),
        "strings": pa.array(["a", "b", "c", "d"])
    })
    pq.write_table(
        table,
        f"{OUTPUT_DIR}/snappy.parquet",
        compression="snappy"
    )


# 9. delta_binary.parquet
def gen_delta_binary():
    # PyArrow automatically chooses DELTA_BINARY_PACKED for int sequences
    table = pa.table({
        "delta_ints": pa.array([1, 2, 3, 10, 11, 12, 100, 101])
    })
    pq.write_table(
        table,
        f"{OUTPUT_DIR}/delta_binary.parquet",
        version="2.6",
        data_page_size=512
    )


# 10. tiny_rowgroup.parquet
def gen_tiny_rowgroup():
    table = pa.table({
        "ints": pa.array([1, 2, 3, 4, 5, 6]),
        "strings": pa.array(["a", "b", "c", "d", "e", "f"])
    })
    pq.write_table(
        table,
        f"{OUTPUT_DIR}/tiny_rowgroup.parquet",
        row_group_size=2
    )


def main():
    gen_alltypes_plain()
    gen_alltypes_dictionary()
    gen_binary()
    gen_nulls()
    gen_nested_list()
    gen_nested_struct()
    gen_map()
    gen_snappy()
    gen_delta_binary()
    gen_tiny_rowgroup()

    print(f"Generated Parquet test files in: {OUTPUT_DIR}")


if __name__ == "__main__":
    main()
```


## To Do
- Compression Algorithms
- Encryption
- Parquet Writer
