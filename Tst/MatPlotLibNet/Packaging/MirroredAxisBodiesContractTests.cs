// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.RegularExpressions;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>An X member and its Y twin never restate one body: where two members of one file are named alike but for X and Y (FoldXRange,
/// FoldYRange) and the body of the one, with X and Y swapped, is the body of the other, the work is one member that takes the direction
/// (<c>isX</c>, <c>alongX</c>) and the X and Y members, when they stay, forward to it in one line. A body of up to
/// <see cref="ShortestBody"/> characters is such a forward (or a field read) and is not compared; a longer pair at least
/// <see cref="MirrorSimilarity"/> alike after the swap is a mirror. The scan reads every file under <c>Src</c>, as the measure
/// (<c>xybodies</c>) that found the mirrors does: the same declarations, the same swap, the same ratio of matched characters.</summary>
public class MirroredAxisBodiesContractTests
{
    /// <summary>The longest body that is not compared: a one-line forward or a field read of either direction.</summary>
    private const int ShortestBody = 120;

    /// <summary>How alike two bodies are, after the swap, to be one body written twice.</summary>
    private const double MirrorSimilarity = 0.85;

    private static readonly Regex Declaration = new(
        @"(?m)^[ \t]*(?:public |internal |private |protected |static |override |virtual |sealed |async |unsafe )*[\w<>\[\],\.\?\(\) ]+?\s+([A-Za-z0-9_]*[XY][A-Za-z0-9_]*)\s*(?:<[^>]*>)?\s*\(",
        RegexOptions.CultureInvariant);

    private static readonly Regex Space = new(@"\s+", RegexOptions.CultureInvariant);

    /// <summary>A member named for a way a line runs (horizontal, vertical): the other spelling of a direction.</summary>
    private static readonly Regex OrientationDeclaration = new(
        @"(?m)^[ \t]*(?:public |internal |private |protected |static |override |virtual |sealed |async |unsafe )*[\w<>\[\],\.\?\(\) ]+?\s+([A-Za-z0-9_]*(?:Horizontal|Vertical)[A-Za-z0-9_]*)\s*(?:<[^>]*>)?\s*\(",
        RegexOptions.CultureInvariant);

    /// <summary>A property or a constant named for a way a line runs: a pair of them is a pair of members too.</summary>
    private static readonly Regex OrientationProperty = new(
        @"(?m)^[ \t]*(?:(?:public|internal|private|protected|static|override|virtual|sealed|readonly|const|required) )+[\w<>\[\],\.\?]+\s+([A-Za-z0-9_]*(?:Horizontal|Vertical)[A-Za-z0-9_]*)\s*(?:=>|\{|=|;)",
        RegexOptions.CultureInvariant);

    private static readonly Regex Construction = new(@"\bnew\s+\w+\s*(?:<[^>]*>)?\s*\($", RegexOptions.CultureInvariant);

    private static readonly Regex TypeDeclaration = new(@"\b(?:class|struct|record|interface)\s+([A-Za-z0-9_]*[XY][A-Za-z0-9_]*)\b", RegexOptions.CultureInvariant);

    /// <summary>Why an X member and its Y twin stay two members.</summary>
    public enum TwinKind
    {
        /// <summary>A published pair (a public verb or member, which stays where it is): each body forwards in one line to the one
        /// member that takes the direction, the axis' own verbs or the per-direction owner.</summary>
        Forward,

        /// <summary>The entries for callers whose direction is fixed (no caller names a direction by a literal: the OneOwner row of a
        /// run's edge): each body forwards in one line to the one member that takes the direction.</summary>
        FixedDirection,

        /// <summary>Not one rule: the X member and the Y member answer different things (another column of data, another behaviour
        /// per direction).</summary>
        TwoRules,
    }

    /// <summary>A pair of members declared in one file, named alike but for X and Y (or for horizontal and vertical), with why it stays
    /// two.</summary>
    public sealed record Twin(string Path, string XName, TwinKind Kind, string Why)
    {
        /// <summary>The pair as the scan names it.</summary>
        public string Key => $"{Path}: {XName} / {TwinOf(XName)}";
    }

