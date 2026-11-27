// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Builders;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Under the matplotlib themes a box plot stands where matplotlib's <c>boxplot</c> stands its boxes. matplotlib 3.11.2, printed
/// for 1, 2, 3, 4, 5 and 7 datasets in both styles: <c>ax.boxplot(data)</c> puts the boxes at 1, 2, ..., n (<c>bxp</c>'s
/// <c>positions</c> default), draws each <c>clip(0.15 * ptp(positions), 0.15, 0.5)</c> wide (0.15, 0.15, 0.30, 0.45, 0.5, 0.5), ends the axis
/// at <c>min(positions) - 0.5</c> and <c>max(positions) + 0.5</c> with no margin (<c>xlim</c> (0.5, n + 0.5), whatever the width) and sets a
/// <c>FixedLocator</c> at the positions (<c>xticks</c> 1 ... n); <c>vert=False</c> does the same along Y. The library stood them at
/// 0, 1, ... and 0.5 wide, with the documented defaults of <see cref="BoxSeries.Positions"/> and <see cref="BoxSeries.Widths"/>, and a
/// caller who wanted the picture said all of it. A position or a width the caller gave wins under every theme, and the library's own themes
/// keep the documented defaults.</summary>
public sealed class BoxPlacementTests
{
    private static readonly Theme V2 = Theme.MatplotlibV2;

    private static double[][] Data(int boxes) =>
        [.. Enumerable.Range(0, boxes).Select(g => Enumerable.Range(0, 20).Select(i => g + (i - 9.5) * 0.1).ToArray())];

    private static Figure Of(Theme theme, int boxes, Action<BoxSeries>? configure = null) =>
        Plt.Create().WithSize(800, 600).WithTheme(theme).AddSubPlot(1, 1, 1, ax => ax.BoxPlot(Data(boxes), configure)).Build();

    private static BoxSeries BoxOf(Figure figure) => Assert.IsType<BoxSeries>(figure.SubPlots[0].Series[0]);

    [Theory]
    [MemberData(nameof(ThemeCases.Matplotlib), MemberType = typeof(ThemeCases))]
    public void UnderTheMatplotlibThemes_TheBoxesStandAtOneToN(string style)
    {
        var theme = style.AsTheme();
        var box = BoxOf(Of(theme, 4));

        Assert.Equal([1.0, 2, 3, 4], Enumerable.Range(0, 4).Select(i => box.PositionIn(i, theme)));
    }

    [Theory]
    [MemberData(nameof(ThemeCases.Matplotlib), MemberType = typeof(ThemeCases))]
    public void UnderTheMatplotlibThemes_TheDefaultWidthIsAShareOfTheSpanOfThePositions_BetweenTheLeastAndTheMost(string style)
    {
        var theme = style.AsTheme();

        // clip(0.15 * (n - 1), 0.15, 0.5), as matplotlib's bxp: printed 0.15, 0.15, 0.30, 0.45, 0.5, 0.5 for 1, 2, 3, 4, 5 and 7 boxes.
        double[] expected = [0.15, 0.15, 0.30, 0.45, 0.5, 0.5];
        int[] boxes = [1, 2, 3, 4, 5, 7];
        for (int i = 0; i < boxes.Length; i++)
            Assert.Equal(expected[i], BoxOf(Of(theme, boxes[i])).WidthIn(theme), 9);
    }

    [Theory]
    [MemberData(nameof(ThemeCases.Matplotlib), MemberType = typeof(ThemeCases))]
    public void UnderTheMatplotlibThemes_TheAxisEndsHalfAUnitPastTheOuterBoxes_WithNoMargin(string style)
    {
        var theme = style.AsTheme();

        var range = Of(theme, 4).SubPlots[0].ComputeDataRanges(theme);

        Assert.Equal(0.5, range.XMin, 9);
        Assert.Equal(4.5, range.XMax, 9);
    }

    [Theory]
    [MemberData(nameof(ThemeCases.Matplotlib), MemberType = typeof(ThemeCases))]
    public void UnderTheMatplotlibThemes_TheTicksStandAtTheBoxes(string style)
    {
        var theme = style.AsTheme();
        var figure = Of(theme, 4);

        var locator = figure.SubPlots[0].PrimaryTickSource(isX: true, theme).Locator;

        Assert.NotNull(locator);
        Assert.Equal([1.0, 2, 3, 4], locator.Locate(0.5, 4.5));
    }

