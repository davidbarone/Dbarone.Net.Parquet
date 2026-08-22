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
- (Physical) types
- Logical types

### (Physical types)
The physical types represent the different formats actually written to disk. There are only a handful of these types, shown in the table below:
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

| Logical Type      | Description                                                                        | Type                 | .NET CLR Type   |
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

## Reading a Parquet file


