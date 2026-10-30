// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction;

/// <summary>The seams of the interaction layer that a host reaches through an interface: the layout a host of its own writes, the
/// snapshot a standalone modifier keeps, and the flag that says which events move the view every viewer shares.</summary>
public class ChartLayoutSeamTests
{
    /// <summary>A layout that states nothing but a plot area and a range, which is all an implementer has to write.</summary>
    private sealed class BareLayout : IChartLayout
    {
        public int AxesCount => 1;

        public Rect GetPlotArea(int axesIndex) => new(10, 20, 200, 100);

        public DataRange GetDataRange(int axesIndex) => new(0, 10, 0, 100);

        public int? HitTestAxes(double pixelX, double pixelY) => 0;

        public int? HitTestLegendItem(double pixelX, double pixelY, int axesIndex) => 7;
    }

    [Fact]
    public void APixelOutsideThePlotArea_HasNoDataPoint_OnALayoutThatOnlyStatesTheArea()
    {
        IChartLayout layout = new BareLayout();

        Assert.Null(layout.PixelToData(5, 50, 0));     // left of the area
        Assert.Null(layout.PixelToData(300, 50, 0));   // right of it
        Assert.Null(layout.PixelToData(50, 5, 0));     // above it
        Assert.Null(layout.PixelToData(50, 200, 0));   // below it
    }

    [Fact]
    public void APixelInsideThePlotArea_ReadsTheRangeLinearly_OnALayoutThatOnlyStatesTheArea()
    {
        IChartLayout layout = new BareLayout();

        var point = layout.PixelToData(110, 70, 0);   // the middle of the area

        Assert.NotNull(point);
        Assert.Equal(5.0, point.Value.DataX, 6);
        Assert.Equal(50.0, point.Value.DataY, 6);
    }

    [Fact]
    public void ASnapshot_AsksTheLiveLayoutForTheAreaTheConversionAndTheLegend_AndKeepsTheRangeItWasTakenWith()
    {
        var figure = Plt.Create().Plot([1.0, 2.0, 3.0], [1.0, 4.0, 9.0]).Build();
        var live = ChartLayout.Create(figure, [new Rect(10, 10, 200, 100)]);
        var snapshot = new SnapshotLayout(live);

        Assert.Equal(live.GetPlotArea(0), snapshot.GetPlotArea(0));
        Assert.Equal(live.PixelToData(110, 60, 0), snapshot.PixelToData(110, 60, 0));
        Assert.Equal(live.HitTestLegendItem(110, 60, 0), snapshot.HitTestLegendItem(110, 60, 0));
        Assert.Equal(live.GetDataRange(0), snapshot.GetDataRange(0));
    }

    public static TheoryData<string, FigureInteractionEvent, bool> Events() => new()
    {
        { "zoom", new ZoomEvent("c", 0, 0, 1, 0, 1), true },
        { "rectangle zoom", new RectangleZoomEvent("c", 0, 0, 1, 0, 1), true },
        { "reset", new ResetEvent("c", 0, 0, 1, 0, 1), true },
        { "pan", new PanEvent("c", 0, 1, 1), true },
        { "rotation", new Rotate3DEvent("c", 0, 5, 5), true },
        { "legend toggle", new LegendToggleEvent("c", 0, 0), false },
        { "brush", new BrushSelectEvent("c", 0, 0, 0, 1, 1), false },
        { "hover", new HoverEvent("c", 0, 1, 1), false },
    };

    [Theory]
    [MemberData(nameof(Events))]
    public void AnEvent_SaysWhetherItMovesTheViewTheViewersShare(string label, FigureInteractionEvent evt, bool moves)
    {
        Assert.True(evt.ChangesView == moves, $"{label}: ChangesView is {evt.ChangesView}, expected {moves}");
    }
}
