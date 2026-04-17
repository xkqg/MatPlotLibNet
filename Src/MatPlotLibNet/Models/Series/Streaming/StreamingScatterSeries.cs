// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series.Streaming;

/// <summary>A streaming scatter series that accepts data via <see cref="IStreamingSeries.AppendPoint"/>,
/// backed by a ring buffer. Visual properties mirror <see cref="ScatterSeries"/>.</summary>
public sealed class StreamingScatterSeries : StreamingSeriesBase, IHasColor, IHasAlpha
{
    /// <summary>Marker color. When <c>null</c> the theme's prop-cycler assigns one.</summary>
    public Color? Color { get; set; }

    /// <summary>Marker opacity. Range [0, 1], default 1.0.</summary>
    public double Alpha { get; set; } = 1.0;

    /// <summary>Marker diameter in pixels. Default 6.</summary>
    public double MarkerSize { get; set; } = 6;

    /// <summary>Initializes a new streaming scatter series with the specified buffer capacity.</summary>
    /// <param name="capacity">Maximum data points retained. Default 10,000.</param>
    public StreamingScatterSeries(int capacity = 10_000) : base(capacity) { }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
