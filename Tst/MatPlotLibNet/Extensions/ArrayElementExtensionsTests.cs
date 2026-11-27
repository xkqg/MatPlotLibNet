// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Extensions;

/// <summary>The one answer to "the i-th value the caller gave for this part, when the array reaches it, else the resting one": a
/// series that takes a value for each of its parts takes it as an optional array that may be shorter than the series, and each part
/// the array does not reach wears the resting value.</summary>
public sealed class ArrayElementExtensionsTests
{
    /// <summary>An element the array reaches (the middle one, the last one) is the element; the part after the last wears the fallback.</summary>
    [Theory]
    [InlineData(1, 20.0)]
    [InlineData(2, 30.0)]
    [InlineData(3, -1.0)]
    public void APartOfAnArrayOfThree_IsTheElementItReachesOrTheFallback(int place, double expected)
    {
        Assert.Equal(expected, new[] { 10.0, 20.0, 30.0 }.ElementOr(place, -1.0));
    }

    [Fact]
    public void NoArrayAtAll_WearsTheFallbackEverywhere()
    {
        double[]? none = null;

        Assert.Equal(7.0, none.ElementOr(0, 7.0));
        Assert.Equal(7.0, none.ElementOr(5, 7.0));
    }

    [Fact]
    public void AnEmptyArray_WearsTheFallback()
    {
        Assert.Equal("none", Array.Empty<string>().ElementOr(0, "none"));
    }

    /// <summary>A place before the first is no place of the array: it falls back instead of failing.</summary>
    [Fact]
    public void ANegativePlace_WearsTheFallback()
    {
        Assert.Equal(-1.0, new[] { 10.0 }.ElementOr(-1, -1.0));
    }

    [Fact]
    public void AFallbackThatIsAReference_IsHandedBackAsItIs()
    {
        var resting = new[] { "a" };
        string[]? names = ["x", "y"];

        Assert.Equal("y", names.ElementOr(1, "z"));
        Assert.Same(resting[0], new[] { "q" }.ElementOr(4, resting[0]));
    }

    [Fact]
    public void ALazyFallback_IsAskedForOnlyWhereTheArrayDoesNotReach()
    {
        int asked = 0;
        string[] names = ["first"];

        string reached = names.ElementOr(0, i => { asked++; return "column " + i; });
        string missed = names.ElementOr(2, i => { asked++; return "column " + i; });

        Assert.Equal("first", reached);
        Assert.Equal("column 2", missed);
        Assert.Equal(1, asked);
    }

    /// <summary>An item that was given no name is called by its place in the list, counted from 1, behind the word that says what is counted
    /// (<c>column 3</c>), or by the bare number where there is no word; one that was given a name keeps it, and a place before the first or
    /// no list at all is an unnamed item too.</summary>
    [Theory]
    [InlineData(0, "column ", "first")]
    [InlineData(1, "column ", "column 2")]
    [InlineData(-1, "column ", "column 0")]
    [InlineData(1, "", "2")]
    public void AnItemWithoutAName_IsCalledByItsPlaceInTheList(int place, string word, string expected)
    {
        string[]? names = ["first"];

        Assert.Equal(expected, names.NameOr(place, word));
        Assert.Equal(expected, (place == 0 ? "first" : null).NameOr(place, word));   // a name that may be missing: the one rule for a list and for a single name
        Assert.Equal(word + (place + 1), ((string[]?)null).NameOr(place, word));
    }

    /// <summary>An item that has no name at all (an image of a figure) is called by its place counted from 1 behind the word, as an item of a
    /// list that was given none is.</summary>
    [Theory]
    [InlineData(0, "image ", "image 1")]
    [InlineData(4, "", "5")]
    public void AnItemThatHasNoName_IsCalledByItsPlace(int place, string word, string expected) =>
        Assert.Equal(expected, place.Numbered(word));

    [Fact]
    public void ALazyFallback_IsAskedForWhenThereIsNoArray()
    {
        string[]? none = null;

        Assert.Equal("slice 4", none.ElementOr(3, i => "slice " + (i + 1)));
    }

    [Fact]
    public void AnOptionalElement_IsTheElementOrNothing()
    {
        double[]? widths = [1.5, 2.5];

        Assert.Equal(2.5, widths.ElementOrNull(1));
        Assert.Null(widths.ElementOrNull(2));
        Assert.Null(((double[]?)null).ElementOrNull(0));
        Assert.Null(widths.ElementOrNull(-1));
    }

    [Fact]
    public void AnOptionalElement_LetsTheCallerChooseTheFallbackLazily()
    {
        Color[]? colors = [Colors.Red];
        int asked = 0;
        Color Resting() { asked++; return Colors.Blue; }

        Assert.Equal(Colors.Red, colors.ElementOrNull(0) ?? Resting());
        Assert.Equal(0, asked);
        Assert.Equal(Colors.Blue, colors.ElementOrNull(1) ?? Resting());
        Assert.Equal(1, asked);
    }

    [Fact]
    public void AListIsReadTheSameWay()
    {
        IReadOnlyList<string>? states = new List<string> { "a", "b" };

        Assert.Equal("b", states.ElementOr(1, "-"));
        Assert.Equal("-", states.ElementOr(2, "-"));
        Assert.Equal("-", ((IReadOnlyList<string>?)null).ElementOr(0, "-"));
        Assert.Equal("-", states.ElementOr(-1, "-"));
    }

    [Fact]
    public void AVector_IsReadLikeItsArray()
    {
        var wind = new Vec([3.0, 4.0]);

        Assert.Equal(4.0, wind.ElementOr(1, 0));
        Assert.Equal(0.0, wind.ElementOr(2, 0));
        Assert.Equal(7.0, default(Vec).ElementOr(0, 7.0));
    }

    [Fact]
    public void AnOptionalElementOfAList_IsTheElementOrNothing()
    {
        IReadOnlyList<int>? digits = new List<int> { 4, 5 };

        Assert.Equal(5, digits.ElementOrNull(1));
        Assert.Null(digits.ElementOrNull(2));
        Assert.Null(digits.ElementOrNull(-1));
        Assert.Null(((IReadOnlyList<int>?)null).ElementOrNull(0));
        Assert.Equal("5", digits.ElementOrNull(1)?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);
        Assert.Equal(string.Empty, digits.ElementOrNull(9)?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);
    }

    /// <summary>An array handed over as an array is read as an array, not through the list's interface: the answer is the same, and
    /// the call stays the plain bounds check a hand-written one was.</summary>
    [Fact]
    public void AnArrayAndTheListOfIt_GiveTheSameAnswer()
    {
        double[] values = [1, 2, 3];
        IReadOnlyList<double> list = values;

        for (int i = -1; i < 5; i++)
        {
            Assert.Equal(values.ElementOr(i, -9), list.ElementOr(i, -9));
        }
    }
}
