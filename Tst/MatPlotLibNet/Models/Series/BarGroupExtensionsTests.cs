// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>The one check every verb that takes a <see cref="BarGroup"/> holds it to: one value per category. A group of
/// two values over three categories would leave a category without a bar, or draw a value under the wrong name.</summary>
public class BarGroupExtensionsTests
{
    /// <summary>A group with one value per category passes.</summary>
    [Fact]
    public void AGroupWithOneValuePerCategory_Passes()
    {
        var group = new BarGroup("north", [3.0, 4.0, 5.0]);

        var error = Record.Exception(() => group.ThrowIfMisaligned(3, "groups"));

        Assert.Null(error);
    }

    /// <summary>Too few or too many values are refused, by the group's name, both counts and the caller's parameter.</summary>
    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public void AGroupWithMoreOrFewerValues_IsRefusedByName(int values)
    {
        var group = new BarGroup("south", new double[values]);

        var error = Assert.Throws<ArgumentException>(() => group.ThrowIfMisaligned(3, "from"));

        Assert.Equal("from", error.ParamName);
        Assert.Equal($"Group 'south' carries {values} values; there are 3 categories. (Parameter 'from')", error.Message);
    }

    /// <summary>A group whose values are not there at all (<c>new BarGroup("north", null!)</c>, or <c>default</c>) is refused in
    /// the same words by name, not with a null reference deep inside the verb.</summary>
    [Fact]
    public void AGroupWithNoValuesAtAll_IsRefusedByName()
    {
        var group = new BarGroup("north", null!);

        var error = Assert.Throws<ArgumentException>(() => group.ThrowIfMisaligned(3, "to"));

        Assert.Equal("to", error.ParamName);
        Assert.Equal("Group 'north' carries no values; there are 3 categories. (Parameter 'to')", error.Message);
    }

    /// <summary>A verb whose values stand over x positions, not over named categories (the bump chart), says so: the message
    /// keeps its words and takes the caller's name for what the values are drawn over.</summary>
    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public void TheCallersNameForThePlaces_ReplacesCategories_WhenTheCountIsWrong(int values)
    {
        var group = new BarGroup("south", new double[values]);

        var error = Assert.Throws<ArgumentException>(() => group.ThrowIfMisaligned(3, "ranks", "x values"));

        Assert.Equal("ranks", error.ParamName);
        Assert.Equal($"Group 'south' carries {values} values; there are 3 x values. (Parameter 'ranks')", error.Message);
    }

    /// <summary>The same for a group with no values at all.</summary>
    [Fact]
    public void TheCallersNameForThePlaces_ReplacesCategories_WhenTheValuesAreMissing()
    {
        var group = new BarGroup("north", null!);

        var error = Assert.Throws<ArgumentException>(() => group.ThrowIfMisaligned(3, "ranks", "x values"));

        Assert.Equal("Group 'north' carries no values; there are 3 x values. (Parameter 'ranks')", error.Message);
    }
}