    /// <summary>The name of the Y (vertical) twin of an X (horizontal) member.</summary>
    private static string TwinOf(string name) =>
        name.Contains("Horizontal", StringComparison.Ordinal) ? name.Replace("Horizontal", "Vertical", StringComparison.Ordinal) : name.Replace('X', 'Y');

    /// <summary>Every X member with a Y twin in <c>Src</c>: one that does one thing along either direction is one member that takes the
    /// direction (<c>AlongExtensions</c>, <c>AxisPair</c>) and is not listed here; what is listed is a published pair that forwards or two
    /// rules. A new twin, or a row whose pair is gone, turns the test red.</summary>
    public static readonly Twin[] Twins =
    [
        .. new (string Verb, string Owner)[]
        {
            ("InvertXAxis", "Limit"), ("SetXAxisColor", "Label"), ("SetXDateFormat", "Tick"), ("SetXLabel", "Label"), ("SetXLim", "Limit"),
            ("SetXMargin", "Limit"), ("SetXScale", "Scale"), ("SetXTickFormatter", "Tick"), ("SetXTickLocator", "Tick"), ("ShareX", "LinkedAxes"),
            ("WithSymlogXScale", "Scale"), ("WithXBreak", "Limit"), ("WithXTickLabelRotation", "Tick"), ("WithXTicksMirrored", "Tick"),
        }.Select(pair => new Twin($"Src/MatPlotLibNet/Builders/AxesBuilder{pair.Owner}Verbs.cs", pair.Verb, TwinKind.Forward,
            "a public verb of the X axis beside the Y axis', each one line to the axis' verbs (AxisVerbs) or the axes' member")),
        new("Src/MatPlotLibNet/Builders/CategoryTickExtensions.cs", "SetXCategoryTicks", TwinKind.Forward,
            "the verb families' entries; every overload forwards to NamesCategoriesAlong(isX, ticks)"),
        new("Src/MatPlotLibNet/Models/Axes.cs", "AddXBreak", TwinKind.Forward, "public; both forward to AddBreak(isX, ...)"),
        new("Src/MatPlotLibNet/Rendering/DataTransform.cs", "TransformX", TwinKind.Forward,
            "public; each forwards to its direction's PixelDirection.Transform"),
        new("Src/MatPlotLibNet/Rendering/DataTransform.cs", "DataToPixelClippedX", TwinKind.FixedDirection,
            "a histogram's, a density's and a grid's base; each forwards to DataToPixelClippedAlong"),
        new("Src/MatPlotLibNet/Rendering/DataTransform.cs", "TransformXUnclipped", TwinKind.FixedDirection,
            "the paths of a step line, an ECDF, an error bar and a stack; each forwards to TransformUnclippedAlong"),
        new("Src/MatPlotLibNet/Rendering/ValueObjects/LineSegmentExtensions.cs", "HorizontalAcross", TwinKind.FixedDirection,
            "a horizontal reference line, a level, a Fibonacci level, a residual's zero line, and a vertical reference line; each forwards to AcrossAlong"),
        new("Src/MatPlotLibNet/Rendering/LegendRendering/LegendPositionStrategy.cs", "CenterX", TwinKind.Forward,
            "protected members of a public abstract record a caller can derive from; each forwards to Rect.CentreAlong"),
        new("Src/MatPlotLibNet/Rendering/ValueObjects/Box3D.cs", "WithX", TwinKind.Forward,
            "public; each a copy of the box with one of its three ranges replaced (WithZ beside them)"),
        new("Src/MatPlotLibNet/Transforms/SvgTransform.ServerInteractionScript.cs", "dataX", TwinKind.Forward,
            "the browser script's reader of a view's X and Y, one line each over the one unscale/unbreak rule; the script is written into the SVG"),
        new("Src/MatPlotLibNet/Models/Series/XY/IMonotonicXY.cs", "XAt", TwinKind.TwoRules, "the X and the Y of a sample: two columns of data"),
        new("Src/MatPlotLibNet/Models/Series/XY/SignalSeries.cs", "XAt", TwinKind.TwoRules,
            "X is worked out from the start and the rate, Y is read from the samples"),
        new("Src/MatPlotLibNet/Models/Series/XY/SignalXYSeries.cs", "XAt", TwinKind.TwoRules, "the X and the Y of a sample: two columns of data"),
        new("Src/MatPlotLibNet/Models/SubPlotSpacing.cs", "HorizontalGap", TwinKind.TwoRules, "two settings of the grid: the gap between columns and between rows"),
        new("Src/MatPlotLibNet/Models/SubPlotSpacing.cs", "HorizontalGapFraction", TwinKind.TwoRules, "two settings of the grid, as a share of a cell"),
        new("Src/MatPlotLibNet/Serialization/SubPlotSpacingDto.cs", "HorizontalGap", TwinKind.TwoRules, "the wire fields of the two settings"),
        new("Src/MatPlotLibNet/Serialization/SubPlotSpacingDto.cs", "HorizontalGapFraction", TwinKind.TwoRules, "the wire fields of the two settings"),
        new("Src/MatPlotLibNet/Rendering/TickLocators/MaxNLocator.cs", "HorizontalLabelSpan", TwinKind.TwoRules,
            "matplotlib's two numbers (3 label sizes along X, 2 along Y), held as the one pair LabelSpans a direction picks from"),
        new("Src/MatPlotLibNet/Models/Streaming/AxisScaleMode.cs", "ScaleX", TwinKind.TwoRules,
            "a mode does another thing per direction: AutoScale pads only Y, a sliding window spans X only"),
    ];

