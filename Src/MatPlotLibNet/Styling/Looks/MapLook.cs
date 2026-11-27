// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Styling;

/// <summary>How a map axes (one a map series stands on) is framed.</summary>
internal sealed record MapLook : IThemeKeys<MapLook>
{
    /// <summary>The library's own map: the plot's rectangle painted, no outline.</summary>
    internal static MapLook Library { get; } = new();

    /// <inheritdoc />
    public void WriteKeys(ThemeDto keys) => keys.MapsLikeCartopy = LikeCartopy.OrNullAtRest(false);

    /// <inheritdoc />
    public static MapLook ReadKeys(ThemeDto keys) => new() { LikeCartopy = keys.MapsLikeCartopy ?? false };

    /// <summary>Whether a map axes is framed as cartopy frames its <c>GeoAxes</c>: the background is the area inside the map's
    /// outline (<c>ax.patch</c>), and the outline is drawn over the map as its spine (<c>ax.spines['geo']</c>, in the colour and width
    /// of the theme's spines).</summary>
    public bool LikeCartopy { get; init; }
}
