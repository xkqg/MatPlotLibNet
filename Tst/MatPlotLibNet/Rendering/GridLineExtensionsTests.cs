// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The drawing of a 2-D grid (<see cref="GridLineExtensions"/>): a line across the plot at each major tick the plot
/// holds and, for a minor grid, at each minor tick of the axis, in the grid's stroke; the minor lines as the theme says, in the
/// grid's own stroke under the matplotlib themes and at half its width and half its colour's alpha under the others. A recording
/// context counts and reads the lines.</summary>
public class GridLineExtensionsTests
{
    private static readonly Rect Plot = new(0, 0, 400, 300);

    private static readonly StrokeStyle Gray = new(Colors.Gray, 1.0, LineStyle.Solid);

    // ── RenderGridLines ──────────────────────────────────────────────────────

    [Fact]
    public void RenderGridLines_Horizontal_Major_CountEqualsTickCount()
    {
        var ctx = new RecordingRenderContext();

        ctx.RenderGridLines(EdgeTerms.Bottom, [0.0, 1.0, 2.0], Plot.Linear(xMax: 2, yMax: 2), Gray, Theme.Default);

        Assert.Equal(3, ctx.Count<DrawLineCall>());
    }

    [Fact]
    public void RenderGridLines_Vertical_Major_CountEqualsTickCount_EachAcrossThePlot()
    {
        var ctx = new RecordingRenderContext();

        ctx.RenderGridLines(EdgeTerms.Left, [0.0, 1.0, 2.0], Plot.Linear(xMax: 2, yMax: 2), Gray, Theme.Default);

        Assert.Equal(3, ctx.Count<DrawLineCall>());
        Assert.All(ctx.Of<DrawLineCall>(), l =>
        {
            Assert.Equal(Plot.X, (l.P1).X);
            Assert.Equal(Plot.X + Plot.Width, (l.P2).X);
        });
    }

    [Fact]
    public void RenderGridLines_Minor_CountIsSubdivisionsMinusMajorOverlapAndBounds()
    {
        // ticks = [0,1,2], majorStep=1, minorStep=0.2
        // Minor positions in (0,2) exclusive: 0.2,0.4,0.6,0.8, 1.2,1.4,1.6,1.8 → 8 lines
        var ctx = new RecordingRenderContext();

        ctx.RenderGridLines(EdgeTerms.Bottom, [0.0, 1.0, 2.0], Plot.Linear(xMax: 2, yMax: 2),
            new StrokeStyle(Colors.LightGray, 0.5, LineStyle.Solid), Theme.Default, new Axes().XAxis);

        Assert.Equal(8, ctx.Count<DrawLineCall>());
    }

    // ── RenderGrid ───────────────────────────────────────────────────────────

    private static GridStyle Grid(GridAxis axis, GridWhich which) =>
        new() { Visible = true, Axis = axis, Which = which, Color = Colors.Blue, LineWidth = 2, LineStyle = LineStyle.Dashed };

    /// <summary>Three major X ticks and three major Y ticks over [0, 2]: three lines a direction for the major grid, eight for
    /// the minor (the five-way split of each unit step, the majors left out).</summary>
    [Theory]
    [InlineData(GridAxis.Both, GridWhich.Major, 3, 3)]
    [InlineData(GridAxis.X, GridWhich.Major, 3, 0)]
    [InlineData(GridAxis.Y, GridWhich.Major, 0, 3)]
    [InlineData(GridAxis.Both, GridWhich.Minor, 8, 8)]
    [InlineData(GridAxis.X, GridWhich.Both, 11, 0)]
    [InlineData(GridAxis.Y, GridWhich.Both, 0, 11)]
    [InlineData(GridAxis.Both, GridWhich.Both, 11, 11)]
    public void RenderGrid_DrawsTheLinesOfTheAxesAndTheTicksItAsksFor(GridAxis axis, GridWhich which, int vertical, int horizontal)
    {
        var ctx = new RecordingRenderContext();

        ctx.RenderGrid(Grid(axis, which), new Axes(), [0.0, 1.0, 2.0], [0.0, 1.0, 2.0], Plot.Linear(xMax: 2, yMax: 2), Theme.Default);

        var lines = ctx.Of<DrawLineCall>().Select(l => (l.P1, l.P2)).ToList();
        Assert.Equal(vertical, lines.Count(l => l.Item1.X == l.Item2.X));
        Assert.Equal(horizontal, lines.Count(l => l.Item1.Y == l.Item2.Y));
    }

