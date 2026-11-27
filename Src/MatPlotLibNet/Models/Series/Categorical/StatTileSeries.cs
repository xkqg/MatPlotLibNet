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
/// nothing — the tile occupies the whole region it is placed in (use a mosaic / sub-plot per tile). The tile draws
/// its label itself, so it never appears as a legend entry.</summary>
public sealed class StatTileSeries : ChartSeries, IHatched, ISeriesKind<StatTileSeries>, IGivenTextSize
{
    /// <inheritdoc />
    internal override bool DrawsWithoutAxes => true;

    /// <summary>The value displayed as the tile's headline number.</summary>
    public double Value { get; }

    /// <summary>An optional accent colour for the headline number (e.g. a warning colour for an alarm count);
    /// the theme cycle colour is used when null.</summary>
    public Color? AccentColor { get; set => Change(ref field, value); }

    /// <summary>The format string applied when <see cref="Value"/> carries no explicit one.</summary>
    internal const string DefaultFormat = "0.##";

    /// <summary>The numeric format string applied to <see cref="Value"/> (invariant culture; default <c>"0.##"</c>).</summary>
    public string Format { get; set => Change(ref field, value); } = DefaultFormat;

    /// <summary>The comparative this value is measured against, or <see langword="null"/> for none.
    /// <para>A number without a comparative cannot be judged. "68%" is not good or bad until you know what it
    /// was supposed to be, and a reader who has to supply that from memory usually does not.</para></summary>
    public double? Target { get; set => Change(ref field, value); }

    /// <summary>The gap line, drawn under the headline — e.g. <c>"target 25 ms · +3.1 over"</c>.
    /// <para>Supplied by the caller, never computed here: what counts as "over" is a domain judgement, and a
    /// charting library that decides it has started holding opinions about when something is broken.</para>
    /// <para>NEWLINES stack: a caption may answer more than one question — "is this good or bad" and
    /// "measured over what" — and two answers on one row run wider than the tile. Split at
    /// <c>\r\n</c> or <c>\n</c>, whichever operating system the caption was written on, each line drawn centred under the
    /// one above.</para></summary>
    public string? Caption { get; set => Change(ref field, value); }

    /// <summary>An inline trend — a Tufte sparkline drawn inside the tile: no axis, no frame, no ticks, just
    /// the line. <see langword="null"/> (default) draws none.
    /// <para>It deliberately contributes nothing to the axes' data range: the tile's headline is the subject,
    /// and its own history must not drag the scale around.</para></summary>
    public IReadOnlyList<double>? Trend { get; set => Change(ref field, value); }

    /// <summary>The colour of the trend line, or <see langword="null"/> for the tile's own headline colour.</summary>
    public Color? TrendColor { get; set => Change(ref field, value); }

    /// <summary>A fill pattern across the tile, or <see cref="HatchPattern.None"/> (the default) for none.
    /// <para>This is how a tile says <i>no information</i> — the source has gone silent. It is a pattern and
    /// not a colour on purpose: "I can no longer see you" is a different fault from "you are broken", and a
    /// wall that paints them the same lies exactly when it matters most.</para></summary>
    public HatchPattern Hatch { get; set => Change(ref field, value); } = HatchPattern.None;

    /// <summary>The colour of the hatch strokes, or <see langword="null"/> to contrast automatically.</summary>
    public Color? HatchColor { get; set => Change(ref field, value); }

    /// <summary>What this reading is in — how bad, and whether it can be believed. Resting by default, so a
    /// tile nobody judged draws quiet rather than alarmed.
    /// <para>The caller decides what counts as bad; this says only what was decided. Set it and the tile takes
    /// the colour its severity earned and the pattern its visibility demands, in one place instead of at every
    /// call site. An explicit <see cref="AccentColor"/> or <see cref="Hatch"/> still wins: the condition is the
    /// default form, never an override of what the caller said in so many words.</para></summary>
    public OpsCondition Condition { get; set => Change(ref field, value); } = OpsCondition.Resting;

    /// <summary>Where the tile LEADS, or <see langword="null"/> for a tile that is only read — matplotlib's own
    /// <c>Artist.set_url</c> idiom, rendered as an SVG <c>&lt;a href&gt;</c> around the whole tile.
    /// <para>A link and not a click event on purpose: an anchor needs no script, so it works in a static SVG,
    /// in an inline Blazor render and in a saved file alike; it is focusable and Enter-activatable for free
    /// (the accessible-card pattern); and the state it leads to lives in the URL, which a wall that redraws its
    /// tiles twice a second cannot lose and an operator can paste to a colleague.</para>
    /// <para>The tile SAYS it leads somewhere, three ways at once and never by colour alone (WCAG 1.4.1): the
    /// pointer cursor, an <c>aria-label</c>, and a chevron drawn in the tile's corner — see <see cref="Expanded"/>.</para></summary>
    public string? Url { get; set => Change(ref field, value); }

    /// <summary>Whether what <see cref="Url"/> leads to is currently OPEN. Only the chevron reads it: it points
    /// right (▸ "there is more") while closed and down (▾ "shown below") while open — the disclosure idiom
    /// every expandable card uses, so nobody has to learn it. Ignored on a tile without a <see cref="Url"/>.</summary>
    public bool Expanded { get; set => Change(ref field, value); }

