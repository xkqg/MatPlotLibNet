// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json.Serialization;
using MatPlotLibNet.Models;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Tests.Serialization;

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>An axis shows the primary axis' range in another unit when it is linked (the top X axis, or a Y axis past the primary):
/// an agent writes the <c>link</c> of an axis by kind, the way it writes a tick locator. The spec reader takes the four kinds the
/// figure JSON names, at every seat that can draw one, refuses a kind or a field it does not know by name and place with what it
/// accepts, as it does for every other kind of the document, and the schema tells the agent what to write.</summary>
public class SpecLinkedAxisTests
{
    private static readonly ChartSpecReader Reader = new(new ChartTypeCatalog(), RenderLimits.Default);
    private static readonly ChartSchemaDescription Description = new(new ChartTypeCatalog());

    private const string Line = """{"type":"line","xData":[1,2,3,4,5],"yData":[2,4,3,5,4]}""";

    /// <summary>One kind of link as an agent writes it and the same link made in code.</summary>
    /// <param name="Json">The value of the axis' <c>link</c> field.</param>
    /// <param name="Build">The transform the entry stands for.</param>
    private readonly record struct KindCase(string Json, Func<AxisTransform> Build);

    private static readonly IReadOnlyDictionary<string, KindCase> Cases = new Dictionary<string, KindCase>(StringComparer.Ordinal)
    {
        ["linear"] = new("""{"type":"linear","scale":1.8,"offset":32}""", () => AxisTransform.Linear(1.8, 32)),
        ["reciprocal"] = new("""{"type":"reciprocal","numerator":2}""", () => AxisTransform.Reciprocal(2)),
        ["logarithm"] = new("""{"type":"logarithm","base":2}""", () => AxisTransform.Logarithm(2)),
        ["power"] = new("""{"type":"power","exponent":2}""", () => AxisTransform.Power(2)),
    };

    /// <summary>One seat that draws a link: the subplot members that open it with the link at the place of LINK, the same figure made
    /// in code, and the path a refusal names for the link.</summary>
    /// <param name="Subplot">The subplot's JSON members after the series.</param>
    /// <param name="Library">Makes the figure in code, with the link.</param>
    /// <param name="Path">The path of the link inside the subplot.</param>
    private readonly record struct SeatCase(string Subplot, Action<AxesBuilder, AxisTransform> Library, string Path);

    private static readonly IReadOnlyDictionary<string, SeatCase> SeatCases = new Dictionary<string, SeatCase>(StringComparer.Ordinal)
    {
        ["top"] = new(""" "secondaryXAxis":{"label":"linked","link":LINK} """,
            (ax, link) => ax.WithSecondaryXAxis(top => top.SetXLabel("linked").Link(link)), "secondaryXAxis.link"),
        ["right"] = new(""" "secondaryYAxis":{"label":"linked","link":LINK} """,
            (ax, link) => ax.WithAddRightYAxis(slot => slot.SetYLabel("linked").Link(link)), "secondaryYAxis.link"),
        ["left"] = new(""" "leftYAxes":[{"axis":{"label":"linked","link":LINK}}] """,
            (ax, link) => ax.WithAddLeftYAxis(slot => slot.SetYLabel("linked").Link(link)), "leftYAxes[0].axis.link"),
    };

    /// <summary>The kinds, by the name a spec writes them with.</summary>
    public static TheoryData<string> Kinds { get; } = new(Cases.Keys);

    /// <summary>The seats that draw a link.</summary>
    public static TheoryData<string> Seats { get; } = new(SeatCases.Keys);

    /// <summary>Every kind at every seat.</summary>
    public static TheoryData<string, string> KindsAtSeats { get; } = new(Cases.Keys.SelectMany(kind => SeatCases.Keys.Select(seat => (kind, seat))));

    private static string Spec(string seat, string link) =>
        $$"""{"width":640,"height":480,"subPlots":[{"series":[{{Line}}],{{SeatCases[seat].Subplot.Replace("LINK", link)}}}]}""";

    private static Figure Read(string seat, string link) => Reader.Read(new ChartSpec(Spec(seat, link)));

    /// <summary>The figure a caller of the library builds for <paramref name="link"/> at <paramref name="seat"/>: one line, and the
    /// linked axis.</summary>
    private static Figure Library(string seat, AxisTransform link) =>
        Plt.Create().WithSize(640, 480).AddSubPlot(1, 1, 1, ax =>
        {
            ax.Plot([1.0, 2, 3, 4, 5], [2.0, 4, 3, 5, 4]);
            SeatCases[seat].Library(ax, link);
        }).Build();

    private static Axis AxisAt(Figure figure, string seat)
    {
        var axes = figure.SubPlots[0];
        return seat switch
        {
            "top" => axes.SecondaryXAxis!,
            "right" => axes.YAxes.Right[0].Axis,
            _ => axes.YAxes.Left[1].Axis,
        };
    }

