// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Read-only view of axes state needed by series for data range computation.</summary>
public interface IAxesContext
{
    double? XAxisMin { get; }

    double? XAxisMax { get; }

    double? YAxisMin { get; }

    double? YAxisMax { get; }

    BarMode BarMode { get; }

    IReadOnlyList<ISeries> AllSeries { get; }

    /// <summary>The x axis scale. A <see cref="AxisScale.Log"/> axis ranges over POSITIVE data only — a
    /// non-positive value has no place on it and must be masked, never allowed to drive the floor to NaN.</summary>
    AxisScale XScale => AxisScale.Linear;

    /// <summary>The y axis scale — see <see cref="XScale"/>.</summary>
    AxisScale YScale => AxisScale.Linear;
}
