// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

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
}
