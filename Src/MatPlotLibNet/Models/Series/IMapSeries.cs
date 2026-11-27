// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Models.Series;

/// <summary>A series that draws a map. The axes it stands on is a map axes, as cartopy's <c>GeoAxes</c> is: one unit of Y as long
/// as one unit of X (<c>set_aspect(1)</c> with the box adjusted and centred, unless the axes was given an aspect of its own),
/// the map's own limits, which no theme rounds out (<c>set_global</c>), and under a theme that frames maps as cartopy does
/// (<c>MapLook.LikeCartopy</c>) the map's outline as its background and its spine. The Geo package's map series says so; the
/// layout (<c>PlotAspectExtensions.FittedTo</c>) and the range read what it asks (<see cref="AxesAsk.Map"/>, folded by
/// <c>AxesRangeExtensions.AskUnder</c>), and the Cartesian renderer draws the outline.</summary>
internal interface IMapSeries : IAsksOfAxes
{
    /// <inheritdoc cref="AxesAsk.Map"/>
    AxesAsk IAsksOfAxes.AskUnder(Styling.Theme theme) => AxesAsk.Map;

    /// <summary>The edge of the map in the plot, in pixels, closed: cartopy's <c>projection.boundary</c>, placed as the map itself is,
    /// by the transform of the axes.</summary>
    /// <param name="transform">The transform of the axes, which holds the plot area the map is drawn in and the range it shows: the
    /// map's own limits when nothing has zoomed or moved them.</param>
    /// <returns>The outline's points, the first again at the end; a zoomed map's outline reaches beyond the plot.</returns>
    IReadOnlyList<Point> OutlineIn(DataTransform transform);
}
