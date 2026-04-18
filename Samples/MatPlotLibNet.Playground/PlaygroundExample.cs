// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Reflection;

namespace MatPlotLibNet.Playground;

/// <summary>Typed identifier for every example the Playground knows how to build.
/// Replaces the pre-Phase-N <c>Dictionary&lt;string, Func&lt;…&gt;&gt;</c> keyed by
/// free-form strings — the compiler now guarantees that UI, examples registry,
/// predicates, and the code-snippet generator all agree on every example name.
/// <para>The <see cref="DescriptionAttribute"/> on each member carries the
/// human-readable label shown in the Playground dropdown — changing the display
/// label never drifts the dispatcher (single source of truth).</para></summary>
/// <remarks><b>Append-only ordinal contract (Phase O of v1.7.2):</b> never reorder,
/// remove, or renumber. New examples get the next unused ordinal at the end.
/// See <c>EnumOrdinalContractTests</c> for the enforcement gate.</remarks>
public enum PlaygroundExample
{
    [Description("Line Chart")]    LineChart    = 0,
    [Description("Bar Chart")]     BarChart     = 1,
    [Description("Scatter Plot")]  ScatterPlot  = 2,
    [Description("Multi-Series")]  MultiSeries  = 3,
    [Description("Heatmap")]       Heatmap      = 4,
    [Description("Pie Chart")]     PieChart     = 5,
    [Description("Histogram")]     Histogram    = 6,
    [Description("Contour Plot")]  ContourPlot  = 7,
    [Description("3D Surface")]    Surface3D    = 8,
    [Description("Radar Chart")]   RadarChart   = 9,
    [Description("Violin Plot")]   ViolinPlot   = 10,
    [Description("Candlestick")]   Candlestick  = 11,
    [Description("Treemap")]       Treemap      = 12,
    [Description("Sankey Flow")]   SankeyFlow   = 13,
    [Description("Polar Line")]    PolarLine    = 14,
    [Description("Multi-Subplot")] MultiSubplot = 15,
}

/// <summary>Display-name helpers for <see cref="PlaygroundExample"/>.</summary>
public static class PlaygroundExampleExtensions
{
    /// <summary>Returns the human-readable label (from the <see cref="DescriptionAttribute"/>)
    /// for the supplied example, falling back to the enum member name.</summary>
    public static string DisplayName(this PlaygroundExample example)
    {
        var member = typeof(PlaygroundExample).GetMember(example.ToString()).FirstOrDefault();
        return member?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? example.ToString();
    }

    /// <summary>Reverse lookup — resolves a display name back to its enum value.
    /// Returns <see langword="null"/> when no match exists (used by UI round-trips
    /// where a user-visible label must map back to the typed identifier).</summary>
    public static PlaygroundExample? FromDisplayName(string displayName)
    {
        foreach (var example in Enum.GetValues<PlaygroundExample>())
            if (example.DisplayName() == displayName)
                return example;
        return null;
    }
}
