// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Tests.Rendering;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>The PNG that Skia exports draws the colours the SVG of the same figure draws. A composed chart is several series
/// and overlays, and each part of it has a colour decided once (a theme slot, or one the verb fixes: the dumbbell's grey
/// connector, a violin's half-transparent body); two renderers that read the same figure must not disagree about it. The
/// SVG is read for its parts, a pixel is read from the PNG at a place inside each part, and the two colours must meet:
/// <list type="bullet">
/// <item>a solid part (a marker of 2.5 px or more, a cell, a line 2.5 px or more wide, an opaque area) is that exact colour;</item>
/// <item>a part with an opacity, or a thin line (its edge is anti-aliased), is that colour blended into the white page by
/// some coverage, wherever the page under it is plain white;</item>
/// <item>a part another part is drawn over at that place is left out: the later part decides the pixel there.</item>
/// </list></summary>
public class ComposedChartPixelTests
{
    private const int Tolerance = 3;

    private readonly record struct Rgb(int Red, int Green, int Blue)
    {
        public static Rgb FromHex(string hex) =>
            new(int.Parse(hex.AsSpan(1, 2), NumberStyles.HexNumber), int.Parse(hex.AsSpan(3, 2), NumberStyles.HexNumber), int.Parse(hex.AsSpan(5, 2), NumberStyles.HexNumber));

        public static Rgb Of(SKColor color) => new(color.Red, color.Green, color.Blue);

        /// <summary>This colour with <paramref name="coverage"/> of it on a white page: 1 is the colour itself.</summary>
        public Rgb OnWhite(double coverage) =>
            new((int)Math.Round(Red * coverage + 255 * (1 - coverage)), (int)Math.Round(Green * coverage + 255 * (1 - coverage)), (int)Math.Round(Blue * coverage + 255 * (1 - coverage)));

        public bool IsNear(Rgb other, int tolerance) =>
            Math.Abs(Red - other.Red) <= tolerance && Math.Abs(Green - other.Green) <= tolerance && Math.Abs(Blue - other.Blue) <= tolerance;

        public override string ToString() => $"#{Red:X2}{Green:X2}{Blue:X2}";
    }

    /// <summary>One element of the SVG, in the order it is drawn, with the geometry needed to say where it lies.</summary>
    private sealed class SvgPart(int index, string kind, IReadOnlyDictionary<string, string> attributes)
    {
        private static readonly Regex AttributeRx = new(@"(?<name>[a-z0-9\-]+)=""(?<value>[^""]*)""", RegexOptions.Compiled);

        public int Index { get; } = index;

        public string Kind { get; } = kind;

        public IReadOnlyDictionary<string, string> Attributes { get; } = attributes;

        public double[] Points { get; } = attributes.TryGetValue("points", out var points)
            ? [.. points.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries).Select(Parse)]
            : [];

        public static SvgPart Read(int index, Match tag) =>
            new(index, tag.Groups["kind"].Value, AttributeRx.Matches(tag.Value).ToDictionary(m => m.Groups["name"].Value, m => m.Groups["value"].Value, StringComparer.Ordinal));

        private static double Parse(string text) => double.Parse(text, CultureInfo.InvariantCulture);

        public double Number(string name) => Parse(Attributes[name]);

        public string? Text(string name) => Attributes.GetValueOrDefault(name);

        public double Opacity => Text("fill-opacity") is { } fill ? Parse(fill) : Text("opacity") is { } any ? Parse(any) : 1.0;

        public double StrokeWidth => Text("stroke-width") is { } width ? Parse(width) : 1.0;

        public bool HasFill => Text("fill") is { } fill && fill != "none";

        public bool HasStroke => Text("stroke") is { } stroke && stroke != "none";

        /// <summary>Whether this part paints the point (<paramref name="x"/>, <paramref name="y"/>), half a pixel's margin
        /// included: its fill when it has one, its stroke when it has one.</summary>
        public bool Covers(double x, double y)
        {
            const double margin = 0.6;
            switch (Kind)
            {
                case "circle":
                    return HasFill && Distance(x, y, Number("cx"), Number("cy")) <= Number("r") + margin;
                case "rect":
                    double left = Number("x"), top = Number("y"), right = left + Number("width"), bottom = top + Number("height");
                    bool inside = x >= left - margin && x <= right + margin && y >= top - margin && y <= bottom + margin;
                    double nearestEdge = Math.Min(Math.Min(Math.Abs(x - left), Math.Abs(x - right)), Math.Min(Math.Abs(y - top), Math.Abs(y - bottom)));
                    return inside && (HasFill || (HasStroke && nearestEdge <= StrokeWidth / 2 + margin));
                case "polygon":
                    return (HasFill && IsInsidePolygon(x, y)) || (HasStroke && DistanceToPath(x, y, closed: true) <= StrokeWidth / 2 + margin);
                case "line":
                    return HasStroke && SegmentDistance(x, y, Number("x1"), Number("y1"), Number("x2"), Number("y2")) <= StrokeWidth / 2 + margin;
                case "polyline":
                    return HasStroke && DistanceToPath(x, y, closed: false) <= StrokeWidth / 2 + margin;
                default:
                    return false;
            }
        }

