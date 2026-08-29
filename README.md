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

## Starting the Journey
To start building a Parquet library, we need to start with as much existing documentation as possible. The following are good starting points:
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
Parquet files are somewhat like non-human-readable csv files on steroids. Csv files could be used for many tasks that Parquet is currently used for - however, Parquet files have at least 2 major advantages over csv:
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

### Nullable Columns, Repeated Columns, Defition Levels and Repetition Levels
Parquet supports the following types of data:
- Null / optional values
- Repeated values
- Nested objects

For example the following structure is possible:
```
optional group person {
    optional group address {
        optional int32 zip;
    }
}
```
The top level persion object includes multiple / optional addresses, and each of those has an optional zip code. The data has nested objects within it. Some data storage systems will store the entire object from the root together. Parquet does not work that way. in order to allow for efficient data storage, Parquet adopts a flat columnar structure where each nested / leaf column is stored separately. Parquet then re-constitutes objects as needed.

In order to do this, 2 additional pieces of information are stored
- Definition Level: This indicates how many optional fields in the schema path are actually present. A level of 0 means the value is null at the highest possible level. A higher number means more nested fields are defined.
- Repetition Level: This tells the reader when a new item in a repeated field (a list) begins. A level of 0 marks the start of a new record.

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

Dremel encoding is used for nested structures. Some key ideas of the Dremel encoding include:
- Fields are stored in separate column stripes
- Nested records are split across multiple column stripes
- Repetition levels indicate at what repeated field a value appears
- Definition levels indicate what level of nesting a value is defined for

The dremel paper can be found here: https://static.googleusercontent.com/media/research.google.com/en//pubs/archive/36632.pdf
An example can be found here: https://github.com/julienledem/redelm/wiki/The-striping-and-assembly-algorithms-from-the-Dremel-paper

Repetition and definition level sections are optional. They are present only when the column’s max repetition level > 0 or max definition level > 0, which is determined entirely by the schema.

If a column is required and non‑repeated, then:
- max definition level = 0
- max repetition level = 0
→ no levels are stored in the Data Page

Levels appear in the Data Page only if the schema requires them:
- Definition levels → appear when the field is optional OR the field is a list or repeated group
- Repetition levels → appear when the field is repeated (lists, repeated groups)

Note: groups are implicitly optional, so generate definition levels too.


#### Step-by-Step Algorithm
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

## Reading a Parquet file


