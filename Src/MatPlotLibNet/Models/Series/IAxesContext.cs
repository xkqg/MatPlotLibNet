// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Read-only view of axes state needed by series for data range computation.</summary>
public interface IAxesContext
{
    /// <summary>The lower x limit the user pinned on the axes, in data units, or <see langword="null"/> when the x
    /// axis is left to follow the data.</summary>
    double? XAxisMin { get; }

    /// <summary>The upper x limit the user pinned on the axes, in data units, or <see langword="null"/> when the x
    /// axis is left to follow the data.</summary>
    double? XAxisMax { get; }

    /// <summary>The lower y limit the user pinned on the axes, in data units, or <see langword="null"/> when the y
    /// axis is left to follow the data.</summary>
    double? YAxisMin { get; }

    /// <summary>The upper y limit the user pinned on the axes, in data units, or <see langword="null"/> when the y
    /// axis is left to follow the data.</summary>
    double? YAxisMax { get; }

    /// <summary>How bar series on these axes share the category slot — side by side, stacked or overlaid — which
    /// decides how far a bar's extent reaches.</summary>
    BarMode BarMode { get; }

    /// <summary>The series that share this context's axis pair — the set a stacked bar sums over and a range
    /// computation ranges with. NOT every series drawn on the subplot: secondary-axis series live in their own
    /// context. For "everything the picture shows" read <see cref="Models.Axes.AllSeries"/>.</summary>
    IReadOnlyList<ISeries> AllSeries { get; }

    /// <summary>The x axis scale. A <see cref="AxisScale.Log"/> axis ranges over POSITIVE data only — a
    /// non-positive value has no place on it and must be masked, never allowed to drive the floor to NaN.</summary>
    AxisScale XScale => AxisScale.Linear;

    /// <summary>The y axis scale — see <see cref="XScale"/>.</summary>
    AxisScale YScale => AxisScale.Linear;
}
