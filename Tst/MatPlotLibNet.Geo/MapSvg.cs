// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>Reads the marks of a map's picture back out of its SVG, for the tests of the layers: the frame (the ocean path, the
/// first path of a map that has one), the pixel a position stands at on a PlateCarree map, the circles and the polylines.</summary>
internal static class MapSvg
{
    /// <summary>How far a number read back from the picture may lie from the one the layer worked out. The SVG writes a
    /// coordinate or a length to two decimals, so one number is within half a hundredth of a pixel of the layer's; a test that
    /// compares a read-back number with one worked out from other read-back numbers (a pixel from the frame) allows a
    /// hundredth. A hair over, so a difference of exactly a hundredth in binary arithmetic passes.</summary>
    internal const double WrittenPrecision = 0.01 + 1e-9;

    /// <summary>The corners of the frame: the ocean path, which is the first path of the picture.</summary>
    internal static (double Left, double Right, double Top, double Bottom) Frame(string svg)
    {
        var points = svg.Elements("path").First(path => path["d"] is not null).PathPoints();
        return (points.Min(p => p.X), points.Max(p => p.X), points.Min(p => p.Y), points.Max(p => p.Y));
    }

    /// <summary>The pixel of a longitude and latitude on a PlateCarree map whose frame is drawn.</summary>
    internal static (double X, double Y) PixelOf(string svg, double lon, double lat)
    {
        var (left, right, top, bottom) = Frame(svg);
        return (left + (lon + 180) / 360 * (right - left), top + (90 - lat) / 180 * (bottom - top));
    }

    /// <summary>The circles of the picture: the centre and the radius of each, in the order they are drawn.</summary>
    internal static (double X, double Y, double R)[] Circles(string svg) =>
        [.. svg.Elements("circle").Where(circle => circle["cx"] is not null && circle["cy"] is not null && circle["r"] is not null)
            .Select(circle => (circle.Number("cx"), circle.Number("cy"), circle.Number("r")))];

    /// <summary>The polylines of the picture: each element and its vertices.</summary>
    internal static (SvgElement Element, (double X, double Y)[] Points)[] Polylines(string svg) =>
        [.. svg.Elements("polyline").Where(polyline => polyline["points"] is not null)
            .Select(polyline => (polyline, polyline.Points().Select(point => (point.X, point.Y)).ToArray()))];

    /// <summary>The fills (hex, upper case) of the paths of the picture, in the order drawn.</summary>
    internal static string[] PathFills(string svg) =>
        [.. svg.Elements("path").Where(path => path["d"] is not null).Select(path => path["fill"]).OfType<string>()
            .Where(fill => fill.StartsWith('#')).Select(fill => fill.ToUpperInvariant())];

    /// <summary>The fills of the handles of a legend: its rectangles that have no outline (the frame of a classic legend has one).</summary>
    internal static string[] HandleFills(string legend) =>
        [.. legend.Elements("rect").Where(rect => rect["stroke"] is null).Select(rect => rect["fill"]).OfType<string>().Where(fill => fill.StartsWith('#'))];
}
