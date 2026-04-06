// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a Gantt chart showing task durations as horizontal bars on a timeline.</summary>
public sealed class GanttSeries : ChartSeries, IHasDataRange
{
    public string[] Tasks { get; }
    public double[] Starts { get; }
    public double[] Ends { get; }
    public Color? Color { get; set; }
    public double BarHeight { get; set; } = 0.6;

    public GanttSeries(string[] tasks, double[] starts, double[] ends)
    {
        Tasks = tasks; Starts = starts; Ends = ends;
    }

    /// <inheritdoc />
    public DataRangeContribution ComputeDataRange(IAxesContext context) =>
        new(Math.Min(Starts.Min(), Ends.Min()), Math.Max(Starts.Max(), Ends.Max()),
            context.YAxisMin ?? -0.5, context.YAxisMax ?? (Tasks.Length - 0.5));

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
