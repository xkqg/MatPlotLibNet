// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a line chart series connecting data points with a line.</summary>
public sealed class LineSeries : XYSeries, IHasColor
{
    public Color? Color { get; set; }

    public LineStyle LineStyle { get; set; } = LineStyle.Solid;

    public double LineWidth { get; set; } = 1.5;

    public MarkerStyle? Marker { get; set; }

    public double MarkerSize { get; set; } = 6;

    public Color? MarkerFaceColor { get; set; }

    public Color? MarkerEdgeColor { get; set; }

    public double MarkerEdgeWidth { get; set; } = 1.0;

    public DrawStyle? DrawStyle { get; set; }

    public int? MarkEvery { get; set; }

    /// <summary>When <see langword="true"/>, the renderer applies Fritsch-Carlson monotone cubic
    /// interpolation before drawing. Produces a smooth curve that never overshoots.</summary>
    public bool Smooth { get; set; }

    /// <summary>Number of interpolated sub-points per input interval when <see cref="Smooth"/> is <see langword="true"/>.
    /// Default 10. Higher values produce smoother curves at the cost of larger SVG output.</summary>
    public int SmoothResolution { get; set; } = 10;

    /// <summary>Initializes a new instance of <see cref="LineSeries"/> with the specified data.</summary>
    /// <param name="xData">The X-axis data values.</param>
    /// <param name="yData">The Y-axis data values.</param>
    public LineSeries(double[] xData, double[] yData) : base(xData, yData) { }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "line",
        XData = XData, YData = YData, Color = Color,
        LineStyle = LineStyle.ToString().ToLowerInvariant(),
        LineWidth = LineWidth,
        Smooth = Smooth ? true : null,
        SmoothResolution = Smooth && SmoothResolution != 10 ? SmoothResolution : null
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
