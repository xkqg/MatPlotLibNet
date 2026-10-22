// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>The one check every composed verb holds a plain array of values to when it is drawn over named categories: one
/// value per category. Three values over four categories would leave a category without a mark, or draw a value under the
/// wrong name.</summary>
public class CategoryValuesExtensionsTests
{
    /// <summary>One value per category passes, and so does no category at all with no value.</summary>
    [Theory]
    [InlineData(3, 3)]
    [InlineData(0, 0)]
    public void OneValuePerCategory_Passes(int values, int categories)
    {
        var error = Record.Exception(() => new double[values].ThrowIfMisaligned(categories, "values"));

        Assert.Null(error);
    }

    /// <summary>Too few or too many values are refused, by both counts and the caller's parameter.</summary>
    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public void MoreOrFewerValues_AreRefusedByName(int values)
    {
        var error = Assert.Throws<ArgumentException>(() => new double[values].ThrowIfMisaligned(3, "counts"));

        Assert.Equal("counts", error.ParamName);
        Assert.Equal($"{values} values were given for 3 categories; there must be one value per category. (Parameter 'counts')", error.Message);
    }

    /// <summary>Values drawn over something else than categories (the days of a calendar) are counted in the caller's
    /// words, in the same sentence.</summary>
    [Fact]
    public void ValuesOverOtherPlaces_AreCountedInTheCallersWords()
    {
        var error = Assert.Throws<ArgumentException>(() => new double[3].ThrowIfMisaligned(4, "values", "dates", "date"));

        Assert.Equal("values", error.ParamName);
        Assert.Equal("3 values were given for 4 dates; there must be one value per date. (Parameter 'values')", error.Message);
    }

    /// <summary>No array at all is refused by the caller's parameter, not with a null reference.</summary>
    [Fact]
    public void NoValuesAtAll_AreRefusedByName()
    {
        var error = Assert.Throws<ArgumentNullException>(() => ((double[])null!).ThrowIfMisaligned(3, "values"));

        Assert.Equal("values", error.ParamName);
    }
}
