// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Read-only view of axes state needed by series for data range computation.</summary>
public interface IAxesContext
{
    /// <summary>Gets the user-specified X axis minimum, or null.</summary>
    double? XAxisMin { get; }

    /// <summary>Gets the user-specified X axis maximum, or null.</summary>
    double? XAxisMax { get; }

    /// <summary>Gets the user-specified Y axis minimum, or null.</summary>
    double? YAxisMin { get; }

    /// <summary>Gets the user-specified Y axis maximum, or null.</summary>
    double? YAxisMax { get; }

    /// <summary>Gets the bar mode for this axes.</summary>
    BarMode BarMode { get; }

    /// <summary>Gets all series on this axes.</summary>
    IReadOnlyList<ISeries> AllSeries { get; }
}
