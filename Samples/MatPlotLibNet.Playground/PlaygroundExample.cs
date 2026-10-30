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
    /// <summary>A single line over x and y values, styled by the line-style, width and marker options.</summary>
    [Description("Line Chart")]    LineChart    = 0,
    /// <summary>Vertical bars, one per category, each labelled with its value.</summary>
    [Description("Bar Chart")]     BarChart     = 1,
    /// <summary>Individual points plotted at x and y positions, with no line between them.</summary>
    [Description("Scatter Plot")]  ScatterPlot  = 2,
    /// <summary>Several curves on one set of axes, told apart by a legend.</summary>
    [Description("Multi-Series")]  MultiSeries  = 3,
    /// <summary>A matrix of values drawn as coloured cells, coloured by the chosen colour map.</summary>
    [Description("Heatmap")]       Heatmap      = 4,
    /// <summary>A circle cut into wedges whose angles are each part's share of the total, with percentage labels.</summary>
    [Description("Pie Chart")]     PieChart     = 5,
    /// <summary>The distribution of a set of values, counted into 20 equal-width bins.</summary>
    [Description("Histogram")]     Histogram    = 6,
    /// <summary>Lines of equal value across an x-y grid, coloured by the chosen colour map.</summary>
    [Description("Contour Plot")]  ContourPlot  = 7,
    /// <summary>A height field drawn as a shaded surface on three-dimensional axes, seen from a tilted camera.</summary>
    [Description("3D Surface")]    Surface3D    = 8,
    /// <summary>Two sets of scores on the same categories, each drawn as a translucent polygon around a common centre.</summary>
    [Description("Radar Chart")]   RadarChart   = 9,
    /// <summary>The shape of several groups' distributions, each mirrored around its own centre line.</summary>
    [Description("Violin Plot")]   ViolinPlot   = 10,
    /// <summary>Open, high, low and close prices per date, drawn as candles that are green when the price rose and red when it fell.</summary>
    [Description("Candlestick")]   Candlestick  = 11,
    /// <summary>A hierarchy drawn as nested rectangles whose areas are proportional to each node's value.</summary>
    [Description("Treemap")]       Treemap      = 12,
    /// <summary>Flows between nodes drawn as bands whose widths are proportional to the amount moved.</summary>
    [Description("Sankey Flow")]   SankeyFlow   = 13,
    /// <summary>A curve given as radius against angle, drawn on polar axes.</summary>
    [Description("Polar Line")]    PolarLine    = 14,
    /// <summary>One figure holding two subplots side by side, a line chart and a bar chart, each with its own title.</summary>
    [Description("Multi-Subplot")] MultiSubplot = 15,
    /// <summary>A line whose Y axis skips an empty stretch of values (20 to 80), so the data on either side stays readable.</summary>
    [Description("Axis Breaks")]   AxisBreaks   = 16,
    /// <summary>A line with minor ticks, and grid lines drawn at both the major and the minor ticks.</summary>
    [Description("Minor Grid")]    MinorGrid    = 17,

    // Dashboard-oriented examples (StatTile/StateTimeline, Threshold + LegendValues).
    /// <summary>A row of KPI tiles above a timeline of a service's state over 24 hours; a tile is a number with a label, so its axes are hidden.</summary>
    [Description("Dashboard Tiles")] DashboardTiles = 18,
    /// <summary>A line with a dashed alarm level and a shaded span where it is exceeded, and the legend showing the line's current reading.</summary>
    [Description("Threshold Line")]  ThresholdLine  = 19,

    // Lines named at their end on a dual axis whose axes wear their line's colour.
    /// <summary>Temperature and humidity on two Y axes, each line named at its end and each axis coloured like its line, so no legend is needed.</summary>
    [Description("Direct Labels")]   DirectLabels   = 20,

    // A chart drawn by one verb call: bars largest first and their running share on a second axis.
    /// <summary>Defect counts as bars, largest first, with their running share of the total as a line on a second axis that ends at 100 %.</summary>
    [Description("Pareto Chart")]    ParetoChart    = 21,
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
