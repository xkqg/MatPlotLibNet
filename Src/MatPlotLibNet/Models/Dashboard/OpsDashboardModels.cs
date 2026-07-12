// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Dashboard;

/// <summary>Input model for a single KPI tile in <see cref="FigureTemplates.OpsDashboard"/>.</summary>
/// <param name="Label">The small label shown beneath the headline value.</param>
/// <param name="Value">The headline numeric value.</param>
/// <param name="Format">Invariant-culture numeric format string (default <c>"0.##"</c>).</param>
public sealed record OpsTile(string Label, double Value, string Format = "0.##")
{
    /// <summary>Optional threshold function that returns an accent colour for the headline.
    /// When <c>null</c> the theme cycle colour is used.</summary>
    public Func<double, Color?>? AccentThreshold { get; init; }

    /// <summary>Convenience helper that creates a green/orange/red threshold.</summary>
    public static Func<double, Color?> Threshold(Color? ok, Color? warning, Color? critical, double warningAt, double criticalAt) =>
        value => value >= criticalAt ? critical : value >= warningAt ? warning : ok;
}

/// <summary>Input model for a state timeline row in <see cref="FigureTemplates.OpsDashboard"/>.</summary>
/// <param name="Label">The row label.</param>
/// <param name="Segments">Ordered state segments (coloured bands over time).</param>
public sealed record OpsStateTimeline(string Label, IReadOnlyList<StateSegment> Segments);

/// <summary>Input model for a trend line in <see cref="FigureTemplates.OpsDashboard"/>.</summary>
/// <param name="Label">Legend label for the line.</param>
/// <param name="X">X-axis values.</param>
/// <param name="Y">Y-axis values.</param>
public sealed record OpsTrendLine(string Label, double[] X, double[] Y);
