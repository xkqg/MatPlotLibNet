// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging.OwnedRules;

/// <summary>Rules of how the tests read what a figure draws, read over every file under <c>Tst</c>: the markup is read by one
/// reader (<c>SvgMarkupExtensions</c>), and a test class keeps no reader of its own.</summary>
internal static class TestReadRules
{
    private const string Reader = "Tst/MatPlotLibNet/Rendering/SvgMarkupExtensions.cs";

    // The pieces of the rule over a fact that is only a text drawn and tokens looked for in it. A drawing is a call of a drawing verb
    // (Svg, ToSvg, Render, Json, GetOutput, ...) or a chain that ends in the call that writes the text; a fact that draws once and
    // looks for tokens in it is a row of a theory over DrawnToken, whatever it arranges before the drawing.
    private const string DrawVerb = @"(?:\w*Svg\w*|Render\w*|\w*Json|ServerChart|Written|GlyphOutline|Lines|Generate|BuildDataTable|WrapForIframe|Tabulate|GetChartDataTable)";
    private const string Drawing = @"(?:(?:\w+\.)*" + DrawVerb + @"\([^;]*?|[^;]*?\.(?:ToSvg|GetOutput|ToJson|ToString|Render)\([^;()]*)\)";
    private const string DrawnText = @"(?:(?:\w+\.)*" + DrawVerb + @"\([^;]*|(?:[^;{}]|\{(?:[^{}]|\{[^{}]*\})*\})*\.(?:ToSvg|GetOutput|ToJson|ToString|Render)\([^;()]*\)\s*;)";
    private const string Args = @"(?:[^;()]|\((?:[^;()]|\([^;()]*\))*\))*";
    private const string Atom = @"(?:\w+|\(" + Args + @"\))(?:\(" + Args + @"\)|\[[^\];]*\])*";
    private const string Token = @"(?:""""""[\s\S]*?""""""(?:\.Trim\(\))?|'(?:[^'\\]|\\.)'|\$""(?:[^""\\{]|\\.|\{(?:[^{}""]|""(?:[^""\\]|\\.)*"")*\})*""|""(?:[^""\\]|\\.)*""|" + Atom + @"(?:\." + Atom + @")*)";
    private const string Compared = @"(?:,\s*StringComparison\.\w+)?\);[ \t]*(?://[^\n]*)?";
    private const string Look = @"Assert\.(?:Contains|DoesNotContain)\(";
    private const string Untouched = @"(?:(?!Assert\.|\[Fact\]|\[Theory\]|\bpublic\b)[\s\S])*?";

    // A text read out of a file of the repository (a workflow, a README, a razor page, the architecture document) is not a drawing.
    private const string RepositoryText = @"(?:File\.ReadAllText\((?:Path\.Combine|\w+Path\()|Read\(|SourceFile\.SectionOf\(|Normalised\()";

    /// <summary>A fact with no assertion but token lookups, whose first lookup that is not a padding one (<c>"&lt;svg"</c>) reads a text
    /// declared as a drawing (named by a local) or a drawing written in place. The fact is written with a block or with <c>=&gt;</c>: a
    /// fact of one expression is one lookup of tokens in a drawing written in place, and the same rule reads it.</summary>
    private const string OnlyTokensLookedFor =
        @"\[Fact\]\s*public void \w+\(\)\s*(?:\{(?:" + Untouched + @"(?:string|var(?=\s+\w+\s*=\s*" + DrawnText + @"))(?!\s+\w+\s*=\s*" + RepositoryText + @") (?<v>\w+)\s*=[^;]*;" + Untouched
        + @"(?:" + Look + @"""<svg""\s*,\s*(?:\w+|" + Drawing + @")" + Compared + @"\s*)*"
        + Look + @"(?!""<svg""\s*,)" + Token + @",\s*\k<v>" + Compared
        + @"|" + Untouched + @"(?:" + Look + @"""<svg""\s*,\s*(?:\w+|" + Drawing + @")" + Compared + @"\s*)*"
        + Look + @"(?!""<svg""\s*,)" + Token + @",\s*" + Drawing + Compared + @")"
        + @"\s*(?:" + Look + Token + @",\s*(?:\w+|" + Drawing + @")" + Compared + @"\s*)*(?<=\n    )\}"
        + @"|=>\s*" + Look + @"(?!""<svg""\s*,)" + Token + @",\s*" + Drawing + Compared + @")";

    // A picture's markup in the pattern of a regular expression: the call (or the declaration of the reader) up to the end of its statement
    // holds a start tag of an svg element, a tag opened by an interpolated name, a closing tag, a literal that starts at the end of a tag,
    // or an attribute an svg carries. The declaration of the pattern is read where it is written, so a pattern kept in a variable of its
    // own is read by the first spelling below.
    private const string PictureElements = @"(?:svg|g|rect|line|polyline|polygon|circle|ellipse|path|text|tspan|title|clipPath|linearGradient|radialGradient|stop|defs|use|image|desc|script|\\\?xml)";
    private const string PictureAttributes = @"(?:x|y|x1|y1|x2|y2|cx|cy|r|d|fill|fill-opacity|stroke|stroke-width|stroke-linejoin|font-size|font-family|text-anchor|class|transform|points|width|height|textLength|lengthAdjust|clip-path|xmlns|aria-[\w-]+|data-[\w-]+)";
    private const string PictureRegex = @"(?:\bRegex\s*\.\s*(?:Matches|Match|IsMatch|Count|Replace|Split|EnumerateMatches)\(|\bnew\s+Regex\(|\bRegex(?:\[\])?\s+\w+\s*=|\bRegex\s+\w+\([^)]*\)\s*=>|\[GeneratedRegex\(|\bAssert\.(?:Matches|DoesNotMatch)\()[^;]*?(?:(?<!\?)<\(?(?:\?:)?" + PictureElements + @"\b|(?<!\?)<\{|""<""|</\w|"">|\[ML\] |(?:rotate|translate)\\\(|url\\\(#|(?<![\w-])" + PictureAttributes + @"\)?=)"
        + @"|(?:\bRegex\s*\.\s*\w+\(|\b[A-Z]\w*(?:\(\))?\.(?:Match|Matches|IsMatch|EnumerateMatches)\()\s*(?:[\w.]*[sS]vg\w*|markup|[\w.()]*\.ToSvg\(\)|[\w.()]*\.GetOutput\(\))\s*[,)]";

    /// <summary>The rules of the area, in the order the scan reads them.</summary>
    internal static OwnedRule[] All { get; } =
    [
        new("a picture is parsed as XML by Xml() only: no test calls XDocument.Parse",
            @"\bXDocument\.Parse\(",
            Reader) { Under = "Tst/", Finds = [
            "        var doc = XDocument." + "Parse(figure.ToSvg());",
            "        return System.Xml.Linq.XDocument." + "Parse(svg).Root!;"], Ignores = [
            "        var doc = figure.ToSvg().Xml();",
            "        var csproj = XDocument.Load(path);"] },
        new("the markup of a series group is read by SeriesGroup(index), which ends at the group's own closing tag: no test finds a group by the index it carries",
            @"IndexOf\([^)]*data-series-index",
            Reader) { Under = "Tst/", Finds = [
            "        int start = svg.IndexOf(\"<g class=\\\"series\\\" data-series-" + "index=\\\"0\\\"\", StringComparison.Ordinal);",
            "        int connector = svg.IndexOf(\"\"\"data-series-" + "index=\"2\" \"\"\".TrimEnd(), StringComparison.Ordinal);"], Ignores = [
            "        string group = svg.SeriesGroup(2);",
            "        Assert.Equal(\"0\", (string?)group.Attribute(\"data-series-index\"));"] },
        new("an element of a picture is found by Each / Children / Direct / Is: no test spells the local-name predicate (the readers of a package's nuspec, which is not a picture, are beside the owner)",
            @"\.Name\.LocalName == ",
            Reader, "Tst/MatPlotLibNet.Geo/GeoDependencyContractTests.cs", "Tst/MatPlotLibNet.Mcp/ThirdPartyNoticesContractTests.cs") { Under = "Tst/", Finds = [
            "        var texts = svg.Descendants().Where(e => e.Name." + "LocalName == \"text\");",
            "        var group = doc.Descendants().Single(e => e.Name." + "LocalName == \"g\" && e.Attribute(\"clip-path\") is not null);"], Ignores = [
            "        var texts = svg.Each(\"text\");",
            "        Assert.Equal(\"svg\", document.Root!.Name.LocalName);"] },
        OwnedRule.Nowhere("a reader of the markup is an extension of SvgMarkupExtensions: no test class keeps a private Group, SeriesGroups, Named, PlotArea, FillOf or TextYs over a picture",
            @"\bstatic\s+[\w<>\[\](),.? ]+\s+(?:Group|SeriesGroups|Named|PlotArea|FillOf|TextYs|TextYsIn)\((?:string svg|XContainer|XDocument)\b",
            "Tst/") with { Finds = [
            "    private static string Gro" + "up(string svg, int seriesIndex)",
            "    private static IEnumerable<XElement> Nam" + "ed(XContainer root, string name) =>",
            "    private static Rect PlotAr" + "ea(string svg)",
            "    internal static (double X, double Y, double W, double H) PlotAr" + "ea(string svg)",
            "    private static string Fill" + "Of(string svg, string text)"], Ignores = [
            "    private static XElement SeriesGroup(XDocument doc, string ariaLabel) =>",
            "        var plot = svg.PlotArea();",
            "    private static Rect PlotArea(Figure figure) => figure.FirstPlotArea();"] },
        OwnedRule.Nowhere("a tag of a picture is found by Elements or Each: no test tests a line of the picture for the tag it opens or compares a whole line with a tag",
            @"(?<!\bAssert)\.StartsWith\(\s*""<(?!svg\b)\w+|[=!]=\s*""</?\w+",
            "Tst/") with { Finds = [
            "            .Where(l => l" + ".StartsWith(\"<polygon\", StringComparison.Ordinal))];",
            "            if (line " + "== \"<g class=\\\"spine\\\">\") { inSpine = true; continue; }"], Ignores = [
            "        Assert.StartsWith(\"<polygon\", fill, StringComparison.Ordinal);",
            "        Assert.StartsWith(\"<svg\", svg.TrimStart());",
            "        bool opens = svg.StartsWith(\"<svg\", StringComparison.Ordinal);"] },
        new("a picture's markup is read through SvgMarkupExtensions: no test hands it to a regular expression (a start tag of an svg element, an attribute of it, a closing tag or the text between two tags in the pattern of Regex.Match / Matches / IsMatch / Count / Replace / Split, of a new Regex, of a GeneratedRegex or of Assert.Matches); the one page that is no picture (the home page of the documents, whose script block is JSON-LD) is beside the owner",
            PictureRegex,
            Reader, "Tst/MatPlotLibNet/Packaging/ReleaseDocumentContractTests.cs") { Under = "Tst/", AcrossLines = true, Finds = [
            "        var m = Reg" + "ex.Match(svg, \"<text[^>]*y=\\\"([-0-9.]+)\\\"[^>]*>X axis</text>\");",
            "        var n = Reg" + "ex.Matches(svg, \"<polyline\").Count;",
            "        var n = Reg" + "ex.Matches(figure.ToSvg(), $@\"<text[^>]*\\sy=\"\"(?<y>[-0-9.]+)\"\"[^>]*>{name}</text>\").Count;",
            "        var n = Reg" + "ex.Matches(svg, \"data-legend-index=\").Count;",
            "        var n = Reg" + "ex.Matches(svg, \">Ldr#\").Count;",
            "        var n = Reg" + "ex.Matches(svg, \"stroke=\\\"#1F77B4\\\"[^>]*fill=\\\"none\\\"\").Count;",
            "        var n = Reg" + "ex.Matches(svg, \"<(circle|polygon|polyline|path) \").Count;",
            "        var m = Reg" + "ex.Match(markup, \"\"\"(?:fill|stroke)=\"(#[0-9A-F]{6})\" \"\"\".TrimEnd());",
            "        var n = Reg" + "ex.Matches(svg, $\"<{tag} [^>]*stroke=\\\"#[0-9A-Fa-f]{{6}}\\\"[^>]*>\");",
            "        Assert.Mat" + "ches(new Reg" + "ex(@\"<g clip-path=\"\"url\\(#[^)]+\\)\"\">\"), svg);",
            "        var scripts = new Reg" + "ex(@\"<script[^>]*>(?<body>.*?)</script>\");",
            "    private static readonly Reg" + "ex Text = new(\"<text[^>]*x=\\\"([-0-9.]+)\\\"[^>]*>Ldr#02a4<\", RegexOptions.Compiled);",
            "    private static readonly Reg" + "ex PathPoint = new(@\"[ML] (?<x>[-0-9.eE]+) (?<y>[-0-9.eE]+)\", RegexOptions.Compiled);",
            "        var match = Align().Mat" + "ch(svg);",
            "    private static Reg" + "ex Arrow(Color color) => new($\"<polygon [^>]*fill=\\\"{color.ToHex()}\\\"\");",
            "        Assert.Mat" + "ches(\"<\" + element + @\"[^>]*\\bdata-v3d=\", svg);",
            "        var turn = Reg" + "ex.Match(t.Value, @\"rotate\\(\\s*[-0-9.]+\\s*[, ]\\s*([-0-9.]+)\\)\");",
            "        foreach (Match reference in Reg" + "ex.Matches(svg, @\"url\\(#([^)]+)\\)\"))",
            "    private static readonly Reg" + "ex[] Faces = [new(\"fill=\\\"#1F77B4\\\"\"), new(\"<circle\")];"], Ignores = [
            "        var rows = Regex.Matches(page, @\"^\\|(?<cells>.+)\\|\\s*$\", RegexOptions.Multiline);",
            "        var names = Regex.Matches(File.ReadAllText(path), @\"<PackageReference Include=\"\"([^\"\"]+)\"\"\");",
            "        var releases = Regex.Matches(changelog, @\"^" + "## \\[(\\d+\\.\\d+\\.\\d+)\\]\", RegexOptions.Multiline);",
            "    private static readonly Regex Name = new(@\"[A-Za-z_]\\w*\", RegexOptions.CultureInvariant);",
            "        var n = svg.Elements(\"polyline\").Count;",
            "        var m = Regex.Match(version, @\"^\\d+\");"] },
        OwnedRule.Nowhere("the plot area a test reads is the Rect of PlotArea(): no test class declares its own area or drawn-text record",
            @"record struct (?:Area\(double X, double Y, double W, double H\)|DrawnLabel\(string Text, double X, double Y\))",
            "Tst/") with { Finds = [
            "    private readonly record struct Ar" + "ea(double X, double Y, double W, double H);",
            "    private readonly record struct DrawnLa" + "bel(string Text, double X, double Y);"], Ignores = [
            "    private readonly record struct Area(string Name);"] },
        OwnedRule.Nowhere("the occurrences of a text in a picture are counted by CountOf: no test class keeps a counting loop of its own",
            @"\bstatic\s+int\s+(?:CountOccurrences|Occurrences|CountOf|Count)\(string \w+, string \w+\)",
            "Tst/") with { Finds = [
            "    private static int CountOcc" + "urrences(string haystack, string needle)",
            "    private static int Cou" + "nt(string text, string part) =>"], Ignores = [
            "        int drawn = svg.CountOf(\"<polygon\");",
            "    private static int CountDistinctFills(string svg, string elemPrefix)"] },
        new("a number read back from markup is read by Number(): no test class keeps a private invariant-culture parse of a text or an attribute, a reader of one attribute under a name of its own (a method or a local function), or an attribute parsed inline",
            @"\bstatic\s+double\s+\w+\((?:string \w+\)\s*=>\s*double\.Parse\(\w+, (?:System\.Globalization\.)?CultureInfo\.InvariantCulture\)|XElement \w+, string \w+\)\s*=>\s*double\.Parse)|\bstatic\s+[\w<>\[\]]+\s+Attr\(\s*string\s+\w+\s*,\s*string\s+\w+\s*\)|\bdouble\.Parse\(\s*(?:\(\s*string\??\s*\)\s*)?[^;]*?\.Attribute\(\s*[^()]*\)!?(?:\.Value)?\s*,\s*(?:System\.Globalization\.)?CultureInfo\.InvariantCulture\s*\)",
            Reader) { Under = "Tst/", Finds = [
            "    private static double N" + "um(string text) => double.Parse(text, CultureInfo.InvariantCulture);",
            "    private static double Num" + "ber(string text) => double.Parse(text, System.Globalization.CultureInfo.InvariantCulture);",
            "    private static double Num" + "ber(XElement e, string attribute) => double." + "Parse((string)e.Attribute(attribute)!, CultureInfo.InvariantCulture);",
            "    private static double At" + "tr(string element, string name) =>",
            "        double Attr(string name) => double." + "Parse(view.Attribute(name)!.Value, CultureInfo.InvariantCulture);",
            "            .Select(e => double." + "Parse(e.Attribute(\"cx\")!.Value, CultureInfo.InvariantCulture))];",
            "        double x = double." + "Parse((string)label.Attribute(\"x\")!, System.Globalization.CultureInfo.InvariantCulture);"], Ignores = [
            "        double size = double.Parse(((string?)text.Attribute(\"font-size\") ?? \"12\").Replace(\"px\", \"\"), CultureInfo.InvariantCulture);",
            "        double width = double.Parse(h.GetAttribute(\".scene\", \"data-w\")!, CultureInfo.InvariantCulture);",
            "        double x = rect.Number(\"x\");",
            "    private static double Num(Match m, string group) => double.Parse(m.Groups[group].Value, CultureInfo.InvariantCulture);"] },

        OwnedRule.Nowhere("two numbers are said to be close by AssertCloseTo (a sequence to an absolute tolerance, a number to a relative one with a floor, a point or a rectangle to an absolute one): no test class keeps an AssertClose or Near of its own over numbers, points or rectangles",
            @"\bstatic\s+(?:void|bool)\s+(?:AssertClose|AssertNear|AssertCloseTo|Close|Near)\((?:double|IReadOnlyList<double>|double\[\]|Point|Rect) ",
            "Tst/") with { Finds = [
            "    private static void AssertCl" + "ose(IReadOnlyList<double> expected, IReadOnlyList<double> actual)",
            "    private static void AssertCl" + "ose(double expected, double actual, string what, double relative = 1e-3)",
            "    private static void Ne" + "ar(double expected, double actual) =>",
            "    private static bool Ne" + "ar(Rect a, Rect b) =>",
            "    private static void AssertNe" + "ar(Point expected, Point actual)",
            "    private static void Clo" + "se(double expected, double actual, double relative)"], Ignores = [
            "        expected.AssertCloseTo(actual, Tolerance);",
            "    private static bool Near(Color a, Color b) => Math.Abs(a.R - b.R) <= 1 && Math.Abs(a.G - b.G) <= 1 && Math.Abs(a.B - b.B) <= 1;",
            "    private static void AssertNear(Color expected, SKColor actual, string where) =>"] },
        OwnedRule.Nowhere("a gesture on a page and what it sends are written by InteractionGestures: no test class keeps its own Svg, Payload, Num, HoverAt, Build or DrawnPoints over the harness",
            @"\bstatic\s+[\w<>\[\](),.? ]+\s+(?:Svg\(InteractionScriptHarness|Payload\(SignalRInvokeMock|Num\(IDictionary<string, object\?> payload|HoverAt\(InteractionScriptHarness|Build\(Action<FigureBuilder>|DrawnPoints\(string svg)",
            "Tst/") with { Finds = [
            "    private static DomElement S" + "vg(InteractionScriptHarness h) => h.Document.QuerySelectorAllRaw(\"svg\").Single();",
            "    private static IDictionary<string, object?> Pay" + "load(SignalRInvokeMock mock, string method) =>",
            "    private static double N" + "um(IDictionary<string, object?> payload, string key) =>",
            "    private static IDictionary<string, object?> Hover" + "At(InteractionScriptHarness h, Point p)",
            "    private static Figure Bui" + "ld(Action<FigureBuilder> configure)",
            "    private static Point[] DrawnPo" + "ints(string svg, string label)"], Ignores = [
            "        var hover = h.HoverAt(p);",
            "    private static Action<FigureBuilder> Figure(Func<AxesBuilder, AxesBuilder> axes, Action<ServerInteractionBuilder>? server = null) => b => b"] },
        new("the chart of a page is h.Chart(): no test finds the svg element by hand",
            @"QuerySelectorAllRaw\(""svg""\)\.Single\(\)",
            "Tst/MatPlotLibNet/Rendering/Svg/Interaction/InteractionGestures.cs") { Under = "Tst/", Finds = [
            "        var svg = h.Document." + "QuerySelectorAllRaw(\"svg\").Single();"], Ignores = [
            "        var svg = h.Chart();",
            "        var all = h.Document.QuerySelectorAllRaw(\"svg\");"] },

        new("what the library announces is listened to through DiagnosticsCapture, which always lets go: no test subscribes to the diagnostics event itself (the class that tests the event is beside the owner)",
            @"ChartDiagnostics\.Emitted\s*[+-]=",
            "Tst/MatPlotLibNet/TestFixtures/DiagnosticsCapture.cs", "Tst/MatPlotLibNet/Diagnostics/ChartDiagnosticsTests.cs") { Under = "Tst/", Finds = [
            "        ChartDiagnostics.Emitted " + "+= Collect;",
            "            ChartDiagnostics.Emitted " + "-= Handler;"], Ignores = [
            "        using var capture = new DiagnosticsCapture();",
            "        var seen = DiagnosticsCapture.During(() => figure.ToSvg());",
            "        ChartDiagnostics.Emit(new ChartDiagnostic(\"Test\", \"message\", null));"] },

        OwnedRule.Nowhere("a transform over the plot rectangle is made by DataTransformFixtures (Plot.Linear, Plot.LogY, Plot.LogLog, ...): no test writes the 15 arguments of a log transform that has no breaks and the default threshold (a transform with breaks, or with another threshold, is the thing its test constructs), or keeps a fixture of four literals",
            @"\bnew(?: DataTransform)?\((?:[^;]*?),\s*(?:null|\[\]),\s*(?:null|\[\]),[^;]*?AxisScale\.(?:Log,\s*AxisScale\.\w+|\w+,\s*AxisScale\.Log\b),\s*(?:1|1\.0),\s*(?:1|1\.0)\)|static DataTransform \w+\(\)\s*=>\s*new(?: DataTransform)?\(\s*-?\d+(?:\.\d+)?, -?\d+(?:\.\d+)?, -?\d+(?:\.\d+)?, -?\d+(?:\.\d+)?, \w+\)",
            "Tst/") with { AcrossLines = true, Finds = [
            "        new(0, 10, 0, 2, Bounds, null, null, 0, 10, 1, 100, AxisScale.Linear, AxisScale." + "Log, 1, 1);",
            "        var logX = new DataTransform(0, 2, 0, 10, Plot, null, null, 1, 100, 0, 10, AxisScale.Log, AxisScale." + "Linear, 1, 1);",
            "    private static DataTransform Log() => new(0, 3, 0, 3, Plot, [], [], 1, 1000, 1, 1000, AxisScale.Log, AxisScale." + "Log, 1, 1);",
            "    private static DataTransform Transform() => ne" + "w(0, 10, 0, 10, Plot);"], Ignores = [
            "        var logY = Plot.LogY();",
            "        var withBreaks = new DataTransform(0, 10, 0, 2, Plot, null, [new AxisBreak(1, 2)], 0, 10, 1, 100, AxisScale.Linear, AxisScale.Log, 1, 1);",
            "        var symlog = new DataTransform(0, 3, 0, 4, Plot, null, null, 1, 1000, -100, 100, AxisScale.Log, AxisScale.SymLog, 1.0, 0.25);",
            "        new(0, 10, 0, 80, Plot, null, [new AxisBreak(40, 60)], 0, 10, 0, 100);"] },

        new("a theme a row names is read by AsTheme(): no test class resolves a theme from a string of its own, by a method, a reflection read, a switch arm or a ternary (the one row that is no built-in theme, a library theme with an edge colour, is a field of its own)",
            @"\bstatic\s+Theme\s+\w+\(string \w+\)|\(Theme\)typeof\(Theme\)\.GetProperty\(|""[^""]+""\s*=>\s*Theme\.\w+|==\s*""[^""]+""\s*\?\s*(?:Theme\.\w+|\w+\.AsTheme\(\))\s*:\s*(?:Theme\.\w+|\w+\.AsTheme\(\))",
            "Tst/MatPlotLibNet/TestFixtures/ThemeCases.cs") { Under = "Tst/", Finds = [
            "    private static Theme Theme" + "Of(string id) => id == \"v2\" ? Theme.MatplotlibV2 : Theme.MatplotlibClassic;",
            "    private static Theme Na" + "med(string name) => (Theme)typeof(Theme)." + "GetProperty(name)!.GetValue(null)!;",
            "        var theme = (Theme)typeof(Theme)." + "GetProperty(themeName)!.GetValue(null)!;",
            "        var theme = themeId switch { \"v2\" => The" + "me.MatplotlibV2, _ => Default };",
            "        var theme = id == \"v2\" ? The" + "me.MatplotlibV2 : Theme.MatplotlibClassic;",
            "        var look = theme == \"default\" ? The" + "me.Default : theme.AsTheme();"], Ignores = [
            "        var theme = themeName.AsTheme();",
            "        var source = from == \"classic\" ? Theme.MatplotlibClassic : fourPoints.ThemeOver(Theme.Default);",
            "    public Theme AsTheme() => name switch",
            "    private static Theme Plain => Theme.Default;",
            "    private static readonly Theme LibraryWithEdge = new(\"library with an edge colour\", Theme.Default) { PatchEdgeColor = Green };"] },
        OwnedRule.Nowhere("a published count counts the public concrete classes of a root through ConcretePublicSubtypesOf: no test selects them by hand, with a test of IsPublic (or IsVisible, or the exported types) and of IsAbstract in whatever form it is written (a lambda variable of any name, a property pattern) next to the test of the root",
            @"\bt\.IsPublic\b[^;\n]*!t\.IsAbstract\b|!t\.IsAbstract\b[^;\n]*\bt\.IsPublic\b|\bGetExportedTypes\(\)(?=[^;]*\bIsAbstract\b)(?=[^;]*\b(?:IsAssignableFrom|IsSubclassOf)\()|\bGetTypes\(\)(?=[^;]*\bIs(?:Public|Visible)\b)(?=[^;]*\bIsAbstract\b)(?=[^;]*\b(?:IsAssignableFrom|IsSubclassOf)\()",
            "Tst/") with { AcrossLines = true, Finds = [
            "            .Where(t => t.IsClass && !t.IsAbstract && t.Is" + "Public",
            "            .Where(t => t.Is" + "Public && !t.IsAbstract && t.Namespace == \"MatPlotLibNet.Indicators.Streaming\"",
            "        typeof(IGeoProjection).Assembly.Get" + "Types()\n            .Count(type => type is { IsClass: true, IsAbstract: false, IsPublic: true } && typeof(IGeoProjection).IsAssignableFrom(type));",
            "        var concrete = typeof(Root).Assembly.Get" + "Types().Where(t => t is { IsAbstract: false, IsPublic: true } && typeof(Root).IsAssignableFrom(t));",
            "        typeof(ISeries).Assembly.GetExported" + "Types()\n            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(ISeries).IsAssignableFrom(t));",
            "        var figures = typeof(Axes).Assembly.GetExported" + "Types().Where(type => typeof(FacetedFigure).IsAssignableFrom(type) && !type.IsAbstract);"], Ignores = [
            "        typeof(ChartSeries).Assembly.ConcretePublicSubtypesOf(typeof(ChartSeries));",
            "        foreach (var t in ConcreteSeries())",
            "        var statics = core.GetExportedTypes().Where(type => type.IsAbstract && type.IsSealed);",
            "        var everything = typeof(Root).Assembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false } && typeof(Root).IsAssignableFrom(t));"] },
        new("the label kinds that the documentation and the emitted text both refuse (a phase, a date, a version number, a test or tool name, the follow-on of a plan) are written by InternalLabelExtensions.SharedKinds: no test keeps a regex of its own for a tool name, a phase of a plan or its follow-on",
            @"\\bPlaywright\\b\|\\bJint\\b|\\bPhase\[ -\]|Regex\(@""[^""]*follow-on",
            "Tst/MatPlotLibNet/Rendering/Svg/InternalLabelExtensions.cs") { Under = "Tst/", Finds = [
            "        new(\"a tool name\", new Regex(@\"\\bPlay" + "wright\\b|\\bJint\\b\", RegexOptions.CultureInvariant)),",
            "        new(\"a phase label\", new Regex(@\"\\bPh" + "ase[ -](?:[A-Z]\\b|\\d)\", RegexOptions.CultureInvariant)),",
            "        new(\"a plan reference\", new Regex(@\"follow-" + "on|\\bfloor plan\\b\", RegexOptions.CultureInvariant)),"], Ignores = [
            "        new(\"a promise of later work\", new Regex(@\"follow-up\", RegexOptions.CultureInvariant)),",
            "        .. InternalLabelExtensions.SharedKinds,",
            "    [InlineData(\"a test or tool name\", \"verified by the Playwright harness\", true)]"] },
        new("the sentence a verb refuses arrays of another length in is written by LengthRefusals (3.RefusalAgainst(2, \"values\"), LengthRefusals.Opening, LengthRefusals.Grid): a verb's test names the counts, the holder and the parameter, and only the tests of the check itself pin the words",
            @"Array lengths must\smatch",
            "Tst/MatPlotLibNet/TestFixtures/LengthRefusals.cs", "Tst/MatPlotLibNet/TestFixtures/LengthRefusalsTests.cs",
            "Tst/MatPlotLibNet/Extensions/ArrayLengthExtensionsTests.cs", "Tst/MatPlotLibNet/Models/Series/BarGroupExtensionsTests.cs") { Under = "Tst/", Finds = [
            "        Assert.Equal(\"Array lengths " + "must match. Got 2 and 6 in group 'Before'. (Parameter 'from')\", error.Message);",
            "        Assert.Contains(\"Array lengths " + "must match\", refusal.Message);"], Ignores = [
            "        Assert.Equal(2.RefusalAgainst(6, \"from\", \"group 'Before'\"), error.Message);",
            "        Assert.Contains(LengthRefusals.Opening, refusal.Message);",
            "        Assert.Equal(\"x has 3 values and y has 2; there must be one y per x. (Parameter 'y')\", error.Message);"] },
        new("the labelled contour whose last line stands past the cut axis is made by ContourFigures (theme.LabelledContourPastTheAxes()): no test builds its levels, labels and cut by hand",
            @"LevelValues = \[2\.5, 5\.5, 8\.5\]",
            "Tst/MatPlotLibNet/TestFixtures/ContourFigures.cs") { Under = "Tst/", Finds = [
            "                s.LevelValues = [2.5, 5.5, " + "8.5];"], Ignores = [
            "        var figure = Theme.MatplotlibV2.LabelledContourPastTheAxes();",
            "                s.LevelValues = [1, 2, 3];"] },

        new("the Skia suite loads the Skia assembly once, by SkiaSuiteInitializer: no test class forces the module initializer by hand",
            @"RunModuleConstructor\(typeof\(FigureSkiaExtensions\)",
            "Tst/MatPlotLibNet.Skia/SkiaSuiteInitializer.cs") { Under = "Tst/MatPlotLibNet.Skia/", Finds = [
            "        System.Runtime.CompilerServices.RuntimeHelpers.Run" + "ModuleConstructor(typeof(FigureSkiaExtensions).Module.ModuleHandle);"], Ignores = [
            "    static LinePixelGridTests() => Plt.Create();",
            "        RuntimeHelpers.RunModuleConstructor(typeof(SkiaFonts).Module.ModuleHandle);"] },

        OwnedRule.Nowhere("what every Skia export transform is (a figure transform, a non-empty file that starts with its magic bytes) is held by TransformExportTests<TTransform> over each transform: no test class asserts it for one transform by hand",
            @"Assert\.IsAssignableFrom<(?:IFigureTransform|FigureTransform)>\(new (?:Pdf|Png)Transform\(\)\)",
            "Tst/MatPlotLibNet.Skia/") with { Finds = [
            "        Assert.IsAssignableFrom<IFigureTransform>(new Pdf" + "Transform());",
            "        Assert.IsAssignableFrom<FigureTransform>(new Png" + "Transform());"], Ignores = [
            "        Assert.IsAssignableFrom<IFigureTransform>(new TTransform());",
            "        Assert.IsAssignableFrom<FigureTransform>(new PdfTransform(new ChartRenderer()));"] },
        OwnedRule.Nowhere("the looks a theme carries are read off the same theme after it travels, by Cloned, ThroughDto and ThroughJson: no test spells the three routes by hand",
            @"new ThemeBuilder\(\w+(?:\.\w+)?\)\.Build\(\)\.Looks|ThemeDto\.From\(\w+(?:\.\w+)?\)\.ToTheme\(\)\.Looks|\.WithTheme\(\w+(?:\.\w+)?\)\.Build\(\)\.RoundTripped\(\)\.Theme\.Looks|Theme\.CreateFrom\(\w+(?:\.\w+)?\)\.Build\(\)\.Looks",
            "Tst/") with { Finds = [
            "        Assert.True(new ThemeBuilder(Theme.MatplotlibV2)." + "Build().Looks.ColorBar.TakesRoom);",
            "        Assert.True(ThemeDto.From(Theme.MatplotlibClassic)." + "ToTheme().Looks.Text.TitlePad.HasValue);",
            "        Assert.True(Plt.Create().WithTheme(Theme.MatplotlibClassic).Build()." + "RoundTripped().Theme.Looks.ColorBar.StepPerColour);"], Ignores = [
            "        Assert.True(Theme.MatplotlibV2.Cloned().Looks.ColorBar.TakesRoom);",
            "        var builder = new ThemeBuilder(Theme.MatplotlibV2);"] },

        new("a fidelity figure is drawn from FidelityFigure(themeId), which holds the size every fidelity render uses: no chart sets the size by hand",
            @"\.WithSize\(FigWidth, FigHeight\)",
            "Tst/MatPlotLibNet.Fidelity/FidelityTest.cs") { Under = "Tst/MatPlotLibNet.Fidelity/", Finds = [
            "            .WithSize(FigWidth, " + "FigHeight)"], Ignores = [
            "        var figure = FidelityFigure(themeId)",
            "        var small = Plt.Create().WithSize(400, 300);"] },
        OwnedRule.Nowhere("a fidelity theory takes its rows from FidelityRows: no test lists the two styles and the category itself",
            @"\[InlineData\(""(?:classic|v2)""\)\]|\[Trait\(""Category"", ""Fidelity""\)\]",
            "Tst/MatPlotLibNet.Fidelity/") with { Finds = [
            "    [InlineData(\"cla" + "ssic\")]",
            "    [InlineData(\"v" + "2\")]",
            "    [Trait(\"Category\", \"Fid" + "elity\")]"], Ignores = [
            "    [FidelityRows]",
            "    [InlineData(\"constrained\")]"] },

        new("an axes is drawn into a rectangle by Drawn(area, theme): no test makes the recording surface and runs the renderer of the axes by hand",
            @"var ctx = new RecordingRenderContext\(\);\s*\n\s*(?:new \w+AxesRenderer|AxesRenderer\.Create)\(\w+, [^,]+, ctx, [^;]+\)\.Render\(\);",
            "Tst/MatPlotLibNet/TestFixtures/DrawExtensions.cs") { Under = "Tst/MatPlotLibNet/", AcrossLines = true, Finds = [
            "        var ctx = new RecordingRenderContext();\n        new CartesianAxesRenderer(axes, Plot, ctx, Theme." + "Default).Render();\n        return ctx;",
            "        var ctx = new RecordingRenderContext();\n        AxesRenderer.Create(axes, Area, ctx, " + "theme).Render();"], Ignores = [
            "        var ctx = axes.Drawn(Plot, theme);",
            "        var ctx = new RecordingRenderContext();\n        var renderer = new CartesianAxesRenderer(axes, Plot, ctx, theme);\n        renderer.Render();"] },
        OwnedRule.Nowhere("the first plot area of a figure is figure.FirstPlotArea(): no test runs the layout and takes the first area by hand",
            @"ComputeLayout\(\w+, new (?:MatPlotLibNet\.Rendering\.Svg\.)?SvgRenderContext\(\)\)\.PlotAreas\[0\]",
            "Tst/") with { Finds = [
            "        var plot = new ChartRenderer().Compute" + "Layout(figure, new SvgRenderContext()).PlotAreas[0];"], Ignores = [
            "        var plot = figure.FirstPlotArea();",
            "        var layout = new ChartRenderer().ComputeLayout(figure, new SvgRenderContext());"] },
        new("a Skia test finds the plot area of a figure by PlotAreaOnSkia(), the layout on the surface the figure is later drawn on",
            @"ComputeLayout\(\w+, new SkiaRenderContext\(\w+\)\)\.PlotAreas\[0\]",
            "Tst/MatPlotLibNet.Skia/TestFixtures/SkiaLayoutExtensions.cs") { Under = "Tst/", Finds = [
            "        return new ChartRenderer().Comp" + "uteLayout(figure, new SkiaRenderContext(canvas)).PlotAreas[0];"], Ignores = [
            "        var plot = figure.PlotAreaOnSkia();"] },
        OwnedRule.Nowhere("the distance of two points, the corners of an outline, the markup of an axes and the closeness of a range or of ticks are members of the test extensions: no test class keeps its own",
            @"\bstatic\s+(?:double\s+Distance\(Point a, Point b\)|List<Point>\s+Corners\(IEnumerable<Point>|string\s+RenderAxes\(Axes|void\s+AssertTicks\(double\[\]|void\s+AssertRange\(double lo, double hi,)",
            "Tst/") with { Finds = [
            "    private static double Dist" + "ance(Point a, Point b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));",
            "    private static List<Point> Cor" + "ners(IEnumerable<Point> outline)",
            "    private static string RenderA" + "xes(Axes axes)",
            "    private static void AssertTi" + "cks(double[] expected, double[] actual)",
            "    private static void AssertRa" + "nge(double lo, double hi, double actualLo, double actualHi)"], Ignores = [
            "        double d = a.DistanceTo(b);",
            "    private static double Distance(Point p) => Math.Sqrt((p.X - Centre.X) * (p.X - Centre.X) + (p.Y - Centre.Y) * (p.Y - Centre.Y));"] },

        new("a sample surface the tests draw is made by SampleSurface: the peaks function and the cardinal sine are written there and nowhere else",
            @"\(1 - \w+(?:\[\w+\])? / 2 \+ Math\.Pow\(|Math\.Sin\(r\) / r",
            "Tst/MatPlotLibNet/TestFixtures/SampleSurface.cs", "Tst/MatPlotLibNet/TestFixtures/SampleSurfaceTests.cs") { Under = "Tst/", Finds = [
            "                z[iy, ix] = (1 - x[ix] / 2 + Math." + "Pow(x[ix], 5) + Math.Pow(y[iy], 3)) * Math.Exp(-x[ix] * x[ix] - y[iy] * y[iy]);",
            "                sz[i, j] = r < 1e-10 ? 1.0 : Math.Sin(r) " + "/ r;"], Ignores = [
            "        var (x, y, z) = SampleSurface.Peaks();",
            "        var wave = Math.Sin(r) * 2;"] },
        OwnedRule.Nowhere("a sample the tests draw is made once, by the fixture that holds it: no test class keeps its own discrete Fourier transform, ramp grid, sample tree, surface axes, flow field, 3-D scene, camera rows or recording of a figure",
            @"\bstatic\s+(?:Complex\[\]\s+DirectDft\(|double\[,\]\s+BuildGrid\(|Axes\s+With(?:Surface|Trisurf)\(Action<|StreamplotSeries\s+Field\(|double\[\]\s+Axis\(\)\s*$|InteractionScriptHarness\s+Build3D(?:Scene)?\(\)|IEnumerable<object\[\]>\s+CameraAngles\(\)|RecordingRenderContext\s+Drawn\(Figure \w+\))",
            "Tst/") with { Finds = [
            "    private static Complex[] DirectD" + "ft(double[] x)",
            "    private static double[,] BuildG" + "rid(double[] x, double[] y)",
            "    private static Axes WithSur" + "face(Action<SurfaceSeries>? configure = null)",
            "    private static StreamplotSeries Fie" + "ld(double density = 1)",
            "    private static InteractionScriptHarness Build3" + "DScene() =>",
            "    public static IEnumerable<object[]> CameraAng" + "les() =>",
            "    private static RecordingRenderContext Dra" + "wn(Figure figure)"], Ignores = [
            "        var spectrum = new double[] { 1, 0, 0, 0 }.DirectDft();",
            "    private static TreeNode Build(int depth) => new();"] },
        OwnedRule.Nowhere("a fidelity chart lists the points of a grid by Meshgrid: no chart writes the row-by-row index of its own",
            @"int k = iy \* \d+ \+ ix;",
            "Tst/MatPlotLibNet.Fidelity/") with { Finds = [
            "                int k = iy * 1" + "0 + ix;"], Ignores = [
            "        var (x, y) = Meshgrid(xs, ys);"] },
        new("a SignalR test starts its host by SignalRTestHost.Start(): no test builds the test server and adds the chart hub by hand",
            @"\.ConfigureServices\([^)\n]*AddMatPlotLibNetSignalR",
            "Tst/MatPlotLibNet.AspNetCore/TestFixtures/SignalRTestHost.cs") { Under = "Tst/", Finds = [
            "            web.ConfigureServices(services => services.AddMatPlotLib" + "NetSignalR().AddRouting());"], Ignores = [
            "        using var host = SignalRTestHost.Start();"] },
        new("a SignalR test connects to its host by host.ConnectToTheHub(): no test builds the hub connection of the test server by hand",
            @"new HubConnectionBuilder\(\)\s*$",
            "Tst/MatPlotLibNet.AspNetCore/TestFixtures/HubConnections.cs") { Under = "Tst/MatPlotLibNet.AspNetCore/", Finds = [
            "        var connection = new HubConnection" + "Builder()"], Ignores = [
            "        var connection = host.ConnectToTheHub();"] },
        OwnedRule.Nowhere("a modifier or controller test gets its figure and layout as a FigureLayout, never as a tuple of the two",
            @"\(Figure fig, IChartLayout layout(?:, \w+ \w+)?\) \w+\(",
            "Tst/") with { Finds = [
            "    private static (Figure fig, IChartLayout lay" + "out) Make()"], Ignores = [
            "    private static FigureLayout Make()"] },
        new("the spec reader and the schema description the Mcp tests read are SpecFixtures.Reader and SpecFixtures.Description; the lines of a schema section are read by SectionLines, SectionFields and FieldsListedUnder",
            @"var reader = new ChartSpecReader\(new ChartTypeCatalog\(\), RenderLimits\.Default\);|= new ChartSchemaDescription\(new ChartTypeCatalog\(\)\)\.Describe\(null\)|\bstring\[\] (?:Section\w*|FieldsListedUnder)\(",
            "Tst/MatPlotLibNet.Mcp/SpecFixtures.cs") { Under = "Tst/MatPlotLibNet.Mcp/", Finds = [
            "        var reader = new ChartSpecReader(new ChartTypeCatalog(), RenderLimits." + "Default);",
            "        string schema = new ChartSchemaDescription(new ChartTypeCatalog())." + "Describe(null);",
            "    private static string[] SectionLines" + "(string heading)",
            "    private static string[] SectionOf" + "(string schema, string heading)"], Ignores = [
            "        var reader = new ChartSpecReader(catalog, RenderLimits.Default);",
            "        string schema = Description.Describe(null);"] },
        OwnedRule.Nowhere("a call a recording surface kept is read as its typed leaf (ctx.Of<DrawTextCall>(), ctx.Count<PopClipCall>(), call.Text, call is DrawPathCall): no test reads an argument as dynamic, by reflection over the call, or picks calls by the name of their kind",
            @"\(dynamic\)|(?<![\w.])dynamic\s+\w|<dynami[c]\b|\bOfKind\(|\.Arg[s]\b|\.Kind\s*(?:==|!=|is)\s*(?:not\s*)?\(?""|\bCountOf\(""(?:Draw|Push|Pop|Set|Begin|End)|Assert\.Equal\(""(?:Draw|Push|Pop|Set|Begin|End)\w*"",[^;]*\.Kind\)",
            "Tst/MatPlotLibNet/") with { Finds = [
            "        var text = (string)((dyn" + "amic)ctx.Of" + "Kind(\"DrawText\")[0].Ar" + "gs!).text;",
            "            dyn" + "amic a = call.Ar" + "gs!;",
            "    private static List<dyn" + "amic> Texts(RecordingRenderContext ctx) =>",
            "        var rects = ctx.Of" + "Kind(\"DrawRectangle\");",
            "    private static T Arg<T>(CallRecord call, string name) => (T)call.Ar" + "gs!.GetType().GetProperty(name)!.GetValue(call.Ar" + "gs)!;",
            "        var first = ctx.Calls.First(c => c.Kind " + "== \"DrawPath\");",
            "        Assert.Equal(3, ctx.Count" + "Of(\"DrawLine\"));",
            "        Assert.Equal(\"DrawPolygon\", call." + "Kind);",
            "        Assert.Equal(\"DrawCircle\", Assert.Single(SwatchOf(resting))." + "Kind);",
            "        return [.. calls.TakeWhile(c => c.Kind " + "is not (\"DrawText\" or \"DrawRichText\"))];"], Ignores = [
            "        var text = ctx.Of<DrawTextCall>()[0].Text;",
            "        var first = ctx.Of<DrawPathCall>().First();",
            "        Assert.Equal([\"PushClip\", \"DrawPath\", \"PopClip\"], calls.Select(c => c.Kind));",
            "        int drawn = svg.CountOf(\"<polygon\");",
            "        // the heuristic needs real phase dynamics, so only finite output is asked"] },
        OwnedRule.Nowhere("every argument of every recorded call is walked by the recorder (AssertNothingUndrawableReached, Signature): no test class keeps a walk of its own, nor a list of one kind's rectangles, polygons or lines",
            @"\bstatic\s+[\w<>\[\]]+\s+(?:AssertNothingUndrawableReached|Signature|RoundedSignature|Rects|Polygons|Polylines|Lines)\(RecordingRenderContext\b",
            "Tst/MatPlotLibNet/") with { Finds = [
            "    private static void AssertNothingUndrawable" + "Reached(RecordingRenderContext ctx)",
            "    private static List<string> Signa" + "ture(RecordingRenderContext ctx, string? kind = null) =>",
            "    private static List<Rect> Re" + "cts(RecordingRenderContext ctx) => [.. ctx.Of<DrawRectangleCall>().Select(c => c.Rect)];",
            "    private static List<Point[]> Polyg" + "ons(RecordingRenderContext ctx) => [.. ctx.Of<DrawPolygonCall>().Select(c => c.Points)];"], Ignores = [
            "        withGap.AssertNothingUndrawableReached();",
            "        Assert.Equal(without.Signature(), withGap.Signature());",
            "    private static List<Point[]> Stems(RecordingRenderContext drawn) => [.. drawn.Of<DrawLinesCall>().Select(c => c.Points)];"] },
        OwnedRule.Nowhere("a property of one object is held to the same property of another by Assert.Equivalent, never by a ladder of four or more Assert.Equal lines",
            @"(?:[ ]*Assert\.Equal\(\w+\.(\w+), \w+\.\1\);\n){4}",
            "Tst/") with { AcrossLines = true, Finds = [
            "        Assert.Equal(a.X, b.X);\n        Assert.Equal(a.Y, b.Y);\n        Assert.Equal(a.Z, b.Z);\n        Assert." + "Equal(a.W, b.W);\n"], Ignores = [
            "        Assert.Equal(a.X, b.X);\n        Assert.Equal(a.Y, b.Y);\n        Assert.Equal(a.Z, b.Z);\n"] },
        OwnedRule.Nowhere("a Maui render-context test gets its substitute canvas and the context over it from MauiRig.Fresh(): no test pairs them by hand",
            @"var canvas = Substitute\.For<ICanvas>\(\);\s*\n\s*var ctx = new MauiGraphicsRenderContext\(canvas\);|new MauiGraphicsRenderContext\(Substitute\.For<ICanvas>\(\)\)",
            "Tst/MatPlotLibNet.Maui/") with { AcrossLines = true, Finds = [
            "        var canvas = Substitute.For<ICanvas>();\n        var ctx = new MauiGraphics" + "RenderContext(canvas);",
            "        var layout = Layout(new MauiGraphics" + "RenderContext(Substitute.For<ICanvas>()));"], Ignores = [
            "        var (canvas, ctx) = MauiRig.Fresh();",
            "        var canvas = Substitute.For<ICanvas>();\n        var drawable = new Drawable(canvas);"] },
        new("a Skia context test draws onto the bitmap, canvas and context of a SkiaCanvasFixture: no test class makes the three by hand",
            @"\w+ = new SKBitmap\(\w+, \w+\);\s*\n\s*\w+ = new SKCanvas\(\w+\);\s*\n\s*\w+\.Clear\(SKColors\.Transparent\);",
            "Tst/MatPlotLibNet.Skia/SkiaCanvasFixture.cs") { Under = "Tst/MatPlotLibNet.Skia/", AcrossLines = true, Finds = [
            "        _bitmap = new SKBitmap(100, 100);\n        _canvas = new SKCanvas(_bitmap);\n        _canvas.Clear(SKColors." + "Transparent);"], Ignores = [
            "        using var bitmap = new SKBitmap(10, 10);\n        using var canvas = new SKCanvas(bitmap);"] },
        new("a vertical mark is held to the pixel grid by VerticalMarkPixelGridTests: no leaf lays out the plot, the edge and the row of its own",
            @"\.SetXLim\(0, 620\)\.SetYLim\(-1, 1\)",
            "Tst/MatPlotLibNet.Skia/VerticalMarkPixelGridTests.cs") { Under = "Tst/MatPlotLibNet.Skia/", Finds = [
            "                .Stem([value], [1.0], s => s.StemColor = Colors.Blue).SetXLim(0, 620)." + "SetYLim(-1, 1)"], Ignores = [
            "                .SetXLim(0, 10).SetYLim(-1, 1)"] },
        OwnedRule.Nowhere("what the markup of a drawing carries (a token it holds, a token it does not) is a row of a theory over DrawnToken: no fact is only a figure drawn into markup, whatever it arranges before, and tokens looked for in it",
            OnlyTokensLookedFor,
            "Tst/") with { AcrossLines = true, Finds = [
            "    [Fact]\n    public void Pie_Draws" + "Percentages()\n    {\n        var svg = Render(new PieSeries([10.0]));\n        Assert.Contains(\">100%<\", svg);\n    }",
            "    [Fact]\n    public void Hover_" + "IsEmitted()\n    {\n        string svg = Plt.Create().Plot([1.0], [2.0]).ToSvg();\n        Assert.Contains(\"OnHover\", svg, StringComparison.Ordinal);\n        Assert.DoesNotContain(\"OnZoom\", svg);\n    }",
            "    [Fact]\n    public void A_Figure_" + "IsBuiltThenDrawn()\n    {\n        var figure = Build();\n        figure.Mark();\n        var svg = figure.ToSvg();\n        Assert.Contains(\">x<\", svg);\n    }",
            "    [Fact]\n    public void A_Figure_" + "IsBuiltInSeveralStatements()\n    {\n        var figure = new Figure();\n        var ax = figure.AddSubPlot();\n        ax.Plot([0.0, 3.0], [0.0, 3.0]);\n        string svg = figure.ToSvg();\n\n        // The label is written once.\n        Assert.Contains(\"support zone\", svg);\n        Assert.DoesNotContain(\"nolabel\", svg);\n    }",
            "    [Fact]\n    public void Two_Figures_" + "AreDrawn()\n    {\n        string plain = Plt.Create().WithSize(400, 300).Plot(X, Y).ToSvg();\n        string linked = Plt.Create().WithSize(400, 300).Plot(X, Y).LinkSelection(\"orders\").ToSvg();\n\n        Assert.Contains(\"mpl:rows\", linked);\n        Assert.DoesNotContain(\"mpl:rows\", plain);\n    }",
            "    [Fact]\n    public void A_Context_" + "WritesALine()\n    {\n        var ctx = new SvgRenderContext();\n        ctx.DrawLine(new Point(1, 2), new Point(3, 4), Stroke);\n\n        Assert.Contains(\"<line\", ctx.GetOutput());\n    }",
            "    [Fact]\n    public void A_Context_" + "WritesALineAndItsColour()\n    {\n        var ctx = new SvgRenderContext();\n        ctx.DrawLine(new Point(1, 2), new Point(3, 4), Stroke);\n\n        string svg = ctx.GetOutput();\n        Assert.Contains(\"<line\", svg);\n        Assert.Contains(\"#FF0000\", svg);\n    }",
            "    [Fact]\n    public void A_Tile_" + "WearsItsColour()\n    {\n        string svg = Plt.Create()\n            .WithTheme(Theme.OpsNight)\n            .AddSubPlot(1, 1, 1, ax => ax.StatTile(42))\n            .ToSvg();\n\n        Assert.Contains(Theme.OpsNight.Alarm.Resting.ToHex(), svg);\n        Assert.DoesNotContain(Theme.OpsNight.CycleColors[0].ToHex(), svg);\n    }",
            "    [Fact]\n    public void A_Hatch_" + "FadesEachDot()\n    {\n        string svg = RenderRect(Hatched());\n        string tile = svg[svg." + "IndexOf(\"<pattern\", StringComparison.Ordinal)..];\n\n        Assert.Contains($\"opacity=\\\"{(64 / 255.0).ToString(\"G\", CultureInfo.InvariantCulture)}\\\"\", tile);\n    }",
            "    [Fact]\n    public void A_Surface_" + "IsDrawnWithABlockLambda()\n    {\n        string svg = Plt.Create().Surface(axis, axis, z, s => { s.A = 1; s.B = 2; }).Build().ToSvg();\n\n        Assert.Contains(Viridis(0.5).ToHex(), svg, StringComparison.OrdinalIgnoreCase);\n    }",
            "    [Fact]\n    public void A_Heatmap_" + "WritesNoDate()\n    {\n        var json = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } })).Build().ToJson();\n\n        Assert.DoesNotContain(\"firstCellDate\", json);\n    }",
            "    [Fact]\n    public void A_Page_" + "CarriesItsId()\n    {\n        var html = ChartServer.Generate(\"id\", \"<svg/>\");\n        Assert.Contains($\"\\\"{Id}\\\"\", html);\n    }",
            "    [Fact]\n    public void A_Json_" + "IsIndented()\n    {\n        var json = Serializer.ToJson(figure, indented: true);\n        Assert.Contains('\\n', json);\n    }",
            "    [Fact]\n    public void TheLines_" + "AreDrawn() =>\n        Assert.Contains(\"<polyline\", Svg(Theme.Default), StringComparison.Ordinal);",
            "    [Fact]\n    public void ATile_" + "WritesTheNumber() =>\n        Assert.Contains(\">4217<\", Svg(ax => ax.StatTile(4217, t => t.Label = \"Processes\")));",
            "    [Fact]\n    public void AMissingValue_" + "NeverReachesThePicture() => Assert.DoesNotContain(\"NaN\", Plt.Create().Plot(X, Y).ToSvg());",
            "    [Fact]\n    public void A_Json_" + "NamesNoDate() =>\n        Assert.DoesNotContain(\"firstCellDate\", figure.ToJson());",
            "    [Fact]\n    public void A_Json_" + "IsIndentedInOneExpression() =>\n        Assert.Contains('\\n', Serializer.ToJson(figure, indented: true));"], Ignores = [
            "    [Fact]\n    public void A_Figure_" + "Renders()\n    {\n        var svg = Render(new PieSeries([10.0]));\n        Assert.Contains(\"<svg\", svg);\n    }",
            "    [Fact]\n    public void A_Figure_" + "IsDrawnTwice()\n    {\n        var svg = Render(new PieSeries([10.0]));\n        Assert.Contains(\">100%<\", svg);\n        Assert.Equal(svg, Render(new PieSeries([10.0])));\n    }",
            "    [Fact]\n    public void A_Readme_" + "SaysItsLicence()\n    {\n        string readme = File.ReadAllText(Path.Combine(Root, \"README.md\"));\n        Assert.Contains(\"MIT\", readme);\n    }",
            "    [Fact]\n    public void A_Workflow_" + "PushesEveryPackage()\n    {\n        string workflow = Read(\".github\", \"workflows\", \"publish.yml\");\n        Assert.Contains(\"dotnet nuget push\", workflow);\n    }",
            "    [Fact]\n    public void A_Registry_" + "NamesTheBuiltIns()\n    {\n        var names = Registry.Names.Select(n => n.ToLowerInvariant()).ToArray();\n        Assert.Contains(\"nearest\", names);\n    }",
            "    [Fact]\n    public void A_Figure_" + "NamesItsFills()\n    {\n        var fills = Fills(Render(new PieSeries([10.0])));\n        Assert.Contains(\"#FF0000\", fills);\n    }",
            "    [Fact]\n    public void A_Figure_" + "IsDrawnInsideAGlobalState()\n    {\n        var prev = Current;\n        try\n        {\n            var svg = Render(new PieSeries([10.0]));\n            Assert.Contains(\">100%<\", svg);\n        }\n        finally\n        {\n            Current = prev;\n        }\n    }",
            "    [Fact]\n    public void A_Figure_" + "Renders() =>\n        Assert.Contains(\"<svg\", Svg(Theme.Default));",
            "    [Fact]\n    public void A_Figure_" + "IsTheSameTwice() =>\n        Assert.Equal(Svg(Theme.Default), Svg(Theme.Default));",
            "    [Fact]\n    public void A_Readme_" + "SaysItsLicence() =>\n        Assert.Contains(\"MIT\", File.ReadAllText(Path.Combine(Root, \"README.md\")));",
            "    [Fact]\n    public void A_Registry_" + "NamesTheBuiltIns() =>\n        Assert.Contains(\"nearest\", Registry.Names);",
            "    [Fact]\n    public void A_Figure_" + "NamesItsFills() =>\n        Assert.Contains(\"#FF0000\", Fills(Render(new PieSeries([10.0]))));"] },
    ];
}
