// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>One start tag of an SVG document as a test reads it: the element name and its attributes, in document order.</summary>
/// <param name="Name">The element name (<c>rect</c>, <c>ellipse</c>, ...).</param>
/// <param name="Attributes">The attribute values by name, as written.</param>
/// <param name="Text">What lies between the start tag and the next tag, as written (empty for a tag that closes itself).</param>
public readonly partial record struct SvgElement(string Name, IReadOnlyDictionary<string, string> Attributes, string Text)
{
    /// <summary>An element of a parsed tree read the way a start tag is: its name, its attributes by name and the text it starts with.</summary>
    /// <param name="element">An element of an <see cref="XDocument"/>.</param>
    public static SvgElement Of(XElement element) =>
        new(element.Name.LocalName, element.Attributes().ToDictionary(attribute => attribute.Name.LocalName, attribute => attribute.Value),
            element.Nodes().OfType<XText>().FirstOrDefault()?.Value ?? string.Empty);

    /// <summary>The value of <paramref name="attribute"/>, or <see langword="null"/> when the tag does not carry it.</summary>
    public string? this[string attribute] => Attributes.TryGetValue(attribute, out string? value) ? value : null;

    /// <summary>The value of <paramref name="attribute"/> as an invariant-culture number.</summary>
    public double Number(string attribute) => double.Parse(Attributes[attribute], CultureInfo.InvariantCulture);

    /// <summary>The angle of the <c>rotate(</c> transform the element carries, as written; <see langword="null"/> when it carries none.</summary>
    public double? Rotation() =>
        this["transform"] is { } transform && RotationPattern().Match(transform) is { Success: true } turn
            ? double.Parse(turn.Groups[1].Value, CultureInfo.InvariantCulture)
            : null;

    [GeneratedRegex(@"rotate\(\s*(-?[0-9.]+)")]
    private static partial Regex RotationPattern();

    /// <summary>The point the <c>rotate(angle, x, y)</c> transform turns about; <see langword="null"/> when the element carries no rotation
    /// that names one.</summary>
    public Point? RotationCentre() =>
        this["transform"] is { } transform && CentrePattern().Match(transform) is { Success: true } turn
            ? new Point(double.Parse(turn.Groups["x"].Value, CultureInfo.InvariantCulture), double.Parse(turn.Groups["y"].Value, CultureInfo.InvariantCulture))
            : null;

    [GeneratedRegex(@"rotate\(\s*[-0-9.]+\s*[, ]\s*(?<x>-?[0-9.]+)\s*[, ]\s*(?<y>-?[0-9.]+)\s*\)")]
    private static partial Regex CentrePattern();

    /// <summary>Every number of the path data <c>d</c>, curves included, in the order written.</summary>
    public double[] PathNumbers() => [.. NumberPattern().Matches(Attributes["d"]).Select(number => number.Value.Number())];

    [GeneratedRegex(@"-?[0-9]+(?:\.[0-9]+)?")]
    private static partial Regex NumberPattern();

    /// <summary>The vertices of the path data <c>d</c>: its move-to and line-to points, in the order written (a curve's points are left out).</summary>
    public Point[] PathPoints() => PathPointsIn(Attributes["d"]);

    /// <summary>The vertices of each subpath of the path data <c>d</c>, in the order written: a subpath ends at its <c>Z</c>.</summary>
    public Point[][] Subpaths() => [.. Attributes["d"].Split('Z').Where(subpath => subpath.Trim().Length > 0).Select(PathPointsIn)];

    private static Point[] PathPointsIn(string data) =>
        [.. PathPointPattern().Matches(data).Select(point => new Point(point.Groups["x"].Value.Number(), point.Groups["y"].Value.Number()))];

    [GeneratedRegex(@"[ML] (?<x>[-0-9.eE]+) (?<y>[-0-9.eE]+)")]
    private static partial Regex PathPointPattern();

    /// <summary>The vertices of the <c>data-v3d</c> attribute, each as its numbers, in the order written.</summary>
    public double[][] Vertices() =>
        [.. Attributes["data-v3d"].Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(vertex => vertex.Split(',').Select(number => number.Number()).ToArray())];

    /// <summary>The pairs of the <c>points</c> attribute, in the order written.</summary>
    public Point[] Points() =>
    [
        .. Attributes["points"].Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split(',')).Select(xy => new Point(xy[0].Number(), xy[1].Number())),
    ];
}

