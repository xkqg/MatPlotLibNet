// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The five parts a control chart is made of, handed to the callback of <see cref="XYAxesExtensions.ControlChart"/>
/// so each can be styled on its own: the values, the points beyond a limit, the centre line and the two limits. They are the
/// very series and reference lines on the axes, so a change made through them is the chart's.
/// <para>A value of this type can only be made by the library: it has no public constructor, and its properties have no
/// setters.</para></summary>
public readonly record struct ControlChartParts
{
    /// <summary>Makes the parts of one control chart.</summary>
    /// <param name="values">The values as a marked line.</param>
    /// <param name="violations">The values strictly beyond a limit.</param>
    /// <param name="center">The centre line.</param>
    /// <param name="lower">The lower control limit.</param>
    /// <param name="upper">The upper control limit.</param>
    internal ControlChartParts(LineSeries values, ScatterSeries violations, ReferenceLine center, ReferenceLine lower,
        ReferenceLine upper)
    {
        Values = values;
        Violations = violations;
        Center = center;
        Lower = lower;
        Upper = upper;
    }

    /// <summary>The values, in the order given, as a line with a circle on each value, wearing the theme's colour for the
    /// chart's first series. It has no legend label until one is given here.</summary>
    public LineSeries Values { get; }

    /// <summary>The values strictly beyond a limit (rule 1), at their x positions, as a scatter series drawn over the line's
    /// markers in the theme's colour for the chart's second series; empty when no value is beyond a limit. It has no legend
    /// label until one is given here.</summary>
    public ScatterSeries Violations { get; }

    /// <summary>The centre line: a solid reference line named "CL", in the colour a reference line has when none is set. Its
    /// <see cref="ReferenceLine.Value"/> is the centre the chart was drawn with, estimated or given.</summary>
    public ReferenceLine Center { get; }

    /// <summary>The lower control limit: a dashed reference line named "LCL", in the colour a reference line has when none is
    /// set. Its <see cref="ReferenceLine.Value"/> is the limit the chart was drawn with, estimated or given.</summary>
    public ReferenceLine Lower { get; }

    /// <summary>The upper control limit: a dashed reference line named "UCL", in the colour a reference line has when none is
    /// set. Its <see cref="ReferenceLine.Value"/> is the limit the chart was drawn with, estimated or given.</summary>
    public ReferenceLine Upper { get; }
}
