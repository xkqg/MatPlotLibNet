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
        Assert.Equal($"Array lengths must match. Got {values} and 3 in group 'south'. (Parameter 'from')", error.Message);
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

    /// <summary>A verb whose values stand over x positions, not over named categories (the bump chart), names them so when a
    /// group carries no values.</summary>
    [Fact]
    public void TheCallersNameForThePlaces_ReplacesCategories_WhenTheValuesAreMissing()
    {
        var group = new BarGroup("north", null!);

        var error = Assert.Throws<ArgumentException>(() => group.ThrowIfMisaligned(3, "ranks", "x values"));

        Assert.Equal("Group 'north' carries no values; there are 3 x values. (Parameter 'ranks')", error.Message);
    }

    /// <summary>A list of groups that each carry one value per place passes.</summary>
    [Fact]
    public void AListOfGroups_EachWithOneValuePerPlace_Passes()
    {
        BarGroup[] groups = [new("a", [1.0, 2.0, 3.0]), new("b", [4.0, 5.0, 6.0])];

        Assert.Null(Record.Exception(() => groups.ThrowIfNotOneEach(3, "groups")));
    }

    /// <summary>The first group of the list that does not fit is refused, by its name and the caller's parameter.</summary>
    [Fact]
    public void AListOfGroups_RefusesTheFirstGroupThatDoesNotFit()
    {
        BarGroup[] groups = [new("a", [1.0, 2.0, 3.0]), new("b", [4.0, 5.0]), new("c", [1.0])];

        var error = Assert.Throws<ArgumentException>(() => groups.ThrowIfNotOneEach(3, "groups"));

        Assert.Equal("Array lengths must match. Got 2 and 3 in group 'b'. (Parameter 'groups')", error.Message);
    }

    /// <summary>Every value of every group of a list is an amount, the first one that is not named by the group, the place and its
    /// number; a missing value passes, and a rank can ask for 1 or more.</summary>
    [Fact]
    public void AListOfGroups_RefusesTheFirstValueThatIsNoAmount_ByItsGroupAndPlace()
    {
        BarGroup[] groups = [new("a", [1.0, double.NaN]), new("b", [3.0, -1.0])];
        string[] places = ["north", "south"];
        static string What(LabelledPlaces<string[]> of, int i) => $"The value of '{of.Label}' in '{of.Places[i]}'";

        var error = Assert.Throws<ArgumentOutOfRangeException>(() => groups.ThrowIfAnyNotAmount("groups", places, What));

        Assert.Equal("groups", error.ParamName);
        Assert.StartsWith("The value of 'b' in 'south' must be >= 0 (got -1).", error.Message, StringComparison.Ordinal);
        Assert.Null(Record.Exception(() => groups.ThrowIfAnyNotAmount("groups", places, What, least: -1)));
        var rank = Assert.Throws<ArgumentOutOfRangeException>(() => groups.ThrowIfAnyNotAmount("ranks", places, What, least: 2));
        Assert.StartsWith("The value of 'a' in 'north' must be >= 2 (got 1).", rank.Message, StringComparison.Ordinal);
    }
}