/// <summary>One entry of the legend as the picture writes it: its place in the legend and the name it carries.</summary>
/// <param name="Index">The value of <c>data-legend-index</c>.</param>
/// <param name="Label">The value of <c>aria-label</c>, as written.</param>
public readonly record struct DrawnLegendEntry(int Index, string Label);

/// <summary>A text as the picture draws it: what it reads and where it stands.</summary>
/// <param name="Text">What the text reads.</param>
/// <param name="X">Where it stands along x, in pixels.</param>
/// <param name="Y">Where its baseline stands, in pixels.</param>
public readonly record struct DrawnText(string Text, double X, double Y);

/// <summary>The one way the tests read the markup a figure draws: the start tags of an element, the XML tree, the series group of
/// an index, the plot area, the texts, the polylines, the legend frame, the ids it defines and the ids it refers to. A test class
/// keeps no reader of its own.</summary>
public static partial class SvgMarkupExtensions
{
    /// <summary>How far a number read back from the drawing may lie from the one the layout worked out. The SVG writes a
    /// coordinate, a length, a font size or an angle to two decimals, so one number is within half a hundredth of a pixel
    /// of the layout's; a test that compares two numbers read back (a centre from an edge and a width, a gap between two
    /// marks) or a read-back number with the layout's own allows a hundredth. A hair over, so a difference of exactly a
    /// hundredth in binary arithmetic passes.</summary>
    public const double WrittenPrecision = 0.01 + 1e-9;

    [GeneratedRegex(@"([\w:-]+)=""([^""]*)""")]
    private static partial Regex AttributePattern();

    [GeneratedRegex(@"<([A-Za-z][\w:-]*)(?=[\s/>])([^>]*)>")]
    private static partial Regex TagPattern();

    [GeneratedRegex(@"<clipPath[^>]*>.*?</clipPath>", RegexOptions.Singleline)]
    private static partial Regex ClipPathPattern();

    [GeneratedRegex(@"<script\b[^>]*>(?<body>.*?)</script>", RegexOptions.Singleline)]
    private static partial Regex ScriptPattern();

    [GeneratedRegex(@"<\?xml[^>]*\?>")]
    private static partial Regex DeclarationPattern();

    [GeneratedRegex(@"xmlns\s*=\s*""[^""]*""")]
    private static partial Regex NamespacePattern();

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColourPattern();

    [GeneratedRegex(@"\bid=""([^""]*)""")]
    private static partial Regex IdPattern();

    [GeneratedRegex("""url\(#([^)]*)\)""")]
    private static partial Regex UrlPattern();

    [GeneratedRegex(@"<polyline\s+points=""([^""]+)""")]
    private static partial Regex PolylinePattern();

    [GeneratedRegex(@"<g class=""legend""[^>]*>\s*<(rect|path) ([^>]*)/>")]
    private static partial Regex LegendPattern();

    [GeneratedRegex(@"<(/?)g(?=[\s/>])[^>]*?(/?)>")]
    private static partial Regex GroupTagPattern();

    extension(string svg)
    {
        /// <summary>Every start tag of the element called <paramref name="name"/>, in document order.</summary>
        public IReadOnlyList<SvgElement> Elements(string name) =>
        [
            .. Regex.Matches(svg, $@"<{Regex.Escape(name)}(?=[\s/>])([^>]*)>").Select(tag => new SvgElement(name,
                AttributesOf(tag.Groups[1].Value),
                tag.Groups[1].Value.EndsWith('/') ? string.Empty : TextAfter(svg, tag.Index + tag.Length))),
        ];

        /// <summary>Every start tag of the document, whatever the element, in document order.</summary>
        public IReadOnlyList<SvgElement> AllElements() =>
        [
            .. TagPattern().Matches(svg).Select(tag => new SvgElement(tag.Groups[1].Value,
                AttributesOf(tag.Groups[2].Value),
                tag.Groups[2].Value.EndsWith('/') ? string.Empty : TextAfter(svg, tag.Index + tag.Length))),
        ];

