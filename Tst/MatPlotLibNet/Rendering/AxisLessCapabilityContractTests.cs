// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Serialization;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Whether a series needs the axes around it is a fact about the series, not a list the axes keeps: a series that
/// draws without axes says so (<c>ChartSeries.DrawsWithoutAxes</c>), and the axes draws no grid, ticks or frame when every
/// series on it does. The membership is held to a measurement: a series draws the same marks whatever the limits of the axes
/// are, or it moves with them. Every shipped series type stands in exactly one row of the table below, so a type that is new
/// has to decide: a type that moves with the limits needs no row; one that does not move must be a row, and the row says why.</summary>
public class AxisLessCapabilityContractTests
{
    private static readonly Rect Plot = new(100, 50, 400, 300);

    /// <summary>The series that fill the plot without the axes around them: nothing they draw is a position on an axis, so the
    /// axes draws no grid, ticks or frame for them. Each one's marks stay where they are when the limits change.</summary>
    private static readonly Type[] DrawsWithoutAxes =
    [
        typeof(RadarSeries), typeof(PieSeries), typeof(WaffleSeries), typeof(DonutSeries), typeof(FunnelSeries), typeof(GaugeSeries),
        typeof(ProgressBarSeries), typeof(BulletGraphSeries), typeof(StatTileSeries), typeof(SparklineSeries), typeof(PiperSeries), typeof(ChordSeries), typeof(WordCloudSeries),
        typeof(TableSeries), typeof(TreeGridSeries), typeof(TreemapSeries), typeof(SunburstSeries), typeof(IcicleSeries), typeof(DendrogramSeries),
        typeof(ClustermapSeries), typeof(PairGridSeries), typeof(SankeySeries),
    ];

    /// <summary>The series that stand on a coordinate system of their own, polar, 3-D or ternary, drawn by the renderer of that system:
    /// the limits of the Cartesian axes are not theirs, so their marks do not move with them, and they are not axis-less.</summary>
    private static readonly Type[] OwnCoordinateSystem =
    [
        typeof(PolarLineSeries), typeof(PolarScatterSeries), typeof(PolarBarSeries), typeof(PolarHeatmapSeries),
        typeof(TernaryScatterSeries), typeof(TernaryLineSeries),
        typeof(SurfaceSeries), typeof(WireframeSeries), typeof(Contour3DSeries), typeof(Scatter3DSeries), typeof(Stem3DSeries),
        typeof(Bar3DSeries), typeof(PlanarBar3DSeries), typeof(Line3DSeries), typeof(Trisurf3DSeries), typeof(Quiver3DSeries),
        typeof(Text3DSeries), typeof(VoxelSeries),
    ];

    /// <summary>What the series draws, as SVG, on a plot of 400 by 300 under the limits given.</summary>
    private static string Drawn(ISeries series, double xMin, double xMax, double yMin, double yMax)
    {
        var svg = new SvgRenderContext();
        try
        {
            series.Accept(new SvgSeriesRenderer(new DataTransform(xMin, xMax, yMin, yMax, Plot), svg, Colors.Tab10Blue, Theme.Default,
                plotArea: Plot), new RenderArea(Plot, svg));
        }
        catch (Exception)
        {
            // A series that cannot be drawn on bare axes at all draws nothing here, and so moves with nothing.
            return "";
        }

        return svg.GetOutput();
    }

    /// <summary>Whether the marks of the series stay where they are when the limits of the axes change: the series is drawn
    /// under two sets of limits that have nothing in common, and it has to draw something.</summary>
    private static bool DrawsTheSameUnderAnyLimits(ISeries series)
    {
        string first = Drawn(series, 0, 1, 0, 1), second = Drawn(series, -50, 80, 3, 900);
        return first.Length > 0 && first == second;
    }

    /// <summary>The series a kind is measured with: the roster's instance, but for the three hierarchies whose roster instance is a root
    /// alone and so draws nothing to measure, which are measured over a root with one leaf.</summary>
    private static ISeries DrawnHere(string type) => type switch
    {
        nameof(TreemapSeries) => new TreemapSeries(RootWithALeaf()),
        nameof(SunburstSeries) => new SunburstSeries(RootWithALeaf()),
        nameof(IcicleSeries) => new IcicleSeries(RootWithALeaf()),
        _ => DrawnSeriesFixtures.Instance(type),
    };

    private static TreeNode RootWithALeaf() => new() { Label = "Root", Children = [new TreeNode { Label = "A", Value = 10 }] };

    [Fact]
    public void NoTypeStandsInTwoRows()
    {
        var all = DrawsWithoutAxes.Concat(OwnCoordinateSystem).ToList();

        Assert.Equal(all.Count, all.Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(DrawnSeriesFixtures.TypeNames), MemberType = typeof(DrawnSeriesFixtures))]
    public void EveryShippedSeries_MovesWithTheLimits_OrStandsInExactlyOneRowThatSaysWhyNot(string type)
    {
        var series = DrawnHere(type);
        bool staysPut = DrawsTheSameUnderAnyLimits(series);
        bool inARow = DrawsWithoutAxes.Contains(series.GetType()) || OwnCoordinateSystem.Contains(series.GetType());

        Assert.True(staysPut == inARow,
            staysPut
                ? $"{type} draws the same marks under any limits and is in no row: it either draws without axes, stands on a coordinate system of its own, or ignores limits it should follow."
                : $"{type} moves with the limits (or draws nothing here) and stands in a row that says it does not.");
    }

    [Theory]
    [MemberData(nameof(DrawnSeriesFixtures.TypeNames), MemberType = typeof(DrawnSeriesFixtures))]
    public void ASeriesDeclaresItDrawsWithoutAxes_ExactlyWhenItIsInThatRow(string type)
    {
        var series = DrawnSeriesFixtures.Instance(type);
        bool declared = series is ChartSeries { DrawsWithoutAxes: true };

        Assert.True(declared == DrawsWithoutAxes.Contains(series.GetType()),
            $"{type}: DrawsWithoutAxes is {declared}, the table says {!declared}.");
    }

    /// <summary>An image stands on its extent through the transform, under the library's own themes too, so its cells move with
    /// the limits and it needs no row: it draws something, and something else under other limits.</summary>
    [Fact]
    public void AnImage_MovesWithTheLimits()
    {
        var image = new ImageSeries(new double[,] { { 1, 2 }, { 3, 4 } });
        string first = Drawn(image, 0, 1, 0, 1), second = Drawn(image, -50, 80, 3, 900);

        Assert.NotEqual("", first);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void ALineSeries_NeedsTheAxes() =>
        Assert.False(new LineSeries([1.0], [2.0]).DrawsWithoutAxes);
}
