// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json.Serialization;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>A patch is a shape an agent draws in data coordinates (a band, a marker, a callout box). The spec reader takes the
/// seven kinds the figure JSON names, refuses a kind or an enum word it does not know by name and place with what it accepts,
/// as it does for every other kind and enum of the document, and the schema tells the agent what to write.</summary>
public class SpecPatchesTests
{
    private static readonly ChartSpecReader Reader = new(new ChartTypeCatalog(), RenderLimits.Default);
    private static readonly ChartSchemaDescription Description = new(new ChartTypeCatalog());

    /// <summary>One kind of patch as an agent writes it (colours by name, enum words in the case it likes) and the same patch made in
    /// code.</summary>
    /// <param name="Json">The entry of the <c>patches</c> list.</param>
    /// <param name="Build">The patch the entry stands for.</param>
    private readonly record struct KindCase(string Json, Func<Patch> Build);

    private static readonly IReadOnlyDictionary<string, KindCase> Cases = new Dictionary<string, KindCase>(StringComparer.Ordinal)
    {
        ["rectangle"] = new(
            """{"type":"rectangle","anchor":{"dataX":1,"dataY":1},"width":2,"height":1.5,"angle":20,"faceColor":"steelblue","edgeColor":"navy","edgeWidth":2,"edgeLineStyle":"DASHED","alpha":0.6,"label":"box","hatch":"forwarddiagonal","hatchColor":"white"}""",
            () => new Rectangle(new(1, 1), 2, 1.5)
            {
                Angle = 20, FaceColor = Color.FromName("steelblue"), EdgeColor = Color.FromName("navy"), EdgeWidth = 2,
                EdgeLineStyle = LineStyle.Dashed, Alpha = 0.6, Label = "box", Hatch = HatchPattern.ForwardDiagonal, HatchColor = Color.FromName("white"),
            }),
        ["polygon"] = new(
            """{"type":"polygon","vertices":[{"dataX":1,"dataY":1},{"dataX":3,"dataY":1.5},{"dataX":2,"dataY":3}],"closed":false,"faceColor":"orange","edgeColor":"black","edgeLineStyle":"dotted"}""",
            () => new Polygon([new(1, 1), new(3, 1.5), new(2, 3)])
            {
                Closed = false, FaceColor = Color.FromName("orange"), EdgeColor = Color.FromName("black"), EdgeLineStyle = LineStyle.Dotted,
            }),
        ["arrow"] = new(
            """{"type":"arrow","start":{"dataX":1,"dataY":1},"dx":2,"dy":1,"width":0.05,"headWidth":0.2,"headLength":0.3,"lengthIncludesHead":true,"shape":"LEFT","overhang":0.2,"faceColor":"crimson","edgeColor":"darkred"}""",
            () => new Arrow(new(1, 1), 2, 1)
            {
                Width = 0.05, HeadWidth = 0.2, HeadLength = 0.3, LengthIncludesHead = true, Shape = ArrowHeadShape.Left, Overhang = 0.2,
                FaceColor = Color.FromName("crimson"), EdgeColor = Color.FromName("darkred"),
            }),
        ["circle"] = new(
            """{"type":"circle","center":{"dataX":2,"dataY":2},"radius":1,"faceColor":"seagreen","alpha":0.5,"label":"disc"}""",
            () => new Circle(new(2, 2), 1) { FaceColor = Color.FromName("seagreen"), Alpha = 0.5, Label = "disc" }),
        ["ellipse"] = new(
            """{"type":"ellipse","center":{"dataX":2,"dataY":2},"width":3,"height":1,"angle":30,"faceColor":"gold","edgeColor":"black","edgeWidth":1.5}""",
            () => new Ellipse(new(2, 2), 3, 1) { Angle = 30, FaceColor = Color.FromName("gold"), EdgeColor = Color.FromName("black"), EdgeWidth = 1.5 }),
        ["wedge"] = new(
            """{"type":"wedge","center":{"dataX":2,"dataY":2},"radius":1.5,"theta1":30,"theta2":120,"width":0.5,"faceColor":"purple","hatch":"Dots"}""",
            () => new Wedge(new(2, 2), 1.5, 30, 120) { Width = 0.5, FaceColor = Color.FromName("purple"), Hatch = HatchPattern.Dots }),
        ["arc"] = new(
            """{"type":"arc","center":{"dataX":2,"dataY":2},"width":3,"height":2,"angle":15,"theta1":10,"theta2":200,"edgeColor":"teal","edgeWidth":3,"edgeLineStyle":"dashdot"}""",
            () => new Arc(new(2, 2), 3, 2) { Angle = 15, Theta1 = 10, Theta2 = 200, EdgeColor = Color.FromName("teal"), EdgeWidth = 3, EdgeLineStyle = LineStyle.DashDot }),
    };