        /// <summary>The markup without its XML declarations and its default namespace declarations, so the markup of several pictures can be
        /// set one after the other under one root of a document of the test's own.</summary>
        public string WithoutDeclarations() => NamespacePattern().Replace(DeclarationPattern().Replace(svg, string.Empty).Trim(), string.Empty);

        /// <summary>The bodies of the <c>script</c> elements, as written, in document order (the markup need not be a whole XML document).</summary>
        public IReadOnlyList<string> Scripts() => [.. ScriptPattern().Matches(svg).Select(script => script.Groups["body"].Value)];

        /// <summary>The markup with its <c>script</c> elements left out: what is drawn, without the code that moves it.</summary>
        public string WithoutScripts() => ScriptPattern().Replace(svg, string.Empty);

        /// <summary>The markup with its <c>clipPath</c> elements left out: what is drawn, without the outlines the marks are cut by.</summary>
        public string WithoutClipPaths() => ClipPathPattern().Replace(svg, string.Empty);

        /// <summary>The rectangles filled with <paramref name="fill"/>, as written, in document order.</summary>
        public IReadOnlyList<Rect> RectsFilled(string fill) =>
            [.. svg.Elements("rect").Where(rect => rect["fill"] == fill).Select(rect => new Rect(rect.Number("x"), rect.Number("y"), rect.Number("width"), rect.Number("height")))];

        /// <summary>The x of every upright line (one whose two ends are written with the same x) drawn in <paramref name="stroke"/> that
        /// runs more than <paramref name="longerThan"/> pixels along y: the grid lines, not the tick marks, in document order.</summary>
        public double[] VerticalLineXs(string stroke, double longerThan) =>
        [
            .. svg.Elements("line")
                .Where(line => line["x1"] is not null && line["x1"] == line["x2"] && line["stroke"] == stroke && Math.Abs(line.Number("y2") - line.Number("y1")) > longerThan)
                .Select(line => line.Number("x1")),
        ];

        /// <summary>Every value the document writes for the attribute <paramref name="name"/>, whatever element carries it, in document order.</summary>
        public IReadOnlyList<string> AttributeValues(string name) =>
            [.. AttributePattern().Matches(svg).Where(attribute => attribute.Groups[1].Value == name).Select(attribute => attribute.Groups[2].Value)];

        /// <summary>The entries of the legend, in document order: every group that carries a <c>data-legend-index</c>.</summary>
        public IReadOnlyList<DrawnLegendEntry> LegendEntries() =>
        [
            .. svg.Elements("g").Where(group => group["data-legend-index"] is not null)
                .Select(group => new DrawnLegendEntry(int.Parse(group["data-legend-index"]!, CultureInfo.InvariantCulture), group["aria-label"] ?? string.Empty)),
        ];

        /// <summary>The first fill or stroke the markup writes as a hex colour, as written; empty when there is none.</summary>
        public string FirstColour() =>
            AttributePattern().Matches(svg)
                .Where(attribute => attribute.Groups[1].Value is "fill" or "stroke" && HexColourPattern().IsMatch(attribute.Groups[2].Value))
                .Select(attribute => attribute.Groups[2].Value).FirstOrDefault() ?? string.Empty;

        /// <summary>Every <c>id</c> the document defines, in document order.</summary>
        public IReadOnlyList<string> Ids() => [.. IdPattern().Matches(svg).Select(id => id.Groups[1].Value)];

        /// <summary>Every id a <c>url(#id)</c> reference points at, in document order.</summary>
        public IReadOnlyList<string> UrlReferences() => [.. UrlPattern().Matches(svg).Select(reference => reference.Groups[1].Value)];

        /// <summary>The text as an XML document, a picture or a project file: the one place a test parses markup.</summary>
        public XDocument Xml() => XDocument.Parse(svg);

        /// <summary>Every element called <paramref name="local"/> anywhere in the picture, in document order.</summary>
        public IEnumerable<XElement> Each(string local) => svg.Xml().Each(local);

        /// <summary>The elements called <paramref name="local"/> that are drawn straight into the picture, not inside a group.</summary>
        public IEnumerable<XElement> Direct(string local) => svg.Xml().Root!.Children(local);

        /// <summary>How often <paramref name="needle"/> occurs in the markup, each occurrence counted once (they do not overlap).</summary>
        public int CountOf(string needle) => svg.AsSpan().Count(needle.AsSpan());

