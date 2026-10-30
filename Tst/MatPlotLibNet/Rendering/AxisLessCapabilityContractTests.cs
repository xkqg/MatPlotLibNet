// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Models.Series;

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
        typeof(ProgressBarSeries), typeof(BulletGraphSeries), typeof(StatTileSeries), typeof(SparklineSeries),
        typeof(TableSeries), typeof(TreeGridSeries), typeof(TreemapSeries), typeof(SunburstSeries), typeof(IcicleSeries), typeof(DendrogramSeries),
        typeof(ClustermapSeries), typeof(PairGridSeries), typeof(SankeySeries),
    ];

    /// <summary>The series that stand on a coordinate system of their own, polar or 3-D, drawn by the renderer of that system:
    /// the limits of the Cartesian axes are not theirs, so their marks do not move with them, and they are not axis-less.</summary>
    private static readonly Type[] OwnCoordinateSystem =
    [
        typeof(PolarLineSeries), typeof(PolarScatterSeries), typeof(PolarBarSeries), typeof(PolarHeatmapSeries),
        typeof(SurfaceSeries), typeof(WireframeSeries), typeof(Contour3DSeries), typeof(Scatter3DSeries), typeof(Stem3DSeries),
        typeof(Bar3DSeries), typeof(PlanarBar3DSeries), typeof(Line3DSeries), typeof(Trisurf3DSeries), typeof(Quiver3DSeries),
        typeof(Text3DSeries), typeof(VoxelSeries),
    ];

    /// <summary>The series that fill the plot rectangle whatever its limits are, and are read against the axes all the same:
    /// the cells of an image or of a 2-D histogram are positions on an axis, and the ticks say which. Their renderers ignore
    /// the limits the axes was given (a defect the measurement shows, not a property), so these keep their axes until they
    /// follow the limits.</summary>
    private static readonly Type[] FillsThePlotButIsReadAgainstTheAxes = [typeof(ImageSeries), typeof(Histogram2DSeries)];

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

    [Fact]
    public void NoTypeStandsInTwoRows()
    {
        var all = DrawsWithoutAxes.Concat(OwnCoordinateSystem).Concat(FillsThePlotButIsReadAgainstTheAxes).ToList();

        Assert.Equal(all.Count, all.Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(AllSeriesTests.AllSeriesInstances), MemberType = typeof(AllSeriesTests))]
    public void EveryShippedSeries_MovesWithTheLimits_OrStandsInExactlyOneRowThatSaysWhyNot(ISeries series, string name)
    {
        var type = series.GetType();
        bool staysPut = DrawsTheSameUnderAnyLimits(series);
        bool inARow = DrawsWithoutAxes.Contains(type) || OwnCoordinateSystem.Contains(type)
            || FillsThePlotButIsReadAgainstTheAxes.Contains(type);

        Assert.True(staysPut == inARow,
            staysPut
                ? $"{name} draws the same marks under any limits and is in no row: it either draws without axes, stands on a coordinate system of its own, or ignores limits it should follow."
                : $"{name} moves with the limits (or draws nothing here) and stands in a row that says it does not.");
    }

    [Theory]
    [MemberData(nameof(AllSeriesTests.AllSeriesInstances), MemberType = typeof(AllSeriesTests))]
    public void ASeriesDeclaresItDrawsWithoutAxes_ExactlyWhenItIsInThatRow(ISeries series, string name)
    {
        bool declared = series is ChartSeries { DrawsWithoutAxes: true };

        Assert.True(declared == DrawsWithoutAxes.Contains(series.GetType()),
            $"{name}: DrawsWithoutAxes is {declared}, the table says {!declared}.");
    }

    [Fact]
    public void ALineSeries_NeedsTheAxes() =>
        Assert.False(new LineSeries([1.0], [2.0]).DrawsWithoutAxes);
}
