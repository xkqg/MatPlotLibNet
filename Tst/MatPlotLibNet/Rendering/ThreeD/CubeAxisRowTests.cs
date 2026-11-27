// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.ThreeD;

/// <summary>One axis of a 3-D cube as one draw sees it (<see cref="Rendering.ThreeDAxesRenderer.CubeAxisRow"/>): its tick values worked out once, the edge
/// they run along on the screen, the way out of the cube, and the one rule for where a tick of it stands. The grid, the tick
/// marks and labels and the axis title all read it.</summary>
public sealed class CubeAxisRowTests
{
    private static TestTickFormatter Named => new(value => "x" + value.ToString("0", CultureInfo.InvariantCulture));

    private static readonly Rect Square = new(100, 50, 400, 400);

    private static Box3D Cube(Axes axes) => axes.ComputeCubeRange(Theme.Default);

    private static Axes PointsAxes()
    {
        var axes = new Axes { CoordinateSystem = CoordinateSystem.ThreeD };
        axes.Scatter3D([0.0, 1, 2, 3, 4], [0.0, 1, 2, 3, 4], [0.0, 1, 2, 3, 4]);
        return axes;
    }

    private static ThreeDAxesRenderer.CubeAxisRow RowOf(CubeAxis cubeAxis, Axes axes, double elevation = 30, double azimuth = -60,
        bool writes = false)
    {
        var cube = Cube(axes);
        var box = cube;
        var proj = new Projection3D(elevation, azimuth, Square, box.X.Lo, box.X.Hi, box.Y.Lo, box.Y.Hi, box.Z.Lo, box.Z.Hi);
        var centroid = proj.Project((box.X.Lo + box.X.Hi) / 2, (box.Y.Lo + box.Y.Hi) / 2, (box.Z.Lo + box.Z.Hi) / 2);
        var axis = cubeAxis switch { CubeAxis.X => axes.XAxis, CubeAxis.Y => axes.YAxis, _ => axes.ZAxis };
        return ThreeDAxesRenderer.CubeAxisRow.Of(cubeAxis, axis, cube, proj, centroid, Theme.Default,
            new ThreeDAxesRenderer.SceneVertexWriter(new RecordingRenderContext(), proj, writes), Square.Width);
    }

    [Fact]
    public void AnAxisLocator_WinsOverItsSpacing_AndASpacingOverTheDefault()
    {
        var axis = new Axis();
        var edge = new Range1D(0, 3);
        Assert.Equal([0.0, 0.5, 1.0, 1.5, 2.0, 2.5, 3.0], ThreeDAxesRenderer.CubeAxisRow.ComputeAxisTicks(axis, edge, Theme.Default, 400));

        axis.MajorTicks = axis.MajorTicks with { Spacing = 1.5 };
        Assert.Equal([0.0, 1.5, 3.0], ThreeDAxesRenderer.CubeAxisRow.ComputeAxisTicks(axis, edge, Theme.Default, 400));

        axis.TickLocator = new FixedLocator([0.25, 2.75]);
        Assert.Equal([0.25, 2.75], ThreeDAxesRenderer.CubeAxisRow.ComputeAxisTicks(axis, edge, Theme.Default, 400));
    }

    /// <summary>A locator the axis was given is told how many labels fit along the 3-D axes, as a 2-D axis tells it: a 3-D axis is
    /// matplotlib's <c>XAxis</c> (<c>axis3d.XAxis</c> derives from it), whose <c>get_tick_space</c> is the axes' width over three label
    /// sizes, and <c>LogLocator.tick_values</c> reads <c>np.clip(self.axis.get_tick_space(), 2, 9)</c>. 120 px over 3 × 10 px labels
    /// leaves room for 4: matplotlib 3.11.2's <c>LogLocator(numticks=4).tick_values(1, 1e12)</c> in view is 1, 1e4, 1e8, 1e12, where
    /// the nine a locator asks for with no axis give every second power.</summary>
    [Fact]
    public void AGivenLocator_IsToldTheTickSpaceOfTheAxesWidth()
    {
        var axis = new Axis { TickLocator = new LogLocator() };
        axis.MajorTicks = axis.MajorTicks with { LabelSize = 10 };

        Assert.Equal([1.0, 1e4, 1e8, 1e12], ThreeDAxesRenderer.CubeAxisRow.ComputeAxisTicks(axis, new Range1D(1, 1e12), Theme.MatplotlibV2, 120));
    }

