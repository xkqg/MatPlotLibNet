// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>Where a violin stands and how far its lines reach are values of its theme's look (<see cref="ViolinLook"/>). Under the
/// matplotlib themes they are matplotlib 3.11.2's, read with python: <c>Axes.violinplot</c> stands a dataset given no position at
/// <c>positions = range(1, N + 1)</c>, as <c>Axes.boxplot</c> does, so both read one <see cref="DatasetStanding"/>; <c>Axes.violin</c>
/// ends its extrema, median and mean lines at <c>line_ends = [[-0.25], [0.25]]</c> times the width; and the axis covers what the bodies
/// draw, no more (<c>violinplot([a, b, c])</c>: <c>get_xlim()</c> (0.625, 3.375) under the default style, the bodies from 0.75 to 3.25
/// and a margin of 5 %). The library's own themes keep theirs: from 0, the room of one unit past the outer violins, caps 0.3 and lines
/// 0.5 of the width.</summary>
public class ViolinStandingTests
{
    private static readonly double[][] ThreeGroups = [[0.0, 1, 1, 2, 2, 2, 3, 3, 4], [2.0, 3, 3, 4, 4, 4, 5, 5, 6], [4.0, 5, 5, 6, 6, 6, 7, 7, 8]];

    private static ViolinSeries ViolinOf(Figure figure) => figure.SubPlots[0].Series.OfType<ViolinSeries>().Single();

    private static Figure Violins(Theme theme, Action<ViolinSeries>? configure = null) =>
        Plt.Create().WithTheme(theme).WithSize(800, 600).AddSubPlot(1, 1, 1, ax => ax.Violin(ThreeGroups, configure)).Build();

    [Theory]
    [InlineData("MatplotlibV2", 1)]
    [InlineData("MatplotlibClassic", 1)]
    [InlineData("Default", 0)]
    public void AViolinGivenNoPosition_StandsWhereItsThemeStandsTheFirstDataset(string themeName, double first)
    {
        var theme = themeName.AsTheme();
        var violin = ViolinOf(Violins(theme));

        Assert.Equal([first, first + 1, first + 2], Enumerable.Range(0, 3).Select(i => violin.PositionIn(i, theme)));
    }

    [Fact]
    public void AGivenPosition_IsThePositionUnderEveryTheme()
    {
        var violin = ViolinOf(Violins(Theme.MatplotlibV2, s => s.Positions = [5, 7]));

        Assert.Equal([5.0, 7, 3], Enumerable.Range(0, 3).Select(i => violin.PositionIn(i, Theme.MatplotlibV2)));
        Assert.Equal([5.0, 7, 2], Enumerable.Range(0, 3).Select(i => violin.PositionIn(i, Theme.Default)));
    }

    [Fact]
    public void ABoxAndAViolin_StandByOneRule_UnderEachTheme()
    {
        Assert.Equal(DatasetStanding.Matplotlib, Theme.MatplotlibV2.Looks.Violin.Datasets);
        Assert.Equal(Theme.MatplotlibV2.Looks.Box.Standing.Datasets, Theme.MatplotlibV2.Looks.Violin.Datasets);
        Assert.Equal(Theme.MatplotlibClassic.Looks.Box.Standing.Datasets, Theme.MatplotlibClassic.Looks.Violin.Datasets);
        Assert.Equal(DatasetStanding.Library, Theme.Default.Looks.Violin.Datasets);
        Assert.Equal(Theme.Default.Looks.Box.Standing.Datasets, Theme.Default.Looks.Violin.Datasets);
    }

    [Theory]
    [InlineData("MatplotlibV2", 0.25, 0.125, 0.125)]
    [InlineData("MatplotlibClassic", 0.25, 0.125, 0.125)]
    [InlineData("Default", 0.5, 0.15, 0.25)]
    public void TheBodyAndTheLinesOverIt_ReachTheShareOfItsWidthItsThemeNames(string themeName, double body, double extrema, double stats)
    {
        var look = themeName.AsTheme().Looks.Violin;
        var violin = ViolinOf(Violins(themeName.AsTheme(), s => s.ShowExtrema = false));

        // The widest of a body is its width times the look's reach: the range of one violin with no line over it is that to either side.
        Assert.Equal(new Range1D(-body, body), violin.ReachAround(violin.BodiesUnder(ThemeLooks.Library)[0].Outline, look));
        Assert.Equal(extrema, violin.ExtremaHalfWidthUnder(look), 12);
        Assert.Equal(stats, violin.StatsHalfWidthUnder(look), 12);
    }

    [Fact]
    public void UnderTheMatplotlibThemes_TheAxisCoversTheBodies_AndNoRoomBeyondThem()
    {
        var violin = ViolinOf(Violins(Theme.MatplotlibV2));
        
        var range = ((IThemedRange)violin).RangeUnder(new TestAxesContext(), Theme.MatplotlibV2);

        // Each body at its widest is half the width to either side of its position: 1 - 0.25 to 3 + 0.25.
        Assert.Equal(0.75, range.XMin!.Value, 9);
        Assert.Equal(3.25, range.XMax!.Value, 9);
    }

    [Fact]
    public void UnderTheLibrarysThemes_TheAxisKeepsTheRoomOfOneUnitPastTheOuterViolins()
    {
        var violin = ViolinOf(Violins(Theme.Default));

        var range = ((IThemedRange)violin).RangeUnder(new TestAxesContext(), Theme.Default);

        Assert.Equal(-1, range.XMin!.Value, 9);
        Assert.Equal(3, range.XMax!.Value, 9);
        Assert.Equal(range, violin.ComputeDataRange(new TestAxesContext()));
    }

    [Fact]
    public void UnderTheMatplotlibThemes_TheCapsOfTheFirstViolin_StandAQuarterOfItsWidthEitherSideOfOne()
    {
        // A violin drawn with its extrema alone draws its bar and its two caps in the stats colour (the series' C0 under the default
        // style); the bars of the three violins stand one unit apart, so their distance is the scale of the axis.
        var svg = Violins(Theme.MatplotlibV2).ToSvg().Xml();
        var stats = svg.Each("line").Where(e => (string?)e.Attribute("stroke") == "#1F77B4").ToList();
        var bars = stats.Where(e => Math.Abs(e.Number("x1") - e.Number("x2")) < 1e-9).Select(e => e.Number("x1")).Order().ToList();
        var caps = stats.Where(e => Math.Abs(e.Number("y1") - e.Number("y2")) < 1e-9).ToList();
        double unit = bars[1] - bars[0];

        Assert.Equal(3, bars.Count);
        Assert.Equal(6, caps.Count);
        Assert.All(caps, cap => Assert.Equal(2 * 0.25 * 0.5, Math.Abs(cap.Number("x2") - cap.Number("x1")) / unit, 3));
    }
}
