// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a stem plot series displaying data as vertical lines with markers at the tips.</summary>
public sealed class StemSeries : ChartSeries
{
    /// <summary>Gets the X-axis data values.</summary>
    public double[] XData { get; }

    /// <summary>Gets the Y-axis data values.</summary>
    public double[] YData { get; }

    /// <summary>Gets or sets the color of the markers at the stem tips.</summary>
    public Color? MarkerColor { get; set; }

    /// <summary>Gets or sets the color of the vertical stem lines.</summary>
    public Color? StemColor { get; set; }

    /// <summary>Gets or sets the color of the horizontal baseline.</summary>
    public Color? BaselineColor { get; set; }

    /// <summary>Gets or sets the marker style drawn at each stem tip.</summary>
    public MarkerStyle Marker { get; set; } = MarkerStyle.Circle;


    /// <summary>Initializes a new instance of <see cref="StemSeries"/> with the specified data.</summary>
    /// <param name="xData">The X-axis data values.</param>
    /// <param name="yData">The Y-axis data values.</param>
    public StemSeries(double[] xData, double[] yData)
    {
        XData = xData;
        YData = yData;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        double yMin = YData.Min(), yMax = YData.Max();
        if (0 < yMin) yMin = 0;
        if (0 > yMax) yMax = 0;
        return new(XData.Min(), XData.Max(), yMin, yMax);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new() { Type = "stem", XData = XData, YData = YData };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
