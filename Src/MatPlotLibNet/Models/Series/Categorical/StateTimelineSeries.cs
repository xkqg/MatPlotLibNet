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
public readonly record struct StateSegment(double Start, double End, string Label, Color Color);

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
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
