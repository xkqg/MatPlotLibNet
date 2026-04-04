// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a line chart series connecting data points with a line.</summary>
public sealed class LineSeries : ChartSeries
{
    /// <summary>Gets the X-axis data values.</summary>
    public double[] XData { get; }

    /// <summary>Gets the Y-axis data values.</summary>
    public double[] YData { get; }

    /// <summary>Gets or sets the line color.</summary>
    public Color? Color { get; set; }

    /// <summary>Gets or sets the line style (solid, dashed, etc.).</summary>
    public LineStyle LineStyle { get; set; } = LineStyle.Solid;

    /// <summary>Gets or sets the line width in points.</summary>
    public double LineWidth { get; set; } = 1.5;

    /// <summary>Gets or sets the marker style drawn at each data point.</summary>
    public MarkerStyle? Marker { get; set; }

    /// <summary>Gets or sets the marker size in points.</summary>
    public double MarkerSize { get; set; } = 6;

    /// <summary>Initializes a new instance of <see cref="LineSeries"/> with the specified data.</summary>
    /// <param name="xData">The X-axis data values.</param>
    /// <param name="yData">The Y-axis data values.</param>
    public LineSeries(double[] xData, double[] yData)
    {
        XData = xData;
        YData = yData;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
