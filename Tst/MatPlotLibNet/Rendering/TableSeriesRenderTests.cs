// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="TableSeries"/> rendering.</summary>
public class TableSeriesRenderTests
{
    private static readonly string[][] Data = [["Alice", "90"], ["Bob", "85"], ["Carol", "92"]];

    [Fact]
    public void Table_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Table(Data))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Table_SvgContainsRectangles()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Table(Data))
            .ToSvg();
        Assert.Contains("<rect", svg);
    }

    [Fact]
    public void Table_WithColumnHeaders_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Table(Data, s =>
            {
                s.ColumnHeaders = ["Name", "Score"];
            }))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Table_EmptyCellData_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Table([]))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Table_FluentShortcut_ProducesSvg()
    {
        string svg = Plt.Create()
            .Table(Data)
            .ToSvg();
        Assert.Contains("<svg", svg);
    }
}
