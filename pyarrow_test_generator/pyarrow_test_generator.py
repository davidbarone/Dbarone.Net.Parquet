import pyarrow as pa
import pyarrow.parquet as pq
import os

# --------------------------------------------
# pyarrow_test_generator.py
#
# Generates a suite of basic parquet files for
# testing.
# --------------------------------------------

OUTPUT_DIR = "parquet_test_files"
os.makedirs(OUTPUT_DIR, exist_ok=True)

# 1. alltypes_plain.parquet
# Simple parquet file with physical types.
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
# Dictionary field.
def gen_alltypes_dictionary():
    schema = pa.schema([
        pa.field("dict_col", pa.dictionary(pa.int32(), pa.string()), nullable=False)
    ])

    table = pa.Table.from_arrays([
        pa.array(
            ["a", "b", "a", "c", "b"],
            type=pa.dictionary(pa.int32(), pa.string())
        )
    ], schema = schema)

    pq.write_table(table, f"{OUTPUT_DIR}/alltypes_dictionary.parquet", compression=None)


# 3. binary.parquet
# Binary field.
def gen_binary():
    schema = pa.schema([
        pa.field("bin_col", pa.binary(), nullable=False)
    ])

    table = pa.Table.from_arrays([
        pa.array([b"a", b"", b"c", b"xyz"])
    ], schema = schema)

    pq.write_table(table, f"{OUTPUT_DIR}/binary.parquet", compression=None)


# 4. nulls.parquet
# Nullable fields (int32 + string).
def gen_nulls():

    schema = pa.schema([
        pa.field("int32", pa.int32(), nullable=True),
        pa.field("string", pa.string(), nullable=True)
    ])

    table = pa.Table.from_arrays([
        pa.array([1, None, 2, None, 3, None], pa.int32()),
        pa.array([None, "x", None, "y", None, "z"])
    ], schema = schema)
    
    pq.write_table(table, f"{OUTPUT_DIR}/nulls.parquet", compression=None)


# 5. nested_list.parquet
# Nested list. By default, both list itself and elements can be null.
# Nullability is annotated by standard 3-part LIST schema structure.
def gen_nested_list():
    list_type = pa.list_(pa.int32())
    table = pa.table({
        "list_int32": pa.array([[1, 2], None, [3], []], type=list_type)
    })
    pq.write_table(table, f"{OUTPUT_DIR}/nested_list.parquet", compression=None)

    pf = pq.ParquetFile(f"{OUTPUT_DIR}/nested_list.parquet")
    schema = pf.schema  # parquet.SchemaDescriptor

    # Get the leaf node for the column
    col = schema.column(0)  # "list_int32"

    # MDL and MRL
    mdl = col.max_definition_level
    mrl = col.max_repetition_level
    print("gen_nested_list")
    print("---------------")
    print(schema)

# 6. nested_list_not_null.parquet
# Similar to nested_list, but the list is not nullable.
# Note subtle change to the nullability of the LIST
# node.
def gen_nested_list_not_null():

    # Create not-nullable list field
    schema = pa.schema([
        pa.field("list_int32", pa.list_(pa.int32()), nullable=False)
    ])

    # Build the array and table
    # note all 4 arrays are not null (they can be empty, or contain null elements though)
    data = {
        "items": [
            [1, 2],
            [None],
            [3,4],
            []
        ]
    }

    items_array = pa.array(data["items"], type=pa.list_(pa.int32()))
    table = pa.Table.from_arrays([items_array], schema=schema)
    pq.write_table(table, f"{OUTPUT_DIR}/nested_list_not_null.parquet", compression=None)

    pf = pq.ParquetFile(f"{OUTPUT_DIR}/nested_list_not_null.parquet")
    schema = pf.schema  # parquet.SchemaDescriptor

    # Get the leaf node for the column
    col = schema.column(0)  # "list_int32"

    # MDL and MRL
    mdl = col.max_definition_level
    mrl = col.max_repetition_level
    print("gen_nested_list_not_null")
    print("------------------------")
    print(schema)

# 7. nested_list_not_null_element.parquet
# list is required and elements also required.
def gen_nested_list_not_null_element():

    # Define non-nullable int32 element
    element_field = pa.field("item", pa.int32(), nullable=False)
    list_type = pa.list_(element_field)

    # Define non-nullable list field
    list_field = pa.field("list_int32", list_type, nullable=False)

    # Build array with explicit type
    values = [[1, 2], [], [3, 4], []]
    array = pa.array(values, type=list_type)

    # Build table and write
    schema = pa.schema([list_field])
    table = pa.table([array], schema=schema)

    pq.write_table(table, f"{OUTPUT_DIR}/nested_list_not_null_element.parquet", compression=None)

    pf = pq.ParquetFile(f"{OUTPUT_DIR}/nested_list_not_null_element.parquet")
    schema = pf.schema  # parquet.SchemaDescriptor

    # Get the leaf node for the column
    col = schema.column(0)  # "list_int32"

    # MDL and MRL
    mdl = col.max_definition_level
    mrl = col.max_repetition_level
    print("gen_nested_list_not_null_element")
    print("--------------------------------")
    print(schema)


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
    gen_nested_list_not_null()
    gen_nested_list_not_null_element()
    gen_nested_struct()
    gen_map()
    gen_snappy()
    gen_delta_binary()
    gen_tiny_rowgroup()

    print(f"Generated Parquet test files in: {OUTPUT_DIR}")


if __name__ == "__main__":
    main()