        /// <summary>The number the text spells, read with the invariant culture.</summary>
        public double Number() => double.Parse(svg, CultureInfo.InvariantCulture);

        /// <summary>The groups of class <c>series</c>, in document order.</summary>
        public IReadOnlyList<XElement> SeriesGroups() => [.. svg.Each("g").Where(g => (string?)g.Attribute("class") == "series")];

        /// <summary>The markup of the series group with index <paramref name="index"/>, from its opening tag to its own closing tag:
        /// the groups inside it (the clip the series is drawn in) are counted, so what the group holds after them is read too.</summary>
        public string SeriesGroup(int index) => GroupFrom(svg, svg.IndexOf($"<g class=\"series\" data-series-index=\"{index}\"", StringComparison.Ordinal), $"series group {index}");

        /// <summary>The markup of the first group of class <paramref name="className"/>, from its opening tag to its own closing tag (the
        /// groups inside it are counted); the test fails when the picture holds none.</summary>
        public string GroupOf(string className) => GroupFrom(svg, svg.IndexOf($"<g class=\"{className}\"", StringComparison.Ordinal), $"the group of class '{className}'");

        /// <summary>The plot area: the second rectangle drawn into the figure, after the figure's own background.</summary>
        public Rect PlotArea()
        {
            var rect = svg.Direct("rect").Skip(1).First();
            return new(rect.Number("x"), rect.Number("y"), rect.Number("width"), rect.Number("height"));
        }

        /// <summary>The text element that reads <paramref name="text"/>, as written: the first one when several read it; the test fails
        /// when none does.</summary>
        public SvgElement TextReading(string text)
        {
            var found = svg.Elements("text").Where(element => element.Text == text).ToList();
            Assert.True(found.Count > 0, $"no <text> reading '{text}' in the picture");
            return found[0];
        }

        /// <summary>The fill of the one text that reads <paramref name="text"/>.</summary>
        public string FillOf(string text)
        {
            string? fill = svg.TextReading(text)["fill"];
            Assert.True(fill is not null, $"the text '{text}' carries no fill, so a browser draws it black");
            return fill!;
        }

        /// <summary>The baselines of the texts (those of font size <paramref name="fontSize"/> when it is given), lowest first.</summary>
        public double[] TextYs(string? fontSize = null) =>
        [
            .. svg.Each("text")
                .Where(e => e.Attribute("y") is not null
                    && (fontSize is null || (string?)e.Attribute("font-size") == fontSize))
                .Select(e => e.Number("y")).Order(),
        ];

        /// <summary>The texts drawn with <paramref name="anchor"/>, in document order (left Y labels end at the axis, X labels are
        /// centred under it, right-hand Y labels start at it).</summary>
        public IReadOnlyList<DrawnText> TextsAnchored(string anchor) =>
        [
            .. svg.Each("text").Where(e => (string?)e.Attribute("text-anchor") == anchor)
                .Select(e => new DrawnText(e.Value, e.Number("x"), e.Number("y"))),
        ];

        /// <summary>The points of the polylines of the series group labelled <paramref name="label"/>, run after run, as drawn.</summary>
        /// <param name="label">The series' label, read from the group's <c>aria-label</c>.</param>
        public Point[] PointsOf(string label) =>
        [
            .. svg.Each("g").Single(e => (string?)e.Attribute("aria-label") == label && e.Attribute("data-series-index") is not null)
                .Each("polyline").SelectMany(e => e.Attribute("points")!.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Select(pair => pair.Split(',')).Select(p => new Point(p[0].Number(), p[1].Number())),
        ];

        /// <summary>The points of every <c>&lt;polyline points="..."&gt;</c>. A coordinate that is not a number is NaN, so a test can
        /// assert that a gap exists.</summary>
        public List<Point> PolylinePoints()
        {
            var points = new List<Point>();
            foreach (Match line in PolylinePattern().Matches(svg))
            {
                foreach (string pair in line.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] parts = pair.Split(',');
                    if (parts.Length != 2)
                    {
                        continue;
                    }

                    double x = double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double xv) ? xv : double.NaN;
                    double y = double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double yv) ? yv : double.NaN;
                    points.Add(new Point(x, y));
                }
            }