    [Fact]
    public void ATick_StandsOnTheEdgeGiveOrTakeATenBillionth_InTheMarginsBelowTheDataToo()
    {
        // matplotlib (axis3d.py) draws every tick inside the view interval; it knows no floor under the data.
        var row = new ThreeDAxesRenderer.CubeAxisRow { Ticks = [], Edge = new Range1D(-1, 3) };

        Assert.True(row.Holds(0));
        Assert.True(row.Holds(3 + 4 * 1e-11));
        Assert.False(row.Holds(3 + 4 * 1e-9));
        Assert.True(row.Holds(-0.5));
        Assert.True(row.Holds(-1 - 4 * 1e-11));
        Assert.False(row.Holds(-1 - 4 * 1e-9));
    }

    [Fact]
    public void OnAReversedEdge_ATickAndAGridLine_StandByTheRuleOfItsInterval()
    {
        var row = new ThreeDAxesRenderer.CubeAxisRow { Ticks = [], Edge = new Range1D(3, -1) };

        Assert.True(row.Holds(0));
        Assert.True(row.Holds(3));
        Assert.True(row.Holds(-1 - 4 * 1e-11));
        Assert.False(row.Holds(3.5));
        Assert.True(row.HoldsGridLine(1.5));
        Assert.False(row.HoldsGridLine(3));
        Assert.False(row.HoldsGridLine(-1));
    }

    [Fact]
    public void TheTicksOfAReversedEdge_AreTheTicksOfItsInterval()
    {
        var axis = new Axis();
        Assert.Equal(ThreeDAxesRenderer.CubeAxisRow.ComputeAxisTicks(axis, new Range1D(-1, 3), Theme.Default, 400), ThreeDAxesRenderer.CubeAxisRow.ComputeAxisTicks(axis, new Range1D(3, -1), Theme.Default, 400));

        axis.MajorTicks = axis.MajorTicks with { Spacing = 1.5 };
        Assert.Equal([0.0, 1.5, 3.0], ThreeDAxesRenderer.CubeAxisRow.ComputeAxisTicks(axis, new Range1D(3, -1), Theme.Default, 400));
    }

    [Fact]
    public void AGridLine_StandsWhereATickStands_ButNotOnAnEndOfTheEdge()
    {
        var row = new ThreeDAxesRenderer.CubeAxisRow { Ticks = [], Edge = new Range1D(0, 3) };

        Assert.False(row.HoldsGridLine(0));
        Assert.False(row.HoldsGridLine(3));
        Assert.True(row.HoldsGridLine(1.5));
        Assert.False(row.HoldsGridLine(4));
    }

    [Fact]
    public void ALabel_IsWrittenByTheAxisFormatter_OrElseInTheOneUniformFormat()
    {
        var axes = PointsAxes();
        var plain = RowOf(CubeAxis.X, axes);
        Assert.Equal("0.5", plain.LabelText(0.5));
        Assert.Equal("1.0", plain.LabelText(1));

        axes.XAxis.TickFormatter = Named;
        Assert.Equal("x1", RowOf(CubeAxis.X, axes).LabelText(1));
    }

    [Theory]
    [InlineData(CubeAxis.X)]
    [InlineData(CubeAxis.Y)]
    [InlineData(CubeAxis.Z)]
    public void ATickPoint_IsTheEdgeWithItsOwnComponentReplacedByTheValue(CubeAxis cubeAxis)
    {
        var row = RowOf(cubeAxis, PointsAxes());
        var point = row.TickPoint(2.5);
        var start = row.Line.From;

        var expected = cubeAxis switch
        {
            CubeAxis.X => start with { X = 2.5 },
            CubeAxis.Y => start with { Y = 2.5 },
            _ => start with { Z = 2.5 },
        };
        Assert.Equal(expected, point);
    }

