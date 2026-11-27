// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary><see cref="ContourLevels"/> is the one place that says which levels a contour draws: the levels a user names
/// (sorted, each once, finite) and matplotlib's round levels for a count of levels when it names none, one rule for a line
/// contour, a filled contour and a triangle contour.</summary>
public class ContourLevelsTests
{
    private const double NaN = double.NaN;

    // ---- the levels a user names ------------------------------------------------------------------------------------

    [Fact]
    public void NoLevelsNamed_IsUnset_ForNullAndForAnEmptyList()
    {
        Assert.Null(((double[]?)null).ExplicitLevels());
        Assert.Null(Array.Empty<double>().ExplicitLevels());
    }

    [Theory]
    [InlineData(new[] { 1.0, 5.0, 9.0 }, new[] { 1.0, 5.0, 9.0 })]
    [InlineData(new[] { 9.0, 1.0, 5.0 }, new[] { 1.0, 5.0, 9.0 })]
    [InlineData(new[] { 5.0, 1.0, 5.0, 9.0, 1.0 }, new[] { 1.0, 5.0, 9.0 })]
    [InlineData(new[] { 9.0, NaN, 1.0, double.PositiveInfinity, 5.0, double.NegativeInfinity }, new[] { 1.0, 5.0, 9.0 })]
    [InlineData(new[] { -2.5, 0.0, 2.5 }, new[] { -2.5, 0.0, 2.5 })]
    [InlineData(new[] { 7.0 }, new[] { 7.0 })]
    public void NamedLevels_AreAscending_EachOnce_AndFinite(double[] named, double[] expected) =>
        Assert.Equal(expected, named.ExplicitLevels());

    /// <summary>Levels were named, and none can be drawn: that is a list with nothing in it, not an unset one, so a
    /// renderer draws nothing instead of falling back to a count the user did not ask for.</summary>
    [Theory]
    [InlineData(new[] { NaN })]
    [InlineData(new[] { double.PositiveInfinity, double.NegativeInfinity, NaN })]
    public void NamedLevels_ThatCannotBeDrawn_AreAnEmptyList_NotUnset(double[] named)
    {
        var levels = named.ExplicitLevels();

        Assert.NotNull(levels);
        Assert.Empty(levels);
    }

    [Fact]
    public void NamedLevels_AreCopied_TheCallersArrayIsNotReordered()
    {
        double[] named = [9, 1, 5];

        _ = named.ExplicitLevels();

        Assert.Equal([9.0, 1.0, 5.0], named);
    }

    // ---- the levels a contour draws -------------------------------------------------------------------------------

    /// <summary>The levels a user names win over any count, in the order and the form <see cref="ContourLevels.ExplicitLevels"/>
    /// gives; naming levels none of which can be drawn is an empty list, not a fall back to the count.</summary>
    [Fact]
    public void Under_TakesTheNamedLevels_BeforeTheCount()
    {
        Assert.Equal([1.0, 5.0, 9.0], ContourLevels.Under(Theme.MatplotlibV2, [9, 1, 5], new Range1D(0, 10), 10)!);
        Assert.Empty(ContourLevels.Under(Theme.MatplotlibV2, [NaN], new Range1D(0, 10), 10)!);
        Assert.Equal([7.0], ContourLevels.Under(Theme.MatplotlibV2, [7], null, 10)!);
    }

    /// <summary>With none named, the levels are matplotlib's round levels for the count (ax.contour(X, Y, Z, levels=10).levels of
    /// the peaks surface is -0.8 to 1.2 in steps of 0.2), the theme's autolimit mode read as the locator reads it.</summary>
    [Fact]
    public void Under_NamingNone_IsAutoOverTheValues_ForTheThemesMode()
    {
        double[] peaks = [-0.8, -0.6, -0.4, -0.2, 0.0, 0.2, 0.4, 0.6, 0.8, 1.0, 1.2];

        var levels = ContourLevels.Under(Theme.MatplotlibV2, null, new Range1D(-0.6849773668989229, 1.0560418679341015), 10)!;
        Assert.Equal(peaks.Length, levels.Length);
        for (int i = 0; i < peaks.Length; i++) Assert.Equal(peaks[i], levels[i], 9);

        Assert.Equal(ContourLevels.Auto(2202.046856090197, 2412.9112426911925, 3, roundNumbers: true),
            ContourLevels.Under(Theme.MatplotlibClassic, [], new Range1D(2202.046856090197, 2412.9112426911925), 3));
        Assert.Equal(ContourLevels.Auto(2202.046856090197, 2412.9112426911925, 3, roundNumbers: false),
            ContourLevels.Under(Theme.MatplotlibV2, null, new Range1D(2202.046856090197, 2412.9112426911925), 3));
    }

    /// <summary>No values that can be drawn, or two that are less than 1e-12 apart, lay no level.</summary>
    [Fact]
    public void Under_NamingNone_OverNoRange_OrAFlatOne_IsUnset()
    {
        Assert.Null(ContourLevels.Under(Theme.MatplotlibV2, null, null, 10));
        Assert.Null(ContourLevels.Under(Theme.MatplotlibV2, null, new Range1D(5, 5), 10));
        Assert.Null(ContourLevels.Under(Theme.MatplotlibV2, null, new Range1D(5, 5 + 1e-13), 10));
    }

