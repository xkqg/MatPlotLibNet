// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>Represents a node in a Sankey diagram.</summary>
/// <param name="Label">Primary label drawn beside (or inside) the node rect.</param>
/// <param name="Color">Optional fill colour; falls back to the series cycle colour when null.</param>
/// <param name="SubLabel">Optional secondary label drawn one line below <paramref name="Label"/>
/// at 80 % font size. Useful for node metrics like "$13.9B", "2 % Y/Y", or a timestamp —
/// common in financial Sankeys (revenue / expense breakdowns) where each node carries both
/// a magnitude and a change indicator.</param>
/// <param name="SubLabelColor">Optional colour override for <paramref name="SubLabel"/>.
/// Typically used to colour positive deltas green and negative deltas red without affecting
/// the primary label. Defaults to the theme's foreground colour when null.</param>
/// <param name="Column">Optional explicit column index. When set, overrides the BFS column
/// assignment computed from the link topology — useful for alluvial / time-step Sankeys where
/// the same label may legitimately appear in multiple columns (<c>Home → Home</c>) and the
/// column order is semantic (time progression) rather than topological. When null, the BFS
/// pass assigns columns based on distance from source nodes.</param>
public sealed record SankeyNode(
    string Label,
    Color? Color = null,
    string? SubLabel = null,
    Color? SubLabelColor = null,
    int? Column = null);