    [Fact]
    public void TheWayOut_PointsAwayFromTheCube_AndIsOneLong()
    {
        var outward = ThreeDAxesRenderer.CubeAxisRow.OutwardPerp(new Point(0, 0), new Point(10, 0), new Point(5, 5));
        Assert.Equal(new Point(0, -1), outward);

        var flipped = ThreeDAxesRenderer.CubeAxisRow.OutwardPerp(new Point(0, 0), new Point(10, 0), new Point(5, -5));
        Assert.Equal(new Point(0, 1), flipped);
    }

    /// <summary>An edge that has shrunk to a point (a cube seen end on) has no way out: the perpendicular is nothing, and the tick
    /// marks and labels stand on the point instead of at a position that is not a number.</summary>
    [Fact]
    public void TheWayOut_OfAnEdgeOfNoLength_IsNothing_NotNaN()
    {
        var outward = ThreeDAxesRenderer.CubeAxisRow.OutwardPerp(new Point(3, 3), new Point(3, 3), new Point(5, 5));

        Assert.Equal(0.0, outward.X);
        Assert.Equal(0.0, outward.Y);
    }

    /// <summary>The perpendicular the axis title used to be pushed along, computed at its own point on the edge: the oracle the
    /// one way out replaces.</summary>
    private static Point TitlePerpAtItsPoint(Point a, Point b, Point edgePoint, Point centroid, double px)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y;
        double len = Math.Sqrt(dx * dx + dy * dy);
        var perp = new Point(dy / len * px, -dx / len * px);
        double towardX = centroid.X - edgePoint.X;
        double towardY = centroid.Y - edgePoint.Y;
        if (perp.X * towardX + perp.Y * towardY > 0) perp = new Point(-perp.X, -perp.Y);
        return perp;
    }

    [Fact]
    public void TheWayOutTimesThePad_EqualsThePerpendicularAtTheTitlesOwnPoint_OverACameraSweep()
    {
        // The title sits at 0.35 of the Y edge and the middle of the others; a perpendicular is the same at every point of a
        // line, so the one way out, scaled by the pad, must give the same pixels at every camera, past +-90 degrees too.
        var axes = PointsAxes();
        int compared = 0;
        foreach (double elevation in (double[])[-80, -45, -10, 0, 15, 30, 60, 85])
        {
            for (double azimuth = -180; azimuth <= 180; azimuth += 5)
            {
                foreach (var (cubeAxis, fraction) in ((CubeAxis, double)[])[(CubeAxis.X, 0.5), (CubeAxis.Y, 0.35), (CubeAxis.Z, 0.5)])
                {
                    var cube = Cube(axes);
                    var box = cube;
                    var proj = new Projection3D(elevation, azimuth, Square, box.X.Lo, box.X.Hi, box.Y.Lo, box.Y.Hi, box.Z.Lo,
                        box.Z.Hi);
                    var centroid = proj.Project((box.X.Lo + box.X.Hi) / 2, (box.Y.Lo + box.Y.Hi) / 2,
                        (box.Z.Lo + box.Z.Hi) / 2);
                    var row = ThreeDAxesRenderer.CubeAxisRow.Of(cubeAxis, axes.XAxis, cube, proj, centroid, Theme.Default,
                        new ThreeDAxesRenderer.SceneVertexWriter(new RecordingRenderContext(), proj, false), 400);
                    var a = row.From;
                    var b = row.To;
                    if (Math.Abs(b.X - a.X) + Math.Abs(b.Y - a.Y) < 1e-6) continue;
                    var point = new Point(a.X + (b.X - a.X) * fraction, a.Y + (b.Y - a.Y) * fraction);
                    const double pad = 37.25;

                    var expected = TitlePerpAtItsPoint(a, b, point, centroid, pad);
                    Assert.Equal(expected, new Point(row.Outward.X * pad, row.Outward.Y * pad));
                    compared++;
                }
            }
        }
        Assert.True(compared > 1500, $"only {compared} cameras compared");
    }

    [Fact]
    public void TheTitlePad_WithNoTitleOrHiddenTicks_IsTheLabelOffsetAndTheGap()
    {
        var axes = PointsAxes();
        var ctx = new SvgRenderContext();
        var major = axes.XAxis.MajorTicks;
        double bare = major.Length + major.Pad + ThreeDAxesRenderer.CubeAxisRow.ThreeDExtraPad + ThreeDAxesRenderer.CubeAxisRow.TitleGapPx;

        Assert.Equal(bare, RowOf(CubeAxis.X, axes).TitlePad(ctx, axes.XAxis, axes.XAxis.Label, Theme.Default.LabelFont()));

        axes.XAxis.Label = "x";
        axes.XAxis.MajorTicks = major with { Visible = false };
        Assert.Equal(bare, RowOf(CubeAxis.X, axes).TitlePad(ctx, axes.XAxis, axes.XAxis.Label, Theme.Default.LabelFont()));
    }

    [Fact]
    public void TheTitlePad_ClearsTheWidestLabelDrawn_InTheLabelsOwnSizeWhateverItsColour()
    {
        var axes = PointsAxes();
        axes.XAxis.Label = "a title";
        var ctx = new SvgRenderContext();
        double plain = RowOf(CubeAxis.X, axes).TitlePad(ctx, axes.XAxis, axes.XAxis.Label, Theme.Default.LabelFont());

        axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { LabelSize = 30 };
        double large = RowOf(CubeAxis.X, axes).TitlePad(ctx, axes.XAxis, axes.XAxis.Label, Theme.Default.LabelFont());
        axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { LabelColor = Colors.Red };
        double largeRed = RowOf(CubeAxis.X, axes).TitlePad(ctx, axes.XAxis, axes.XAxis.Label, Theme.Default.LabelFont());

        Assert.True(large > plain, $"{large} <= {plain}");
        Assert.Equal(large, largeRed);
    }

    [Fact]
    public void TheEdgeWire_IsWrittenOnceForTheRow_OnlyWhenTheSceneWritesVertexData()
    {
        var axes = PointsAxes();
        Assert.Null(RowOf(CubeAxis.X, axes).EdgeWire);

        var row = RowOf(CubeAxis.X, axes, writes: true);
        Assert.Matches(new Regex("^[-0-9.eE]+,[-0-9.eE]+,[-0-9.eE]+ [-0-9.eE]+,[-0-9.eE]+,[-0-9.eE]+$", RegexOptions.None,
            TimeSpan.FromSeconds(5)), row.EdgeWire!);
    }

    [Fact]
    public void ADraw_AsksEachLocatorForItsTicksOnce()
    {
        var x = new TestTickLocator((min, max) => [min, (min + max) / 2, max]);
        var y = new TestTickLocator((min, max) => [min, (min + max) / 2, max]);
        var z = new TestTickLocator((min, max) => [min, (min + max) / 2, max]);
        var figure = Plt.Create()
            .WithSize(600, 500)
            .AddSubPlot(1, 1, 1, ax => ax.Scatter3D([0.0, 1, 2], [0.0, 1, 2], [0.0, 1, 2])
                .SetXLabel("x").SetYLabel("y").SetZLabel("z"))
            .Build();
        var axes = figure.SubPlots[0];
        axes.XAxis.TickLocator = x;
        axes.YAxis.TickLocator = y;
        axes.ZAxis.TickLocator = z;

        _ = figure.ToSvg();

        Assert.Equal((1, 1, 1), (x.Calls, y.Calls, z.Calls));
    }

    [Fact]
    public void SetZLimits_AreTheEdgeTheTickRowEndsAt()
    {
        var figure = Plt.Create()
            .WithSize(600, 500)
            .AddSubPlot(1, 1, 1, ax => ax.Scatter3D([0.0, 1, 2], [0.0, 1, 2], [-5.0, 0, 5]).SetZLim(-2, 2))
            .Build();
        var z = figure.SubPlots[0].ZAxis;
        z.TickLocator = new FixedLocator([-3.0, -2, -1, 0, 1, 2, 3]);
        z.TickFormatter = Named;

        var svg = figure.ToSvg();
        string[] labels = [.. svg.Elements("text").Select(text => text.Text).Where(text => text.StartsWith('x')).Order(StringComparer.Ordinal)];

        Assert.Equal(["x-1", "x-2", "x0", "x1", "x2"], labels);
        Assert.Equal(new Range1D(-2, 2), figure.SubPlots[0].ComputeCubeRange(figure.Theme).Z);
    }
}
