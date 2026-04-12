// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a bubble chart — a scatter plot where marker area encodes a third variable.</summary>
public sealed class BubbleSeries : XYSeries, IHasColor, IHasAlpha
{
    public double[] Sizes { get; }
    public Color? Color { get; set; }
    public double Alpha { get; set; } = 0.6;

    /// <summary>Initializes a new <see cref="BubbleSeries"/> with position and size data.</summary>
    /// <param name="xData">X coordinates.</param>
    /// <param name="yData">Y coordinates.</param>
    /// <param name="sizes">Marker area in points² for each data point.</param>
    public BubbleSeries(double[] xData, double[] yData, double[] sizes) : base(xData, yData)
    {
        Sizes = sizes;
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "bubble",
        XData = XData, YData = YData, Sizes = Sizes,
        Color = Color, Alpha = Alpha
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
