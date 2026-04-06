// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a bubble chart — a scatter plot where marker area encodes a third variable.</summary>
public sealed class BubbleSeries : XYSeries, ISeriesSerializable
{
    public double[] Sizes { get; }
    public Color? Color { get; set; }
    public double Alpha { get; set; } = 0.6;

    public BubbleSeries(double[] xData, double[] yData, double[] sizes) : base(xData, yData)
    {
        Sizes = sizes;
    }

    /// <inheritdoc />
    public SeriesDto ToSeriesDto() => new()
    {
        Type = "bubble",
        XData = XData, YData = YData, Sizes = Sizes,
        Color = Color, Alpha = Alpha
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
