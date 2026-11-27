// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>Configurable opacity / transition tokens used by the embedded browser-side
/// interaction scripts. Defaults match the values the scripts hard-coded before they were
/// configurable, so callers who don't opt in see no behaviour change. Set via <see cref="FigureBuilderInteractionVerbs.extension(FigureBuilder).WithInteractionTheme"/>;
/// a value that differs from <see cref="Default"/> is written as an attribute of the chart's root, and the script that uses it
/// (the highlight, the Sankey hover, the treemap drilldown, the tooltips of the page and of the server) reads it there.</summary>
/// <param name="HighlightOpacity">Opacity applied to non-hovered series during highlight (default 0.3).</param>
/// <param name="SankeyDimLinkOpacity">Opacity for unreachable Sankey links during hover (default 0.08).</param>
/// <param name="SankeyDimNodeOpacity">Opacity for unreachable Sankey nodes during hover (default 0.25).</param>
/// <param name="TreemapTransitionMs">How long a treemap drilldown takes to fade a collapsed subtree out and an expanded one in, in
/// milliseconds (default 350); none for a reader who asked for reduced motion.</param>
/// <param name="TooltipOffsetX">Horizontal offset (px) of the tooltip from the cursor (default 12).</param>
/// <param name="TooltipOffsetY">Vertical offset (px) of the tooltip from the cursor (default -4).</param>
public sealed record InteractionTheme(
    double HighlightOpacity     = 0.3,
    double SankeyDimLinkOpacity = 0.08,
    double SankeyDimNodeOpacity = 0.25,
    int    TreemapTransitionMs  = 350,
    double TooltipOffsetX       = 12,
    double TooltipOffsetY       = -4)
{
    /// <summary>Opacity applied to non-hovered series during highlight (default 0.3), from 0 (clear) to 1 (opaque).</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 0, above 1 or not a number.</exception>
    public double HighlightOpacity { get; init => field = value.ThrowIfNotOpacity(nameof(HighlightOpacity)); } = HighlightOpacity;

    /// <summary>Opacity for unreachable Sankey links during hover (default 0.08), from 0 (clear) to 1 (opaque).</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 0, above 1 or not a number.</exception>
    public double SankeyDimLinkOpacity { get; init => field = value.ThrowIfNotOpacity(nameof(SankeyDimLinkOpacity)); } = SankeyDimLinkOpacity;

    /// <summary>Opacity for unreachable Sankey nodes during hover (default 0.25), from 0 (clear) to 1 (opaque).</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 0, above 1 or not a number.</exception>
    public double SankeyDimNodeOpacity { get; init => field = value.ThrowIfNotOpacity(nameof(SankeyDimNodeOpacity)); } = SankeyDimNodeOpacity;

    /// <summary>The default theme used when the caller doesn't supply one. Identity element.</summary>
    public static readonly InteractionTheme Default = new();
}
