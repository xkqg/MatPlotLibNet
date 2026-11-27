// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Styling;

/// <summary>What a raster export does with the marks it draws.</summary>
internal sealed record RasterLook : IThemeKeys<RasterLook>
{
    /// <summary>The library's own raster: every mark where the layout puts it.</summary>
    internal static RasterLook Library { get; } = new();

    /// <inheritdoc />
    public void WriteKeys(ThemeDto keys) => keys.StrokesOnPixelGrid = StrokesOnPixelGrid.OrNullAtRest(false);

    /// <inheritdoc />
    public static RasterLook ReadKeys(ThemeDto keys) => new() { StrokesOnPixelGrid = keys.StrokesOnPixelGrid ?? false };

    /// <summary>Whether the marks matplotlib draws as horizontal and vertical segments (a spine, a tick mark, a grid line, a bar, a
    /// box, a reference line, the ticks of a rug plot, a cell of a matrix) are put on the pixel grid the way its Agg backend puts
    /// each kind there (a whole pixel, half a pixel on for an odd width), and a horizontal or vertical line is covered by the share of each
    /// pixel it takes. The one place this is read: the raster surface the theme is told to (<see cref="Rendering.IThemedRenderContext"/>)
    /// holds a pixel grid or none, and every mark asks the surface.</summary>
    public bool StrokesOnPixelGrid { get; init; }
}
