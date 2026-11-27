// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a Gantt chart showing task durations as horizontal bars on a timeline: one row for each task, named by it on the
/// Y axis, the first task on top, each task in the next colour of the cycle, as the chart matplotlib users draw with one <c>barh</c> a task,
/// the task names for ticks and <c>invert_yaxis()</c>.</summary>
public sealed class GanttSeries : ChartSeries, IHasColor, IEdgedPatch, ICategoryLabeled, ICategoryPlacement, ISeriesKind<GanttSeries>
{
    /// <summary>The name of each task; task <c>i</c> is drawn on row <c>i</c> from the top, named by it on the Y axis.</summary>
    public string[] Tasks { get; }
    /// <summary>Where each task's bar begins along the X axis, one value per task.</summary>
    public double[] Starts { get; }
    /// <summary>Where each task's bar ends along the X axis, one value per task; a task whose start or end is missing
    /// (NaN) or infinite draws no bar.</summary>
    public double[] Ends { get; }
    /// <inheritdoc />
    /// <remarks>A colour given here is every task's; given none, each task wears the next colour of the cycle.</remarks>
    public Color? Color { get; set => Change(ref field, value); }

    /// <inheritdoc />
    string[]? ICategoryLabeled.CategoryLabels => Tasks;

    /// <inheritdoc />
    bool ICategoryPlacement.CategoriesAlongY => true;

    /// <inheritdoc />
    bool ICategoryPlacement.CategoriesTopDown => true;

    /// <inheritdoc />
    /// <remarks>One colour of the cycle for each task, as one <c>barh</c> a task moves matplotlib's patch cycle once a call.</remarks>
    internal override int ColorSlotSpan => Math.Max(1, Tasks.Length);

    /// <inheritdoc />
    /// <remarks>matplotlib draws a task with <c>barh</c>: a patch, which takes the patch cycle.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Patch;

    /// <inheritdoc />
    bool ICategoryPlacement.RangeRoundsOut => true;

    /// <inheritdoc />
    internal override LegendSwatch LegendSwatch => LegendSwatch.EdgedPatch;
    /// <summary>Height of each task bar in data units of the row axis, where rows are 1 apart; 1 makes neighbouring
    /// bars touch. Default 0.6.</summary>
    public double BarHeight { get; set => Change(ref field, value); } = 0.6;

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
        // Over the bars that can be drawn: a bar with an end that is missing (NaN) or infinite is not drawn, so neither
        // of its ends is part of the extent.
        var x = Starts.Zip(Ends, (start, end) => new Range1D(start, end)).DrawableRange();
        // The rows' extent is the bars' own, each in its category slot (CategorySlot.FromTop), and the only sticky edges are the bars'
        // starts: each bar is matplotlib's barh(i, d, left=start), and Axes.bar registers a bar's foot and nothing else.
        var rows = BarHeight.AcrossSlots(Tasks.Length);
        var starts = DrawnStarts();
        return new(x?.Lo, x?.Hi, context.YAxisMin ?? rows.Lo, context.YAxisMax ?? rows.Hi,
            StickyXMin: starts?.Lo, StickyXMax: starts?.Hi);
    }

    /// <summary>The lowest and the highest start among the bars that can be drawn, <see langword="null"/> when none can.</summary>
    private Range1D? DrawnStarts()
    {
        var range = Range1D.Empty;
        for (int i = 0; i < Math.Min(Starts.Length, Ends.Length); i++)
        {
            if (Starts[i].IsDrawable() && Ends[i].IsDrawable()) range = range.Including(Starts[i]);
        }

        return range.OrNone();
    }

    static string ISeriesKind<GanttSeries>.Kind => "gantt";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        Tasks = Tasks, Starts = Starts, Ends = Ends,
        Color = Color, BarHeight = BarHeight
    };

    /// <summary>The reader of a <see cref="GanttSeries"/> that <see cref="SeriesRegistry"/> takes; it also carries bar height.</summary>
    internal static GanttSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Gantt(dto.Tasks ?? [], dto.Starts ?? [], dto.Ends ?? []);
        s.Color = dto.Color;
        if (dto.BarHeight is { } barHeight) s.BarHeight = barHeight;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>One row per task: its name, and the two ends of its bar.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        int count = Math.Min(Tasks.Length, Math.Min(Starts.Length, Ends.Length));
        var rows = new IReadOnlyList<DataCell>[count];
        for (int i = 0; i < count; i++)
        {
            rows[i] = [DataCell.FromText(Tasks[i]), DataCell.FromNumber(Starts[i]), DataCell.FromNumber(Ends[i])];
        }

        return new ChartDataTable(null, [new("task", DataColumnKind.Text), ChartDataColumn.Position(DataAxis.X, "start"), ChartDataColumn.Position(DataAxis.X, "end")], rows);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
