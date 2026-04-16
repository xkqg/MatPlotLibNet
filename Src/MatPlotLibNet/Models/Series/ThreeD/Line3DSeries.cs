// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a 3D polyline connecting data points in three-dimensional space.</summary>
public sealed class Line3DSeries : XYZSeries, IHasColor
{
    /// <summary>Line color. When <c>null</c> the theme's prop-cycler assigns one automatically.</summary>
    public Color? Color { get; set; }

    /// <summary>Line width in pixels. Default 1.5.</summary>
    public double LineWidth { get; set; } = 1.5;

    /// <summary>Dash pattern for the line. Default <see cref="Styling.LineStyle.Solid"/>.</summary>
    public LineStyle LineStyle { get; set; } = LineStyle.Solid;

    /// <summary>Initializes a new 3D line series with the specified data.</summary>
    public Line3DSeries(Vec x, Vec y, Vec z) : base(x, y, z) { }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "line3d",
        XData = X,
        YData = Y,
        ZData = Z,
        Color = Color,
        LineWidth = LineWidth != 1.5 ? LineWidth : null,
        LineStyle = LineStyle == LineStyle.Solid ? null : LineStyle.ToString().ToLowerInvariant(),
        Label = Label
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
