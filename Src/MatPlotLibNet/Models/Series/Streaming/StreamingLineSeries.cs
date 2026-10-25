// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series.Streaming;

/// <summary>A streaming line series that accepts data via <see cref="IStreamingSeries.AppendPoint"/>,
/// backed by a ring buffer. Visual properties mirror <see cref="LineSeries"/>.</summary>
public sealed class StreamingLineSeries : StreamingSeries, IHasColor, IHasLabelPlacement
{
    /// <summary>Line color. When <c>null</c> the theme's prop-cycler assigns one.</summary>
    public Color? Color { get; set; }

    /// <inheritdoc />
    public SeriesLabelPlacement LabelPlacement { get; set; } = SeriesLabelPlacement.Legend;

    /// <inheritdoc />
    /// <remarks>One snapshot of the ring buffer: the points the next render draws, never x and y from two
    /// different moments.</remarks>
    LinePoints IHasLabelPlacement.LinePoints()
    {
        var snapshot = CreateSnapshot();
        return new(snapshot.XData, snapshot.YData);
    }

    /// <summary>Line dash pattern. Default <see cref="Styling.LineStyle.Solid"/>.</summary>
    public LineStyle LineStyle { get; set; } = LineStyle.Solid;

    /// <summary>Line width in pixels. Default 1.5.</summary>
    public double LineWidth { get; set; } = 1.5;

    /// <summary>Initializes a new streaming line series with the specified buffer capacity.</summary>
    /// <param name="capacity">Maximum data points retained. Default 10,000.</param>
    public StreamingLineSeries(int capacity = 10_000) : base(capacity) { }

    /// <inheritdoc />
    /// <remarks>Its own discriminator and the ring buffer's current snapshot: a shared base implementation once
    /// wrote "streaming" for both this and <see cref="StreamingScatterSeries"/>, which collided on read; this
    /// override gives the line its own name and carries what a restored stream needs to start from the picture
    /// it was, not empty.</remarks>
    public override SeriesDto ToSeriesDto()
    {
        // ONE snapshot: X and Y from the same moment, the invariant this series exists to keep (see the
        // class remarks on StreamingSeries about reading two buffers separately).
        var snapshot = CreateSnapshot();
        return new()
        {
            Type = "streaming_line",
            XData = snapshot.XData,
            YData = snapshot.YData,
            Capacity = Capacity,
            Color = Color,
            LineStyle = LineStyle.ToString().ToLowerInvariant(),
            LineWidth = LineWidth,
        };
    }

    /// <summary>Reconstructs a <see cref="StreamingLineSeries"/> at the capacity and the snapshot it was written
    /// with, and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static StreamingLineSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.AddSeries(new StreamingLineSeries(dto.Capacity ?? 10_000));
        s.AppendPoints(dto.XData ?? [], dto.YData ?? []);
        if (dto.Color.HasValue) s.Color = dto.Color.Value;
        dto.LineStyle.ApplyEnum<Styling.LineStyle>(v => s.LineStyle = v);
        if (dto.LineWidth.HasValue) s.LineWidth = dto.LineWidth.Value;
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
