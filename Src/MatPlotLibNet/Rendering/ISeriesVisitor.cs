// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Rendering;

/// <summary>Visitor interface for rendering each series type through the visitor pattern.</summary>
public interface ISeriesVisitor
{
    /// <summary>Renders a line series.</summary>
    void Visit(LineSeries series, RenderArea area);

    /// <summary>Renders a scatter series.</summary>
    void Visit(ScatterSeries series, RenderArea area);

    /// <summary>Renders a bar series.</summary>
    void Visit(BarSeries series, RenderArea area);

    /// <summary>Renders a histogram series.</summary>
    void Visit(HistogramSeries series, RenderArea area);

    /// <summary>Renders a pie series.</summary>
    void Visit(PieSeries series, RenderArea area);

    /// <summary>Renders a heatmap series.</summary>
    void Visit(HeatmapSeries series, RenderArea area);

    /// <summary>Renders a box plot series.</summary>
    void Visit(BoxSeries series, RenderArea area);

    /// <summary>Renders a violin plot series.</summary>
    void Visit(ViolinSeries series, RenderArea area);

    /// <summary>Renders a contour series.</summary>
    void Visit(ContourSeries series, RenderArea area);

    /// <summary>Renders a stem plot series.</summary>
    void Visit(StemSeries series, RenderArea area);
}
