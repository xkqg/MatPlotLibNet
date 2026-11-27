// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>A class with many methods does the work of more than one concern, and that work can be split into types that each own
/// one. Every type of the core library declares at most <see cref="MethodBudget"/> methods of its own; a type over the budget is named
/// in <see cref="OverTheBudget"/> with the reason it holds them, and the list is pinned both ways: a type over the budget that is not
/// named, and a name for a type that is within the budget again or no longer exists, turn the test red. A split that brings a type
/// within the budget takes its name off the list in the same change.</summary>
public class ResponsibilityBudgetContractTests
{
    /// <summary>How many methods a type may declare itself (not its property accessors or operators, not what the compiler writes for
    /// it, not a local function).</summary>
    internal const int MethodBudget = 20;

    /// <summary>A type of the library over the budget, and why it holds that many methods.</summary>
    private readonly record struct OverBudget(string Type, string Reason);

    /// <summary>The types over the budget, by full name, each with its reason.</summary>
    private static readonly OverBudget[] OverTheBudget =
    [
        new("MatPlotLibNet.FigureBuilderDefaultAxesVerbs", "One rule, a figure verb is the axes builder's verb of the same name taken on the figure's single axes, published once for each verb a chart of one axes takes without AddSubPlot; the list is closed (no new forwarder)."),
        new("MatPlotLibNet.Rendering.Svg.SvgSeriesRenderer", "The SVG renderer of the public series visitor: one Visit for each kind of series."),
        new("MatPlotLibNet.Rendering.ISeriesVisitor", "The public visitor of the series: one Visit for each kind of series."),
        new("MatPlotLibNet.Rendering.Svg.SvgRenderContext", "The public SVG drawing surface: each published drawing primitive and each member of the internal surface interfaces, every one handed to the type that owns it (the element buffer, the mark, text and markup writers)."),
        new("MatPlotLibNet.Rendering.AxesRenderer", "The public base of the axes renderers: its protected members are published."),
        new("MatPlotLibNet.Rendering.AlongExtensions", "One rule, an axis direction picking the X or the Y member of a pair, answered once for each kind of value that holds a pair."),
        new("MatPlotLibNet.Rendering.DataTransform", "One mapping between data and pixels and back, published: a member for each coordinate and each shape of a batch, and the cut beyond where a log axis begins and the zoom about a pointer, which read the private pair of directions that mapping is (the cut at the rooms of an axis drawn in parts is RoomCut's)."),
        new("MatPlotLibNet.MissingValueExtensions", "One rule, which values are missing and which can be drawn, answered for each shape of data."),
        new("MatPlotLibNet.Rendering.IRenderContext", "The public drawing surface: each primitive a backend implements."),
    ];

    private static readonly BindingFlags Declared =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    /// <summary>Every type of the core assembly but those the compiler writes.</summary>
    private static IReadOnlyList<Type> LibraryTypes { get; } =
    [
        .. typeof(Plt).Assembly.GetTypes()
            .Where(type => !type.Name.Contains('<', StringComparison.Ordinal) && !type.IsDefined(typeof(CompilerGeneratedAttribute)))
            .OrderBy(type => type.FullName, StringComparer.Ordinal),
    ];

    /// <summary>The methods a type declares itself, as the budget counts them.</summary>
    private static int MethodsOf(Type type) =>
        type.GetMethods(Declared).Count(method => !method.IsSpecialName
            && !method.Name.Contains('<', StringComparison.Ordinal)
            && !method.IsDefined(typeof(CompilerGeneratedAttribute)));

    /// <summary>The types of <paramref name="types"/> that declare more than <paramref name="budget"/> methods, by full name.</summary>
    private static IReadOnlyList<string> OverBudgetAmong(IEnumerable<Type> types, int budget) =>
        [.. types.Where(type => MethodsOf(type) > budget).Select(type => type.FullName!)];