    /// <summary>Types named alike but for X and Y, with why they stay two.</summary>
    public static readonly (string XType, string Why)[] TypeTwins =
    [
        ("XAxisSlotBuilder", "two published builders (a linked X axis below the plot, a Y axis on either side); every verb they share forwards in one line to AxisVerbs"),
    ];

    [Fact]
    public void EveryXMemberWithAYTwin_IsAPublishedForwardOrTwoRules()
    {
        var files = RepositoryTree.SourceFiles().ToList();
        var found = files.SelectMany(DeclaredTwins).ToHashSet(StringComparer.Ordinal);
        var listed = Twins.ToDictionary(t => t.Key, StringComparer.Ordinal);

        var unlisted = found.Where(key => !listed.ContainsKey(key)).Order(StringComparer.Ordinal).ToList();
        var stale = listed.Keys.Where(key => !found.Contains(key)).Order(StringComparer.Ordinal).ToList();
        var longForwards = Twins.Where(t => t.Kind != TwinKind.TwoRules)
            .Where(t => files.First(f => f.Path == t.Path) is var file && AllBodies(file.Text) is var bodies
                && (Longer(bodies, t.XName) || Longer(bodies, TwinOf(t.XName))))
            .Select(t => t.Key).ToList();

        Assert.True(unlisted.Count == 0, "An X member and its Y twin that do one thing are one member that takes the direction:\n" + string.Join("\n", unlisted));
        Assert.True(stale.Count == 0, "A listed twin is gone; take its row out:\n" + string.Join("\n", stale));
        Assert.True(longForwards.Count == 0, "A published or fixed-direction twin forwards in one line:\n" + string.Join("\n", longForwards));

        static bool Longer(Dictionary<string, string> bodies, string name) => !bodies.TryGetValue(name, out var body) || Space.Replace(body, " ").Length > ShortestBody;
    }

    [Fact]
    public void EveryXTypeWithAYTwin_IsListed_AndForwardsWhatItShares()
    {
        var files = RepositoryTree.SourceFiles().ToList();
        var types = files.SelectMany(f => TypeDeclaration.Matches(f.Text).Select(m => (m.Groups[1].Value, File: f))).ToList();
        var names = types.Select(t => t.Value).ToHashSet(StringComparer.Ordinal);
        var twins = names.Where(n => names.Contains(n.Replace('X', 'Y')) && n.Replace('X', 'Y') != n).Order(StringComparer.Ordinal).ToList();

        Assert.Equal(TypeTwins.Select(t => t.XType).Order(StringComparer.Ordinal), twins);
        foreach (var (xType, _) in TypeTwins)
        {
            var x = Bodies(types.First(t => t.Value == xType).File.Text);
            var y = Bodies(types.First(t => t.Value == xType.Replace('X', 'Y')).File.Text);
            var shared = x.Keys.Where(k => y.ContainsKey(k.Replace('X', 'Y'))).ToList();
            Assert.NotEmpty(shared);
            Assert.All(shared, k => Assert.True(Space.Replace(x[k], " ").Length <= ShortestBody && Space.Replace(y[k.Replace('X', 'Y')], " ").Length <= ShortestBody,
                $"{xType}.{k} and its Y twin forward in one line"));
        }
    }