    /// <summary>Creates a stat tile displaying <paramref name="value"/>.</summary>
    public StatTileSeries(double value) => Value = value;

    /// <summary>The headline value rendered with <see cref="Format"/> under the invariant culture.</summary>
    internal string FormattedValue => Value.InvariantText(Format);

    /// <summary>A tile draws its <see cref="ChartSeries.Label"/> itself, beneath the headline number, so it is never
    /// a legend entry: the entry would repeat the name in a swatch that covers the number.</summary>
    internal override bool DrawsOwnLabel => true;

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) => default;

    static string ISeriesKind<StatTileSeries>.Kind => "stattile";

    /// <summary>The size of the tile's label (the headline, the caption and the steps between the lines keep their proportion to it), in pixels (the unit of <see cref="Font.Size"/>). Default <see cref="RestingFontSize"/>. Given no size, the tile's label (the headline, the caption and the steps between the lines keep their proportion to it)
    /// is the size of the theme's text under the matplotlib themes, as matplotlib draws the text it writes beside its data
    /// (<c>font.size</c>), and <see cref="RestingFontSize"/> under every other theme; a size that is given, the default included, is drawn as given.</summary>
    public double FontSize { get => GivenFontSize ?? RestingFontSize; set => GivenFontSize = value; }

    /// <summary>The size the label takes under a theme that names none.</summary>
    private const double RestingFontSize = 14;

    /// <summary>The size that was given, <see langword="null"/> when none was.</summary>
    internal double? GivenFontSize { get; private set => Change(ref field, value); }

    double? IGivenTextSize.GivenTextSize => GivenFontSize;

    double IGivenTextSize.RestingTextSize => RestingFontSize;

    /// <summary>How much larger than its resting anatomy the tile is drawn under <paramref name="theme"/>: the size its label is drawn
    /// at over the size it rests at, so the headline, the caption and the steps between the lines keep their proportion to the label.</summary>
    /// <param name="theme">The theme the tile is drawn under.</param>
    /// <returns>The scale, 1 for a tile drawn at rest.</returns>
    internal double AnatomyScaleUnder(Theme theme) => this.TextSizeUnder(theme) / RestingFontSize;

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        FontSize = GivenFontSize,
        GaugeValue = Value,
        Color = AccentColor,
        // Null at the default, so an unformatted tile emits no format bytes and its golden stays byte-identical.
        // Before v1.14 this was simply absent: a tile came back from the wire reading "0.3" where it had read
        // "0.3 s" — the unit, and with it the meaning, was dropped in transit.
        TileFormat = Format.OrNullAtRest(DefaultFormat),
        BulletTarget = Target,
        CenterText = Caption,
        Values = Trend?.ToArray(),
        TrackColor = TrendColor,
        Hatching = this.Hatching,
        TileUrl = Url,
        TileExpanded = Expanded.OrNullAtRest(false), // null at the default: an unlinked tile adds no bytes
        ConditionSeverity = Condition.Severity.OrNullAtRest(OpsCondition.Resting.Severity),
        ConditionVisibility = Condition.Visibility.OrNullAtRest(OpsCondition.Resting.Visibility),
    };

    /// <summary>The reader of a <see cref="StatTileSeries"/> that <see cref="SeriesRegistry"/> takes; it also carries its value and accent colour.</summary>
    internal static StatTileSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.StatTile(dto.GaugeValue ?? 0);
        if (dto.Color is { } color) s.AccentColor = color;

        if (dto.TileFormat is { } tileFormat) s.Format = tileFormat;

        s.Condition = new OpsCondition(
            dto.ConditionSeverity ?? OpsCondition.Resting.Severity,
            dto.ConditionVisibility ?? OpsCondition.Resting.Visibility);

        s.Target = dto.BulletTarget;
        s.Caption = dto.CenterText;
        s.Trend = dto.Values;
        s.TrendColor = dto.TrackColor;

        s.LayHatching(dto.Hatching);
        s.Url = dto.TileUrl;
        if (dto.TileExpanded is { } tileExpanded) s.Expanded = tileExpanded;
        if (dto.FontSize is { } fontSize) s.FontSize = fontSize;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>A tile is one number, so the table is one row: what the tile is called and what it reads.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        // The same columns for every tile, whatever this one has: a tile row is a row of subplots, and only
        // tables of one shape can become the one table the operator reads that row as. What a tile does not
        // have is an empty cell - not a zero, not a fake.
        var trend = Trend is { Count: > 0 } history
            ? DataCell.FromText(string.Join(", ", history.Select(v => v.InvariantText())))
            : DataCell.Empty;
        return new ChartDataTable(null,
            [
                new("label", DataColumnKind.Text), new("value"), new("target"),
                new("caption", DataColumnKind.Text), new("trend", DataColumnKind.Text),
            ],
            [[
                DataCell.FromText(Label ?? ""), DataCell.FromNumber(Value),
                Target is { } target ? DataCell.FromNumber(target) : DataCell.Empty,
                Caption is { Length: > 0 } caption ? DataCell.FromText(caption) : DataCell.Empty,
                trend,
            ]]);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
