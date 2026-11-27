// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series.Streaming;

/// <summary>A streaming line series that accepts data via <see cref="IStreamingSeries.AppendPoint"/>,
/// backed by a ring buffer. Visual properties mirror <see cref="LineSeries"/>.</summary>
public sealed class StreamingLineSeries : StreamingSeries, IHasColor, IHasLabelPlacement, IHasLinePoints, ISeriesKind<StreamingLineSeries>, IGivenStroked
{
    /// <inheritdoc />
    /// <remarks>2: a line, as <c>plot</c> draws a <c>Line2D</c>.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Line;

    /// <inheritdoc />
    /// <remarks>The handle of the line it is drawn as (a <see cref="LineSeries"/> with its stroke and no marker of its own): a line from
    /// the stroke it was given, with the marker its colour slot cycles to, if any, at a line's resting size.</remarks>
    internal override LegendSwatch LegendSwatch => new(LegendSwatchKind.LineMarker, Stroke, MarkerSize: LineSeries.RestingMarkerSize);

    /// <summary>Line color. When <c>null</c> the theme's prop-cycler assigns one.</summary>
    public Color? Color { get; set => Change(ref field, value); }

    /// <inheritdoc />
    public SeriesLabelPlacement LabelPlacement { get; set => Change(ref field, value); } = RestingLabelPlacement;

    /// <inheritdoc />
    /// <remarks>One snapshot of the ring buffer: the points the next render draws, never x and y from two
    /// different moments.</remarks>
    LinePoints IHasLinePoints.LinePoints()
    {
        var snapshot = CreateSnapshot();
        return new(snapshot.XData, snapshot.YData);
    }

    /// <summary>Line dash pattern. Default <see cref="GivenStroke.RestingStyle"/>. Given none, the line takes its slot's, as a
    /// <see cref="LineSeries"/> does.</summary>
    public LineStyle LineStyle { get => Stroke.StyleOrResting; set => Stroke = Stroke with { Style = value }; }

    /// <inheritdoc cref="LineSeries.LineWidth"/>
    public double LineWidth { get => this.ReportedLineWidth; set => Stroke = Stroke with { Width = value }; }

    /// <summary>The width and the dash pattern the line was given, each <see langword="null"/> while it was not.</summary>
    internal GivenStroke Stroke { get; set => Change(ref field, value); }

    GivenStroke IGivenStroked.Stroke => Stroke;

    /// <summary>Initializes a new streaming line series with the specified buffer capacity.</summary>
    /// <param name="capacity">Maximum data points retained. Default <see cref="StreamingSeries.RestingCapacity"/>.</param>
    public StreamingLineSeries(int capacity = RestingCapacity) : base(capacity) { }

    static string ISeriesKind<StreamingLineSeries>.Kind => "streaming_line";

    /// <inheritdoc />
    /// <remarks>Its own discriminator and the ring buffer's current snapshot: a shared base implementation once
    /// wrote "streaming" for both this and <see cref="StreamingScatterSeries"/>, which collided on read; this
    /// override gives the line its own name and carries what a restored stream needs to start from the picture
    /// it was, not empty.</remarks>
    public override SeriesDto ToSeriesDto() =>
        StreamDto(this.WireKind(), Color) with { Stroking = Stroke };

    /// <summary>The reader of a <see cref="StreamingLineSeries"/> that <see cref="SeriesRegistry"/> takes; it restores the capacity and the snapshot it was written with.</summary>
    internal static StreamingLineSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = FromStreamDto(axes, dto, static capacity => new StreamingLineSeries(capacity));
        s.Stroke = dto.StrokeOver(s.Stroke);
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
