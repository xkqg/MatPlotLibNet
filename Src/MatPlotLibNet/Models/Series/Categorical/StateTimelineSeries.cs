// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>A single discrete-coloured state segment on a <see cref="StateTimelineSeries"/> timeline.</summary>
/// <param name="Start">Start value along the X axis (data units).</param>
/// <param name="End">End value along the X axis (data units).</param>
/// <param name="Label">Display label centred within the segment.</param>
/// <param name="Color">Fill colour of the segment rectangle.</param>
public readonly record struct StateSegment(double Start, double End, string Label, Color Color)
{
    /// <summary>An optional fill pattern for this band, or <see cref="HatchPattern.None"/> (the default) for a
    /// flat fill.
    /// <para>This is what lets a timeline distinguish a <i>gap in knowledge</i> from a <i>fault</i>. On a monitored
    /// fleet, "I can no longer see you" is the most common failure and it is not the same failure as "you are
    /// broken" — a dashboard that paints both in a colour lies exactly when it matters. A hatched band reads as
    /// "no information" from across a room, and it spends nothing out of the alarm-colour budget.</para></summary>
    /// <remarks>Declared as an <c>init</c> property rather than a fifth positional parameter: a positional
    /// addition would break every existing construction site of this record struct.</remarks>
    public HatchPattern Hatch { get; init; }

    /// <summary>What this band is in — how bad, and whether it can be believed. Resting by default.
    /// <para>Set it and the band takes the pattern its visibility demands without the caller repeating the
    /// mapping: a silent stretch hatched one way, a shelved one the other. An explicit <see cref="Hatch"/>
    /// still wins. The band's <see cref="Color"/> is left alone — a timeline's colours name the states
    /// themselves ("Up", "Draining"), which is a different vocabulary from severity.</para></summary>
    /// <remarks>An <c>init</c> property for the same reason <see cref="Hatch"/> is one.</remarks>
    public OpsCondition Condition { get; init; } = OpsCondition.Resting;
}

/// <summary>A single-row timeline of discrete coloured state segments along the X axis —
/// e.g. a participant's up/down status over time, or an alarm state over time. Each
/// <see cref="StateSegment"/> defines one horizontal coloured rectangle spanning
/// <c>[Start, End]</c> in data units, with a centred <c>Label</c> text overlay.</summary>
/// <remarks>The Y range is fixed at [0, 1] so the segments fill the full plot height.
/// Use a mosaic or sub-plot layout to stack multiple timelines vertically.</remarks>
public sealed class StateTimelineSeries : ChartSeries
{
    /// <summary>The ordered list of state segments rendered on the timeline.</summary>
    public IReadOnlyList<StateSegment> Segments { get; }

    /// <summary>Initialises a <see cref="StateTimelineSeries"/> from a list of state segments.</summary>
    /// <param name="segments">Ordered state segments. An empty list is valid and renders nothing.</param>
    public StateTimelineSeries(IReadOnlyList<StateSegment> segments) => Segments = segments;

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (Segments.Count == 0) return new(null, null, null, null);
        double xMin = Segments.Min(s => s.Start);
        double xMax = Segments.Max(s => s.End);
        return new(xMin, xMax, 0.0, 1.0);
    }

    /// <inheritdoc />
    /// <remarks>Round-trips: <c>Type = "statetimeline"</c>; segment <c>Start</c> values via
    /// <see cref="SeriesDto.Starts"/>; <c>End</c> values via <see cref="SeriesDto.Ends"/>;
    /// <c>Label</c> values via <see cref="SeriesDto.Categories"/>; <c>Color</c> values via
    /// <see cref="SeriesDto.StateSegmentColors"/>. All other <see cref="ChartSeries"/> properties
    /// (e.g. <see cref="ChartSeries.Label"/>, <see cref="ChartSeries.Visible"/>) are NOT
    /// serialized — consistent with the StatTile minimal-DTO approach.</remarks>
    public override SeriesDto ToSeriesDto() => new()
    {
        Type       = "statetimeline",
        Starts     = Segments.Select(s => s.Start).ToArray(),
        Ends       = Segments.Select(s => s.End).ToArray(),
        Categories = Segments.Select(s => s.Label).ToArray(),
        StateSegmentColors = Segments.Select(s => s.Color).ToList(),
        // Null unless at least one segment is hatched, so an ordinary timeline emits no hatch bytes and its
        // golden stays byte-identical.
        StateSegmentHatches = Segments.Any(s => s.Hatch != HatchPattern.None)
            ? Segments.Select(s => s.Hatch).ToList()
            : null,
        // Same rule for the condition: written only when a band names one, so nothing changes on the wire for
        // a timeline that never did.
        StateSegmentSeverities = Segments.Any(s => s.Condition.Severity != OpsSeverity.Normal)
            ? Segments.Select(s => s.Condition.Severity).ToList()
            : null,
        StateSegmentVisibilities = Segments.Any(s => s.Condition.Visibility != OpsVisibility.Observed)
            ? Segments.Select(s => s.Condition.Visibility).ToList()
            : null,
    };

    /// <summary>Reconstructs a <see cref="StateTimelineSeries"/> from its serialization DTO, restoring
    /// segment starts, ends, labels, and per-segment colours, and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static StateTimelineSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var starts  = dto.Starts     ?? [];
        var ends    = dto.Ends       ?? [];
        var labels  = dto.Categories ?? [];
        var colors  = dto.StateSegmentColors ?? [];
        var hatches = dto.StateSegmentHatches;
        var severities = dto.StateSegmentSeverities;
        var visibilities = dto.StateSegmentVisibilities;
        int count   = Math.Min(Math.Min(starts.Length, ends.Length),
                               Math.Min(labels.Length, colors.Count));
        var segments = new Models.Series.StateSegment[count];
        for (int i = 0; i < count; i++)
        {
            segments[i] = new Models.Series.StateSegment(starts[i], ends[i], labels[i], colors[i])
            {
                Hatch = hatches is not null && i < hatches.Count ? hatches[i] : HatchPattern.None,
                Condition = new OpsCondition(
                    severities is not null && i < severities.Count ? severities[i] : OpsSeverity.Normal,
                    visibilities is not null && i < visibilities.Count ? visibilities[i] : OpsVisibility.Observed)
            };
        }
        return axes.StateTimeline(segments);
    }


    /// <inheritdoc />
    /// <remarks>One row per segment: the state it names and the span it covers. The colour and the hatch are how
    /// the segment is DRAWN; the state name is what it MEANS, and that is what a reader needs.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        var rows = new List<IReadOnlyList<DataCell>>(Segments.Count);
        foreach (var segment in Segments)
        {
            rows.Add([DataCell.FromText(segment.Label), DataCell.FromNumber(segment.Start), DataCell.FromNumber(segment.End)]);
        }

        return new ChartDataTable(null, [new("state", DataColumnKind.Text), new("start", Axis: DataAxis.X), new("end", Axis: DataAxis.X)], rows);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