    /// <summary>The seven kinds, by the name a spec writes them with.</summary>
    public static TheoryData<string> Kinds { get; } = new(Cases.Keys);

    private static string Spec(string patch) => $$"""{"width":400,"height":300,"subPlots":[{"patches":[{{patch}}]}]}""";

    private static Figure Read(string patch) => Reader.Read(new ChartSpec(Spec(patch)));

    /// <summary>The figure a caller of the library builds for <paramref name="patch"/>: one subplot that holds it.</summary>
    private static Figure Library(Patch patch) =>
        Plt.Create().WithSize(400, 300).AddSubPlot(1, 1, 1, ax => ax.AddPatch(patch)).Build();

    private static Type PatchRecord() =>
        typeof(SeriesDto).Assembly.GetType("MatPlotLibNet.Serialization.PatchDto") ?? throw new InvalidOperationException("The serializer no longer declares PatchDto.");

    // ---- the seven kinds, read from a spec ---------------------------------------------------------------------

    /// <summary>The cases are a case for every kind the wire family names: an eighth kind turns this red until a case stands for it.</summary>
    [Fact]
    public void TheCases_StandForEveryKindThePatchFamilyDeclares()
    {
        var declared = PatchRecord().GetCustomAttributes<JsonDerivedTypeAttribute>().Select(derived => (string)derived.TypeDiscriminator!);

        Assert.Equal(declared.Order(StringComparer.Ordinal), Cases.Keys.Order(StringComparer.Ordinal));
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void EachKindOfPatch_ReadFromASpec_DrawsTheSamePictureAsTheSamePatchMadeInCode(string kind)
    {
        var read = Read(Cases[kind].Json);

        Assert.Single(read.SubPlots[0].Patches);
        Assert.Equal(Library(Cases[kind].Build()).ToSvg(), read.ToSvg());
    }

    /// <summary>The same patch written by the library and handed back to the reader draws the same picture: the writer's spelling
    /// (lower-camel enum words, colours as hex) passes the reader as it passes the serializer.</summary>
    [Theory]
    [MemberData(nameof(Kinds))]
    public void EachKindOfPatch_WrittenByTheLibrary_PassesTheReaderAndDrawsTheSame(string kind)
    {
        var figure = Library(Cases[kind].Build());

        var read = Reader.Read(new ChartSpec(figure.ToJson()));

        Assert.Equal(figure.ToSvg(), read.ToSvg());
    }

    [Fact]
    public void AKindInAnyCase_AndWrittenAfterItsFields_IsTakenAsWritten()
    {
        var figure = Read("""{"center":{"dataX":2,"dataY":2},"radius":1,"faceColor":"seagreen","type":"CIRCLE"}""");

        var circle = Assert.IsType<Circle>(Assert.Single(figure.SubPlots[0].Patches));
        Assert.Equal(1, circle.Radius);
        Assert.Equal(Color.FromName("seagreen"), circle.FaceColor);
    }

    [Fact]
    public void ThePatchesOfAnInset_AreReadToo()
    {
        var figure = Reader.Read(new ChartSpec(
            """{"width":400,"height":300,"subPlots":[{"insets":[{"insetBounds":{"x":0.5,"y":0.5,"width":0.3,"height":0.3},"patches":[{"type":"CIRCLE","center":{"dataX":0,"dataY":0},"radius":1}]}]}]}"""));

        Assert.IsType<Circle>(Assert.Single(figure.SubPlots[0].Insets[0].Patches));
    }

    // ---- what is refused, by name and place ---------------------------------------------------------------------

    private const string Anchor = "\"anchor\":{\"dataX\":1,\"dataY\":1},\"width\":1,\"height\":1";

    [Theory]
    [InlineData("""{"type":"rectange",""" + Anchor + "}", "'rectange'", "$.subPlots[0].patches[0].type", "rectangle")]
    [InlineData("""{"type":"star",""" + Anchor + "}", "'star'", "$.subPlots[0].patches[0].type", "ellipse")]
    [InlineData("""{"type":"rectangle","edgeLineStyle":"dashy",""" + Anchor + "}", "'dashy'", "$.subPlots[0].patches[0].edgeLineStyle", "DashDot")]
    [InlineData("""{"type":"rectangle","hatch":"stripes",""" + Anchor + "}", "'stripes'", "$.subPlots[0].patches[0].hatch", "ForwardDiagonal")]
    [InlineData("""{"type":"arrow","start":{"dataX":0,"dataY":0},"dx":1,"dy":1,"shape":"both"}""", "'both'", "$.subPlots[0].patches[0].shape", "Full, Left, Right")]
    [InlineData("""{"type":"rectangle","faceColor":"nosuchcolour",""" + Anchor + "}", "'nosuchcolour'", "$.subPlots[0].patches[0].faceColor", "CSS4")]
    [InlineData("""{"type":"rectangle","hatchColor":"#12345",""" + Anchor + "}", "'#12345'", "$.subPlots[0].patches[0].hatchColor", "#RRGGBB")]
    public void AMisspelledKindOrValueOfAPatch_IsRefusedByValueAndPlace_WithWhatIsAccepted(string patch, string wrong, string where, string accepted)
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Read(patch));

