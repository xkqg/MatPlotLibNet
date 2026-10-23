// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series.Streaming;

/// <summary>A streaming scatter series that accepts data via <see cref="IStreamingSeries.AppendPoint"/>,
/// backed by a ring buffer. Visual properties mirror <see cref="ScatterSeries"/>.</summary>
public sealed class StreamingScatterSeries : StreamingSeries, IHasColor, IHasAlpha
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
    /// <remarks>Its own discriminator and the ring buffer's current snapshot — see <see cref="StreamingLineSeries.ToSeriesDto"/>
    /// for why the base class' "streaming" discriminator cannot serve two series kinds.</remarks>
    public override SeriesDto ToSeriesDto()
    {
        var snapshot = CreateSnapshot();
        return new()
        {
            Type = "streaming_scatter",
            XData = snapshot.XData,
            YData = snapshot.YData,
            Capacity = Capacity,
            Color = Color,
            Alpha = Alpha.OrNullAtRest(1.0),
            MarkerSize = MarkerSize.OrNullAtRest(6.0),
        };
    }

    /// <summary>Reconstructs a <see cref="StreamingScatterSeries"/> at the capacity and the snapshot it was written
    /// with, and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static StreamingScatterSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.AddSeries(new StreamingScatterSeries(dto.Capacity ?? 10_000));
        s.AppendPoints(dto.XData ?? [], dto.YData ?? []);
        if (dto.Color.HasValue) s.Color = dto.Color.Value;
        if (dto.Alpha.HasValue) s.Alpha = dto.Alpha.Value;
        if (dto.MarkerSize.HasValue) s.MarkerSize = dto.MarkerSize.Value;
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
