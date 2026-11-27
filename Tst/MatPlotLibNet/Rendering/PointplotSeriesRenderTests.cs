// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="PointplotSeries"/> rendering.</summary>
public class PointplotSeriesRenderTests
{
    private static readonly double[][] Datasets = [[1.0, 2.0, 3.0, 2.5], [4.0, 5.0, 4.5, 5.5]];

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("Pointplot_SvgContainsCircles", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Pointplot(Datasets))
            .ToSvg(), "<circle"),
        new DrawnToken("Pointplot_SvgContainsLines", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Pointplot(Datasets))
            .ToSvg(), "<line"),
        // PointplotSeriesRenderer.Render: data.Length == 0 continue-branch
        new DrawnToken("Pointplot_EmptyInnerDataset_ContinueBranch", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Pointplot([[], [1.0, 2.0, 3.0]]))
            .ToSvg(), "<circle"),
        // PointplotSeriesRenderer.ComputeCI: v.Length < 2 branch → returns 0
        new DrawnToken("Pointplot_SingleValueDataset_ComputeCI_ReturnsZero", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Pointplot([[5.0]]))
            .ToSvg(), "<circle"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    // ── Branch coverage ──────────────────────────────────────────────────────

    private static readonly Color Ink = Color.FromHex("#C03020");
    private static readonly double[][] ThreeGroups = [[1.0, 2.0, 3.0, 4.0], [3.0, 4.0, 5.0, 6.0], [5.0, 6.0, 7.0, 9.0]];

    private static RecordingRenderContext Drawn(Theme theme, Action<PointplotSeries>? configure = null)
    {
        var figure = Plt.Create().WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax => ax.Pointplot(ThreeGroups, s => { s.Color = Ink; configure?.Invoke(s); })).Build();
        var recording = new RecordingRenderContext();
        new ChartRenderer().Render(figure, recording);
        return recording;
    }

    private static List<TCall> InInk<TCall>(RecordingRenderContext drawn) where TCall : StrokeCall =>
        [.. drawn.Of<TCall>().Where(args => args.Color == Ink)];

    /// <summary>seaborn 0.13.2: the means are joined by a line (the first <c>Line2D</c> of the axes), each interval is a vertical line of
    /// its own, 2.7 points wide under the default style, with no caps.</summary>
    [Fact]
    public void UnderTheMatplotlibThemes_TheMeansAreJoined_AndEachIntervalIsALineWithNoCaps()
    {
        var drawn = Drawn(Theme.MatplotlibV2);

        var joined = Assert.Single(InInk<DrawLinesCall>(drawn));
        Assert.Equal(3, (joined.Points).Length);
        Assert.Equal(2.7 * 100 / 72, joined.Thickness, 9);
        var intervals = InInk<DrawLineCall>(drawn);
        Assert.Equal(3, intervals.Count);
        Assert.All(intervals, line => Assert.Equal((line.P1).X, (line.P2).X, 6));
        Assert.All(intervals, line => Assert.Equal(2.7 * 100 / 72, line.Thickness, 9));
    }

    [Fact]
    public void UnderTheLibrarysOwnThemes_NothingIsJoined_AndEachIntervalHasItsTwoCaps()
    {
        var drawn = Drawn(Theme.Default);

        Assert.Empty(InInk<DrawLinesCall>(drawn));
        var lines = InInk<DrawLineCall>(drawn);
        Assert.Equal(9, lines.Count);
        Assert.All(lines, line => Assert.Equal(1.5, line.Thickness));
    }

    [Fact]
    public void AGivenJoin_IsDrawnUnderTheLibrarysOwnThemesToo()
    {
        Assert.Single(InInk<DrawLinesCall>(Drawn(Theme.Default, s => s.JoinPoints = true)));
    }

    /// <summary>seaborn 0.13.2: the dots are the markers of the line through the means, <c>markeredgewidth = linewidth * 0.75</c> (printed
    /// 2.025 pt under the default style, 1.35 pt under classic) and <c>markeredgecolor = 'auto'</c>: the colour of the line under the default
    /// style, black under the classic one (<c>Line2D.get_markeredgecolor</c> in classic mode).</summary>
    [Fact]
    public void UnderTheMatplotlibThemes_TheDotsAreOutlinedAsALineMarkerIs()
    {
        var v2 = Assert.Single(Drawn(Theme.MatplotlibV2).Of<DrawCircleCall>().Where(c => c.Fill == Ink).Take(1));
        var classic = Assert.Single(Drawn(Theme.MatplotlibClassic).Of<DrawCircleCall>().Where(c => c.Fill == Ink).Take(1));

        Assert.Equal(Ink, v2.Stroke);
        Assert.Equal(2.7 * 100 / 72 * 0.75, v2.StrokeThickness, 9);
        Assert.Equal(Colors.Black, classic.Stroke);
        Assert.Equal(1.8 * 100 / 72 * 0.75, classic.StrokeThickness, 9);
    }

    [Fact]
    public void UnderTheLibrarysOwnThemes_TheDotsAreNotOutlined()
    {
        var dot = Assert.Single(Drawn(Theme.Default).Of<DrawCircleCall>().Where(c => c.Fill == Ink).Take(1));

        Assert.Null(dot.Stroke);
    }
}
