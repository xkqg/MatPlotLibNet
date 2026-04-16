// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a scatter chart series displaying individual data points as markers.</summary>
public sealed class ScatterSeries : XYSeries, IColormappable, INormalizable, IHasColor, IHasAlpha, IHasMarkerStyle
{
    public double[]? Sizes { get; set; }

    public Color[]? Colors { get; set; }

    public Color? Color { get; set; }

    public MarkerStyle Marker { get; set; } = MarkerStyle.Circle;

    /// <inheritdoc />
    MarkerStyle IHasMarkerStyle.MarkerStyle { get => Marker; set => Marker = value; }

    public double MarkerSize { get; set; } = 36;

    public double Alpha { get; set; } = 1.0;

    public IColorMap? ColorMap { get; set; }

    public Color[]? EdgeColors { get; set; }

    public double[]? LineWidths { get; set; }

    public double[]? C { get; set; }

    public double? VMin { get; set; }

    public double? VMax { get; set; }

    public INormalizer? Normalizer { get; set; }


    /// <summary>Initializes a new instance of <see cref="ScatterSeries"/> with the specified data.</summary>
    /// <param name="xData">The X-axis data values.</param>
    /// <param name="yData">The Y-axis data values.</param>
    public ScatterSeries(double[] xData, double[] yData) : base(xData, yData) { }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "scatter",
        XData = XData, YData = YData, Color = Color,
        MarkerSize = MarkerSize
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
