// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Models.Series;

internal sealed class TestSeriesVisitor : ISeriesVisitor
{
    public string? LastVisited { get; private set; }

    public void Visit(LineSeries series, RenderArea area) => LastVisited = nameof(LineSeries);
    public void Visit(ScatterSeries series, RenderArea area) => LastVisited = nameof(ScatterSeries);
    public void Visit(BarSeries series, RenderArea area) => LastVisited = nameof(BarSeries);
    public void Visit(HistogramSeries series, RenderArea area) => LastVisited = nameof(HistogramSeries);
    public void Visit(PieSeries series, RenderArea area) => LastVisited = nameof(PieSeries);
    public void Visit(HeatmapSeries series, RenderArea area) => LastVisited = nameof(HeatmapSeries);
    public void Visit(BoxSeries series, RenderArea area) => LastVisited = nameof(BoxSeries);
    public void Visit(ViolinSeries series, RenderArea area) => LastVisited = nameof(ViolinSeries);
    public void Visit(ContourSeries series, RenderArea area) => LastVisited = nameof(ContourSeries);
    public void Visit(StemSeries series, RenderArea area) => LastVisited = nameof(StemSeries);
}
