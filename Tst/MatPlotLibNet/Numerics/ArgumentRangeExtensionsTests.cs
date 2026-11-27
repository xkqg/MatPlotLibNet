// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>The checks a number given to the library is held to, written once: finite, other than a value, at least a floor, above a
/// floor, in a closed range, strictly between two others, a power of 2. Each returns the value it accepts and refuses the rest with an
/// <see cref="ArgumentOutOfRangeException"/> naming the parameter and the value, written the same in every culture; a site that checks
/// more than one thing chains them. matplotlib keeps the rule in one place as well: <c>axes/_base.py:1769-1770</c> (3.11.2),
/// <c>if aspect &lt;= 0 or not np.isfinite(aspect): raise ValueError("aspect must be finite and positive ")</c>, which refuses inf,
/// nan, 0 and -1 alike (python -I).</summary>
public sealed class ArgumentRangeExtensionsTests
{
    [Fact]
    public void ThrowIfBelow_AcceptsTheFloorAndAbove_AndRefusesBelowIt()
    {
        Assert.Equal(2, 2.ThrowIfBelow(2, "period"));
        Assert.Equal(9, 9.ThrowIfBelow(2, "period"));
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => 1.ThrowIfBelow(2, "period"));
        Assert.Equal("period", error.ParamName);
        Assert.StartsWith("period must be >= 2 (got 1).", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ThrowIfNotIn_AcceptsTheClosedRange_AndRefusesOutsideIt()
    {
        Assert.Equal(2, 2.ThrowIfNotIn(2, 7, "order"));
        Assert.Equal(7, 7.ThrowIfNotIn(2, 7, "order"));
        Assert.Throws<ArgumentOutOfRangeException>(() => 1.ThrowIfNotIn(2, 7, "order"));
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => 8.ThrowIfNotIn(2, 7, "order"));
        Assert.StartsWith("order must be in [2, 7] (got 8).", error.Message, StringComparison.Ordinal);
    }

    /// <summary>The closed range is one rule for whole and real numbers: a centre line between its limits, a count of defectives
    /// between none and the sample.</summary>
    [Fact]
    public void ThrowIfNotIn_HoldsARealNumberToTheClosedRangeToo()
    {
        Assert.Equal(2.5, 2.5.ThrowIfNotIn(1.0, 2.5, "center"));
        Assert.Throws<ArgumentOutOfRangeException>(() => double.NaN.ThrowIfNotIn(1.0, 2.5, "center"));
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => 0.5.ThrowIfNotIn(1.0, 2.5, "center"));
        Assert.StartsWith("center must be in [1, 2.5] (got 0.5).", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ThrowIfNotPowerOfTwo_AcceptsAPowerOfTwoFromTheFloor_AndRefusesTheRest()
    {
        Assert.Equal(64, 64.ThrowIfNotPowerOfTwo(4, "window"));
        Assert.Throws<ArgumentOutOfRangeException>(() => 48.ThrowIfNotPowerOfTwo(4, "window"));
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => 2.ThrowIfNotPowerOfTwo(4, "window"));
        Assert.StartsWith("window must be a power of 2 and >= 4 (got 2).", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ThrowIfNotAbove_AcceptsAboveTheFloor_AndRefusesTheFloorBelowAndNaN()
    {
        Assert.Equal(0.5, 0.5.ThrowIfNotAbove(0, "scale"));
        Assert.Throws<ArgumentOutOfRangeException>(() => 0.0.ThrowIfNotAbove(0, "scale"));
        Assert.Throws<ArgumentOutOfRangeException>(() => (-1.0).ThrowIfNotAbove(0, "scale"));
        Assert.Throws<ArgumentOutOfRangeException>(() => double.NaN.ThrowIfNotAbove(0, "scale"));
    }

    [Fact]
    public void TheDoubleThrowIfBelow_AcceptsTheFloor_AndRefusesBelowIt()
    {
        Assert.Equal(0.0, 0.0.ThrowIfBelow(0.0, "regularization"));
        Assert.Throws<ArgumentOutOfRangeException>(() => (-1e-9).ThrowIfBelow(0.0, "regularization"));
    }

    [Fact]
    public void ThrowIfNotBetween_AcceptsTheOpenInterval_AndRefusesItsEnds()
    {
        Assert.Equal(0.07, 0.07.ThrowIfNotBetween(0, 1, "alpha"));
        Assert.Throws<ArgumentOutOfRangeException>(() => 0.0.ThrowIfNotBetween(0, 1, "alpha"));
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => 1.0.ThrowIfNotBetween(0, 1, "alpha"));
        Assert.StartsWith("alpha must be in (0, 1) (got 1).", error.Message, StringComparison.Ordinal);
    }

    /// <summary>A finite number passes as it is; NaN and both infinities are refused by name, with the value.</summary>
    [Theory]
    [InlineData(double.NaN, "NaN")]
    [InlineData(double.PositiveInfinity, "Infinity")]
    [InlineData(double.NegativeInfinity, "-Infinity")]
    public void ThrowIfNotFinite_RefusesNaNAndBothInfinities(double value, string written)
    {
        Assert.Equal(-3.5, (-3.5).ThrowIfNotFinite("offset"));
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => value.ThrowIfNotFinite("offset"));
        Assert.Equal("offset", error.ParamName);
        Assert.StartsWith($"offset must be finite (got {written}).", error.Message, StringComparison.Ordinal);
    }