    private static Type TransformRecord() =>
        typeof(SeriesDto).Assembly.GetType("MatPlotLibNet.Serialization.AxisTransformDto")
        ?? throw new InvalidOperationException("The serializer no longer declares AxisTransformDto.");

    // ---- the four kinds, read from a spec ---------------------------------------------------------------------------

    /// <summary>The cases are a case for every kind the wire family names: a fifth kind turns this red until a case stands for it.</summary>
    [Fact]
    public void TheCases_StandForEveryKindTheTransformFamilyDeclares()
    {
        var declared = TransformRecord().GetCustomAttributes<JsonDerivedTypeAttribute>().Select(derived => (string)derived.TypeDiscriminator!);

        Assert.Equal(declared.Order(StringComparer.Ordinal), Cases.Keys.Order(StringComparer.Ordinal));
    }

    [Theory]
    [MemberData(nameof(KindsAtSeats))]
    public void EachKindOfLink_ReadFromASpec_DrawsTheSamePictureAsTheSameLinkMadeInCode(string kind, string seat)
    {
        var read = Read(seat, Cases[kind].Json);

        Assert.True(DrawnSeriesFixtures.SameValue(Cases[kind].Build(), AxisAt(read, seat).Link),
            $"{kind} at {seat}: read as {DrawnSeriesFixtures.Show(AxisAt(read, seat).Link)}.");
        Assert.NotEqual(Library(seat, AxisTransform.Linear(1)).ToSvg(), read.ToSvg());
        Assert.Equal(Library(seat, Cases[kind].Build()).ToSvg(), read.ToSvg());
    }

    /// <summary>The same link written by the library and handed back to the reader draws the same picture: the writer's spelling
    /// passes the reader as it passes the serializer.</summary>
    [Theory]
    [MemberData(nameof(KindsAtSeats))]
    public void EachKindOfLink_WrittenByTheLibrary_PassesTheReaderAndDrawsTheSame(string kind, string seat)
    {
        var figure = Library(seat, Cases[kind].Build());

        var read = Reader.Read(new ChartSpec(figure.ToJson()));

        Assert.NotNull(AxisAt(read, seat).Link);
        Assert.Equal(figure.ToSvg(), read.ToSvg());
    }

    [Fact]
    public void AKindInAnyCase_AndWrittenAfterItsFields_IsTakenAsWritten()
    {
        var figure = Read("top", """{"scale":1.8,"offset":32,"type":"LINEAR"}""");

        Assert.True(DrawnSeriesFixtures.SameValue(AxisTransform.Linear(1.8, 32), figure.SubPlots[0].SecondaryXAxis!.Link));
    }

    // ---- what is refused, by name and place ---------------------------------------------------------------------------

