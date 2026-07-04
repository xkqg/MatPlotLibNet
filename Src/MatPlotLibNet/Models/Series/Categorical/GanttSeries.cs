// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a Gantt chart showing task durations as horizontal bars on a timeline.</summary>
public sealed class GanttSeries : ChartSeries, IHasColor
{
    public string[] Tasks { get; }
    public double[] Starts { get; }
    public double[] Ends { get; }
    public Color? Color { get; set; }
    public double BarHeight { get; set; } = 0.6;

    /// <summary>Initializes a new <see cref="GanttSeries"/> with task names and their start/end data positions.</summary>
    /// <param name="tasks">Task label for each row.</param>
    /// <param name="starts">Start value for each task bar.</param>
    /// <param name="ends">End value for each task bar.</param>
    public GanttSeries(string[] tasks, double[] starts, double[] ends)
    {
        Tasks = tasks; Starts = starts; Ends = ends;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        double xMin = Math.Min(Starts.Min(), Ends.Min());
        double xMax = Math.Max(Starts.Max(), Ends.Max());
        double yMin = context.YAxisMin ?? -0.5;
        double yMax = context.YAxisMax ?? (Tasks.Length - 0.5);
        return new(xMin, xMax, yMin, yMax,
            StickyXMin: xMin, StickyXMax: xMax, StickyYMin: yMin, StickyYMax: yMax);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "gantt",
        Tasks = Tasks, Starts = Starts, Ends = Ends,
        Color = Color, BarHeight = BarHeight
    };

    /// <summary>Reconstructs a <see cref="GanttSeries"/> from its serialization DTO, including bar height, and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static GanttSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Gantt(dto.Tasks ?? [], dto.Starts ?? [], dto.Ends ?? []);
        s.Color = dto.Color;
        if (dto.BarHeight.HasValue)
        {
            s.BarHeight = dto.BarHeight.Value;
        }
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