    [Fact]
    public void EveryTypeOfTheLibrary_DeclaresAtMostTheBudget_OrGivesItsReason()
    {
        var named = OverTheBudget.Select(entry => entry.Type).ToHashSet(StringComparer.Ordinal);

        var unnamed = OverBudgetAmong(LibraryTypes, MethodBudget).Where(type => !named.Contains(type))
            .Select(type => $"{type} ({MethodsOf(LibraryTypes.Single(t => t.FullName == type))} methods)").ToList();

        Assert.True(unnamed.Count == 0,
            $"A type declares at most {MethodBudget} methods: split the concerns it holds into types of their own. Over the budget with no reason: {string.Join(", ", unnamed)}.");
    }

    [Fact]
    public void EveryReason_NamesATypeOfTheLibraryStillOverTheBudget()
    {
        var over = OverBudgetAmong(LibraryTypes, MethodBudget).ToHashSet(StringComparer.Ordinal);

        var stale = OverTheBudget.Where(entry => !over.Contains(entry.Type)).Select(entry => entry.Type).ToList();

        Assert.True(stale.Count == 0,
            $"These types are within the budget again, or no longer exist: take them off the list: {string.Join(", ", stale)}.");
    }

    [Fact]
    public void EveryReason_IsWritten_AndNamesItsTypeOnce()
    {
        Assert.All(OverTheBudget, entry => Assert.False(string.IsNullOrWhiteSpace(entry.Reason), $"{entry.Type} gives no reason."));
        Assert.Equal(OverTheBudget.Length, OverTheBudget.Select(entry => entry.Type).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void TheCount_IsTheMethodsATypeWritesItself()
    {
        Assert.Equal(3, MethodsOf(typeof(Counted)));
    }

    [Fact]
    public void ATypeOverTheBudget_IsFoundByTheCheck_AndOneWithinIsNot()
    {
        Assert.Equal([typeof(Counted).FullName!], OverBudgetAmong([typeof(Counted), typeof(Small)], 2));
        Assert.Empty(OverBudgetAmong([typeof(Counted), typeof(Small)], 3));
    }

    /// <summary>The types a class was split into each stand in a file of their own, named after the type: the drawing of the ticks, the SVG
    /// drawing surface, and the bidi algorithm by clause of UAX #9. A part that only the one type it was split from uses is declared inside
    /// that type (<see cref="SingleUserTypeContractTests"/>) and still stands alone, in the part file of that type named after both
    /// (<c>Owner.Part.cs</c>).</summary>
    [Theory]
    [InlineData("Rendering", "TickSide")]
    [InlineData("Rendering", "TickDrawContext")]
    [InlineData("Rendering", "LabelReach")]
    [InlineData("Rendering", "TickRun")]
    [InlineData("Rendering", "TickInk")]
    [InlineData("Rendering", "TickInkExtensions")]
    [InlineData("Rendering", "TickMarkSpan")]
    [InlineData("Rendering", "TickMark")]
    [InlineData("Rendering", "TitleClearance")]
    [InlineData("Rendering", "AxisTickExtensions")]
    [InlineData("Rendering", "SeriesPass")]
    [InlineData("Rendering", "LayerOrder")]
    [InlineData("Rendering", "ILayerHost")]
    [InlineData("Rendering", "PlacedAxis")]
    [InlineData("Rendering", "CartesianEdges")]
    [InlineData("Rendering", "OppositeTicks")]
    [InlineData("Rendering", "EdgeTitle")]
    [InlineData("Rendering", "UprightEdgeTitle")]
    [InlineData("Rendering", "TurnedEdgeTitle")]
    [InlineData("Rendering/Svg", "SvgElementBuffer")]
    [InlineData("Rendering/Svg", "ISvgElement")]
    [InlineData("Rendering/Svg", "SvgMarkupWriter")]
    [InlineData("Rendering/Svg", "ISvgGeometry")]
    [InlineData("Rendering/Svg", "PolygonGeometry")]
    [InlineData("Rendering/Svg", "CircleGeometry")]
    [InlineData("Rendering/Svg", "RectGeometry")]
    [InlineData("Rendering/Svg", "EllipseGeometry")]
    [InlineData("Rendering/Svg", "PathGeometry")]
    [InlineData("Rendering/Text", "CodePointExtensions")]
    [InlineData("Rendering/Text", "BidiExplicitLevels")]
    [InlineData("Rendering/Text", "BidiRunSequence")]
    [InlineData("Rendering/Text", "BidiNeutralTypes")]
    public void EachTypeOfASplitClass_StandsInAFileOfItsOwn(string folder, string type)
    {
        string directory = Path.Combine([RepositoryRoot.Folder, "Src", "MatPlotLibNet", .. folder.Split('/')]);
        string own = Path.Combine(directory, type + ".cs");
        string[] parts = Directory.GetFiles(directory, "*." + type + ".cs");
        Assert.True(File.Exists(own) || parts.Length == 1, $"{type} stands in no file of its own.");
        string text = File.ReadAllText(File.Exists(own) ? own : parts[0]);

        var declared = Declarations(text, indent: "");
        if (File.Exists(own))
        {
            Assert.Equal([type], declared);
            return;
        }

        Assert.Equal([Path.GetFileName(parts[0]).Split('.')[0]], declared);
        Assert.Equal([type], Declarations(text, indent: "    "));
    }

    /// <summary>The names of the types <paramref name="text"/> declares at the depth <paramref name="indent"/> stands for.</summary>
    private static string[] Declarations(string text, string indent) =>
    [
        .. Regex.Matches(text,
                "^" + indent + @"(?:internal|public|private)\s+(?:(?:static|sealed|abstract|readonly|partial|ref)\s+)*(?:class|struct|enum|interface|record(?:\s+struct)?)\s+(\w+)",
                RegexOptions.Multiline)
            .Select(match => match.Groups[1].Value),
    ];

    /// <summary>The public SVG surface is the entry only: every method it declares is published or implements a member of an interface
    /// of the library, and hands the work to the type that owns it. A private method of its own is work the entry does itself.</summary>
    [Fact]
    public void ThePublicSvgSurface_DeclaresNoMethodButWhatItPublishesOrImplements()
    {
        var own = typeof(MatPlotLibNet.Rendering.Svg.SvgRenderContext).GetMethods(Declared)
            .Where(method => !method.IsSpecialName && !method.Name.Contains('<', StringComparison.Ordinal))
            .Where(method => !method.IsPublic && !method.Name.Contains('.', StringComparison.Ordinal))
            .Select(method => method.Name).Order(StringComparer.Ordinal).ToList();

        Assert.True(own.Count == 0, $"The SVG surface does work of its own in: {string.Join(", ", own)}.");
    }

    /// <summary>The one envelope of an SVG element is written once: the element buffer applies the lifted clips at one place, lifts them
    /// at one place and writes what was asked of the next element at one place.</summary>
    [Theory]
    [InlineData(@"_clips\.Mark\(")]
    [InlineData(@"_clips\.Lift\(")]
    [InlineData(@"\bAppendAskedAttributes\(\);")]
    public void TheEnvelopeOfAnSvgElement_IsWrittenAtOnePlace(string spelling)
    {
        string path = Path.Combine(RepositoryRoot.Folder, "Src", "MatPlotLibNet", "Rendering", "Svg", "SvgElementBuffer.cs");
        Assert.True(File.Exists(path), "SvgElementBuffer stands in no file of its own.");

        Assert.Single(Regex.Matches(File.ReadAllText(path), spelling));
    }

    /// <summary>Three methods of its own beside a property, an operator and a local function: the check counts the three.</summary>
    private sealed class Counted
    {
        public int Property { get; set; }

        public static Counted operator +(Counted left, Counted right) => left.Second() > right.Third() ? left : right;

        public static int First()
        {
            return Inner();

            static int Inner() => 0;
        }

        public int Second() => Property;

        public int Third() => Property + 1;
    }

    /// <summary>One method of its own.</summary>
    private sealed class Small
    {
        public static int Only() => 0;
    }
}