        Assert.Contains(wrong, refusal.Message);
        Assert.Contains(where, refusal.Message);
        Assert.Contains(accepted, refusal.Message);
    }

    /// <summary>Every kind of the family is named in the refusal, as a tick locator's kinds are.</summary>
    [Fact]
    public void AMisspelledKind_IsRefusedWithEveryKindTheFamilyDeclares()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Read("""{"type":"rectange","anchor":{"dataX":1,"dataY":1},"width":1,"height":1}"""));

        Assert.All(Cases.Keys, kind => Assert.Contains(kind, refusal.Message));
    }

    /// <summary>A patch without a kind is refused by what it is, not as the unknown field the first of its fields would read as.</summary>
    [Fact]
    public void APatchWithoutAKind_IsRefusedWithTheAcceptedKinds()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Read("""{"anchor":{"dataX":1,"dataY":1},"width":1,"height":1}"""));

        Assert.Contains("$.subPlots[0].patches[0]", refusal.Message);
        Assert.Contains("no 'type'", refusal.Message);
        Assert.Contains("rectangle", refusal.Message);
    }

    [Fact]
    public void AMisspelledKindInAnInset_IsRefusedWithThePathOfTheInset()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(
            """{"width":400,"height":300,"subPlots":[{"insets":[{"insetBounds":{"x":0.5,"y":0.5,"width":0.3,"height":0.3},"patches":[{"type":"circel","center":{"dataX":0,"dataY":0},"radius":1}]}]}]}""")));

        Assert.Contains("'circel'", refusal.Message);
        Assert.Contains("insets[0].patches[0].type", refusal.Message);
    }

    [Fact]
    public void AFieldOfAnotherKind_IsRefusedByName()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Read("""{"type":"rectangle","anchor":{"dataX":1,"dataY":1},"width":1,"height":1,"radius":2}"""));

        Assert.Contains("'radius'", refusal.Message);
    }

    [Fact]
    public void ALabelOverTheLimit_IsRefusedWithItsPlace()
    {
        string label = new('x', RenderLimits.Default.MaxTextLength + 1);

        var refusal = Assert.Throws<ToolRefusalException>(() => Read($$"""{"type":"rectangle","label":"{{label}}",{{Anchor}}}"""));

        Assert.Contains("$.subPlots[0].patches[0]", refusal.Message);
        Assert.Contains("'label'", refusal.Message);
    }

    [Fact]
    public void ANullEntryOfThePatchList_IsRefusedWithItsPlace()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(
            """{"width":400,"height":300,"subPlots":[{"patches":[null]}]}""")));

        Assert.Contains("$.subPlots[0].patches[0]", refusal.Message);
    }

    // ---- what the schema tells an agent -------------------------------------------------------------------------

    private static string[] SectionLines(string heading)
    {
        string schema = Description.Describe(null);
        int at = schema.IndexOf(heading, StringComparison.Ordinal);
        Assert.True(at >= 0, $"'{heading}' is not in the schema.");
        return schema[at..].Split(Environment.NewLine).Skip(1).TakeWhile(line => line.Trim().Length > 0).ToArray();
    }

    private static string FieldName(string line) => line.Trim().Split(':')[0];

    /// <summary>The fields of one kind as the schema lists them under the kind: the lines indented under the kind's own line.</summary>
    private static string[] FieldsListedUnder(string kind)
    {
        var lines = SectionLines("Patch kinds");
        int at = Array.FindIndex(lines, line => line.StartsWith($"  {kind} ", StringComparison.Ordinal));
        Assert.True(at >= 0, $"The schema lists no patch kind '{kind}'.");
        return [.. lines.Skip(at + 1).TakeWhile(line => line.StartsWith("    ", StringComparison.Ordinal)).Select(FieldName)];
    }

    [Theory]
    [InlineData("rectangle", "anchor,width,height,angle")]
    [InlineData("polygon", "vertices,closed")]
    [InlineData("arrow", "start,dx,dy,width,headWidth,headLength,lengthIncludesHead,shape,overhang")]
    [InlineData("circle", "center,radius")]
    [InlineData("ellipse", "center,width,height,angle")]
    [InlineData("wedge", "center,radius,theta1,theta2,width")]
    [InlineData("arc", "center,width,height,angle,theta1,theta2")]
    public void TheSchema_NamesEveryKindOfPatch_WithTheFieldsThatKindTakes(string kind, string fields) =>
        Assert.Equal(fields.Split(','), FieldsListedUnder(kind));

    [Fact]
    public void TheSchema_ListsNoKindThePatchFamilyDoesNotDeclare()
    {
        var kinds = SectionLines("Patch kinds").Where(line => !line.StartsWith("    ", StringComparison.Ordinal)).Select(line => line.Trim().Split(' ')[0]);

        Assert.Equal(Cases.Keys.Order(StringComparer.Ordinal), kinds.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void TheSchema_SaysHowAPointAnAngleAndASizeAreWritten()
    {
        string heading = Description.Describe(null).Split(Environment.NewLine).Single(line => line.StartsWith("Patch kinds", StringComparison.Ordinal));

        Assert.Contains("subPlots[].patches", heading);
        Assert.Contains("dataX", heading);
        Assert.Contains("dataY", heading);
        Assert.Contains("data units", heading);
        Assert.Contains("counter-clockwise", heading);
    }

    [Theory]
    [InlineData("edgeColor", "outline")]
    [InlineData("edgeWidth", "pixels")]
    [InlineData("edgeLineStyle", "Dashed")]
    [InlineData("alpha", "opacity")]
    [InlineData("label", "legend")]
    [InlineData("hatch", "ForwardDiagonal")]
    [InlineData("hatchColor", "edge")]
    [InlineData("faceColor", "arc")]
    public void TheSchema_ListsTheStyleEveryPatchTakes_AndExplainsIt(string field, string explanation)
    {
        var line = Assert.Single(SectionLines("Patch style fields"), l => FieldName(l) == field);

        Assert.Contains(explanation, line, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The patch's own <c>label</c> is a legend entry on a rectangle swatch: the global note about a series' label does not speak for it.</summary>
    [Fact]
    public void TheLabelOfAPatch_IsNotExplainedAsTheLabelOfASeries()
    {
        var line = Assert.Single(SectionLines("Patch style fields"), l => FieldName(l) == "label");

        Assert.DoesNotContain("series", line, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheSubplotFields_ListThePatches_AndSayWhatTheyAre()
    {
        var line = Assert.Single(SectionLines("Subplot fields"), l => l.TrimStart().StartsWith("patches: object[]", StringComparison.Ordinal));

        Assert.Contains("shape", line, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("matplotlib", line, StringComparison.OrdinalIgnoreCase);
    }
}
