// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>Configurable opacity / transition tokens used by the embedded browser-side
/// interaction scripts. Defaults match the v1.7.1 hard-coded values, so callers who don't
/// opt in see no behaviour change. Set via <see cref="MatPlotLibNet.FigureBuilder.WithInteractionTheme"/>;
/// the values are emitted as <c>data-mpl-*</c> attributes on each owning SVG and the
/// scripts read them at runtime.</summary>
/// <param name="HighlightOpacity">Opacity applied to non-hovered series during highlight (default 0.3).</param>
/// <param name="SankeyDimLinkOpacity">Opacity for unreachable Sankey links during hover (default 0.08).</param>
/// <param name="SankeyDimNodeOpacity">Opacity for unreachable Sankey nodes during hover (default 0.25).</param>
/// <param name="TreemapTransitionMs">Duration of the treemap drilldown viewBox animation in milliseconds (default 350).</param>
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
    /// <summary>The default theme used when the caller doesn't supply one. Identity element.</summary>
    public static readonly InteractionTheme Default = new();
}