    [Fact]
    public void RenderGrid_UnderTheLibrarysOwnThemes_DrawsAMinorLine_AtHalfTheWidth_AndHalfTheAlpha_OfAMajorLine()
    {
        var ctx = new RecordingRenderContext();

        ctx.RenderGrid(Grid(GridAxis.X, GridWhich.Both), new Axes(), [0.0, 1.0, 2.0], [], Plot.Linear(xMax: 2, yMax: 2), Theme.Default);

        var strokes = ctx.Of<DrawLineCall>().Select(l => new StrokeStyle(l.Color, l.Thickness, l.Style)).Distinct().ToList();
        Assert.Equal(2, strokes.Count);
        Assert.Equal(new StrokeStyle(Colors.Blue, 2, LineStyle.Dashed), strokes[0]);
        Assert.Equal(new StrokeStyle(Colors.Blue.ScaleAlpha(0.5), 1, LineStyle.Dashed), strokes[1]);
    }

    [Fact]
    public void RenderGrid_StandsTheMinorLines_OnTheMinorTicksOfTheirOwnAxis()
    {
        var axes = new Axes();
        axes.YAxis.MinorTicks = axes.YAxis.MinorTicks with { Spacing = 0.5 };
        var ctx = new RecordingRenderContext();

        ctx.RenderGrid(Grid(GridAxis.Both, GridWhich.Minor), axes, [0.0, 1.0, 2.0], [0.0, 1.0, 2.0], Plot.Linear(xMax: 2, yMax: 2), Theme.Default);

        var lines = ctx.Of<DrawLineCall>().Select(l => (l.P1, l.P2)).ToList();
        Assert.Equal(8, lines.Count(l => l.Item1.X == l.Item2.X));
        Assert.Equal([75.0, 225.0], lines.Where(l => l.Item1.Y == l.Item2.Y).Select(l => l.Item1.Y).Order());
    }

    /// <summary>matplotlib draws a minor grid line in the grid's own colour, width and line style. matplotlib 3.11,
    /// <c>axis.Tick.__init__</c>: <c>grid_linewidth = mpl._val_or_rc(grid_linewidth, f"grid.{major_minor}.linewidth",
    /// "grid.linewidth")</c>, and the same for <c>color</c>, <c>linestyle</c> and <c>alpha</c>; <c>grid.minor.linewidth</c>,
    /// <c>grid.minor.color</c>, <c>grid.minor.linestyle</c> and <c>grid.minor.alpha</c> are <c>None</c> in the default and the
    /// classic style, so a minor line takes <c>grid.*</c> as a major one does.</summary>
    [Theory]
    [InlineData("matplotlib-v2")]
    [InlineData("matplotlib-classic")]
    public void RenderGrid_UnderAMatplotlibTheme_DrawsAMinorLine_InTheMajorLinesOwnStroke(string name)
    {
        var theme = name.AsTheme();
        var axes = new Axes();
        axes.XAxis.MinorTicks = axes.XAxis.MinorTicks with { Visible = true };
        var ctx = new RecordingRenderContext();

        ctx.RenderGrid(Grid(GridAxis.X, GridWhich.Both), axes, [0.0, 1.0, 2.0], [], Plot.Linear(xMax: 2, yMax: 2), theme);

        var strokes = ctx.Of<DrawLineCall>().Select(l => new StrokeStyle(l.Color, l.Thickness, l.Style)).Distinct().ToList();
        Assert.Equal(11, ctx.Count<DrawLineCall>());
        Assert.Equal([new StrokeStyle(Colors.Blue, 2, LineStyle.Dashed)], strokes);
    }

