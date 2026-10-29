// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction;

public class ChartLayoutTests
{
    private static (Figure figure, ChartLayout layout) MakeLayout(
        double xMin = 0, double xMax = 10, double yMin = 0, double yMax = 5,
        double plotX = 10, double plotY = 10, double plotW = 100, double plotH = 50)
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        figure.SubPlots[0].XAxis.Min = xMin;
        figure.SubPlots[0].XAxis.Max = xMax;
        figure.SubPlots[0].YAxis.Min = yMin;
        figure.SubPlots[0].YAxis.Max = yMax;
        var areas = new[] { new Rect(plotX, plotY, plotW, plotH) };
        return (figure, ChartLayout.Create(figure, areas));
    }

    [Fact]
    public void AxesCount_ReturnsPlotAreaCount()
    {
        var (_, layout) = MakeLayout();
        Assert.Equal(1, layout.AxesCount);
    }

    [Fact]
    public void GetPlotArea_ReturnsCorrectRect()
    {
        var (_, layout) = MakeLayout(plotX: 20, plotY: 30, plotW: 80, plotH: 60);
        var area = layout.GetPlotArea(0);
        Assert.Equal(20, area.X);
        Assert.Equal(30, area.Y);
        Assert.Equal(80, area.Width);
        Assert.Equal(60, area.Height);
    }

    [Fact]
    public void GetDataRange_ReadsFromFigureAxes()
    {
        var (_, layout) = MakeLayout(xMin: -1, xMax: 1, yMin: -2, yMax: 2);
        var (xMin, xMax, yMin, yMax) = layout.GetDataRange(0);
        Assert.Equal(-1, xMin);
        Assert.Equal(1,  xMax);
        Assert.Equal(-2, yMin);
        Assert.Equal(2,  yMax);
    }

    /// <summary>ROADMAP.md row 100: an interaction modifier reads every outward-stacked secondary axis' own
    /// current range through this, the counterpart of GetDataRange for everything beyond the primary.</summary>
    [Fact]
    public void GetSecondaryDataRanges_ReturnsEverySlotsOwnRange_RightSlotsThenLeftSlots()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .WithAddRightYAxis(y2 => y2.SetYLim(10, 20).Plot([1.0, 2], [12.0, 18]))
                .WithAddRightYAxis(outer => outer.SetYLim(100, 200).Plot([1.0, 2], [120.0, 180]))
                .WithAddLeftYAxis(left => left.SetYLim(1, 2).Plot([1.0, 2], [1.2, 1.8])))
            .Build();
        var areas = new[] { new Rect(10, 10, 100, 50) };
        var layout = ChartLayout.Create(figure, areas);

        var views = layout.GetSecondaryDataRanges(0);

        Assert.Equal(3, views.Count);
        Assert.Equal(AxisSide.Right, views[0].Side);
        Assert.Equal(0, views[0].SlotIndex);
        Assert.Equal(10, views[0].Range.YMin);
        Assert.Equal(20, views[0].Range.YMax);
        Assert.Equal(AxisSide.Right, views[1].Side);
        Assert.Equal(1, views[1].SlotIndex);
        Assert.Equal(100, views[1].Range.YMin);
        Assert.Equal(200, views[1].Range.YMax);
        Assert.Equal(AxisSide.Left, views[2].Side);
        Assert.Equal(0, views[2].SlotIndex);
        Assert.Equal(1, views[2].Range.YMin);
        Assert.Equal(2, views[2].Range.YMax);
    }

    /// <summary>A host that implements nothing but the required members (every other <see cref="IChartLayout"/>
    /// implementer in this repo, and any third party's) gets the default-interface empty list, not a refusal —
    /// the additive promise behind the new member.</summary>
    [Fact]
    public void GetSecondaryDataRanges_DefaultImplementation_IsEmpty()
    {
        IChartLayout minimal = new MinimalChartLayoutStub();
        Assert.Empty(minimal.GetSecondaryDataRanges(0));
    }

    private sealed class MinimalChartLayoutStub : IChartLayout
    {
        public int AxesCount => 1;
        public Rect GetPlotArea(int axesIndex) => new(0, 0, 100, 100);
        public DataRange GetDataRange(int axesIndex) => new(0, 1, 0, 1);
        public int? HitTestAxes(double pixelX, double pixelY) => 0;
        public int? HitTestLegendItem(double pixelX, double pixelY, int axesIndex) => null;
    }

    /// <summary>ROADMAP.md row 100: a host OTHER than the library's own <see cref="ChartLayout"/> — "nothing but a
    /// plot area and a range" — states a secondary axis' range only; <see cref="ChartLayoutViewExtensions.SecondaryViewsOf"/>
    /// must still build a usable (plain linear) transform from it for a modifier to convert a pixel through.</summary>
    [Fact]
    public void SecondaryViewsOf_ForAHostThatIsNotChartLayout_BuildsALinearTransformFromItsStatedRange()
    {
        IChartLayout host = new StatedSecondaryChartLayoutStub();

        var views = host.SecondaryViewsOf(0);

        var view = Assert.Single(views);
        Assert.Equal(AxisSide.Right, view.Side);
        Assert.Equal(0, view.SlotIndex);
        // Linear over the stated plot area and the stated primary X range + this slot's own Y range.
        var mid = view.Transform.PixelToData(new Point(50, 50));
        Assert.Equal(0.5, mid.X, 6);
        Assert.Equal(15.0, mid.Y, 6);
    }

    private sealed class StatedSecondaryChartLayoutStub : IChartLayout
    {
        public int AxesCount => 1;
        public Rect GetPlotArea(int axesIndex) => new(0, 0, 100, 100);
        public DataRange GetDataRange(int axesIndex) => new(0, 1, 0, 1);
        public int? HitTestAxes(double pixelX, double pixelY) => 0;
        public int? HitTestLegendItem(double pixelX, double pixelY, int axesIndex) => null;
        public IReadOnlyList<SecondaryAxisRange> GetSecondaryDataRanges(int axesIndex) =>
            [new SecondaryAxisRange(AxisSide.Right, 0, new DataRange(0, 1, 10, 20))];
    }

    [Fact]
    public void HitTestAxes_PointInsidePlotArea_ReturnsAxesIndex()
    {
        var (_, layout) = MakeLayout(plotX: 10, plotY: 10, plotW: 100, plotH: 50);
        Assert.Equal(0, layout.HitTestAxes(50, 30));
    }

    [Fact]
    public void HitTestAxes_PointOutsidePlotArea_ReturnsNull()
    {
        var (_, layout) = MakeLayout(plotX: 10, plotY: 10, plotW: 100, plotH: 50);
        Assert.Null(layout.HitTestAxes(5, 5));   // above/left of plot area
        Assert.Null(layout.HitTestAxes(200, 200)); // far outside
    }

    [Fact]
    public void HitTestAxes_EdgePoint_Included()
    {
        var (_, layout) = MakeLayout(plotX: 10, plotY: 10, plotW: 100, plotH: 50);
        Assert.Equal(0, layout.HitTestAxes(10, 10));   // top-left corner
        Assert.Equal(0, layout.HitTestAxes(110, 60));  // bottom-right corner
    }

    [Fact]
    public void HitTestLegendItem_NoLegend_ReturnsNull()
    {
        // Layout created via the plotAreas-only overload has no legend data.
        var (_, layout) = MakeLayout();
        Assert.Null(layout.HitTestLegendItem(50, 30, 0));
    }

    [Fact]
    public void HitTestLegendItem_ReturnsSeriesIndex()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        var plotAreas = new[] { new Rect(10, 10, 100, 50) };
        // Legend item for series 0 at pixel rect (20, 5, 60, 14)
        var legendItems = new IReadOnlyList<LegendItemBounds>[]
        {
            new[] { new LegendItemBounds(0, new Rect(20, 5, 60, 14)) }
        };
        var layoutResult = new LayoutResult(plotAreas, legendItems);
        var layout = ChartLayout.Create(figure, layoutResult);

        // Click inside the legend item bounds
        Assert.Equal(0, layout.HitTestLegendItem(30, 10, 0));
    }

    [Fact]
    public void HitTestLegendItem_OutsideBounds_ReturnsNull()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        var plotAreas = new[] { new Rect(10, 10, 100, 50) };
        var legendItems = new IReadOnlyList<LegendItemBounds>[]
        {
            new[] { new LegendItemBounds(0, new Rect(20, 5, 60, 14)) }
        };
        var layoutResult = new LayoutResult(plotAreas, legendItems);
        var layout = ChartLayout.Create(figure, layoutResult);

        // Click outside legend item bounds
        Assert.Null(layout.HitTestLegendItem(5, 5, 0));
    }

    [Fact]
    public void HitTestLegendItem_MultipleItems_ReturnsCorrectIndex()
    {
        var figure = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "A")
            .Plot([1.0, 2.0], [5.0, 6.0], s => s.Label = "B")
            .Build();
        var plotAreas = new[] { new Rect(10, 10, 200, 100) };
        var legendItems = new IReadOnlyList<LegendItemBounds>[]
        {
            new[]
            {
                new LegendItemBounds(0, new Rect(150, 15, 50, 12)),
                new LegendItemBounds(1, new Rect(150, 27, 50, 12))
            }
        };
        var layoutResult = new LayoutResult(plotAreas, legendItems);
        var layout = ChartLayout.Create(figure, layoutResult);

        Assert.Equal(0, layout.HitTestLegendItem(160, 20, 0));
        Assert.Equal(1, layout.HitTestLegendItem(160, 32, 0));
    }

    [Fact]
    public void HitTestLegendItem_InvalidAxesIndex_ReturnsNull()
    {
        var figure = Plt.Create().Plot([1.0], [2.0]).Build();
        var plotAreas = new[] { new Rect(10, 10, 100, 50) };
        var legendItems = new IReadOnlyList<LegendItemBounds>[]
        {
            new[] { new LegendItemBounds(0, new Rect(20, 5, 60, 14)) }
        };
        var layoutResult = new LayoutResult(plotAreas, legendItems);
        var layout = ChartLayout.Create(figure, layoutResult);

        // Axes index out of range
        Assert.Null(layout.HitTestLegendItem(30, 10, 5));
        Assert.Null(layout.HitTestLegendItem(30, 10, -1));
    }

    [Fact]
    public void PixelToData_TopLeftOfPlotArea_ReturnsMinXMaxY()
    {
        // top-left pixel = (xMin, yMax) because screen-Y increases downward
        var (_, layout) = MakeLayout(xMin: 0, xMax: 10, yMin: 0, yMax: 5,
                                     plotX: 0, plotY: 0, plotW: 100, plotH: 50);
        var result = layout.PixelToData(0, 0, 0);
        Assert.NotNull(result);
        Assert.Equal(0.0, result!.Value.DataX, precision: 6);
        Assert.Equal(5.0, result.Value.DataY,  precision: 6);
    }

    [Fact]
    public void PixelToData_BottomRightOfPlotArea_ReturnsMaxXMinY()
    {
        var (_, layout) = MakeLayout(xMin: 0, xMax: 10, yMin: 0, yMax: 5,
                                     plotX: 0, plotY: 0, plotW: 100, plotH: 50);
        var result = layout.PixelToData(100, 50, 0);
        Assert.NotNull(result);
        Assert.Equal(10.0, result!.Value.DataX, precision: 6);
        Assert.Equal(0.0,  result.Value.DataY,  precision: 6);
    }

    [Fact]
    public void PixelToData_CentreOfPlotArea_ReturnsMidpoint()
    {
        var (_, layout) = MakeLayout(xMin: 0, xMax: 10, yMin: 0, yMax: 4,
                                     plotX: 0, plotY: 0, plotW: 100, plotH: 100);
        var result = layout.PixelToData(50, 50, 0);
        Assert.NotNull(result);
        Assert.Equal(5.0, result!.Value.DataX, precision: 6);
        Assert.Equal(2.0, result.Value.DataY,  precision: 6);
    }

    [Fact]
    public void PixelToData_OutsidePlotArea_ReturnsNull()
    {
        var (_, layout) = MakeLayout(plotX: 10, plotY: 10, plotW: 100, plotH: 50);
        Assert.Null(layout.PixelToData(5, 5, 0));
    }

    [Fact]
    public void Create_NullFigure_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ChartLayout.Create(null!, [new Rect(0, 0, 100, 50)]));
    }

    [Fact]
    public void Create_NullFigureWithALayoutResult_Throws()
    {
        var layoutResult = new LayoutResult([new Rect(0, 0, 100, 50)], []);
        var ex = Assert.Throws<ArgumentNullException>(() => ChartLayout.Create(null!, layoutResult));
        Assert.Equal("figure", ex.ParamName);
    }

    [Fact]
    public void Create_NullPlotAreas_Throws()
    {
        var figure = Plt.Create().Build();
        Assert.Throws<ArgumentNullException>(() =>
            ChartLayout.Create(figure, (IReadOnlyList<Rect>)null!));
    }

    [Fact]
    public void Create_NullLayoutResult_Throws()
    {
        var figure = Plt.Create().Build();
        Assert.Throws<ArgumentNullException>(() =>
            ChartLayout.Create(figure, (LayoutResult)null!));
    }
}
