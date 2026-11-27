// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Stephen Few's bullet graph: a measure, a target, and the qualitative ranges they sit in — three
/// layered parts in one thin strip.
///
/// <para><b>Why this and not a dial.</b> A radial gauge spends a quarter of a panel to communicate what a bar
/// communicates in a fifth of it, and it cannot be stacked. Few designed the bullet graph as its replacement,
/// and the high-performance-HMI literature rejects decorative dials for the same reason: on a wall where twenty
/// numbers must be legible at a glance, screen area is the scarce resource.</para>
///
/// <para><b>The three parts.</b> The <see cref="Value"/> is the feature bar. The optional <see cref="Target"/>
/// is a perpendicular tick — the comparative, which is what turns a number into a judgement ("2412" says
/// nothing; "2412 against a target of 2500" says everything). The optional <see cref="Bands"/> are the
/// qualitative ranges behind it: poor, satisfactory, good.</para>
///
/// <para><b>The bands are one hue at varying intensity, not red/amber/green.</b> Two reasons, and both matter.
/// Roughly eight percent of men cannot separate red from green, and a background that encodes meaning in those
/// two hues excludes them. And on an operations display colour is a scarce alarm signal — spending red on a
/// permanent backdrop leaves nothing for the thing that is actually wrong.</para></summary>
public sealed class BulletGraphSeries : ChartSeries, ISeriesKind<BulletGraphSeries>
{
    /// <inheritdoc />
    internal override bool DrawsWithoutAxes => true;

    /// <summary>The measure — the bar itself.</summary>
    public double Value { get; }

    /// <summary>The comparative, drawn as a perpendicular tick across the bar, or <see langword="null"/> for
    /// no target. A measure without a comparative cannot be judged: the reader has to supply the missing
    /// context from memory, and mostly does not.</summary>
    public double? Target { get; set => Change(ref field, value); }

    /// <summary>The qualitative ranges behind the bar, in ascending order of threshold, or
    /// <see langword="null"/> for none. Each band spans from its predecessor's threshold up to its own.</summary>
    /// <remarks>Deliberately the same <see cref="GaugeBand"/> the gauge uses: a (threshold, colour) pair is a
    /// (threshold, colour) pair, and minting a second type for the same shape is how a codebase acquires two
    /// band models that drift apart.
    /// <para>To say that the last band covers everything above the one before it, give it the threshold
    /// <see cref="double.PositiveInfinity"/>: that band is kept and runs to the end of the strip. An infinite
    /// threshold sizes nothing, though. The strip is as long as the furthest of the value, the target and the last
    /// band's threshold when that is a finite number, so with an infinite last threshold the strip ends where the
    /// value or the target asks, and the band fills what is left of it. A band whose threshold is missing
    /// (<see cref="double.NaN"/>) is left out.</para></remarks>
    public IReadOnlyList<GaugeBand>? Bands { get; set => Change(ref field, value); }

    /// <summary>The bar's colour, or <see langword="null"/> for the theme's foreground. The bar is the
    /// measure, not an alarm — colouring it by default would spend a signal that has nowhere else to come from.</summary>
    public Color? BarColor { get; set => Change(ref field, value); }

    /// <summary>The colour of the target tick, or <see langword="null"/> for the theme's foreground.</summary>
    public Color? TargetColor { get; set => Change(ref field, value); }

    private const Orientation RestingOrientation = Orientation.Horizontal;
    /// <summary>Horizontal (default) draws the strip left-to-right; vertical draws it bottom-up.</summary>
    /// <remarks>Reuses the library's shared <see cref="Models.Orientation"/> (Horizontal = 0), not
    /// <see cref="BarOrientation"/> — whose ordinals are the other way round, so reusing it would silently flip
    /// this default.</remarks>
    public Orientation Orientation { get; set => Change(ref field, value); } = RestingOrientation;

    /// <summary>Creates a bullet graph for <paramref name="value"/>.</summary>
    /// <param name="value">The measure.</param>
    public BulletGraphSeries(double value) => Value = value;

    /// <summary>The upper bound of the strip: whichever is furthest out of the value, the target and the
    /// outermost band. Everything the reader is asked to compare is therefore on screen without the caller
    /// having to compute limits.</summary>
    internal double UpperBound
    {
        get
        {
            // Only what can be placed bounds the strip: a missing or infinite value, target or threshold is not on it.
            double max = double.NegativeInfinity;
            if (Value.IsDrawable()) max = Value;
            if (Target is { } target && target.IsDrawable()) max = Math.Max(max, target);
            if (Bands is { Count: > 0 } && Bands[^1].Threshold.IsDrawable())
            {
                max = Math.Max(max, Bands[^1].Threshold);
            }

            return max <= 0 ? 1 : max;
        }
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        DataRangeContribution.Along(Orientation.RunsAlongX, 0, UpperBound);

    static string ISeriesKind<BulletGraphSeries>.Kind => "bulletgraph";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        GaugeValue = Value,
        BulletTarget = Target,
        BandThresholds = Bands?.Select(b => b.Threshold).ToArray(),
        BandColors = Bands?.Select(b => b.Color).ToList(),
        Color = BarColor,
        TrackColor = TargetColor,
        Orientation = Orientation.OrNullAtRest(RestingOrientation)?.ToWireName(),
    };

    /// <summary>The reader of a <see cref="BulletGraphSeries"/> that <see cref="SeriesRegistry"/> takes; it also carries the measure, the target, the qualitative bands, the colours and the orientation.</summary>
    internal static BulletGraphSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Bullet(dto.GaugeValue ?? 0);
        s.Target = dto.BulletTarget;
        s.BarColor = dto.Color;
        s.TargetColor = dto.TrackColor;

        if (dto.BandThresholds is { } thresholds && dto.BandColors is { } colors)
        {
            int count = Math.Min(thresholds.Length, colors.Count);
            var bands = new GaugeBand[count];
            for (int i = 0; i < count; i++)
            {
                bands[i] = new GaugeBand(thresholds[i], colors[i]);
            }

            s.Bands = bands;
        }

        if (dto.Orientation.WireEnum<Orientation>() is { } orientation) s.Orientation = orientation;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>The measure and, when the graph carries one, the target it is read against - the two numbers a
    /// bullet graph exists to compare.</remarks>
    public override ChartDataTable? ToDataTable() =>
        // The target column is always there, empty when there is no target: bullet graphs on one axes become
        // one table only when they all have the same shape.
        new(null,
            [new("label", DataColumnKind.Text), new("value"), new("target")],
            [[
                DataCell.FromText(Label ?? ""), DataCell.FromNumber(Value),
                Target is { } target ? DataCell.FromNumber(target) : DataCell.Empty,
            ]]);

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
