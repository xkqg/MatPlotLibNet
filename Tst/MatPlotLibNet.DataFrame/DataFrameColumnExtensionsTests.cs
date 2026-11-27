// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Data.Analysis;
using MsDataFrame = Microsoft.Data.Analysis.DataFrame;

namespace MatPlotLibNet.DataFrame.Tests;

/// <summary>The one way every verb of the package reads a column by its name (<see cref="DataFrameColumnExtensions"/>, partly internal to the
/// package and granted to this suite): the column as it is, or its values as numbers or as text, and a name the frame has no column
/// of refused with that name.</summary>
public class DataFrameColumnExtensionsTests
{
    private static MsDataFrame Frame() => new(
        new PrimitiveDataFrameColumn<int>("n", [1, 2, 3]),
        new StringDataFrameColumn("s", ["a", "b", "c"]));

    [Fact]
    public void AColumn_IsReadByItsName_AsNumbers_AsText_OrAsItIs()
    {
        var df = Frame();

        Assert.Equal([1.0, 2.0, 3.0], df.Values("n"));
        Assert.Equal(["a", "b", "c"], df.StringCol("s"));
        Assert.Same(df["s"], df.AnyCol("s"));
    }

    [Fact]
    public void ANameTheFrameHasNoColumnOf_IsRefusedWithThatName()
    {
        var thrown = Assert.Throws<ArgumentException>(() => Frame().AnyCol("missing"));

        Assert.Contains("'missing'", thrown.Message, StringComparison.Ordinal);
        Assert.Equal("column", thrown.ParamName);
    }
}
