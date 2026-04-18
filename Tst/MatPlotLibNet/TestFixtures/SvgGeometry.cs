// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace MatPlotLibNet.Tests.TestFixtures;

/// <summary>SVG geometry extraction for visual-regression tests. The SVG output is the
/// project's primary contract — extracting concrete coordinates from it lets us assert
/// "data is in the plot area", "ticks are spaced", "polylines have N points" without
/// brittle string matching. Used by every rendering test in the coverage uplift.
///
/// <para>The extractors are deliberately tolerant of whitespace + attribute ordering.
/// They use regex (not full XML parsing) because (a) MatPlotLibNet SVG is well-formed,
/// (b) speed matters across thousands of test iterations, (c) regex makes the
/// extraction declarative.</para></summary>
public static partial class SvgGeometry
{
    /// <summary>Extracts (X, Y) point pairs from every <c>&lt;polyline points="..."&gt;</c>
    /// in the SVG. NaN values (from break regions) are preserved so callers can assert
    /// that gaps exist.</summary>
    public static List<(double X, double Y)> ExtractPolylinePoints(string svg)
    {
        var pts = new List<(double X, double Y)>();
        foreach (Match m in PolylineRegex().Matches(svg))
        foreach (var pair in m.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split(',');
            if (parts.Length != 2) continue;
            double x = double.TryParse(parts[0], out var xv) ? xv : double.NaN;
            double y = double.TryParse(parts[1], out var yv) ? yv : double.NaN;
            pts.Add((x, y));
        }
        return pts;
    }

    /// <summary>Extracts Y-axis tick mark positions — short horizontal lines on the LEFT
    /// edge of the plot area (x &lt; 110 by convention, length &lt; 10px).</summary>
    public static List<double> ExtractYAxisTickPositions(string svg)
    {
        var positions = new List<double>();
        foreach (Match m in LineRegex().Matches(svg))
        {
            double x1 = double.Parse(m.Groups["x1"].Value);
            double y1 = double.Parse(m.Groups["y1"].Value);
            double x2 = double.Parse(m.Groups["x2"].Value);
            double y2 = double.Parse(m.Groups["y2"].Value);
            if (x1 < 110 && Math.Abs(y1 - y2) < 0.5 && Math.Abs(x2 - x1) < 10)
                positions.Add(y1);
        }
        return positions;
    }

    /// <summary>Counts <c>&lt;polygon&gt;</c> elements (used by Geo land/coastlines and
    /// area fills).</summary>
    public static int CountPolygons(string svg) => PolygonRegex().Matches(svg).Count;

    /// <summary>Counts <c>&lt;script&gt;</c> elements — used to verify
    /// <c>WithBrowserInteraction()</c> embedded JS.</summary>
    public static int CountScripts(string svg) => ScriptRegex().Matches(svg).Count;

    /// <summary>Asserts that every point in <paramref name="points"/> falls within the
    /// pixel <paramref name="canvas"/> rect (or is NaN, which represents a hidden gap).
    /// Throws Xunit assertion failure on violation with the offending coordinate.</summary>
    public static void AssertPointsInCanvas(IEnumerable<(double X, double Y)> points,
        double xMin, double xMax, double yMin, double yMax, string label = "")
    {
        foreach (var (x, y) in points)
        {
            if (double.IsNaN(x) || double.IsNaN(y)) continue;
            if (x < xMin || x > xMax || y < yMin || y > yMax)
                throw new Xunit.Sdk.XunitException(
                    $"{(string.IsNullOrEmpty(label) ? "" : label + ": ")}point ({x}, {y}) is outside canvas [{xMin}..{xMax}] × [{yMin}..{yMax}]");
        }
    }

    [GeneratedRegex(@"<polyline\s+points=""([^""]+)""")]
    private static partial Regex PolylineRegex();

    [GeneratedRegex(@"<line\s+x1=""(?<x1>[\d\.\-]+)""\s+y1=""(?<y1>[\d\.\-]+)""\s+x2=""(?<x2>[\d\.\-]+)""\s+y2=""(?<y2>[\d\.\-]+)""")]
    private static partial Regex LineRegex();

    [GeneratedRegex(@"<polygon\b")]
    private static partial Regex PolygonRegex();

    [GeneratedRegex(@"<script\b")]
    private static partial Regex ScriptRegex();
}
