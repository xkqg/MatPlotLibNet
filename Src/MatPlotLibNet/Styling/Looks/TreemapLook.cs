// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Styling;

/// <summary>How a treemap is laid out.</summary>
internal sealed record TreemapLook : IThemeKeys<TreemapLook>
{
    /// <summary>The library's own treemap: laid out from the top left, as a treemap is read, its tiles unclipped.</summary>
    internal static TreemapLook Library { get; } = new();

    /// <inheritdoc />
    public void WriteKeys(ThemeDto keys) => keys.TreemapLikeSquarify = LikeSquarify.OrNullAtRest(false);

    /// <inheritdoc />
    public static TreemapLook ReadKeys(ThemeDto keys) => new() { LikeSquarify = keys.TreemapLikeSquarify ?? false };

    /// <summary>Whether a treemap is drawn as squarify draws one in a matplotlib axes: its tiles are bars
    /// (<c>ax.bar(x, dy, width=dx, bottom=y, align='edge')</c> from y = 0, in a Y that runs up), so the largest tile stands at the
    /// bottom left, and the tiles are clipped to the axes.</summary>
    public bool LikeSquarify { get; init; }
}