        private bool IsInsidePolygon(double x, double y)
        {
            bool inside = false;
            int count = Points.Length / 2;
            for (int i = 0, j = count - 1; i < count; j = i++)
            {
                double xi = Points[2 * i], yi = Points[2 * i + 1], xj = Points[2 * j], yj = Points[2 * j + 1];
                if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi) inside = !inside;
            }
            return inside;
        }

        private double DistanceToPath(double x, double y, bool closed)
        {
            double nearest = double.MaxValue;
            int count = Points.Length / 2;
            for (int i = 0; i < (closed ? count : count - 1); i++)
            {
                int next = (i + 1) % count;
                nearest = Math.Min(nearest, SegmentDistance(x, y, Points[2 * i], Points[2 * i + 1], Points[2 * next], Points[2 * next + 1]));
            }
            return nearest;
        }

        private static double Distance(double x1, double y1, double x2, double y2) => Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));

        private static double SegmentDistance(double x, double y, double x1, double y1, double x2, double y2)
        {
            double dx = x2 - x1, dy = y2 - y1, lengthSquared = dx * dx + dy * dy;
            double t = lengthSquared == 0 ? 0 : Math.Clamp(((x - x1) * dx + (y - y1) * dy) / lengthSquared, 0, 1);
            return Distance(x, y, x1 + t * dx, y1 + t * dy);
        }
    }

    /// <summary>A place inside a part, the colour the part has there, and how it is to be compared.</summary>
    private readonly record struct Probe(SvgPart Part, double X, double Y, Rgb Colour, double Opacity, bool Blended);

    /// <summary>What a figure's probes found: how many met, how many lay under a later part, and the ones that did not.</summary>
    private readonly record struct Verdict(int Matched, int Covered, IReadOnlyList<string> Mismatches)
    {
        public int Total => Matched + Covered + Mismatches.Count;
    }

    private static readonly Regex Tag = new("""<(?<kind>circle|rect|line|polyline|polygon)\s[^>]*>""", RegexOptions.Compiled);

    private static IReadOnlyList<SvgPart> PartsOf(string svg) => [.. Tag.Matches(svg).Select((tag, index) => SvgPart.Read(index, tag))];

    private static bool IsThePage(SvgPart part) =>
        part.Kind == "rect" && part.Text("fill") == "#FFFFFF" && !part.Attributes.ContainsKey("fill-opacity");

    /// <summary>The places inside each part whose colour is known: the centre of a marker, a cell or a thick line; the middle
    /// of the longest stretch of a line; a point of an area away from its outline.</summary>
    private static IEnumerable<Probe> ProbesOf(SvgPart part)
    {
        switch (part.Kind)
        {
            case "circle" when part.HasFill && part.Text("fill")!.Length == 7 && part.Number("r") >= 2.5:
                yield return new Probe(part, part.Number("cx"), part.Number("cy"), Rgb.FromHex(part.Text("fill")!), part.Opacity, Blended: part.Opacity < 1);
                break;
            case "rect" when part.HasFill && part.Text("fill")!.Length == 7 && !IsThePage(part) && part.Number("width") > 3 && part.Number("height") > 3:
                yield return new Probe(part, part.Number("x") + part.Number("width") / 2, part.Number("y") + part.Number("height") / 2, Rgb.FromHex(part.Text("fill")!), part.Opacity, Blended: part.Opacity < 1);
                break;
            case "line" when part.HasStroke && part.Text("stroke")!.Length == 7 && part.Text("stroke") != "#262626":
                double x1 = part.Number("x1"), y1 = part.Number("y1"), x2 = part.Number("x2"), y2 = part.Number("y2");
                if (Math.Abs(x1 - x2) + Math.Abs(y1 - y2) > 6)
                {
                    yield return new Probe(part, (x1 + x2) / 2, (y1 + y2) / 2, Rgb.FromHex(part.Text("stroke")!), 1.0, Blended: part.StrokeWidth < 2.5);
                }
                break;
            case "polyline" when part.HasStroke && part.Text("stroke")!.Length == 7:
                int longest = -1;
                double longestLength = 8;
                for (int i = 0; i + 3 < part.Points.Length; i += 2)
                {
                    double length = Math.Abs(part.Points[i] - part.Points[i + 2]) + Math.Abs(part.Points[i + 1] - part.Points[i + 3]);
                    if (length > longestLength) { longestLength = length; longest = i; }
                }
                if (longest >= 0)
                {
                    yield return new Probe(part, (part.Points[longest] + part.Points[longest + 2]) / 2, (part.Points[longest + 1] + part.Points[longest + 3]) / 2,
                        Rgb.FromHex(part.Text("stroke")!), 1.0, Blended: part.StrokeWidth < 2.5);
                }
                break;
            case "polygon" when part.HasFill && part.Text("fill")!.Length == 7 && part.Points.Length >= 8:
                if (InteriorPointOf(part) is { } interior)
                {
                    yield return new Probe(part, interior.X, interior.Y, Rgb.FromHex(part.Text("fill")!), part.Opacity, Blended: part.Opacity < 1);
                }
                break;
        }
    }

    /// <summary>A point inside an area, at least 3 px from its left and right ends: on the vertical through its middle vertex,
    /// halfway between the first two places that vertical crosses the outline, where the area is at least 8 px tall.</summary>
    private static Point? InteriorPointOf(SvgPart area)
    {
        double[] xs = [.. area.Points.Where((_, i) => i % 2 == 0).Order()];
        double x = xs[xs.Length / 2] + 0.37;
        if (x - xs[0] <= 3 || xs[^1] - x <= 3) return null;
        var crossings = new List<double>();
        int count = area.Points.Length / 2;
        for (int i = 0; i < count; i++)
        {
            int next = (i + 1) % count;
            double xa = area.Points[2 * i], ya = area.Points[2 * i + 1], xb = area.Points[2 * next], yb = area.Points[2 * next + 1];
            if ((xa <= x) != (xb <= x)) crossings.Add(ya + (x - xa) / (xb - xa) * (yb - ya));
        }
        crossings.Sort();
        return crossings.Count >= 2 && crossings[1] - crossings[0] >= 8 ? new Point(x, (crossings[0] + crossings[1]) / 2) : null;
    }

    /// <summary>Whether the pixel at the probe has the colour the part has there: the colour itself for a solid part, the
    /// colour blended into white by some coverage (for the pixel itself or one beside it) for a blended one.</summary>
    private static bool Meets(SKBitmap picture, double scaleX, double scaleY, Probe probe)
    {
        int px = (int)Math.Floor(probe.X * scaleX), py = (int)Math.Floor(probe.Y * scaleY);
        if (!probe.Blended)
        {
            return Rgb.Of(picture.GetPixel(px, py)).IsNear(probe.Colour.OnWhite(probe.Opacity), Tolerance);
        }

        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                int x = px + dx, y = py + dy;
                if (x < 0 || y < 0 || x >= picture.Width || y >= picture.Height) continue;
                var pixel = Rgb.Of(picture.GetPixel(x, y));
                for (double coverage = 1.0; coverage >= 0.2; coverage -= 0.02)
                {
                    if (pixel.IsNear(probe.Colour.OnWhite(coverage), Tolerance + 1)) return true;
                }
            }
        }
        return false;
    }

    private static Verdict Compare(Figure figure)
    {
        string svg = figure.ToSvg();
        using var picture = SKBitmap.Decode(figure.ToPng());
        double scaleX = picture.Width / figure.Width, scaleY = picture.Height / figure.Height;
        var parts = PartsOf(svg);

        int matched = 0, covered = 0;
        var mismatches = new List<string>();
        foreach (var probe in parts.SelectMany(ProbesOf))
        {
            if (probe.X < 0 || probe.Y < 0 || probe.X * scaleX >= picture.Width || probe.Y * scaleY >= picture.Height) continue;

            bool paintedOver = parts.Skip(probe.Part.Index + 1).Any(later => later.Covers(probe.X, probe.Y));
            // Under a blended part the page must be plain white, else the colour to expect is not known.
            bool unknownPage = probe.Blended && parts.Take(probe.Part.Index).Any(earlier => !IsThePage(earlier) && earlier.Covers(probe.X, probe.Y));
            if (paintedOver || unknownPage) { covered++; continue; }

            if (Meets(picture, scaleX, scaleY, probe)) matched++;
            else
            {
                var found = Rgb.Of(picture.GetPixel((int)Math.Floor(probe.X * scaleX), (int)Math.Floor(probe.Y * scaleY)));
                mismatches.Add($"{probe.Part.Kind} at ({probe.X:F1}, {probe.Y:F1}): the SVG draws {probe.Colour}{(probe.Opacity < 1 ? $" at {probe.Opacity:F2}" : "")}, the PNG has {found}");
            }
        }
        return new Verdict(matched, covered, mismatches);
    }

    [Theory]
    [MemberData(nameof(ComposedChartShapes.Names), MemberType = typeof(ComposedChartShapes))]
    public void ThePngDrawsEveryPartInTheColourTheSvgGivesIt(string name)
    {
        var verdict = Compare(ComposedChartShapes.Named(name).Build(false).Build());

        Assert.True(verdict.Mismatches.Count == 0,
            $"{name}: {verdict.Mismatches.Count} of {verdict.Total} parts have another colour in the PNG than in the SVG: {string.Join("; ", verdict.Mismatches.Take(5))}");
        Assert.True(verdict.Matched > 0, $"{name}: no part could be compared ({verdict.Covered} lie under a later part), so the pin says nothing about it.");
    }

    /// <summary>Liveness: the probes find the parts the pin is about, so it cannot go green by comparing almost nothing.</summary>
    [Fact]
    public void TheShapes_OfferManyPartsToCompare()
    {
        int matched = ComposedChartShapes.All.Sum(shape => Compare(shape.Build(false).Build()).Matched);

        Assert.True(matched >= 500, $"only {matched} parts were compared across {ComposedChartShapes.All.Count} shapes; the calendar alone has over 400 cells.");
    }
}
