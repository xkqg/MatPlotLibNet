// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.ThreeD;

/// <summary>Under the matplotlib themes the walls of a 3-D box carry a grid line at every tick of an axis in view, minor ticks
/// included. matplotlib 3.11.2, printed for a 3-D axes with <c>set_xlim(1, 3)</c>, a <c>FixedLocator([1, 2, 3])</c> and, then,
/// <c>xaxis.set_minor_locator(AutoMinorLocator(5))</c>: <c>xaxis.gridlines.get_segments()</c> has 3 segments before the minor locator
/// and 11 after it (3 majors and 8 minors), in the default and in the classic style, and <c>ax.grid(True, which='major')</c> leaves
/// all 11: <c>axis3d.Axis.draw_grid</c> draws one line for each tick <c>_update_ticks()</c> returns, major and minor, and a 3-D axes
/// has no <c>which</c> to choose between them. The library's own themes draw major lines only, as they always did.</summary>
public sealed class ThreeDMinorWallGridTests
{
    private static Figure Cube(Theme theme, bool minorTicks)
    {
        var figure = Plt.Create()
            .WithSize(600, 500)
            .WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax => ax.Scatter3D([0.0, 1, 2, 3, 4], [0.0, 1, 2, 3, 4], [0.0, 1, 2, 3, 4])
                .WithGrid(g => g with { Color = Colors.Green }))
            .Build();
        var axes = figure.SubPlots[0];
        axes.Emit3DVertexData = true;
        // The X edge runs from 1 to 3 with majors at 1, 2 and 3; the five-way split is 0.2, which is matplotlib's AutoMinorLocator(5).
        axes.XAxis.Min = 1;
        axes.XAxis.Max = 3;
        axes.XAxis.TickLocator = new FixedLocator([1.0, 2, 3]);
        axes.XAxis.MinorTicks = axes.XAxis.MinorTicks with { Visible = minorTicks, Color = Colors.Red };
        return figure;
    }

    private static List<(double X1, double X2)> Lines(string svg, string stroke) =>
        [.. svg.Elements("line").Where(line => line["stroke"] == stroke && line["data-v3d"] is not null).Select(line => line.Vertices())
            .Where(vertices => vertices.Length > 1).Select(vertices => (vertices[0][0], vertices[1][0]))];

    /// <summary>The normalised X of each wall line that runs across the X axis, inside the cube (a line at one X): one for each wall
    /// a tick of the X axis stands on.</summary>
    private static double[] XStripes(string svg)
    {
        var grid = Lines(svg, Colors.Green.ToHex());
        double lo = grid.Min(l => Math.Min(l.X1, l.X2)), hi = grid.Max(l => Math.Max(l.X1, l.X2));
        return [.. grid.Where(l => l.X1 == l.X2 && l.X1 > lo + 1e-6 && l.X1 < hi - 1e-6).Select(l => l.X1).Order()];
    }

    [Theory]
    [InlineData("matplotlib-v2")]
    [InlineData("matplotlib-classic")]
    public void UnderTheMatplotlibThemes_AMinorTickHasAWallLineToo(string themeName)
    {
        var theme = themeName.AsTheme();

        var svg = Cube(theme, minorTicks: true).ToSvg();
        double[] stripes = XStripes(svg);
        double[] minorMarks = [.. Lines(svg, Colors.Red.ToHex()).Select(l => l.X1).Distinct().Order()];

        // The major at 2 (the ones at the ends stand on the cube's own edges) and the eight minors, on the floor and on the Y wall.
        Assert.Equal(8, minorMarks.Length);
        Assert.Equal(2 * (1 + 8), stripes.Length);
        Assert.Equal(1 + 8, stripes.Distinct().Count());
        Assert.All(minorMarks, m => Assert.Contains(stripes, s => Math.Abs(s - m) < 1e-6));
    }

    [Fact]
    public void UnderTheMatplotlibThemes_WithNoMinorTicks_TheWallsCarryTheMajorLinesOnly()
    {
        double[] stripes = XStripes(Cube(Theme.MatplotlibV2, minorTicks: false).ToSvg());

        Assert.Equal(2, stripes.Length);
    }

    [Fact]
    public void UnderTheLibrarysOwnThemes_TheWallsCarryTheMajorLinesOnly_WhateverTheMinorTicks()
    {
        double[] stripes = XStripes(Cube(Theme.Default, minorTicks: true).ToSvg());

        Assert.Equal(2, stripes.Length);
    }

    [Fact]
    public void ARowsWallTicks_AreItsMajorsAndItsMinors_UnderAThemeWhoseGridLineIsATicks()
    {
        var axes = new Axes();
        axes.XAxis.MinorTicks = axes.XAxis.MinorTicks with { Visible = true };
        var proj = new Projection3D(30, -60, new Rect(0, 0, 400, 400), 1, 3, 0, 4, 0, 4);
        var cube = new Box3D(1, 3, 0, 4, 0, 4);
        var centroid = proj.Project(2, 2, 2);
        axes.XAxis.TickLocator = new FixedLocator([1.0, 2, 3]);

        var themed = ThreeDAxesRenderer.CubeAxisRow.Of(CubeAxis.X, axes.XAxis, cube, proj, centroid, Theme.MatplotlibV2, new ThreeDAxesRenderer.SceneVertexWriter(new RecordingRenderContext(), proj, writes: false), 400);
        var own = ThreeDAxesRenderer.CubeAxisRow.Of(CubeAxis.X, axes.XAxis, cube, proj, centroid, Theme.Default, new ThreeDAxesRenderer.SceneVertexWriter(new RecordingRenderContext(), proj, writes: false), 400);

        // The wall lines stand where a grid line stands: the three majors, the ends included (matplotlib's 11 with the eight minors).
        Assert.Equal(3 + 8, themed.GridTicks.Count(themed.HoldsGridLine));
        Assert.Equal(3, themed.GridTicks.Count(t => themed.Ticks.Contains(t)));
        Assert.Equal([1.0, 2, 3], own.GridTicks);
        Assert.Same(own.Ticks, own.GridTicks);
    }

    /// <summary>matplotlib 3.11.2, printed above: <c>set_xlim(1, 3)</c> with the ticks 1, 2, 3 has 3 wall segments along that axis, one at
    /// each tick, the two at the ends included, where the library left the lines on an end of the edge out "where the cube draws its own
    /// edge". The matplotlib themes bound the box with axis lines instead of the nine edges, so no edge is there to stand for them.</summary>
    [Fact]
    public void UnderTheMatplotlibThemes_AWallLineStandsOnAnEndOfTheEdgeToo_WhereTheCubeDrawsNoEdgeOfItsOwn()
    {
        var axes = new Axes();
        axes.XAxis.TickLocator = new FixedLocator([1.0, 2, 3]);
        var proj = new Projection3D(30, -60, new Rect(0, 0, 400, 400), 1, 3, 0, 4, 0, 4);
        var cube = new Box3D(1, 3, 0, 4, 0, 4);
        var centroid = proj.Project(2, 2, 2);
        var vertices = new ThreeDAxesRenderer.SceneVertexWriter(new RecordingRenderContext(), proj, writes: false);

        var themed = ThreeDAxesRenderer.CubeAxisRow.Of(CubeAxis.X, axes.XAxis, cube, proj, centroid, Theme.MatplotlibV2, vertices, 400);
        var own = ThreeDAxesRenderer.CubeAxisRow.Of(CubeAxis.X, axes.XAxis, cube, proj, centroid, Theme.Default, vertices, 400);

        Assert.Equal(3, themed.GridTicks.Count(themed.HoldsGridLine));
        Assert.Equal(1, own.GridTicks.Count(own.HoldsGridLine));
    }
}
