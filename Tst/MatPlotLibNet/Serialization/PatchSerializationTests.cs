// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>The figure JSON carries the patches of an axes (the <c>patches</c> list of a subplot, one entry per patch in the order
/// they were added, each told apart by its <c>type</c>): a figure with patches draws the same picture after a round trip and every
/// property of every patch reads back as it was. A property is written only while it differs from the value a new patch of its
/// kind has, so a patch at rest writes its kind and its geometry and nothing else, and a figure without patches writes the bytes it
/// wrote before patches reached the JSON. A kind this build does not know is skipped with a diagnostic, and the other patches still
/// load.
/// <para>Tagged into the <c>ChartSerializerGlobalState</c> collection: the figures read back through the process-global series
/// registry, which <see cref="SeriesRegistryTests"/> briefly rewrites, and the unknown-kind rows listen to the process-global
/// <see cref="ChartDiagnostics"/> channel.</para></summary>
[Collection("ChartSerializerGlobalState")]
public class PatchSerializationTests
{
    private static readonly ChartSerializer Serializer = new();

    private static Figure RoundTrip(Figure figure) => Serializer.FromJson(Serializer.ToJson(figure));

    /// <summary>A new patch of each kind, at rest, by the kind's name.</summary>
    private static readonly IReadOnlyDictionary<string, Func<Patch>> Leaves = new Dictionary<string, Func<Patch>>(StringComparer.Ordinal)
    {
        [nameof(Rectangle)] = () => new Rectangle(new(1, 1), 2, 1),
        [nameof(Polygon)] = () => new Polygon([new(0, 0), new(2, 0), new(1, 1.5)]),
        [nameof(Arrow)] = () => new Arrow(new(0, 0), 3, 2),
        [nameof(Circle)] = () => new Circle(new(3, 2), 1.5),
        [nameof(Ellipse)] = () => new Ellipse(new(2, 1), 4, 1.5),
        [nameof(Wedge)] = () => new Wedge(new(0, 0), 2, 30, 120),
        [nameof(Arc)] = () => new Arc(new(0, 0), 4, 2),
    };

    /// <summary>The name of each kind of patch.</summary>
    public static TheoryData<string> LeafNames { get; } = new(Leaves.Keys);

    /// <summary>One row per kind of patch and settable property of it.</summary>
    public static TheoryData<string, string> LeafProperties { get; } =
        new(Leaves.SelectMany(leaf => DrawnSeriesFixtures.DrawnProperties(leaf.Value().GetType()).Select(property => (leaf.Key, property.Name))));