    /// <summary>A figure under the classic theme writes its theme into the JSON; the rule comes back with it, and a theme made
    /// from it keeps it.</summary>
    [Fact]
    public void TheMinorGridRule_SurvivesJson_AndAThemeMadeFromIt()
    {
        var figure = Plt.Create().WithTheme(Theme.MatplotlibClassic).AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1.0], [0.0, 1.0])).Build();

        var again = figure.RoundTripped().Theme;

        Assert.True(again.Looks.Ticks.MinorGridLikeMajor);
        Assert.True(Theme.MatplotlibV2.Cloned().Looks.Ticks.MinorGridLikeMajor);
        Assert.False(Theme.Dark.ThroughJson().Looks.Ticks.MinorGridLikeMajor);
    }

    // ── A minor grid stands where the minor locator places ticks (matplotlib themes) ─────────────────────────────

    private static IEnumerable<(Point P1, Point P2)> LinesOf(RecordingRenderContext ctx) =>
        ctx.Of<DrawLineCall>().Select(l => (l.P1, l.P2));

    /// <summary>matplotlib 3.11.2, <c>ax.plot(...); ax.grid(which='minor')</c> on a linear axis: the minor locator is a
    /// <c>NullLocator</c>, <c>len(ax.yaxis.get_minor_ticks())</c> and <c>len(get_minorticklocs())</c> are 0, so no minor grid line
    /// is drawn until <c>minorticks_on()</c>. A grid line is a tick's, so under the matplotlib themes an axis that switched no
    /// minor ticks on draws no minor line.</summary>
    [Theory]
    [InlineData("matplotlib-v2")]
    [InlineData("matplotlib-classic")]
    public void RenderGrid_UnderAMatplotlibTheme_DrawsNoMinorLine_OnAnAxisWithNoMinorTicks(string name)
    {
        var theme = name.AsTheme();
        var ctx = new RecordingRenderContext();

        ctx.RenderGrid(Grid(GridAxis.Both, GridWhich.Minor), new Axes(), [0.0, 1.0, 2.0], [0.0, 1.0, 2.0], Plot.Linear(xMax: 2, yMax: 2), theme);

        Assert.Equal(0, ctx.Count<DrawLineCall>());
    }

    [Fact]
    public void RenderGrid_UnderAMatplotlibTheme_WithBothLevelsAndNoMinorTicks_DrawsTheMajorLinesOnly()
    {
        var ctx = new RecordingRenderContext();

        ctx.RenderGrid(Grid(GridAxis.Both, GridWhich.Both), new Axes(), [0.0, 1.0, 2.0], [0.0, 1.0, 2.0], Plot.Linear(xMax: 2, yMax: 2), Theme.MatplotlibV2);

        Assert.Equal(6, ctx.Count<DrawLineCall>());
    }

    /// <summary>Each axis answers for itself: matplotlib's <c>ax.xaxis.minorticks_on()</c> gives the X axis a minor locator and
    /// leaves the Y axis' a <c>NullLocator</c>.</summary>
    [Fact]
    public void RenderGrid_UnderAMatplotlibTheme_DrawsTheMinorLines_OfTheAxisThatSwitchedItsMinorTicksOn_Only()
    {
        var axes = new Axes();
        axes.XAxis.MinorTicks = axes.XAxis.MinorTicks with { Visible = true };
        var ctx = new RecordingRenderContext();

        ctx.RenderGrid(Grid(GridAxis.Both, GridWhich.Minor), axes, [0.0, 1.0, 2.0], [0.0, 1.0, 2.0], Plot.Linear(xMax: 2, yMax: 2), Theme.MatplotlibV2);

        var lines = LinesOf(ctx).ToList();
        Assert.Equal(8, lines.Count(l => l.P1.X == l.P2.X));
        Assert.Equal(0, lines.Count(l => l.P1.Y == l.P2.Y));
    }

    /// <summary>matplotlib 3.11.2, a minor-grid figure with the scale set after the plot: <c>log</c> 24 minor ticks (a
    /// <c>LogLocator</c>), <c>logit</c> 22 (a <c>LogitLocator</c>), <c>linear</c> 0 (<c>NullLocator</c>), <c>symlog</c> 0
    /// (<c>SymmetricalLogLocator</c>, which places none without <c>subs</c>). A log and a logit axis place their minor ticks
    /// with no call, so their minor grid lines stand without minor tick marks switched on.</summary>
    [Theory]
    [InlineData(AxisScale.Log, 8)]
    [InlineData(AxisScale.Linear, 0)]
    [InlineData(AxisScale.SymLog, 0)]
    [InlineData(AxisScale.Date, 0)]
    public void RenderGrid_UnderAMatplotlibTheme_DrawsTheMinorLinesOfAScaleThatBringsMinorTicks_WithoutMinorTicksOn(AxisScale scale, int expected)
    {
        var axes = new Axes();
        axes.XAxis.Scale = scale;
        var ctx = new RecordingRenderContext();

        ctx.RenderGrid(Grid(GridAxis.X, GridWhich.Minor), axes, [0.0, 1.0, 2.0], [], Plot.Linear(xMax: 2, yMax: 2), Theme.MatplotlibV2);

        Assert.Equal(expected, ctx.Count<DrawLineCall>());
    }

    /// <summary>The library's own themes draw the minor grid on the places the minor ticks would stand, with or without the
    /// marks (the picture of every chart that asked for a minor grid alone): the same eight lines per direction.</summary>
    [Fact]
    public void RenderGrid_UnderTheLibrarysOwnThemes_StillDrawsTheMinorLines_OnAnAxisWithNoMinorTicks()
    {
        foreach (var theme in new[] { Theme.Default, Theme.Dark })
        {
            var ctx = new RecordingRenderContext();

            ctx.RenderGrid(Grid(GridAxis.Both, GridWhich.Minor), new Axes(), [0.0, 1.0, 2.0], [0.0, 1.0, 2.0], Plot.Linear(xMax: 2, yMax: 2), theme);

            Assert.Equal(16, ctx.Count<DrawLineCall>());
        }
    }

}