    [Fact]
    public void ADeclaredTwin_IsFound_AndACallOrAConstructionIsNot()
    {
        var file = new SourceFile("Src/MatPlotLibNet/Rendering/Any.cs", """
                internal bool LiesBeyondX(double x) => _dir.X.LiesBeyond(x);
                internal bool LiesBeyondY(double y) => _dir.Y.LiesBeyond(y);
                private void RenderHorizontal(Rect bounds) => Draw(bounds);
                private void RenderVertical(Rect bounds) => Draw(bounds);
                internal double VerticalEnd => Rising ? From.Y : To.Y;
                internal double HorizontalEnd => Rising ? To.X : From.X;
                internal Point Make() => Pick(a) ? TransformX(a) : TransformY(b);
                    configure(new XAxisSlotBuilder(axes, slot));
                    configure(new YAxisSlotBuilder(axes, slot));
            """);

        Assert.Equal(["Src/MatPlotLibNet/Rendering/Any.cs: HorizontalEnd / VerticalEnd", "Src/MatPlotLibNet/Rendering/Any.cs: LiesBeyondX / LiesBeyondY",
                "Src/MatPlotLibNet/Rendering/Any.cs: RenderHorizontal / RenderVertical"],
            DeclaredTwins(file));
    }

    /// <summary>The pairs of members declared in <paramref name="file"/> named alike but for X and Y, or for horizontal and vertical
    /// (a construction <c>new X...(</c> is no declaration).</summary>
    private static IEnumerable<string> DeclaredTwins(SourceFile file)
    {
        var names = Declaration.Matches(file.Text).Concat(OrientationDeclaration.Matches(file.Text)).Concat(OrientationProperty.Matches(file.Text))
            .Where(m => !Construction.IsMatch(m.Value))
            .Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);
        return names.Where(n => TwinOf(n) is var twin && twin != n && names.Contains(twin))
            .Order(StringComparer.Ordinal).Select(n => $"{file.Path}: {n} / {TwinOf(n)}");
    }

    /// <summary>The bodies of the members named for a direction of either spelling (<see cref="Bodies"/>).</summary>
    private static Dictionary<string, string> AllBodies(string text)
    {
        var bodies = Bodies(text);
        foreach (var (name, body) in Bodies(text, OrientationDeclaration)) bodies.TryAdd(name, body);
        return bodies;
    }

    [Fact]
    public void NoXMemberAndItsYTwin_RestateOneBody()
    {
        var mirrors = RepositoryTree.SourceFiles().SelectMany(Mirrors).ToList();

        Assert.True(mirrors.Count == 0, "An X member and its Y twin restate one body; give it one member that takes the direction:\n"
            + string.Join("\n", mirrors));
    }

    [Fact]
    public void TwoBodiesThatSwapXForY_AreFound()
    {
        var file = new SourceFile("Src/MatPlotLibNet/Rendering/Any.cs", """
                public Range1D FoldXRange(Range1D seed)
                {
                    var running = seed;
                    foreach (var c in contribs)
                        running = running.Merge(c.XMin, c.XMax);
                    return running;
                }

                public Range1D FoldYRange(Range1D seed)
                {
                    var running = seed;
                    foreach (var c in contribs)
                        running = running.Merge(c.YMin, c.YMax);
                    return running;
                }
            """);

        Assert.Equal(["Src/MatPlotLibNet/Rendering/Any.cs: FoldXRange / FoldYRange"], Mirrors(file));
    }

    [Fact]
    public void OneLineForwards_AndBodiesThatDiffer_AreNoMirror()
    {
        var file = new SourceFile("Src/MatPlotLibNet/Rendering/Any.cs", """
                internal double[] TransformX(ReadOnlySpan<double> xData) => _dir.X.Transform(xData);
                internal double[] TransformY(ReadOnlySpan<double> yData) => _dir.Y.Transform(yData);

                internal double LabelX(Rect plot, double band)
                {
                    double centre = plot.X + plot.Width / 2;
                    return centre + band * 3 - Math.Floor(centre / 2) * 7 + LookUpTheFontOfTheTitle(plot).Size;
                }

                internal double LabelY(Rect plot, double band)
                {
                    var turned = new TextPlacement(new Point(0, 0), TextAlignment.Right) { Rotation = 90 };
                    return Measure(turned).Bounds().Height - band;
                }
            """);

        Assert.Empty(Mirrors(file));
    }

    /// <summary>The pairs of members of <paramref name="file"/> named alike but for X and Y whose bodies are one body, X and Y swapped.</summary>
    private static List<string> Mirrors(SourceFile file)
    {
        var bodies = Bodies(file.Text);
        var found = new List<string>();
        foreach (var (name, body) in bodies)
        {
            string twin = name.Replace('X', 'Y');
            if (twin == name || !bodies.TryGetValue(twin, out var twinBody) || body.Length <= ShortestBody) continue;
            if (Similarity(Swapped(body), Space.Replace(twinBody, " ")) >= MirrorSimilarity)
                found.Add($"{file.Path}: {name} / {twin}");
        }

        return found;
    }

    /// <summary>The body of every member whose name holds an X or a Y, the first one of each name: from its <c>=&gt;</c> to its
    /// <c>;</c>, or its braces.</summary>
    private static Dictionary<string, string> Bodies(string text, Regex? declaration = null)
    {
        var bodies = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match m in (declaration ?? Declaration).Matches(text))
        {
            int end = m.Index + m.Length;
            int brace = text.IndexOf('{', end), semicolon = text.IndexOf(';', end), arrow = text.IndexOf("=>", end, StringComparison.Ordinal);
            if (brace == -1) continue;
            string body;
            if (arrow >= 0 && arrow < brace && (semicolon == -1 || arrow < semicolon))
            {
                body = text[arrow..(semicolon != -1 ? semicolon : Math.Min(text.Length, arrow + 200))];
            }
            else
            {
                if (semicolon >= 0 && semicolon < brace) continue;
                int depth = 0, k = brace;
                for (; k < text.Length; k++)
                {
                    if (text[k] == '{') depth++;
                    else if (text[k] == '}' && --depth == 0) break;
                }

                body = text[brace..Math.Min(text.Length, k + 1)];
            }

            bodies.TryAdd(m.Groups[1].Value, body);
        }

        return bodies;
    }

    /// <summary>A body with its white space made single and X and Y swapped, upper case and lower case.</summary>
    private static string Swapped(string body)
    {
        var swapped = new StringBuilder(Space.Replace(body, " "));
        for (int i = 0; i < swapped.Length; i++)
        {
            swapped[i] = swapped[i] switch { 'X' => 'Y', 'Y' => 'X', 'x' => 'y', 'y' => 'x', var c => c };
        }

        return swapped.ToString();
    }

    /// <summary>The share of two strings that matches, as Python's <c>difflib.SequenceMatcher.ratio</c> counts it without its junk
    /// heuristic: twice the characters of the longest common run, and of the runs found the same way left and right of it, over both
    /// lengths.</summary>
    private static double Similarity(string a, string b) => a.Length + b.Length == 0 ? 1 : 2.0 * Matched(a, 0, a.Length, b, 0, b.Length) / (a.Length + b.Length);

    private static int Matched(string a, int aLo, int aHi, string b, int bLo, int bHi)
    {
        if (aLo >= aHi || bLo >= bHi) return 0;
        int best = 0, bestA = aLo, bestB = bLo;
        var run = new int[bHi - bLo + 1];
        for (int i = aLo; i < aHi; i++)
        {
            int previous = 0;
            for (int j = bLo; j < bHi; j++)
            {
                int current = run[j - bLo + 1];
                run[j - bLo + 1] = a[i] == b[j] ? previous + 1 : 0;
                if (run[j - bLo + 1] > best)
                {
                    best = run[j - bLo + 1];
                    bestA = i - best + 1;
                    bestB = j - best + 1;
                }

                previous = current;
            }
        }

        return best == 0 ? 0 : best + Matched(a, aLo, bestA, b, bLo, bestB) + Matched(a, bestA + best, aHi, b, bestB + best, bHi);
    }
}
