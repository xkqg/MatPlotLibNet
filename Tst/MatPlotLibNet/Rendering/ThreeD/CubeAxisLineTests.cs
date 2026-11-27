// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.ThreeD;

/// <summary>What bounds a 3-D box. matplotlib 3.11, mpl_toolkits/mplot3d/axis3d.py: each axis draws its pane
/// (<c>draw_pane</c>), then its grid (<c>draw_grid</c>), then itself (<c>Axis.draw</c>: its <c>line</c>, a <c>Line2D</c> from
/// one edge point to the other with <c>linewidth=self._axinfo['axisline']['linewidth']</c>, <c>color=...['color']</c>, and its
/// ticks); <c>Axis.__init__</c> sets <c>'axisline': {'linewidth': mpl.rcParams['axes.linewidth'], 'color':
/// mpl.rcParams['axes.edgecolor']}</c>, and in classic mode <c>{'linewidth': 0.75, 'color': (0, 0, 0, 1)}</c>. No other edge of
/// the box is drawn (a pane's own edge is the pane's colour). So the matplotlib themes draw three axis lines in the theme's
/// text colour, 0.8 pt (1.11 px) wide under v2 and 0.75 pt (1.04 px) under classic, after the wall grid; every other theme keeps
/// the nine grey half-pixel edges that bound the back panes.</summary>
public sealed class CubeAxisLineTests
{
    private const double PointsToPixels = 100.0 / 72.0;

    private static readonly Rect Plot = new(0, 0, 500, 500);

    private sealed record Line(Point From, Point To, Color Color, double Width, int Order)
    {
        internal double Length => Math.Sqrt((To.X - From.X) * (To.X - From.X) + (To.Y - From.Y) * (To.Y - From.Y));
    }

    private static List<Line> LinesOf(Theme theme)
    {
        var axes = new Axes { CoordinateSystem = CoordinateSystem.ThreeD };
        axes.Scatter3D([0.0, 1, 2], [0.0, 1, 2], [0.0, 1, 2]);
        var ctx = axes.Drawn(Plot, theme);
        return [.. ctx.Calls.Select((call, order) => call is DrawLineCall a ? new Line(a.P1, a.P2, a.Color, a.Thickness, order) : null).OfType<Line>()];
    }

    /// <summary>The lines that run a whole edge of the box in <paramref name="theme"/>'s text colour: the axis lines (a tick
    /// mark is a few pixels long).</summary>
    private static List<Line> AxisLines(Theme theme) =>
        [.. LinesOf(theme).Where(l => l.Color == theme.ForegroundText && l.Length > 50)];

    private static List<Line> GreyEdges(Theme theme) =>
        [.. LinesOf(theme).Where(l => l.Color == Colors.EdgeGray && l.Width == 0.5)];

    [Theory]
    [InlineData("v2", 0.8)]
    [InlineData("classic", 0.75)]
    public void UnderAMatplotlibTheme_TheBoxIsBoundByThreeAxisLines_AtMatplotlibsWidth(string id, double points)
    {
        var theme = id.AsTheme();

        var axisLines = AxisLines(theme);

        Assert.Equal(3, axisLines.Count);
        Assert.All(axisLines, l => Assert.Equal(points * PointsToPixels, l.Width, 12));
        Assert.Empty(GreyEdges(theme));
    }

    [Theory]
    [InlineData("v2")]
    [InlineData("classic")]
    public void TheAxisLines_AreDrawnAfterTheWallGrid(string id)
    {
        var theme = id.AsTheme();
        var wallGrid = (theme.Looks.Cube.WallGrid ?? theme.DefaultGrid).Color;

        var lines = LinesOf(theme);

        int lastGridLine = lines.Where(l => l.Color == wallGrid).Max(l => l.Order);
        Assert.All(AxisLines(theme), l => Assert.True(l.Order > lastGridLine));
    }

    [Fact]
    public void UnderEveryOtherTheme_TheBoxKeepsItsNineGreyEdges_AndNoAxisLine()
    {
        Assert.Equal(9, GreyEdges(Theme.Default).Count);
        Assert.Empty(AxisLines(Theme.Default));
        Assert.Equal(9, GreyEdges(Theme.Dark).Count);
    }

    [Fact]
    public void TheAxisLineRuleOfEachTheme()
    {
        Assert.Equal(0.8 * PointsToPixels, Theme.MatplotlibV2.Looks.Cube.AxisLineWidth!.Value, 12);
        Assert.Equal(0.75 * PointsToPixels, Theme.MatplotlibClassic.Looks.Cube.AxisLineWidth!.Value, 12);
        Assert.Null(Theme.Default.Looks.Cube.AxisLineWidth);
        Assert.Null(Theme.Dark.Looks.Cube.AxisLineWidth);
    }

    [Theory]
    [InlineData("v2")]
    [InlineData("classic")]
    public void TheAxisLineRule_SurvivesJson_AndAThemeMadeFromIt(string id)
    {
        var theme = id.AsTheme();

        var again = Plt.Create().WithTheme(theme).Build().RoundTripped().Theme;

        Assert.Equal(theme.Looks.Cube.AxisLineWidth, again.Looks.Cube.AxisLineWidth);
        Assert.Equal(theme.Looks.Cube.AxisLineWidth, theme.Cloned().Looks.Cube.AxisLineWidth);
    }
}
