// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Data.Analysis;
using MatPlotLibNet.DataFrame;

namespace MatPlotLibNet.DataFrame.Tests;

/// <summary>Verifies <see cref="DataFrameColumnReader"/> column-to-array conversions.</summary>
public class DataFrameColumnReaderTests
{
    // ── ToDoubleArray ─────────────────────────────────────────────────────────

    public static TheoryData<DataFrameColumn, double[]> ExactColumns => new()
    {
        { new PrimitiveDataFrameColumn<int>("x", [1, 2, 3]), [1.0, 2.0, 3.0] },
        { new PrimitiveDataFrameColumn<double>("x", [1.5, 2.5, 3.5]), [1.5, 2.5, 3.5] },
        { new PrimitiveDataFrameColumn<long>("x", [10L, 20L, 30L]), [10.0, 20.0, 30.0] },
    };

    [Theory]
    [MemberData(nameof(ExactColumns))]
    public void ToDoubleArray_AnIntegerOrDoubleColumn_ConvertsAllRows(DataFrameColumn column, double[] expected)
    {
        var result = DataFrameColumnReader.ToDoubleArray(column);
        Assert.Equal(expected, result);
    }

    public static TheoryData<DataFrameColumn, double[]> InexactColumns => new()
    {
        { new PrimitiveDataFrameColumn<float>("x", [1.0f, 2.0f, 3.0f]), [1.0, 2.0, 3.0] },
        { new PrimitiveDataFrameColumn<decimal>("x", [1.1m, 2.2m, 3.3m]), [1.1, 2.2, 3.3] },
    };

    [Theory]
    [MemberData(nameof(InexactColumns))]
    public void ToDoubleArray_AFloatOrDecimalColumn_ConvertsAllRows(DataFrameColumn column, double[] expected)
    {
        var result = DataFrameColumnReader.ToDoubleArray(column);
        Assert.Equal(expected.Length, result.Length);
        for (int i = 0; i < expected.Length; i++) Assert.Equal(expected[i], result[i], 5);
    }

    [Fact]
    public void ToDoubleArray_NullEntry_BecomesNaN()
    {
        var col = new PrimitiveDataFrameColumn<double>("x", [1.0, null, 3.0]);
        var result = DataFrameColumnReader.ToDoubleArray(col);
        Assert.Equal(1.0, result[0]);
        Assert.True(double.IsNaN(result[1]));
        Assert.Equal(3.0, result[2]);
    }

    [Fact]
    public void ToDoubleArray_DateTimeColumn_UsesOADate()
    {
        var dt = new DateTime(2026, 1, 1);
        var col = new PrimitiveDataFrameColumn<DateTime>("d", [dt]);
        var result = DataFrameColumnReader.ToDoubleArray(col);
        Assert.Equal(dt.ToOADate(), result[0], 5);
    }

    // ── ToStringArray ─────────────────────────────────────────────────────────

    [Fact]
    public void ToStringArray_StringColumn_PreservesValues()
    {
        var col = new StringDataFrameColumn("g", ["A", "B", "C"]);
        var result = DataFrameColumnReader.ToStringArray(col);
        Assert.Equal(new[] { "A", "B", "C" }, result);
    }

    [Fact]
    public void ToStringArray_NullEntry_BecomesEmptyString()
    {
        var col = new StringDataFrameColumn("g", ["A", null, "C"]);
        var result = DataFrameColumnReader.ToStringArray(col);
        Assert.Equal("A", result[0]);
        Assert.Equal(string.Empty, result[1]);
        Assert.Equal("C", result[2]);
    }

    [Fact]
    public void ToStringArray_IntColumn_UsesToString()
    {
        var col = new PrimitiveDataFrameColumn<int>("n", [10, 20, 30]);
        var result = DataFrameColumnReader.ToStringArray(col);
        Assert.Equal(new[] { "10", "20", "30" }, result);
    }
}