    /// <summary>A misspelt kind is refused by the value written and the place it stands at, with every kind the family declares.</summary>
    [Theory]
    [MemberData(nameof(Seats))]
    public void AMisspelledKind_IsRefusedByValueAndPlace_WithEveryKindTheFamilyDeclares(string seat)
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Read(seat, """{"type":"linnear","scale":2}"""));

        Assert.Contains("'linnear'", refusal.Message);
        Assert.Contains($"$.subPlots[0].{SeatCases[seat].Path}.type", refusal.Message);
        Assert.All(Cases.Keys, kind => Assert.Contains(kind, refusal.Message));
    }

    /// <summary>A link without a kind is refused by what it is, not as the unknown field the first of its fields would read as.</summary>
    [Theory]
    [MemberData(nameof(Seats))]
    public void ALinkWithoutAKind_IsRefusedWithTheAcceptedKinds(string seat)
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Read(seat, """{"scale":2}"""));

        Assert.Contains($"$.subPlots[0].{SeatCases[seat].Path}", refusal.Message);
        Assert.Contains("no 'type'", refusal.Message);
        Assert.All(Cases.Keys, kind => Assert.Contains(kind, refusal.Message));
    }

    /// <summary>A misspelt field of a kind, and a field of another kind, are refused by name and place like any unknown field of the
    /// document.</summary>
    [Theory]
    [InlineData("""{"type":"linear","scal":1.8}""", "scal")]
    [InlineData("""{"type":"linear","scale":1.8,"ofset":32}""", "ofset")]
    [InlineData("""{"type":"reciprocal","numerater":2}""", "numerater")]
    [InlineData("""{"type":"logarithm","bass":2}""", "bass")]
    [InlineData("""{"type":"power","exponet":2}""", "exponet")]
    [InlineData("""{"type":"linear","scale":2,"exponent":3}""", "exponent")]
    public void AMisspelledOrForeignFieldOfAKind_IsRefusedByName(string link, string field)
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Read("top", link));

        Assert.Contains($"'{field}'", refusal.Message);
        Assert.Contains("$.subPlots[0].secondaryXAxis.link", refusal.Message);
    }

    /// <summary>A number the transform cannot map with is refused as the library refuses it, naming the parameter.</summary>
    [Theory]
    [InlineData("""{"type":"linear","scale":0}""", "scale")]
    [InlineData("""{"type":"reciprocal","numerator":0}""", "numerator")]
    [InlineData("""{"type":"logarithm","base":1}""", "base")]
    [InlineData("""{"type":"power","exponent":0}""", "exponent")]
    public void ANumberTheTransformRefuses_IsRefusedByTheLibrary_NamingTheParameter(string link, string parameter)
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Read("top", link));

        Assert.Contains("The library refused the figure", refusal.Message);
        Assert.Contains(parameter, refusal.Message);
    }

    /// <summary>A linked axis describes what the primary draws in another unit: with no series on the subplot there is nothing for it
    /// to describe, so a spec that holds only a link is refused as nothing to draw, as a legend or a colour bar alone is.</summary>
    [Theory]
    [MemberData(nameof(Seats))]
    public void ASpecWhoseOnlyContentIsALinkedAxis_IsRefusedAsNothingToDraw(string seat)
    {
        string spec = $$"""{"width":400,"height":300,"subPlots":[{{{SeatCases[seat].Subplot.Replace("LINK", Cases["linear"].Json)}}}]}""";

        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(spec)));

        Assert.Contains("Nothing to draw", refusal.Message);
    }

    [Fact]
    public void TheVocabulary_AcceptsEveryKindOfTheFamily_InTheSpellingTheSerializerReads()
    {
        var rule = new SpecVocabulary().RuleFor(SpecNodeKind.AxisLink, "type", seriesType: null);

        Assert.NotNull(rule);
        Assert.Equal(Cases.Keys.Order(StringComparer.Ordinal), rule.Accepted.Order(StringComparer.Ordinal));
        Assert.Equal("logarithm", rule.Canonical("LogArithm"));
        Assert.Null(rule.Canonical("log"));
    }

    // ---- what the schema tells an agent ---------------------------------------------------------------------------------

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
        var lines = SectionLines("Axis link kinds");
        int at = Array.FindIndex(lines, line => line.StartsWith($"  {kind} ", StringComparison.Ordinal));
        Assert.True(at >= 0, $"The schema lists no axis link kind '{kind}'.");
        return [.. lines.Skip(at + 1).TakeWhile(line => line.StartsWith("    ", StringComparison.Ordinal)).Select(FieldName)];
    }

    [Theory]
    [InlineData("linear", "scale,offset")]
    [InlineData("reciprocal", "numerator")]
    [InlineData("logarithm", "base")]
    [InlineData("power", "exponent")]
    public void TheSchema_NamesEveryKindOfLink_WithTheFieldsThatKindTakes(string kind, string fields) =>
        Assert.Equal(fields.Split(','), FieldsListedUnder(kind));

    [Fact]
    public void TheSchema_ListsNoKindOfLinkTheTransformFamilyDoesNotDeclare()
    {
        var kinds = SectionLines("Axis link kinds").Where(line => !line.StartsWith("    ", StringComparison.Ordinal)).Select(line => line.Trim().Split(' ')[0]);

        Assert.Equal(Cases.Keys.Order(StringComparer.Ordinal), kinds.Order(StringComparer.Ordinal));
    }

    /// <summary>The heading says what a link is, where it counts and that the axis then holds no series.</summary>
    [Fact]
    public void TheSchema_SaysWhatALinkIsAndWhereItIsWritten()
    {
        string heading = Description.Describe(null).Split(Environment.NewLine).Single(line => line.StartsWith("Axis link kinds", StringComparison.Ordinal));

        Assert.Contains("secondaryXAxis", heading);
        Assert.Contains("secondaryYAxis", heading);
        Assert.Contains("no series", heading);
        Assert.Contains("secondary_xaxis", heading);
    }

    /// <summary>The kinds say what they compute, and the fields say what they scale.</summary>
    [Theory]
    [InlineData("scale", "multiplied")]
    [InlineData("offset", "added")]
    [InlineData("numerator", "divided")]
    [InlineData("base", "logarithm")]
    [InlineData("exponent", "power")]
    public void TheSchema_ExplainsTheFieldsOfALink(string field, string explanation)
    {
        var line = Assert.Single(SectionLines("Axis link kinds"), l => FieldName(l) == field);

        Assert.Contains(explanation, line, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheAxisFields_ListTheLink_AndSayItIsWrittenByKind()
    {
        string schema = Description.Describe(null);
        int at = schema.IndexOf("Axis fields", StringComparison.Ordinal);
        var axisFields = schema[at..].Split(Environment.NewLine).Skip(1).TakeWhile(line => line.Trim().Length > 0).ToArray();

        var line = Assert.Single(axisFields, l => l.TrimStart().StartsWith("link: object", StringComparison.Ordinal));

        Assert.Contains("type", line);
        Assert.Contains("link kind", line);
        Assert.Contains("secondaryXAxis", line);
        Assert.Contains("range", line, StringComparison.OrdinalIgnoreCase);
    }
}
