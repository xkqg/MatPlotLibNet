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
public sealed class BulletGraphSeries : ChartSeries
{
    /// <summary>The measure — the bar itself.</summary>
    public double Value { get; }

    /// <summary>The comparative, drawn as a perpendicular tick across the bar, or <see langword="null"/> for
    /// no target. A measure without a comparative cannot be judged: the reader has to supply the missing
    /// context from memory, and mostly does not.</summary>
    public double? Target { get; set; }

    /// <summary>The qualitative ranges behind the bar, in ascending order of threshold, or
    /// <see langword="null"/> for none. Each band spans from its predecessor's threshold up to its own.</summary>
    /// <remarks>Deliberately the same <see cref="GaugeBand"/> the gauge uses: a (threshold, colour) pair is a
    /// (threshold, colour) pair, and minting a second type for the same shape is how a codebase acquires two
    /// band models that drift apart.</remarks>
    public IReadOnlyList<GaugeBand>? Bands { get; set; }

    /// <summary>The bar's colour, or <see langword="null"/> for the theme's foreground. The bar is the
    /// measure, not an alarm — colouring it by default would spend a signal that has nowhere else to come from.</summary>
    public Color? BarColor { get; set; }

    /// <summary>The colour of the target tick, or <see langword="null"/> for the theme's foreground.</summary>
    public Color? TargetColor { get; set; }

    /// <summary>Horizontal (default) draws the strip left-to-right; vertical draws it bottom-up.</summary>
    /// <remarks>Reuses the library's shared <see cref="Models.Orientation"/> (Horizontal = 0), not
    /// <see cref="BarOrientation"/> — whose ordinals are the other way round, so reusing it would silently flip
    /// this default.</remarks>
    public Orientation Orientation { get; set; } = Orientation.Horizontal;

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
            double max = Math.Max(Value, Target ?? Value);
            if (Bands is { Count: > 0 })
            {
                max = Math.Max(max, Bands[^1].Threshold);
            }

            return max <= 0 ? 1 : max;
        }
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        Orientation == Orientation.Horizontal
            ? new(0, UpperBound, null, null)
            : new(null, null, 0, UpperBound);

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "bulletgraph",
        GaugeValue = Value,
        BulletTarget = Target,
        BandThresholds = Bands?.Select(b => b.Threshold).ToArray(),
        BandColors = Bands?.Select(b => b.Color).ToList(),
        Color = BarColor,
        TrackColor = TargetColor,
        Orientation = Orientation != Orientation.Horizontal ? Orientation.ToString().ToLowerInvariant() : null,
    };

    /// <summary>Reconstructs a <see cref="BulletGraphSeries"/> from its serialization DTO, restoring the
    /// measure, the target, the qualitative bands, the colours and the orientation, and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
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

        ChartSerializer.ApplyEnum<Orientation>(dto.Orientation, v => s.Orientation = v);
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