            return points;
        }

        /// <summary>The frame of the legend box the picture draws: the rectangle (or, with rounded corners, the path of the
        /// rounded rectangle, whose bounds are read) inside its <c>legend</c> group, in pixels; <see langword="null"/> when the
        /// picture holds no legend.</summary>
        public Rect? LegendFrame() => svg.LegendFrames().Cast<Rect?>().FirstOrDefault();

        /// <summary>The frame of every legend box the picture draws, in the order they are written.</summary>
        public List<Rect> LegendFrames() => [.. LegendPattern().Matches(svg).Select(FrameOf)];
    }

    extension(XContainer node)
    {
        /// <summary>Every element called <paramref name="local"/> below <paramref name="node"/>, in document order.</summary>
        public IEnumerable<XElement> Each(string local) => node.Descendants().Where(e => e.Name.LocalName == local);

        /// <summary>The elements called <paramref name="local"/> that are direct children of <paramref name="node"/>.</summary>
        public IEnumerable<XElement> Children(string local) => node.Elements().Where(e => e.Name.LocalName == local);
    }

    extension(XElement element)
    {
        /// <summary>Whether the element is called <paramref name="local"/>.</summary>
        public bool Is(string local) => element.Name.LocalName == local;

        /// <summary>The attribute <paramref name="attribute"/> as an invariant-culture number.</summary>
        public double Number(string attribute) => double.Parse(element.Attribute(attribute)!.Value, CultureInfo.InvariantCulture);
    }

    /// <summary>Asserts that every point in <paramref name="points"/> falls within the pixel rectangle given (or is NaN, which
    /// represents a hidden gap), naming the offending coordinate.</summary>
    /// <param name="points">The points read from the picture.</param>
    /// <param name="xMin">The left edge.</param>
    /// <param name="xMax">The right edge.</param>
    /// <param name="yMin">The top edge.</param>
    /// <param name="yMax">The bottom edge.</param>
    /// <param name="label">What the points belong to, in the failure message.</param>
    public static void AssertPointsInCanvas(IEnumerable<Point> points, double xMin, double xMax, double yMin, double yMax, string label = "")
    {
        foreach (var (x, y) in points)
        {
            if (double.IsNaN(x) || double.IsNaN(y))
            {
                continue;
            }

            if (x < xMin || x > xMax || y < yMin || y > yMax)
            {
                throw new Xunit.Sdk.XunitException(
                    $"{(string.IsNullOrEmpty(label) ? "" : label + ": ")}point ({x}, {y}) is outside canvas [{xMin}..{xMax}] × [{yMin}..{yMax}]");
            }
        }
    }

    private static string GroupFrom(string svg, int start, string what)
    {
        Assert.True(start >= 0, $"{what} is not in the picture");
        int depth = 0;
        foreach (Match tag in GroupTagPattern().Matches(svg, start))
        {
            bool closing = tag.Groups[1].Length > 0, selfClosing = tag.Groups[2].Length > 0;
            depth += closing ? -1 : selfClosing ? 0 : 1;
            if (depth == 0)
            {
                return svg[start..(tag.Index + tag.Length)];
            }
        }

        Assert.Fail($"{what} does not close");
        return string.Empty;
    }

    private static string TextAfter(string svg, int start) => svg[start..(svg.IndexOf('<', start) is var next and >= 0 ? next : svg.Length)];

    private static Dictionary<string, string> AttributesOf(string tag) =>
        AttributePattern().Matches(tag).ToDictionary(attribute => attribute.Groups[1].Value, attribute => attribute.Groups[2].Value);

    private static Rect FrameOf(Match group)
    {
        var frame = new SvgElement(group.Groups[1].Value, AttributesOf(group.Groups[2].Value), string.Empty);
        if (frame.Name == "path")
        {
            var numbers = frame.PathNumbers();
            double[] xs = [.. numbers.Where((_, i) => i % 2 == 0)], ys = [.. numbers.Where((_, i) => i % 2 == 1)];
            return new Rect(xs.Min(), ys.Min(), xs.Max() - xs.Min(), ys.Max() - ys.Min());
        }

        return new Rect(frame.Number("x"), frame.Number("y"), frame.Number("width"), frame.Number("height"));
    }
}
