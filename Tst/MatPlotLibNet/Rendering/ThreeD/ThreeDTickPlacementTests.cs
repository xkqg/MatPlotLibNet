// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.ThreeD;

/// <summary>Where a tick of a 3-D axis stands: on its edge, the ends included give or take a ten-billionth of its length (the
/// rule of the 2-D axes), below the lowest value its data reaches too, as matplotlib draws every tick in view. The major marks,
/// their labels, the minor marks and the grid lines on the walls all stand by that one rule, at the same tick values.</summary>
public sealed class ThreeDTickPlacementTests
{
    private const string TickPrefix = "tick ";

    /// <summary>Names a tick so its label can be told from any other text.</summary>
    private static TestTickFormatter Prefixed => new(value => TickPrefix + value.ToString("0", CultureInfo.InvariantCulture));

    /// <summary>One line of the SVG with its two vertex points, normalised (linear in the data value).</summary>
    private readonly record struct Line3D(double Nx1, double Nx2);

    /// <summary>Every line of <paramref name="stroke"/> that carries two vertex points.</summary>
    private static List<Line3D> Lines(string svg, string stroke) =>
        [.. svg.Elements("line").Where(line => line["stroke"] == stroke && line["data-v3d"] is not null).Select(line => line.Vertices())
            .Where(vertices => vertices.Length > 1).Select(vertices => new Line3D(vertices[0][0], vertices[1][0]))];

    /// <summary>The normalised X of every grid line that runs across the X axis (both ends at one X inside the cube): the
    /// grid lines drawn at an X tick.</summary>
    private static double[] XStripes(string svg)
    {
        var grid = Lines(svg, Colors.Green.ToHex());
        double lo = grid.Min(l => Math.Min(l.Nx1, l.Nx2)), hi = grid.Max(l => Math.Max(l.Nx1, l.Nx2));
        return [.. grid.Where(l => l.Nx1 == l.Nx2 && l.Nx1 > lo + 1e-6 && l.Nx1 < hi - 1e-6).Select(l => l.Nx1).Order()];
    }

    /// <summary>The normalised X of every major mark of the X axis (blue).</summary>
    private static double[] MajorMarks(string svg) =>
        [.. Lines(svg, Colors.Blue.ToHex()).Select(l => l.Nx1).Order()];

    private static Figure Bars() => Plt.Create()
        .WithSize(600, 500)
        .AddSubPlot(1, 1, 1, ax => ax.Bar3D([0.0, 1, 2], [0.0, 1, 2], [1.0, 2, 3]).WithGrid(g => g with { Color = Colors.Green }))
        .Build();

    private static Figure Points(Action<Axes> setUp)
    {
        var figure = Plt.Create()
            .WithSize(600, 500)
            .AddSubPlot(1, 1, 1, ax => ax.Scatter3D([0.0, 1, 2, 3, 4], [0.0, 1, 2, 3, 4], [0.0, 1, 2, 3, 4])
                .WithGrid(g => g with { Color = Colors.Green }))
            .Build();
        var axes = figure.SubPlots[0];
        axes.Emit3DVertexData = true;
        axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { Color = Colors.Blue };
        setUp(axes);
        return figure;
    }

    [Fact]
    public void AWallGridLine_StandsAtEveryMark_BelowTheLowestBarToo()
    {
        // Bars from x = 0: the cube's margins run below 0, so a tick at -0.1 lies on the edge below the lowest bar. matplotlib
        // draws every tick in view, so the axis draws its mark and its label there, and the walls a line.
        var figure = Bars();
        var axes = figure.SubPlots[0];
        axes.Emit3DVertexData = true;
        axes.XAxis.TickLocator = new FixedLocator([-0.1, 0.0, 1, 2]);
        axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { Color = Colors.Blue };

        var svg = figure.ToSvg();
        double[] majors = MajorMarks(svg);
        double[] stripes = XStripes(svg);

        Assert.Equal(4, majors.Length);
        Assert.Contains(stripes, s => Math.Abs(s - majors[0]) < 1e-6);
        Assert.All(stripes, s => Assert.Contains(majors, m => Math.Abs(m - s) < 1e-6));
    }

    [Fact]
    public void AMarkAndItsLabel_StandOnTheEdge_BelowTheLowestDataValue_AsMatplotlibDrawsThem()
    {
        // The stems of a spiral (matplotlib's stem3d reference) reach x = cos(2 pi 9 / 19), about -0.986, and y about -0.997 at
        // their lowest; the cube's margins carry both edges past -1.1, and matplotlib draws the tick at -1.00 on each.
        double[] theta = [.. Enumerable.Range(0, 20).Select(i => 2 * Math.PI * i / 19)];
        double[] x = [.. theta.Select(Math.Cos)];
        double[] y = [.. theta.Select(Math.Sin)];
        var svg = Plt.Create()
            .WithSize(800, 600)
            .AddSubPlot(1, 1, 1, ax => ax.Stem3D(x, y, theta))
            .Build()
            .ToSvg();

        string[] labels = [.. svg.Elements("text").Select(text => text.Text)];
        Assert.Equal(2, labels.Count(l => l == "−1.00"));   // matplotlib writes the true minus sign (axes.unicode_minus)
    }

