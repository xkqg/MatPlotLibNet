// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series.Streaming;

/// <summary>A streaming scatter series that accepts data via <see cref="IStreamingSeries.AppendPoint"/>,
/// backed by a ring buffer. Visual properties mirror <see cref="ScatterSeries"/>.</summary>
public sealed class StreamingScatterSeries : StreamingSeries, IHasColor, IHasAlpha, ISeriesKind<StreamingScatterSeries>
{
    /// <inheritdoc />
    /// <remarks>A streaming scatter is drawn as matplotlib's <c>scatter</c>.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Dots;

    /// <summary>Marker color. When <c>null</c> the theme's prop-cycler assigns one.</summary>
    public Color? Color { get; set => Change(ref field, value); }

    private const double RestingAlpha = 1.0;
    /// <summary>Marker opacity. Range [0, 1], default <see cref="RestingAlpha"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 0, above 1 or not a number.</exception>
    public double Alpha { get; set => Change(ref field, value.ThrowIfNotOpacity(nameof(Alpha))); } = RestingAlpha;

    /// <summary>Marker diameter in pixels. Default <see cref="MarkerLook.RestingSize"/>.</summary>
    public double MarkerSize { get; set => Change(ref field, value); } = MarkerLook.RestingSize;

    /// <summary>Initializes a new streaming scatter series with the specified buffer capacity.</summary>
    /// <param name="capacity">Maximum data points retained. Default <see cref="StreamingSeries.RestingCapacity"/>.</param>
    public StreamingScatterSeries(int capacity = RestingCapacity) : base(capacity) { }

    static string ISeriesKind<StreamingScatterSeries>.Kind => "streaming_scatter";

    /// <inheritdoc />
    /// <remarks>Its own discriminator and the ring buffer's current snapshot — see <see cref="StreamingLineSeries.ToSeriesDto"/>
    /// for why the base class' "streaming" discriminator cannot serve two series kinds.</remarks>
    public override SeriesDto ToSeriesDto() =>
        StreamDto(this.WireKind(), Color) with
        {
            Alpha = Alpha.OrNullAtRest(RestingAlpha),
            MarkerSize = MarkerSize.OrNullAtRest(MarkerLook.RestingSize),
        };

    /// <summary>The reader of a <see cref="StreamingScatterSeries"/> that <see cref="SeriesRegistry"/> takes; it restores the capacity and the snapshot it was written with.</summary>
    internal static StreamingScatterSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = FromStreamDto(axes, dto, static capacity => new StreamingScatterSeries(capacity));
        if (dto.Alpha is { } alpha) s.Alpha = alpha;
        if (dto.MarkerSize is { } markerSize) s.MarkerSize = markerSize;
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
