// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>A single-value "stat tile": a big formatted headline number with the series
/// <see cref="ChartSeries.Label"/> beneath it, filling its plot area. Built for compact dashboard KPIs
/// ("12 participants", "0 alerts"). It carries no axes/data, so <see cref="ComputeDataRange"/> contributes
/// nothing — the tile occupies the whole region it is placed in (use a mosaic / sub-plot per tile).</summary>
public sealed class StatTileSeries : ChartSeries
{
    /// <summary>The value displayed as the tile's headline number.</summary>
    public double Value { get; }

    /// <summary>An optional accent colour for the headline number (e.g. a warning colour for an alarm count);
    /// the theme cycle colour is used when null.</summary>
    public Color? AccentColor { get; set; }

    /// <summary>The format string applied when <see cref="Value"/> carries no explicit one.</summary>
    internal const string DefaultFormat = "0.##";

    /// <summary>The numeric format string applied to <see cref="Value"/> (invariant culture; default <c>"0.##"</c>).</summary>
    public string Format { get; set; } = DefaultFormat;

    /// <summary>The comparative this value is measured against, or <see langword="null"/> for none.
    /// <para>A number without a comparative cannot be judged. "68%" is not good or bad until you know what it
    /// was supposed to be, and a reader who has to supply that from memory usually does not.</para></summary>
    public double? Target { get; set; }

    /// <summary>The gap line, drawn under the headline — e.g. <c>"target 25 ms · +3.1 over"</c>.
    /// <para>Supplied by the caller, never computed here: what counts as "over" is a domain judgement, and a
    /// charting library that decides it has started holding opinions about when something is broken.</para>
    /// <para>NEWLINES stack: a caption may answer more than one question — "is this good or bad" and
    /// "measured over what" — and two answers on one row run wider than the tile. Split on
    /// <see cref="System.Environment.NewLine"/> or <c>\n</c>, each line drawn centred under the one above.</para></summary>
    public string? Caption { get; set; }

    /// <summary>An inline trend — a Tufte sparkline drawn inside the tile: no axis, no frame, no ticks, just
    /// the line. <see langword="null"/> (default) draws none.
    /// <para>It deliberately contributes nothing to the axes' data range: the tile's headline is the subject,
    /// and its own history must not drag the scale around.</para></summary>
    public IReadOnlyList<double>? Trend { get; set; }

    /// <summary>The colour of the trend line, or <see langword="null"/> for the tile's own headline colour.</summary>
    public Color? TrendColor { get; set; }

    /// <summary>A fill pattern across the tile, or <see cref="HatchPattern.None"/> (the default) for none.
    /// <para>This is how a tile says <i>no information</i> — the source has gone silent. It is a pattern and
    /// not a colour on purpose: "I can no longer see you" is a different fault from "you are broken", and a
    /// wall that paints them the same lies exactly when it matters most.</para></summary>
    public HatchPattern Hatch { get; set; } = HatchPattern.None;

    /// <summary>The colour of the hatch strokes, or <see langword="null"/> to contrast automatically.</summary>
    public Color? HatchColor { get; set; }

    /// <summary>Creates a stat tile displaying <paramref name="value"/>.</summary>
    public StatTileSeries(double value) => Value = value;

    /// <summary>The headline value rendered with <see cref="Format"/> under the invariant culture.</summary>
    internal string FormattedValue => Value.ToString(Format, CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) => new(null, null, null, null);

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "stattile",
        GaugeValue = Value,
        Color = AccentColor,
        // Null at the default, so an unformatted tile emits no format bytes and its golden stays byte-identical.
        // Before v1.14 this was simply absent: a tile came back from the wire reading "0.3" where it had read
        // "0.3 s" — the unit, and with it the meaning, was dropped in transit.
        TileFormat = Format != DefaultFormat ? Format : null,
        BulletTarget = Target,
        CenterText = Caption,
        Values = Trend?.ToArray(),
        TrackColor = TrendColor,
        Hatch = Hatch != HatchPattern.None ? Hatch : null,
        HatchColor = HatchColor,
    };

    /// <summary>Reconstructs a <see cref="StatTileSeries"/> from its serialization DTO, restoring its value and accent colour, and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static StatTileSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.StatTile(dto.GaugeValue ?? 0);
        if (dto.Color.HasValue)
        {
            s.AccentColor = dto.Color.Value;
        }

        if (dto.TileFormat is not null)
        {
            s.Format = dto.TileFormat;
        }

        s.Target = dto.BulletTarget;
        s.Caption = dto.CenterText;
        s.Trend = dto.Values;
        s.TrendColor = dto.TrackColor;

        if (dto.Hatch.HasValue)
        {
            s.Hatch = dto.Hatch.Value;
        }

        s.HatchColor = dto.HatchColor;
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