    /// <summary>The fields a patch of each kind writes while everything about it is at rest: its kind and its geometry, the values
    /// it was made with.</summary>
    private static readonly IReadOnlyDictionary<string, string[]> FieldsAtRest = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        [nameof(Rectangle)] = ["type", "anchor", "width", "height"],
        [nameof(Polygon)] = ["type", "vertices"],
        [nameof(Arrow)] = ["type", "start", "dx", "dy"],
        [nameof(Circle)] = ["type", "center", "radius"],
        [nameof(Ellipse)] = ["type", "center", "width", "height"],
        [nameof(Wedge)] = ["type", "center", "radius", "theta1", "theta2"],
        [nameof(Arc)] = ["type", "center", "width", "height"],
    };

    private static Figure FigureOf(params Patch[] patches) => Plt.Create().WithSize(640, 480).AddSubPlot(1, 1, 1, ax =>
    {
        foreach (var patch in patches)
        {
            ax.AddPatch(patch);
        }
    }).Build();

    /// <summary>The <c>patches</c> entries of the first subplot of <paramref name="figure"/>, as written.</summary>
    private static List<JsonElement> WrittenPatches(Figure figure)
    {
        using var json = JsonDocument.Parse(Serializer.ToJson(figure));
        return [.. json.RootElement.GetProperty("subPlots")[0].GetProperty("patches").EnumerateArray().Select(e => e.Clone())];
    }

    private static string[] FieldNames(JsonElement entry) => [.. entry.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal)];

    private static List<ChartDiagnostic> Announced(Action act)
    {
        var seen = new List<ChartDiagnostic>();
        void Collect(ChartDiagnostic d) => seen.Add(d);
        ChartDiagnostics.Emitted += Collect;
        try
        {
            act();
        }
        finally
        {
            ChartDiagnostics.Emitted -= Collect;
        }

        return seen;
    }

    // ---- every kind, every property ---------------------------------------------------------------------------------

    /// <summary>The fixture holds one patch of each kind, so a kind added tomorrow turns this red until a row of it is listed.</summary>
    [Fact]
    public void EveryKindOfPatchTheLibraryShips_HasARow()
    {
        var shipped = typeof(Patch).Assembly.GetTypes().Where(type => type.IsPublic && !type.IsAbstract && typeof(Patch).IsAssignableFrom(type))
            .Select(type => type.Name).Order(StringComparer.Ordinal);

        Assert.Equal(shipped, Leaves.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(shipped, FieldsAtRest.Keys.Order(StringComparer.Ordinal));
    }

    /// <summary>A patch of every kind with every settable property off its value at rest draws the same picture after a round trip,
    /// and every property reads back as it was set.</summary>
    [Theory]
    [MemberData(nameof(LeafNames))]
    public void APatchWithEveryPropertySet_DrawsTheSameAfterARoundTrip_AndReadsBackEqual(string leaf)
    {
        var patch = Leaves[leaf]();
        var properties = DrawnSeriesFixtures.DrawnProperties(patch.GetType());
        for (int k = 0; k < properties.Count; k++)
        {
            properties[k].SetValue(patch, DrawnSeriesFixtures.NotTheDefault(properties[k], properties[k].GetValue(patch), k));
        }

        var figure = FigureOf(patch);
        string drawn = figure.ToSvg();
        var read = RoundTrip(figure);

        var back = Assert.Single(read.SubPlots[0].Patches);
        Assert.IsType(patch.GetType(), back);
        foreach (var property in properties)
        {
            Assert.True(DrawnSeriesFixtures.SameValue(property.GetValue(patch), property.GetValue(back)),
                $"{leaf}.{property.Name}: set to {DrawnSeriesFixtures.Show(property.GetValue(patch))}, read back as {DrawnSeriesFixtures.Show(property.GetValue(back))}.");
        }

        Assert.NotEmpty(patch.Outline());
        Assert.NotEqual(FigureOf().ToSvg(), drawn);
        Assert.Equal(drawn, read.ToSvg());
    }

    /// <summary>One property changed and every other at rest: the one comes back, and none of the others moves off its value at
    /// rest. (The row above changes every property at once, so it never sees a patch that is only partly changed, nor two fields
    /// swapped on the way.)</summary>
    [Theory]
    [MemberData(nameof(LeafProperties))]
    public void APatchWithOnePropertyChanged_KeepsThatOne_AndEveryOtherAtRest(string leaf, string changed)
    {
        var patch = Leaves[leaf]();
        var rest = Leaves[leaf]();
        var property = patch.GetType().GetProperty(changed)!;
        property.SetValue(patch, DrawnSeriesFixtures.NotTheDefault(property, property.GetValue(patch), k: 3));

        var back = Assert.Single(RoundTrip(FigureOf(patch)).SubPlots[0].Patches);

        Assert.IsType(patch.GetType(), back);
        foreach (var other in DrawnSeriesFixtures.DrawnProperties(patch.GetType()))
        {
            object? expected = other.Name == changed ? other.GetValue(patch) : other.GetValue(rest);
            Assert.True(DrawnSeriesFixtures.SameValue(expected, other.GetValue(back)),
                $"{leaf}.{other.Name} (with only {changed} changed): expected {DrawnSeriesFixtures.Show(expected)}, read back as {DrawnSeriesFixtures.Show(other.GetValue(back))}.");
        }
    }

    // ---- what is written ---------------------------------------------------------------------------------------------

    /// <summary>A patch at rest writes its kind and its geometry and no other field, so a document stays short and a field added to
    /// a kind later is not written for the patches that do not use it.</summary>
    [Theory]
    [MemberData(nameof(LeafNames))]
    public void APatchAtRest_WritesItsKindAndItsGeometryOnly(string leaf)
    {
        var entry = Assert.Single(WrittenPatches(FigureOf(Leaves[leaf]())));

        Assert.Equal(FieldsAtRest[leaf].Order(StringComparer.Ordinal), FieldNames(entry));
    }

    /// <summary>The kinds travel as the lower-camel names of matplotlib's classes.</summary>
    [Theory]
    [InlineData(nameof(Rectangle), "rectangle")]
    [InlineData(nameof(Polygon), "polygon")]
    [InlineData(nameof(Arrow), "arrow")]
    [InlineData(nameof(Circle), "circle")]
    [InlineData(nameof(Ellipse), "ellipse")]
    [InlineData(nameof(Wedge), "wedge")]
    [InlineData(nameof(Arc), "arc")]
    public void TheKindOfAPatch_IsItsName(string leaf, string type)
    {
        var entry = Assert.Single(WrittenPatches(FigureOf(Leaves[leaf]())));

        Assert.Equal(type, entry.GetProperty("type").GetString());
    }

    /// <summary>A figure without patches writes no list, so every figure written before patches reached the JSON keeps its bytes.</summary>
    [Fact]
    public void AnAxesWithoutPatches_WritesNoPatchesField()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2], [1.0, 2]).AddInset(0.5, 0.5, 0.3, 0.3, inset => inset.Plot([1.0, 2], [2.0, 1]))).Build();

        Assert.DoesNotContain("patches", Serializer.ToJson(figure), StringComparison.Ordinal);
    }

    /// <summary>The enum-valued fields travel as lower-camel names, never as numbers, so a reader outside .NET and the MCP spec
    /// reader see the same words.</summary>
    [Fact]
    public void TheEnumValuedFields_TravelAsNames()
    {
        var arrow = new Arrow(new(0, 0), 3, 2) { Shape = ArrowHeadShape.Left, Hatch = HatchPattern.ForwardDiagonal, EdgeLineStyle = LineStyle.DashDot };

        var entry = Assert.Single(WrittenPatches(FigureOf(arrow)));

        Assert.Equal("left", entry.GetProperty("shape").GetString());
        Assert.Equal("forwardDiagonal", entry.GetProperty("hatch").GetString());
        Assert.Equal("dashDot", entry.GetProperty("edgeLineStyle").GetString());
    }

    // ---- the values that rest at something other than the usual ------------------------------------------------------

    /// <summary>An arrow and an arc rest with a black edge, the rest of the kinds with none: the black one is left out, and a clear
    /// edge on an arrow, which is not its rest, is written and comes back clear.</summary>
    [Fact]
    public void TheEdgeColorOfAnArrowAndAnArc_RestsAtBlack()
    {
        var arrow = new Arrow(new(0, 0), 3, 2);
        var clearArrow = new Arrow(new(0, 0), 3, 2) { EdgeColor = Colors.Transparent };
        var blackCircle = new Circle(new(0, 0), 1) { EdgeColor = Colors.Black };
        var clearArc = new Arc(new(0, 0), 2, 1) { EdgeColor = Colors.Transparent };

        var read = RoundTrip(FigureOf(arrow, clearArrow, blackCircle, clearArc)).SubPlots[0].Patches;

        Assert.DoesNotContain("edgeColor", FieldNames(WrittenPatches(FigureOf(arrow))[0]));
        Assert.Contains("edgeColor", FieldNames(WrittenPatches(FigureOf(clearArrow))[0]));
        Assert.Contains("edgeColor", FieldNames(WrittenPatches(FigureOf(blackCircle))[0]));
        Assert.Equal([Colors.Black, Colors.Transparent, Colors.Black, Colors.Transparent], read.Select(patch => patch.EdgeColor));
    }

    /// <summary>A JSON written by hand that leaves a field out reads as a new patch of the kind: an arrow with a black edge, a
    /// circle with none and the first colour of the cycle as its face.</summary>
    [Fact]
    public void AHandWrittenPatchWithItsOptionalFieldsLeftOut_ReadsAsANewPatchOfItsKind()
    {
        const string json = """
        {"width":400,"height":300,"subPlots":[{"patches":[
          {"type":"arrow","start":{"dataX":0,"dataY":0},"dx":1,"dy":1},
          {"type":"circle","center":{"dataX":1,"dataY":2},"radius":3},
          {"type":"arc","center":{"dataX":1,"dataY":2},"width":3,"height":2}
        ]}]}
        """;

        var patches = Serializer.FromJson(json).SubPlots[0].Patches;

        var arrow = Assert.IsType<Arrow>(patches[0]);
        Assert.Equal(Colors.Black, arrow.EdgeColor);
        Assert.Equal(0.001, arrow.Width);
        Assert.Null(arrow.HeadWidth);
        var circle = Assert.IsType<Circle>(patches[1]);
        Assert.Equal(Colors.Transparent, circle.EdgeColor);
        Assert.Equal(Colors.Tab10Blue, circle.FaceColor);
        Assert.Equal(new Circle(new(1, 2), 3).EdgeWidth, circle.EdgeWidth);
        var arc = Assert.IsType<Arc>(patches[2]);
        Assert.Equal(Colors.Black, arc.EdgeColor);
        Assert.Equal(360, arc.Theta2);
    }

    /// <summary>A polygon written by hand with no vertices is a polygon of none, which draws nothing, as the model's is.</summary>
    [Fact]
    public void AHandWrittenPolygonWithNoVertices_IsAnEmptyPolygon()
    {
        var polygon = Assert.IsType<Polygon>(Assert.Single(Serializer.FromJson("""{"width":400,"height":300,"subPlots":[{"patches":[{"type":"polygon"}]}]}""").SubPlots[0].Patches));

        Assert.Empty(polygon.Vertices);
        Assert.True(polygon.Closed);
        Assert.Empty(polygon.Outline());
    }

    /// <summary>A size that is unset (null) and a size of 0 are two different things: a wedge to the centre and a wedge with no
    /// ring, an arrow whose head follows its shaft and an arrow with a head of no size. Each comes back as it was.</summary>
    [Fact]
    public void ANullableSize_KeepsTheDifferenceBetweenUnsetAndZero()
    {
        var toTheCentre = new Wedge(new(0, 0), 2, 0, 90);
        var noRing = new Wedge(new(0, 0), 2, 0, 90) { Width = 0 };
        var followsTheShaft = new Arrow(new(0, 0), 3, 2) { Width = 0.1 };
        var noHead = new Arrow(new(0, 0), 3, 2) { Width = 0.1, HeadWidth = 0, HeadLength = 0 };

        var written = WrittenPatches(FigureOf(toTheCentre, noRing, followsTheShaft, noHead));
        var read = RoundTrip(FigureOf(toTheCentre, noRing, followsTheShaft, noHead)).SubPlots[0].Patches;

        Assert.DoesNotContain("width", FieldNames(written[0]));
        Assert.Contains("width", FieldNames(written[1]));
        Assert.DoesNotContain("headWidth", FieldNames(written[2]));
        Assert.DoesNotContain("headLength", FieldNames(written[2]));
        Assert.Null(((Wedge)read[0]).Width);
        Assert.Equal(0, ((Wedge)read[1]).Width);
        Assert.Null(((Arrow)read[2]).HeadWidth);
        Assert.Null(((Arrow)read[2]).HeadLength);
        Assert.Equal(0, ((Arrow)read[3]).HeadWidth);
        Assert.Equal(0, ((Arrow)read[3]).HeadLength);
    }

    // ---- the picture ---------------------------------------------------------------------------------------------------

    /// <summary>Every kind on one axes, among a line and in the order they were added, draws the same picture after a round trip.</summary>
    [Fact]
    public void AFigureWithEveryKind_KeepsTheirKindsAndTheirOrder_AndDrawsTheSameAfterARoundTrip()
    {
        var figure = Plt.Create().WithSize(640, 480).AddSubPlot(1, 1, 1, ax => ax
            .Plot([0.0, 4], [0.0, 4])
            .AddPatch(new Wedge(new(1, 1), 1, 20, 100) { Width = 0.4, FaceColor = Colors.Tab10Orange })
            .AddPatch(new Rectangle(new(2, 0), 1.5, 1) { Angle = 30, Alpha = 0.5 })
            .AddPatch(new Arc(new(3, 3), 2, 1) { Theta1 = 20, Theta2 = 200, EdgeLineStyle = LineStyle.Dashed })
            .AddPatch(new Circle(new(1, 3), 0.5) { EdgeColor = Colors.Black, Hatch = HatchPattern.Cross })
            .AddPatch(new Arrow(new(0, 2), 1.5, 1) { Width = 0.1 })
            .AddPatch(new Ellipse(new(3, 1), 1.5, 0.5) { Angle = 45 })
            .AddPatch(new Polygon([new(0, 0), new(1, 0), new(0.5, 0.8)]) { Closed = false })).Build();
        string drawn = figure.ToSvg();

        var read = RoundTrip(figure);

        Assert.Equal(figure.SubPlots[0].Patches.Select(patch => patch.GetType()), read.SubPlots[0].Patches.Select(patch => patch.GetType()));
        Assert.Equal(drawn, read.ToSvg());
    }

    /// <summary>The label is what a legend lists a patch under: it comes back, and so does the legend row.</summary>
    [Fact]
    public void ALabelledPatch_KeepsItsLabel_AndItsLegendRow()
    {
        var figure = Plt.Create().WithSize(640, 480).AddSubPlot(1, 1, 1, ax => ax
            .Plot([0.0, 4], [0.0, 4], s => s.Label = "line")
            .AddPatch(new Rectangle(new(1, 1), 2, 1) { Label = "region" })
            .AddPatch(new Circle(new(3, 3), 0.5) { Label = "_hidden" })
            .AddPatch(new Arc(new(2, 2), 1, 1))).Build();
        string drawn = figure.ToSvg();

        var read = RoundTrip(figure);

        Assert.Equal(["region", "_hidden", null], read.SubPlots[0].Patches.Select(patch => patch.Label));
        Assert.Contains(">region<", drawn, StringComparison.Ordinal);
        Assert.DoesNotContain(">_hidden<", drawn, StringComparison.Ordinal);
        Assert.Equal(drawn, read.ToSvg());
    }

    /// <summary>An empty label is a label, not no label, and survives as one.</summary>
    [Fact]
    public void AnEmptyLabel_StaysEmpty()
    {
        var read = RoundTrip(FigureOf(new Circle(new(0, 0), 1) { Label = "" })).SubPlots[0].Patches;

        Assert.Equal("", Assert.Single(read).Label);
    }

    /// <summary>An inset is an axes read back by the same path as a subplot: its patches come back with it.</summary>
    [Fact]
    public void APatchOnAnInset_DrawsTheSameAfterARoundTrip()
    {
        var figure = Plt.Create().WithSize(640, 480).AddSubPlot(1, 1, 1, ax => ax
            .Plot([0.0, 4], [0.0, 4])
            .AddInset(0.55, 0.1, 0.4, 0.3, inset => inset.Plot([0.0, 1], [1.0, 0.0]).AddPatch(new Circle(new(0.5, 0.5), 0.25) { FaceColor = Colors.Tab10Green }))).Build();
        string drawn = figure.ToSvg();

        var read = RoundTrip(figure);

        Assert.IsType<Circle>(Assert.Single(read.SubPlots[0].Insets.Single().Patches));
        Assert.Equal(drawn, read.ToSvg());
    }

    /// <summary>A coordinate that is not a number is kept as one (written as the string <c>"NaN"</c>) and draws nothing, as the
    /// model keeps it.</summary>
    [Fact]
    public void ACoordinateThatIsNotANumber_IsKept_AndDrawsNothing()
    {
        var figure = FigureOf(new Rectangle(new(double.NaN, 1), 2, 1), new Polygon([new(0, 0), new(1, double.PositiveInfinity), new(1, 0)]));
        string drawn = figure.ToSvg();

        var read = RoundTrip(figure);

        Assert.True(double.IsNaN(((Rectangle)read.SubPlots[0].Patches[0]).Anchor.DataX));
        Assert.True(double.IsPositiveInfinity(((Polygon)read.SubPlots[0].Patches[1]).Vertices[1].DataY));
        Assert.Equal(drawn, read.ToSvg());
    }

    // ---- what a reader refuses and what it skips ---------------------------------------------------------------------

    /// <summary>A kind this build does not know (JSON from a later release, or a typo), or an entry with no kind at all, is skipped
    /// and announced; the patches around it still load, in their order.</summary>
    [Theory]
    [InlineData("""{"type":"__star__","points":5}""")]
    [InlineData("""{"radius":1}""")]
    public void AKindOfPatchThisBuildDoesNotKnow_IsSkippedWithADiagnostic_AndTheOthersStillLoad(string unknown)
    {
        string json = $$"""
        {"width":400,"height":300,"subPlots":[{"patches":[
          {"type":"circle","center":{"dataX":1,"dataY":2},"radius":1},
          {{unknown}},
          {"type":"rectangle","anchor":{"dataX":0,"dataY":0},"width":2,"height":1}
        ]}]}
        """;

        Figure? read = null;
        var seen = Announced(() => read = Serializer.FromJson(json));

        var patches = read!.SubPlots[0].Patches;
        Assert.Equal([typeof(Circle), typeof(Rectangle)], patches.Select(patch => patch.GetType()));
        var announcement = Assert.Single(seen);
        Assert.Equal("ChartSerializer", announcement.Source);
        Assert.Contains("patch", announcement.Message, StringComparison.Ordinal);
    }

    /// <summary>The style of a patch refuses what it cannot draw as it is set, and a JSON that carries such a value is refused the
    /// same way rather than read as a patch that draws wrongly.</summary>
    [Theory]
    [InlineData("""{"type":"circle","center":{"dataX":1,"dataY":2},"radius":1,"alpha":2}""")]
    [InlineData("""{"type":"circle","center":{"dataX":1,"dataY":2},"radius":1,"edgeWidth":-1}""")]
    public void AStyleThePatchRefuses_IsRefusedWhenItIsRead(string patch)
    {
        string json = $$"""{"width":400,"height":300,"subPlots":[{"patches":[{{patch}}]}]}""";

        Assert.Throws<ArgumentOutOfRangeException>(() => Serializer.FromJson(json));
    }
}