    [Fact]
    public void NoMark_StandsBelowTheFloorABarRisesFrom()
    {
        // The bars rise from z = 0, which holds the low end of the Z edge at 0 (the bars' sticky floor): a tick at -0.5 lies
        // past that end, where no mark stands.
        var figure = Bars();
        var axes = figure.SubPlots[0];
        axes.Emit3DVertexData = true;
        axes.ZAxis.TickLocator = new FixedLocator([-0.5, 0.0, 1, 2, 3]);
        axes.ZAxis.MajorTicks = axes.ZAxis.MajorTicks with { Color = Colors.Blue };

        int zMarks = figure.ToSvg().Elements("line").Count(line => line["stroke"] == Colors.Blue.ToHex());
        Assert.Equal(4, zMarks);
    }

    [Fact]
    public void AWallGridLine_StandsOnAMajorMark_WhenTheAxisSetsItsOwnSpacing()
    {
        // Majors every 0.7 by the axis' own spacing; the walls used to draw their lines at the default step of 0.5.
        var svg = Points(axes => axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { Spacing = 0.7 }).ToSvg();
        double[] majors = MajorMarks(svg);
        double[] stripes = XStripes(svg);

        Assert.NotEmpty(stripes);
        Assert.All(stripes, s => Assert.Contains(majors, m => Math.Abs(m - s) < 1e-6));
    }

    [Fact]
    public void AMajorMarkAndItsLabel_StandNowhereBeyondTheEdge()
    {
        // The X edge runs from 1 to 3; a locator of the caller's own also hands back 0 and 4, past each end (0 is above the
        // data floor, so only the edge can leave it out).
        var svg = Points(axes =>
        {
            axes.XAxis.Min = 1;
            axes.XAxis.Max = 3;
            axes.XAxis.TickLocator = new TestTickLocator((min, max) => [min - 1, min, (min + max) / 2, max, max + 1]);
            axes.XAxis.TickFormatter = Prefixed;
        }).ToSvg();

        Assert.Equal(3, MajorMarks(svg).Length);
        string[] labels = [.. svg.Elements("text").Where(text => text.Text.StartsWith(TickPrefix, StringComparison.Ordinal)).Select(text => text.Text[TickPrefix.Length..]).Order()];
        Assert.Equal(["1", "2", "3"], labels);
    }

    [Fact]
    public void AMinorMark_StandsNowhereBeyondTheEdge_LikeTheMajorMarks()
    {
        // The X edge runs from 1 to 3 with majors at 1, 2 and 3; the five-way split runs a step past each end, to 0.8 and
        // 3.2, above the data floor of 0, so only the edge leaves them out.
        var svg = Points(axes =>
        {
            axes.XAxis.Min = 1;
            axes.XAxis.Max = 3;
            axes.XAxis.TickLocator = new FixedLocator([1.0, 2, 3]);
            axes.XAxis.MinorTicks = axes.XAxis.MinorTicks with { Visible = true, Color = Colors.Red };
        }).ToSvg();
        double[] majors = MajorMarks(svg);
        double[] minors = [.. Lines(svg, Colors.Red.ToHex()).Select(l => l.Nx1).Order()];

        Assert.Equal(3, majors.Length);
        Assert.Equal(8, minors.Length);
        Assert.All(minors, m => Assert.InRange(m, majors[0], majors[^1]));
    }

    [Fact]
    public void AReversedLimit_DrawsTheTicksOfItsInterval_AsTheForwardLimitDoes()
    {
        // matplotlib's set_xlim3d(4, -1) draws the X axis from 4 down to -1 with the ticks of -1 to 4; the row asked for ticks
        // from 4 down to -1 and was handed none, so the axis drew no mark, no label and no grid line.
        string[] LabelsBetween(double min, double max)
        {
            var svg = Points(axes =>
            {
                axes.XAxis.Min = min;
                axes.XAxis.Max = max;
                axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { Spacing = 1 };
                axes.XAxis.TickFormatter = Prefixed;
            }).ToSvg();
            return [.. svg.Elements("text").Where(text => text.Text.StartsWith(TickPrefix, StringComparison.Ordinal)).Select(text => text.Text[TickPrefix.Length..]).Order(StringComparer.Ordinal)];
        }

        // Every tick in view stands, -1 at the set limit too, though the data does not reach it.
        string[] forward = LabelsBetween(-1, 4);
        Assert.Equal(["-1", "0", "1", "2", "3", "4"], forward);
        Assert.Equal(forward, LabelsBetween(4, -1));
    }

    [Fact]
    public void AReversedLimit_PutsEachMarkWhereTheForwardLimitPutsItsMirror()
    {
        double[] MarksBetween(double min, double max) => MajorMarks(Points(axes =>
        {
            axes.XAxis.Min = min;
            axes.XAxis.Max = max;
            axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { Color = Colors.Blue, Spacing = 1 };
        }).ToSvg());

        double[] forward = MarksBetween(-1, 4);
        double[] reversed = MarksBetween(4, -1);

        Assert.Equal(6, forward.Length);
        Assert.Equal([.. forward.Select(n => -n).Order()], reversed, (a, b) => Math.Abs(a - b) < 1e-5);
    }

    [Fact]
    public void AReversedLimit_HandsTheLocatorItsEndsLowToHigh_AndDrawsItsGridLines()
    {
        var locator = new TestTickLocator(0.0, 1, 2, 3);
        var svg = Points(axes =>
        {
            axes.XAxis.Min = 4;
            axes.XAxis.Max = -1;
            axes.XAxis.TickLocator = locator;
        }).ToSvg();

        Assert.Equal(new MatPlotLibNet.Rendering.Range1D(-1.0, 4.0), locator.Asked[^1]);
        Assert.Equal(4, MajorMarks(svg).Length);
        // A line at 0, 1, 2 and 3 on the floor and on the Y wall.
        Assert.Equal(8, XStripes(svg).Length);
    }
}
