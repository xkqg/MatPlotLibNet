// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>Reads the marks of a map's picture back out of its SVG, for the tests of the layers: the frame (the ocean path, the
/// first path of a map that has one), the pixel a position stands at on a PlateCarree map, the circles and the polylines.</summary>
internal static class MapSvg
{
    private static readonly Regex PathData = new("<path d=\"(?<d>[^\"]+)\"", RegexOptions.Compiled);
    private static readonly Regex PathPoint = new(@"[ML] (?<x>[-0-9.eE]+) (?<y>[-0-9.eE]+)", RegexOptions.Compiled);
    private static readonly Regex Circle = new("<circle cx=\"(?<x>[-0-9.eE]+)\" cy=\"(?<y>[-0-9.eE]+)\" r=\"(?<r>[-0-9.eE]+)\"", RegexOptions.Compiled);
    private static readonly Regex Polyline = new("<polyline points=\"(?<p>[^\"]+)\"(?<rest>[^>]*)>", RegexOptions.Compiled);

    private static double Number(string text) => double.Parse(text, CultureInfo.InvariantCulture);

    /// <summary>The corners of the frame: the ocean path, which is the first path of the picture.</summary>
    internal static (double Left, double Right, double Top, double Bottom) Frame(string svg)
    {
        var points = PathPoint.Matches(PathData.Match(svg).Groups["d"].Value).Select(m => (X: Number(m.Groups["x"].Value), Y: Number(m.Groups["y"].Value))).ToArray();
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
        [.. Circle.Matches(svg).Select(m => (Number(m.Groups["x"].Value), Number(m.Groups["y"].Value), Number(m.Groups["r"].Value)))];

    /// <summary>The polylines of the picture: the vertices of each and the rest of its attributes.</summary>
    internal static (string Attributes, (double X, double Y)[] Points)[] Polylines(string svg) =>
        [.. Polyline.Matches(svg).Select(m => (m.Groups["rest"].Value,
            m.Groups["p"].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(pair => pair.Split(','))
                .Select(xy => (Number(xy[0]), Number(xy[1]))).ToArray()))];

    /// <summary>The stroke width of a polyline's attributes.</summary>
    internal static double StrokeWidth(string attributes) =>
        Number(Regex.Match(attributes, "stroke-width=\"(?<w>[-0-9.eE]+)\"").Groups["w"].Value);
}
