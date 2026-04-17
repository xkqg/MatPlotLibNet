// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series.Streaming;

/// <summary>A streaming line series that accepts data via <see cref="IStreamingSeries.AppendPoint"/>,
/// backed by a ring buffer. Visual properties mirror <see cref="LineSeries"/>.</summary>
public sealed class StreamingLineSeries : StreamingSeriesBase, IHasColor
{
    /// <summary>Line color. When <c>null</c> the theme's prop-cycler assigns one.</summary>
    public Color? Color { get; set; }

    /// <summary>Line dash pattern. Default <see cref="Styling.LineStyle.Solid"/>.</summary>
    public LineStyle LineStyle { get; set; } = LineStyle.Solid;

    /// <summary>Line width in pixels. Default 1.5.</summary>
    public double LineWidth { get; set; } = 1.5;

    /// <summary>Initializes a new streaming line series with the specified buffer capacity.</summary>
    /// <param name="capacity">Maximum data points retained. Default 10,000.</param>
    public StreamingLineSeries(int capacity = 10_000) : base(capacity) { }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
