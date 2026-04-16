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