    /// <summary>Values that are one of many where NaN marks a missing one: a missing value passes, an infinity and a value below the
    /// floor are refused, the first one named by what it is and the parameter by its name.</summary>
    [Fact]
    public void ThrowIfAnyNotAmount_PassesAMissingValue_AndRefusesAnInfinityAndBelowTheFloor()
    {
        string[] categories = ["a", "b", "c"];
        new[] { 3.0, double.NaN, 0.0 }.ThrowIfAnyNotAmount("counts", categories, static (c, i) => $"The count of '{c[i]}'");
        var infinite = Assert.Throws<ArgumentOutOfRangeException>(
            () => new[] { 3.0, double.PositiveInfinity, -1.0 }.ThrowIfAnyNotAmount("counts", categories, static (c, i) => $"The count of '{c[i]}'"));
        Assert.Equal("counts", infinite.ParamName);
        Assert.StartsWith("The count of 'b' must be finite (got Infinity).", infinite.Message, StringComparison.Ordinal);
        var rank = Assert.Throws<ArgumentOutOfRangeException>(
            () => new[] { 1.0, 0.5 }.ThrowIfAnyNotAmount("ranks", 2022, static (x, i) => $"The rank of 'Owls' at x = {x + i}", least: 1));
        Assert.StartsWith("The rank of 'Owls' at x = 2023 must be >= 1 (got 0.5).", rank.Message, StringComparison.Ordinal);
    }

    /// <summary>A value that is one of many is named only when it is refused: the function that names it does not run for a value the
    /// check accepts, so a check of every value of an array writes no text for the values it takes (the eager subject of a first
    /// cut made a Marimekko chart of 40 by 8 allocate 53.9 KB instead of 31.4 KB and take 7.6 us instead of 3.0 us).</summary>
    [Fact]
    public void TheNameOfOneOfManyValues_IsWrittenOnlyWhenTheValueIsRefused()
    {
        int named = 0;
        string What(int at) { named++; return $"values[{at}]"; }

        for (int i = 0; i < 100; i++)
        {
            ((double)i).ThrowIfNotFinite("values", i, What).ThrowIfBelow(0, "values", i, What).ThrowIfNotAbove(-1, "values", i, What);
            ((double)i).ThrowIfNotIn(0, 100, "values", i, What);
            ((double)i).ThrowIfBeyondADouble("values", i, What);
        }

        double[] all = [.. Enumerable.Range(0, 100).Select(i => (double)i)];
        all.ThrowIfAnyNotAmount("values", 0, (_, at) => What(at));
        Assert.Equal(0, named);
        var refused = Assert.Throws<ArgumentOutOfRangeException>(() => (-1.0).ThrowIfBelow(0, "values", 7, What));
        Assert.Equal(1, named);
        Assert.StartsWith("values[7] must be >= 0 (got -1).", refused.Message, StringComparison.Ordinal);
    }

    /// <summary>A value worked out from the caller's data that a double cannot hold is the data's fault, not one parameter's range:
    /// an <see cref="ArgumentException"/> that is not an <see cref="ArgumentOutOfRangeException"/>.</summary>
    [Fact]
    public void ThrowIfBeyondADouble_RefusesWhatADoubleCannotHold_AsTheDatasFault()
    {
        Assert.Equal(1e300, 1e300.ThrowIfBeyondADouble("rows", "The span of 'huge'"));
        var error = Assert.Throws<ArgumentException>(() => double.PositiveInfinity.ThrowIfBeyondADouble("rows", "The span of 'huge'"));
        Assert.Equal("rows", error.ParamName);
        Assert.StartsWith("The span of 'huge' is beyond what a double holds (got Infinity).", error.Message, StringComparison.Ordinal);
    }