    // ---- the levels of a log-normalised contour -----------------------------------------------------------------------------

    /// <summary>Under a LogNorm a contour takes <c>ticker.LogLocator(numticks=N)</c> for its levels (3.11.2,
    /// <c>ContourSet.__init__</c> sets <c>logscale</c>, <c>_ensure_locator_exists</c> asks for it) and <c>_autolev</c> trims them as
    /// for any locator: one level at or below the lowest value and one at or above the highest, so values from 1 to 1000 asked for 10
    /// levels have 0.1 to 10000. Every row is what matplotlib prints for <c>contourf(..., levels=N, norm=LogNorm()).levels</c> of
    /// a grid whose values run between the two (the default style, and the classic style when it differs).</summary>
    [Theory]
    [InlineData(1.0, 1000.0, 10, new[] { 0.1, 1, 10, 100, 1000, 10000 })]
    [InlineData(0.5, 3000.0, 10, new[] { 0.1, 1, 10, 100, 1000, 10000 })]
    [InlineData(1.0, 1e12, 10, new[] { 1e-2, 1, 1e2, 1e4, 1e6, 1e8, 1e10, 1e12, 1e14 })]
    [InlineData(2.0, 5.0, 10, new[] { 1.0, 10 })]
    [InlineData(1.0, 1e6, 4, new[] { 1e-2, 1, 1e2, 1e4, 1e6, 1e8 })]
    [InlineData(0.01, 1e9, 10, new[] { 1e-4, 1e-2, 1, 1e2, 1e4, 1e6, 1e8, 1e10 })]
    [InlineData(1.0, 10.0, 10, new[] { 0.1, 1, 10, 100 })]
    [InlineData(3.0, 3000.0, 1, new[] { 1.0, 100, 10000 })]
    [InlineData(1.0, 1000.0, 2, new[] { 1e-3, 1, 1e3, 1e6 })]
    [InlineData(1.0, 1e30, 10, new[] { 1e-2, 1e1, 1e4, 1e7, 1e10, 1e13, 1e16, 1e19, 1e22, 1e25, 1e28, 1e31 })]
    public void ALogContour_TakesLogLocatorLevels(double lo, double hi, int count, double[] expected) =>
        AssertRelative(expected, ContourLevels.Auto(lo, hi, count, roundNumbers: false, levelScale: ScaleRule.Log));

    /// <summary>The classic style's <c>_internal.classic_mode</c> makes <c>LogLocator</c> take its older stride: 1 to 1e30 asked for 10
    /// levels has 1e-4, 1, 1e4, ..., 1e32 where the default style has 1e-2, 1e1, 1e4, ..., 1e31.</summary>
    [Theory]
    [InlineData(1.0, 1e30, 10, new[] { 1e-4, 1, 1e4, 1e8, 1e12, 1e16, 1e20, 1e24, 1e28, 1e32 })]
    [InlineData(1.0, 1e12, 5, new[] { 1e-3, 1, 1e3, 1e6, 1e9, 1e12, 1e15 })]
    [InlineData(3.0, 3000.0, 3, new[] { 1.0, 10, 100, 1000, 10000 })]
    [InlineData(1.0, 1e8, 10, new[] { 0.1, 1, 10, 100, 1e3, 1e4, 1e5, 1e6, 1e7, 1e8, 1e9 })]
    public void ALogContour_UnderTheClassicTheme_TakesTheOlderStride(double lo, double hi, int count, double[] expected) =>
        AssertRelative(expected, ContourLevels.Auto(lo, hi, count, roundNumbers: true, levelScale: ScaleRule.Log));

    /// <summary>matplotlib's classic <c>LogLocator</c> divides by <c>numticks - 1</c> and raises for one level; the contour asks for two
    /// at least there instead of failing.</summary>
    [Fact]
    public void ALogContour_UnderTheClassicTheme_OfOneLevel_DoesNotDivideByZero() =>
        AssertRelative(ContourLevels.Auto(1, 1000, 2, roundNumbers: true, levelScale: ScaleRule.Log), ContourLevels.Auto(1, 1000, 1, roundNumbers: true, levelScale: ScaleRule.Log));

    /// <summary>The one rule hands a log-normalised contour the decades, from the values that can sit on a log scale: matplotlib masks
    /// the values of 0 and below (<c>ma.masked_where(z &lt;= 0, z)</c>) and takes the lowest of the rest.</summary>
    [Fact]
    public void Under_ForALogScale_TakesTheDecades_OverThePositiveRange()
    {
        var levels = ContourLevels.Under(Theme.MatplotlibV2, null, new Range1D(1, 1000), 10, levelScale: ScaleRule.Log)!;

        AssertRelative([0.1, 1, 10, 100, 1000, 10000], levels);
        Assert.Equal([1.0, 5.0, 9.0], ContourLevels.Under(Theme.MatplotlibV2, [9, 1, 5], new Range1D(1, 1000), 10, levelScale: ScaleRule.Log)!);
    }

    private static void AssertRelative(double[] expected, double[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++) Assert.Equal(expected[i], actual[i], Math.Abs(expected[i]) * 1e-12);
    }
}
