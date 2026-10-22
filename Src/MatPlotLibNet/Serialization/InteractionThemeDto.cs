// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Serialization;

/// <summary>The figure JSON's form of an <see cref="InteractionTheme"/> (the tokens the embedded interaction scripts
/// read): each token is written only where it differs from <see cref="InteractionTheme.Default"/>, and read back over it.</summary>
internal sealed record InteractionThemeDto
{
    public double? HighlightOpacity { get; init; }
    public double? SankeyDimLinkOpacity { get; init; }
    public double? SankeyDimNodeOpacity { get; init; }
    public int? TreemapTransitionMs { get; init; }
    public double? TooltipOffsetX { get; init; }
    public double? TooltipOffsetY { get; init; }

    private static InteractionTheme Rest => InteractionTheme.Default;

    /// <summary>The JSON's form of <paramref name="theme"/>.</summary>
    internal static InteractionThemeDto From(InteractionTheme theme) => new()
    {
        HighlightOpacity = theme.HighlightOpacity.OrNullAtRest(Rest.HighlightOpacity),
        SankeyDimLinkOpacity = theme.SankeyDimLinkOpacity.OrNullAtRest(Rest.SankeyDimLinkOpacity),
        SankeyDimNodeOpacity = theme.SankeyDimNodeOpacity.OrNullAtRest(Rest.SankeyDimNodeOpacity),
        TreemapTransitionMs = theme.TreemapTransitionMs.OrNullAtRest(Rest.TreemapTransitionMs),
        TooltipOffsetX = theme.TooltipOffsetX.OrNullAtRest(Rest.TooltipOffsetX),
        TooltipOffsetY = theme.TooltipOffsetY.OrNullAtRest(Rest.TooltipOffsetY),
    };

    /// <summary>The interaction theme this record describes.</summary>
    internal InteractionTheme ToInteractionTheme() => Rest with
    {
        HighlightOpacity = HighlightOpacity ?? Rest.HighlightOpacity,
        SankeyDimLinkOpacity = SankeyDimLinkOpacity ?? Rest.SankeyDimLinkOpacity,
        SankeyDimNodeOpacity = SankeyDimNodeOpacity ?? Rest.SankeyDimNodeOpacity,
        TreemapTransitionMs = TreemapTransitionMs ?? Rest.TreemapTransitionMs,
        TooltipOffsetX = TooltipOffsetX ?? Rest.TooltipOffsetX,
        TooltipOffsetY = TooltipOffsetY ?? Rest.TooltipOffsetY,
    };
}