    [Fact]
    public void UnderTheMatplotlibThemes_BoxesThatLieDown_StandAtOneToNAlongY()
    {
        var figure = Of(V2, 4, s => s.Vert = false);

        var range = figure.SubPlots[0].ComputeDataRanges(V2);
        var locator = figure.SubPlots[0].PrimaryTickSource(isX: false, V2).Locator;

        Assert.Equal(0.5, range.YMin, 9);
        Assert.Equal(4.5, range.YMax, 9);
        Assert.Equal([1.0, 2, 3, 4], locator!.Locate(0.5, 4.5));
    }

    [Fact]
    public void APositionAndAWidthTheCallerGave_WinUnderTheMatplotlibThemes()
    {
        var figure = Of(V2, 4, s => { s.Positions = [0, 1, 2, 3]; s.Widths = 0.5; });
        var box = BoxOf(figure);

        Assert.Equal([0.0, 1, 2, 3], Enumerable.Range(0, 4).Select(i => box.PositionIn(i, V2)));
        Assert.Equal(0.5, box.WidthIn(V2));
        var range = figure.SubPlots[0].ComputeDataRanges(V2);
        Assert.Equal(-0.5, range.XMin, 9);
        Assert.Equal(3.5, range.XMax, 9);
        Assert.Equal([0.0, 1, 2, 3], figure.SubPlots[0].PrimaryTickSource(isX: true, V2).Locator!.Locate(-0.5, 3.5));
    }

    [Fact]
    public void AWidthTheCallerGave_IsKept_WhereThePositionsAreTheThemes()
    {
        var box = BoxOf(Of(V2, 4, s => s.Widths = 0.5));

        Assert.Equal(1.0, box.PositionIn(0, V2));
        Assert.Equal(0.5, box.WidthIn(V2));
    }

    [Fact]
    public void ADatasetThePositionsDoNotReach_StandsAtItsIndexPlusOne_UnderTheMatplotlibThemes()
    {
        var box = BoxOf(Of(V2, 4, s => s.Positions = [10, 20]));

        Assert.Equal([10.0, 20, 3, 4], Enumerable.Range(0, 4).Select(i => box.PositionIn(i, V2)));
    }

    [Fact]
    public void UnderTheLibrarysOwnThemes_TheBoxesKeepTheirDocumentedDefaults()
    {
        var theme = Theme.Default;
        var figure = Of(theme, 4);
        var box = BoxOf(figure);

        Assert.Equal([0.0, 1, 2, 3], Enumerable.Range(0, 4).Select(i => box.PositionIn(i, theme)));
        Assert.Equal(0.5, box.WidthIn(theme));
        Assert.Null(figure.SubPlots[0].PrimaryTickSource(isX: true, theme).Locator);
        var range = figure.SubPlots[0].ComputeDataRanges(theme);
        Assert.True(range.XMin <= -0.5 && range.XMax >= 3.5);
    }

    [Fact]
    public void TheWidth_IsGivenOnlyWhenTheCallerSetsIt_WhateverItsPublicGetterReads()
    {
        var unset = BoxOf(Of(V2, 4));
        var set = BoxOf(Of(V2, 4, s => s.Widths = 0.5));

        Assert.Equal(0.5, unset.Widths);
        Assert.Null(unset.ToSeriesDto().Widths);
        Assert.Equal(0.5, set.ToSeriesDto().Widths);
    }

    [Fact]
    public void AWidthThatWasNotGiven_StaysNotGiven_ThroughJson()
    {
        var back = BoxOf(Of(V2, 4).RoundTripped());

        Assert.Null(back.ToSeriesDto().Widths);
        Assert.Equal(0.5, back.Widths);
        Assert.Equal(0.45, back.WidthIn(V2), 9);
    }

    [Fact]
    public void ARaincloudsBox_StandsWithItsViolin_UnderTheMatplotlibThemes()
    {
        var figure = Plt.Create().WithSize(800, 600).WithTheme(V2)
            .AddSubPlot(1, 1, 1, ax => ax.Raincloud([new("a", [1.0, 2, 3, 4, 5]), new("b", [2.0, 3, 4, 5, 6])]))
            .Build();
        var box = figure.SubPlots[0].Series.OfType<BoxSeries>().Single();
        var violin = figure.SubPlots[0].Series.OfType<ViolinSeries>().Single();

        Assert.Equal([0.0, 1], Enumerable.Range(0, 2).Select(i => box.PositionIn(i, V2)));
        Assert.Equal(Enumerable.Range(0, 2).Select(i => violin.PositionIn(i, V2)), Enumerable.Range(0, 2).Select(i => box.PositionIn(i, V2)));
    }
}
