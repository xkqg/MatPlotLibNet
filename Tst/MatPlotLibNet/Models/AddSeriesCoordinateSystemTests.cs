// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Tests.Serialization;

namespace MatPlotLibNet.Tests.Models;

/// <summary>A kind of series that is drawn in a coordinate system of its own (polar, ternary, 3-D) says so once
/// (<c>ChartSeries.DrawnIn</c>), and <see cref="Axes.AddSeries{T}"/> puts the axes in it: a series added as it is draws as the verb that
/// makes it draws, never on a Cartesian axes. Held for every series kind under the drawn-series contract, against the axes its verb
/// builds.</summary>
public class AddSeriesCoordinateSystemTests
{
    /// <summary>The kinds a verb puts the axes in a coordinate system of its own for (<c>AxesPolarVerbs</c>, <c>AxesTernaryVerbs</c>,
    /// <c>AxesThreeDVerbs</c>; a Piper diagram stays Cartesian), and every other kind under the drawn-series contract, which leaves it
    /// Cartesian.</summary>
    public static TheoryData<string, CoordinateSystem> Kinds { get; } = new(DrawnSeriesFixtures.DrawnTypes.Select(t => (t.Name, t.Name switch
    {
        "PolarLineSeries" or "PolarScatterSeries" or "PolarBarSeries" or "PolarHeatmapSeries" => CoordinateSystem.Polar,
        "TernaryLineSeries" or "TernaryScatterSeries" => CoordinateSystem.Ternary,
        "SurfaceSeries" or "WireframeSeries" or "Scatter3DSeries" or "Stem3DSeries" or "Bar3DSeries" or "PlanarBar3DSeries"
            or "Line3DSeries" or "Trisurf3DSeries" or "Contour3DSeries" or "Quiver3DSeries" or "VoxelSeries" or "Text3DSeries"
            => CoordinateSystem.ThreeD,
        _ => CoordinateSystem.Cartesian,
    })));

    [Theory]
    [MemberData(nameof(Kinds))]
    public void ASeriesAddedAsItIs_PutsTheAxesInTheCoordinateSystemItsVerbDoes(string kind, CoordinateSystem expected)
    {
        var series = DrawnSeriesFixtures.Instance(kind);

        var axes = new Axes();
        axes.AddSeries(series);

        Assert.Equal(expected, axes.CoordinateSystem);
    }

    /// <summary>The seats a series can be added at besides the primary Y axis: a Y axis past it on either side and the top X axis. Each is
    /// a real seat (<see cref="YAxisSlot.AddSeries{TSeries}"/>, <see cref="Axes.AddXSecondarySeries{TSeries}"/>), so each asks the series
    /// for the coordinate system it is drawn in, as the reader of a document does for a series it moves onto that seat. What each kind
    /// answers is held over every kind above; here each seat is held to ask: a polar line puts the axes in polar, and a line leaves a
    /// ternary axes where it is.</summary>
    [Theory]
    [InlineData("right", "PolarLineSeries", CoordinateSystem.Cartesian, CoordinateSystem.Polar)]
    [InlineData("left past the primary", "PolarLineSeries", CoordinateSystem.Cartesian, CoordinateSystem.Polar)]
    [InlineData("top", "PolarLineSeries", CoordinateSystem.Cartesian, CoordinateSystem.Polar)]
    [InlineData("right", "LineSeries", CoordinateSystem.Ternary, CoordinateSystem.Ternary)]
    [InlineData("left past the primary", "LineSeries", CoordinateSystem.Ternary, CoordinateSystem.Ternary)]
    [InlineData("top", "LineSeries", CoordinateSystem.Ternary, CoordinateSystem.Ternary)]
    public void ASeriesAddedAtAnotherSeat_PutsTheAxesInTheCoordinateSystemItsVerbDoes(string seat, string kind, CoordinateSystem from, CoordinateSystem expected)
    {
        var series = DrawnSeriesFixtures.Instance(kind);
        var axes = new Axes { CoordinateSystem = from };

        _ = seat switch
        {
            "right" => axes.YAxes.Right.Add().AddSeries(series),
            "left past the primary" => axes.YAxes.Left.Add().AddSeries(series),
            _ => axes.AddXSecondarySeries(series),
        };

        Assert.Equal(expected, axes.CoordinateSystem);
    }

    [Fact]
    public void APolarLineOnARightAxis_DrawsAsItsDocumentReadBackDraws()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.WithAddRightYAxis(_ => { })).Build();
        figure.SubPlots[0].YAxes.Right[0].AddSeries(new PolarLineSeries([1.0, 2, 3], [0.0, 1, 2]));

        var again = figure.RoundTripped();

        Assert.Equal(again.ToSvg(), figure.ToSvg());
    }

    [Fact]
    public void ATernaryAxesHoldingALine_KeepsItsCoordinateSystemThroughADocument()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2], [3.0, 4])).Build();
        figure.SubPlots[0].CoordinateSystem = CoordinateSystem.Ternary;

        var again = figure.RoundTripped();

        Assert.Equal(CoordinateSystem.Ternary, again.SubPlots[0].CoordinateSystem);
    }

    [Fact]
    public void ACartesianSeries_LeavesTheAxesWhereItIs()
    {
        var axes = new Axes { CoordinateSystem = CoordinateSystem.Polar };

        axes.AddSeries(new LineSeries([1.0, 2], [3.0, 4]));

        Assert.Equal(CoordinateSystem.Polar, axes.CoordinateSystem);
    }
}