    /// <summary>A number that has no inverse at one value (a scale of 0, a logarithm of base 1) is refused at that value only.</summary>
    [Fact]
    public void ThrowIfEqual_RefusesTheOneValue()
    {
        Assert.Equal(2.0, 2.0.ThrowIfEqual(1, "base"));
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => 1.0.ThrowIfEqual(1, "base"));
        Assert.StartsWith("base must be other than 1 (got 1).", error.Message, StringComparison.Ordinal);
    }

    /// <summary>A site that checks more than one thing chains the checks, and the first one the value fails names the refusal: an
    /// infinite width is not finite before it is above 0.</summary>
    [Fact]
    public void ChainedChecks_RefuseByTheFirstOneTheValueFails()
    {
        Assert.Equal(2.0, 2.0.ThrowIfNotFinite("width").ThrowIfNotAbove(0, "width"));
        var infinite = Assert.Throws<ArgumentOutOfRangeException>(() => double.PositiveInfinity.ThrowIfNotFinite("width").ThrowIfNotAbove(0, "width"));
        Assert.StartsWith("width must be finite (got Infinity).", infinite.Message, StringComparison.Ordinal);
        var zero = Assert.Throws<ArgumentOutOfRangeException>(() => 0.0.ThrowIfNotFinite("width").ThrowIfNotAbove(0, "width"));
        Assert.StartsWith("width must be > 0 (got 0).", zero.Message, StringComparison.Ordinal);
    }

    /// <summary>A value that is one of many (a count of a category, a flow of a cell) is named in the message by what it is, while the
    /// exception names the parameter that holds it.</summary>
    [Fact]
    public void WhatTheValueIs_NamesTheMessage_AndTheParameterStaysTheParameter()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => (-1.0).ThrowIfBelow(0, "counts", "The count of 'b'"));
        Assert.Equal("counts", error.ParamName);
        Assert.StartsWith("The count of 'b' must be >= 0 (got -1).", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheMessage_WritesItsNumbersTheSameInEveryCulture()
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nl-NL");
            var error = Assert.Throws<ArgumentOutOfRangeException>(() => (-0.5).ThrowIfNotAbove(0, "scale"));
            Assert.StartsWith("scale must be > 0 (got -0.5).", error.Message, StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    /// <summary>An index into a list of <c>count</c> items is from 0 to <c>count - 1</c>: one rule for a ring buffer's, a seat's, a
    /// bar of a waterfall's and a list's, and a list with no item at all names none.</summary>
    [Fact]
    public void ThrowIfNotIndexOf_AcceptsTheFirstAndTheLastItem_AndRefusesTheRest()
    {
        Assert.Equal(0, 0.ThrowIfNotIndexOf(5, "index"));
        Assert.Equal(4, 4.ThrowIfNotIndexOf(5, "index"));
        var past = Assert.Throws<ArgumentOutOfRangeException>(() => 5.ThrowIfNotIndexOf(5, "index"));
        Assert.Equal("index", past.ParamName);
        Assert.StartsWith("index must be from 0 to 4 (got 5).", past.Message, StringComparison.Ordinal);
        var before = Assert.Throws<ArgumentOutOfRangeException>(() => (-1).ThrowIfNotIndexOf(5, "index", "The seat"));
        Assert.StartsWith("The seat must be from 0 to 4 (got -1).", before.Message, StringComparison.Ordinal);
        var none = Assert.Throws<ArgumentOutOfRangeException>(() => 0.ThrowIfNotIndexOf(0, "index"));
        Assert.StartsWith("index must name an item, and there is none (got 0).", none.Message, StringComparison.Ordinal);
    }

    /// <summary>An index that is one of many (a corner of a triangle, an end of a link) is named by a function of a state that runs only
    /// when the index is refused, so a check of every index of an array writes no text for the ones it accepts.</summary>
    [Fact]
    public void ThrowIfNotIndexOf_NamesAnIndexOfManyOnlyWhenItIsRefused()
    {
        int named = 0;
        string Name((int[] list, int i) at) { named++; return $"The corner {at.list[at.i]}"; }

        int[] list = [0, 4, 9];
        Assert.Equal(4, list[1].ThrowIfNotIndexOf(5, "triangles", (list, 1), Name));
        Assert.Equal(0, named);
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => list[2].ThrowIfNotIndexOf(5, "triangles", (list, 2), Name));
        Assert.Equal("triangles", error.ParamName);
        Assert.StartsWith("The corner 9 must be from 0 to 4 (got 9).", error.Message, StringComparison.Ordinal);
        Assert.Equal(1, named);
    }

    /// <summary>A rule no ordered pair of bounds says (a half-open range, a power of ten, a rectangle's four edges, a time above
    /// none) is told by the site as a condition, and is refused in the same sentence and with the same exception type.</summary>
    [Fact]
    public void ThrowIfNot_RefusesAConditionThatDoesNotHold_InTheOwnersSentence()
    {
        Assert.Equal(0.5, 0.5.ThrowIfNot(true, "gap", "must be in [0, 1)"));
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => 1.0.ThrowIfNot(false, "gap", "must be in [0, 1)"));
        Assert.Equal("gap", error.ParamName);
        Assert.StartsWith("gap must be in [0, 1) (got 1).", error.Message, StringComparison.Ordinal);
        var named = Assert.Throws<ArgumentOutOfRangeException>(() => TimeSpan.Zero.ThrowIfNot(false, "interval", "must be a positive time", "The time between frames"));
        Assert.StartsWith("The time between frames must be a positive time (got 00:00:00).", named.Message, StringComparison.Ordinal);
    }
}
