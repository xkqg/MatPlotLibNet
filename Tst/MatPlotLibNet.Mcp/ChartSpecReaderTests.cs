// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using static MatPlotLibNet.Mcp.Tests.SpecFixtures;
using System.Text.Json.Nodes;

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>The reader is the one door between a model-authored document and the library's serializer.
/// <c>ChartSerializer.FromJson</c> is a round-trip reader for its own writer: it drops an unknown series type
/// leniently, skips an unknown property, ignores a misspelled enum value and overwrites the figure defaults
/// with zero when a size is absent. Handed to a model every one of those turns a typo into a blank picture
/// reported as success — so the reader refuses first, and names the field it refused.</summary>
public class ChartSpecReaderTests
{

    private const string Line = """{"type":"line","xData":[1,2,3],"yData":[2,4,3]}""";

    private static string Spec(string series, string figureExtras = "", string axesExtras = "") =>
        $$"""{"width":400,"height":300{{figureExtras}},"subPlots":[{"series":[{{series}}]{{axesExtras}}}]}""";

    [Fact]
    public void AFigureSpec_BecomesAFigureWithItsTitle()
    {
        var figure = Reader.Read(new ChartSpec(Spec(Line, figureExtras: ""","title":"Revenue" """.Trim())));

        Assert.Equal("Revenue", figure.Title);
        Assert.Single(figure.SubPlots);
        Assert.IsType<LineSeries>(Assert.Single(figure.SubPlots[0].Series));
    }

    [Fact]
    public void ASpecWithTwoSubPlots_KeepsBothAxes()
    {
        var figure = Reader.Read(new ChartSpec(
            $$"""{"width":400,"height":300,"subPlots":[{"series":[{{Line}}]},{"series":[{{Line}}]}]}"""));

        Assert.Equal(2, figure.SubPlots.Count);
    }

    [Fact]
    public void MinimalSpecWithoutWidthOrHeight_TakesTheLibraryDefaults()
    {
        var figure = Reader.Read(new ChartSpec($$"""{"subPlots":[{"series":[{{Line}}]}]}"""));

        Assert.Equal(800, figure.Width);
        Assert.Equal(600, figure.Height);
        Assert.Equal(96, figure.Dpi);
    }

    [Fact]
    public void MalformedJson_IsRefusedAsNotJson()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec("not json")));

        Assert.Contains("JSON", refusal.Message);
    }

    [Theory]
    [InlineData("""{"width":400,"height":300}""")]
    [InlineData("""{"width":400,"height":300,"subPlots":[{}]}""")]
    [InlineData("""{"width":400,"height":300,"subPlots":[{"series":[]}]}""")]
    [InlineData("{}")]
    public void ASpecWithNoSeries_IsRefusedAsNothingToDraw(string json)
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(json)));

        Assert.Contains("series", refusal.Message);
    }

    /// <summary>A series the reader cannot take is refused with the fault named and then either its place in the document or what to read
    /// instead: an unknown field, an unknown type with the nearest listed one, a type in the wrong case with its exact name, an unknown colour.</summary>
    [Theory]
    [InlineData("""{"type":"line","x":[1,2],"y":[1,2]}""", "'x'", "subPlots[0].series[0]")]
    [InlineData("""{"type":"lien","xData":[1,2],"yData":[1,2]}""", "'lien'", "'line'")]
    [InlineData("""{"type":"Line","xData":[1,2],"yData":[1,2]}""", "'Line'", "'line'")]
    [InlineData("""{"type":"line","xData":[1,2],"yData":[1,2],"color":"reddish"}""", "'reddish'", "subPlots[0].series[0].color")]
    public void AFaultInASeries_IsRefused_NamingTheFaultAndItsPlaceOrItsRemedy(string series, string fault, string placeOrRemedy)
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(Spec(series))));

        Assert.Contains(fault, refusal.Message);
        Assert.Contains(placeOrRemedy, refusal.Message);
    }

    /// <summary>The library substitutes an empty array for the absent half and refuses the pair, and the reader adds the place. Only a type
    /// whose reader derives the other half may leave it out: a line with y values alone is refused too.</summary>
    [Theory]
    [InlineData("""{"type":"line","xData":[1,2,3],"yData":[1]}""", 3, 1)]
    [InlineData("""{"type":"line","xData":[1,2,3]}""", 3, 0)]
    [InlineData("""{"type":"line","yData":[1,2,3]}""", 0, 3)]
    public void AnUnevenPairOfArrays_IsRefused_NamingTheSeriesAndTheLengths(string series, int x, int y)
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(Spec(series))));

        Assert.Equal($"The 'line' series at $.subPlots[0].series[0] was refused: {x.RefusalAgainst(y)}", refusal.Message);
    }

    [Fact]
    public void AnUnknownFigureField_IsRefusedByName()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() =>
            Reader.Read(new ChartSpec($$"""{"widht":400,"height":300,"subPlots":[{"series":[{{Line}}]}]}""")));

        Assert.Contains("'widht'", refusal.Message);
    }

    [Fact]
    public void ATypeThatCannotBeBuiltFromJson_IsRefusedWithTheReason()
    {
        // No registered type is excluded-with-reason today (JSON-roundtrip plan emptied the set) — a fake one
        // tests the refusal plumbing itself, independent of which real type is excluded on any given day.
        var catalog = new ChartTypeCatalog(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["line"] = "its JSON reader builds a placeholder instead of the data in the document, and cannot yet",
        });
        var reader = new ChartSpecReader(catalog, RenderLimits.Default);

        var refusal = Assert.Throws<ToolRefusalException>(() =>
            reader.Read(new ChartSpec(Spec("""{"type":"line"}"""))));

        Assert.Contains("'line'", refusal.Message);
        Assert.Contains("cannot", refusal.Message);
        Assert.Contains("placeholder", refusal.Message);
    }

    [Theory]
    [InlineData(""","yAxis":{"scale":"logarithmic"}""", "logarithmic", "Log")]
    [InlineData(""","referenceLines":[{"value":1,"lineStyle":"dotty"}]""", "dotty", "Dotted")]
    public void AMisspelledAxesEnumValue_IsRefusedWithTheAcceptedValues(string axesExtras, string wrong, string accepted)
    {
        var refusal = Assert.Throws<ToolRefusalException>(() =>
            Reader.Read(new ChartSpec(Spec(Line, axesExtras: axesExtras))));

        Assert.Contains($"'{wrong}'", refusal.Message);
        Assert.Contains(accepted, refusal.Message);
    }

    [Theory]
    [InlineData("""{"type":"line","xData":[1,2],"yData":[1,2],"lineStyle":"dotty"}""", "dotty", "Dotted")]
    [InlineData("""{"type":"bar","categories":["a","b"],"values":[1,2],"orientation":"diagonal"}""", "diagonal", "Horizontal")]
    [InlineData("""{"type":"step","xData":[1,2],"yData":[1,2],"stepPosition":"middle"}""", "middle", "Mid")]
    [InlineData("""{"type":"line","xData":[1,2],"yData":[1,2],"labelPlacement":"end"}""", "end", "LineEnd")]
    [InlineData("""{"type":"scatter","xData":[1,2],"yData":[1,2],"asksAs":"lines"}""", "lines", "FillBetween")]
    public void AMisspelledSeriesEnumValue_IsRefusedWithTheAcceptedValues(string series, string wrong, string accepted)
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(Spec(series))));

        Assert.Contains($"'{wrong}'", refusal.Message);
        Assert.Contains(accepted, refusal.Message);
    }

    [Fact]
    public void AMisspelledPlacementOnASecondaryLine_IsRefusedByPath_AndNamesTheField()
    {
        const string axesExtras = ""","secondaryYAxis":{},"secondarySeries":[{"type":"line","xData":[1,2],"yData":[1,2],"labelPlacement":"lineEnnd"}]""";

        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(Spec(Line, axesExtras: axesExtras))));

        Assert.Contains("'lineEnnd'", refusal.Message);
        Assert.Contains("subPlots[0].secondarySeries[0].labelPlacement", refusal.Message);
        Assert.Contains("Legend", refusal.Message);
    }

    [Theory]
    [InlineData("lineEnd")]
    [InlineData("LINEEND")]
    public void AnAcceptedPlacement_ReachesTheLineOfTheFigure_OnBothAxes(string written)
    {
        string axesExtras = $$""","secondaryYAxis":{},"secondarySeries":[{"type":"line","xData":[1,2],"yData":[1,2],"labelPlacement":"{{written}}"}]""";
        string primary = $$"""{"type":"line","label":"a","xData":[1,2,3],"yData":[2,4,3],"labelPlacement":"{{written}}"}""";

        var axes = Reader.Read(new ChartSpec(Spec(primary, axesExtras: axesExtras))).SubPlots[0];

        Assert.Equal(SeriesLabelPlacement.LineEnd, ((LineSeries)axes.Series[0]).LabelPlacement);
        Assert.Equal(SeriesLabelPlacement.LineEnd, ((LineSeries)axes.YAxes.Right[0].Series[0]).LabelPlacement);
    }

    [Theory]
    [InlineData("red", "#FF0000")]
    [InlineData("CornflowerBlue", "#6495ED")]
    [InlineData("#f00", "#FF0000")]
    [InlineData("#00ff00", "#00FF00")]
    public void AColourName_OrAShortHex_IsAcceptedAndNormalised(string written, string hex)
    {
        var figure = Reader.Read(new ChartSpec(Spec($$"""{"type":"line","xData":[1,2],"yData":[1,2],"color":"{{written}}"}""")));

        var series = Assert.IsType<LineSeries>(figure.SubPlots[0].Series[0]);
        Assert.Equal(Color.FromHex(hex), series.Color);
    }

    [Fact]
    public void AFigureBackgroundColourName_IsNormalisedToo()
    {
        var figure = Reader.Read(new ChartSpec(Spec(Line, figureExtras: ""","backgroundColor":"white" """.Trim())));

        Assert.Equal(Color.FromHex("#FFFFFF"), figure.BackgroundColor);
    }

    /// <summary>The library's own rule, named by the place the series stands: a grid whose rows are not one per y value is
    /// refused while the document is read, with the JSON path of the series and the library's wording of why — the reader
    /// has no rule of its own for it, so the library never sees a document it would only fail to draw.</summary>
    [Fact]
    public void AGridThatDoesNotFitItsAxes_IsRefused_NamingTheSeriesAndWhy()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() =>
            Reader.Read(new ChartSpec(Spec("""{"type":"surface","xData":[0,1,2],"yData":[0,1,2],"zGridData":[[1,2,3],[4,5,6]]}"""))));

        Assert.Contains("$.subPlots[0].series[0]", refusal.Message);
        Assert.Contains(LengthRefusals.Opening, refusal.Message);
        Assert.Contains("'surface'", refusal.Message);
    }

    /// <summary>A stacked area has x values and its layers but no y values: the library writes exactly this, and the server
    /// lists the type. A reader that wanted a 'yData' beside every 'xData' turned away the document <c>ToJson</c> produces.</summary>
    [Fact]
    public void AStackedArea_IsRead_ItHasXValuesAndLayersButNoYValues()
    {
        var figure = Reader.Read(new ChartSpec(Spec("""{"type":"stackedarea","xData":[1,2],"datasets":[[1,2],[3,4]]}""")));

        var area = Assert.IsType<StackedAreaSeries>(figure.SubPlots[0].Series[0]);
        Assert.Equal([1.0, 2.0], area.X);
        Assert.Equal(2, area.YSets.Length);
    }

    [Fact]
    public void AStackedAreaWithoutXValues_IsRefused_NamingTheSeries()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() =>
            Reader.Read(new ChartSpec(Spec("""{"type":"stackedarea","datasets":[[1,2],[3,4]]}"""))));

        Assert.StartsWith($"The 'stackedarea' series at $.subPlots[0].series[0] was refused: {LengthRefusals.Opening}", refusal.Message);
    }

    /// <summary>A signal given x values and no y values has no samples: it reads as the empty signal it is, the way any
    /// series with no data reads, and does not crash the reader.</summary>
    [Fact]
    public void ASignalWithXDataAlone_ReadsAsAnEmptySignal_NotACrash()
    {
        var figure = Reader.Read(new ChartSpec(Spec("""{"type":"signal","xData":[1,2,3]}""")));

        var signal = Assert.IsType<SignalSeries>(figure.SubPlots[0].Series[0]);
        Assert.Empty(signal.YData);
    }

    /// <summary>A signal is sampled at a rate, so its x values follow from the rate and its JSON carries the y values
    /// alone: the library writes exactly this, and the server lists the type. Refusing it for a missing 'xData' would
    /// turn away the very document <c>ToJson</c> produces.</summary>
    [Fact]
    public void ASignalWithYDataAlone_IsRead_ItsXValuesFollowFromTheSampleRate()
    {
        var figure = Reader.Read(new ChartSpec(Spec("""{"type":"signal","yData":[1,2,3],"signalSampleRate":10}""")));

        var signal = Assert.IsType<SignalSeries>(figure.SubPlots[0].Series[0]);
        Assert.Equal(10, signal.SampleRate);
        Assert.Equal([1.0, 2.0, 3.0], signal.YData);
    }

    [Theory]
    [InlineData(""" "width":-10,"height":300 """, "width")]
    [InlineData(""" "width":0,"height":300 """, "width")]
    [InlineData(""" "width":400,"height":100000 """, "height")]
    public void AnExplicitlyBadSize_IsRefused_WithTheField(string sizes, string field)
    {
        var refusal = Assert.Throws<ToolRefusalException>(() =>
            Reader.Read(new ChartSpec($$"""{{{sizes.Trim()}},"subPlots":[{"series":[{{Line}}]}]}""")));

        Assert.Contains(field, refusal.Message);
    }

    [Fact]
    public void AnOversizeCanvas_IsRefused_WithTheCeiling()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() =>
            Reader.Read(new ChartSpec($$"""{"width":16000,"height":16000,"subPlots":[{"series":[{{Line}}]}]}""")));

        Assert.Contains(RenderLimits.Default.MaxWidth.ToString(), refusal.Message);
    }

    [Fact]
    public void ATitleLongerThanTheLimit_IsRefused_WithTheLimit()
    {
        string title = new('A', RenderLimits.Default.MaxTextLength + 1);
        var refusal = Assert.Throws<ToolRefusalException>(() =>
            Reader.Read(new ChartSpec(Spec(Line, figureExtras: $$""","title":"{{title}}" """.Trim()))));

        Assert.Contains("title", refusal.Message);
        Assert.Contains(RenderLimits.Default.MaxTextLength.ToString(), refusal.Message);
    }

    [Fact]
    public void ASecondaryBarSeries_IsNowAccepted()
    {
        // Was refused: core routed secondary-axis series through a switch that ended in "_ => null" with no
        // diagnostic, so a bar on the secondary axis vanished without a word (ROADMAP.md row 99). Fixed: the
        // secondary restore path now goes through SeriesRegistry like the primary one, so the reader accepts
        // whatever the catalog lists there too, same as on the primary axis.
        var figure = Reader.Read(new ChartSpec(Spec(Line,
            axesExtras: ""","secondaryYAxis":{},"secondarySeries":[{"type":"bar","categories":["a"],"values":[1]}]""")));

        Assert.NotEmpty(figure.SubPlots[0].YAxes.Right);
        Assert.IsType<BarSeries>(Assert.Single(figure.SubPlots[0].YAxes.Right[0].Series));
    }

    [Fact]
    public void ASecondarySeriesOfAnUnlistedType_IsRefused_WithTheNearestListedType()
    {
        // A truly unknown/misspelled type is still refused on the secondary axis, by the SAME catalog check
        // (and the same message) the primary axis uses — not a separate, narrower allowlist.
        var refusal = Assert.Throws<ToolRefusalException>(() =>
            Reader.Read(new ChartSpec(Spec(Line,
                axesExtras: ""","secondaryYAxis":{},"secondarySeries":[{"type":"lien","xData":[1,2],"yData":[1,2]}]"""))));

        Assert.Contains("'lien'", refusal.Message);
        Assert.Contains("'line'", refusal.Message);
    }

    [Fact]
    public void ASecondaryLineSeries_IsAccepted()
    {
        var figure = Reader.Read(new ChartSpec(Spec(Line,
            axesExtras: ""","secondaryYAxis":{},"secondarySeries":[{"type":"line","xData":[1,2],"yData":[5,6]}]""")));

        Assert.NotEmpty(figure.SubPlots[0].YAxes.Right);
    }

    /// <summary>NaN is the library's missing value; a spec states one the way the serializer writes it.</summary>
    [Fact]
    public void AMissingValue_WrittenAsTheSerializerWritesIt_IsAccepted_AndStaysMissing()
    {
        var figure = Reader.Read(new ChartSpec(Spec("""{"type":"line","xData":[1,2,3],"yData":[2,"NaN",3]}""")));

        var line = Assert.IsType<LineSeries>(Assert.Single(figure.SubPlots[0].Series));
        Assert.True(double.IsNaN(line.YData[1]));
    }

    /// <summary>The reader reads with the serializer's own options plus its one refusal: every number the serializer
    /// writes, an infinity included, is one the reader takes.</summary>
    [Fact]
    public void EveryNumberTheSerializerWrites_IsOneTheReaderTakes()
    {
        string json = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1, 2], [double.PositiveInfinity, double.NaN, double.NegativeInfinity])).ToJson();

        var line = (LineSeries)Reader.Read(new ChartSpec(json)).SubPlots[0].Series[0];

        Assert.Equal(double.PositiveInfinity, line.YData[0]);
        Assert.True(double.IsNaN(line.YData[1]));
        Assert.Equal(double.NegativeInfinity, line.YData[2]);
    }

    /// <summary>An item of a number list that is not a number is refused at its own index, so a model can find it: the
    /// reader's strict pass reads lists as System.Text.Json does, which tracks the index (the serializer's faster list
    /// reader cannot know it).</summary>
    [Theory]
    [InlineData("""[2,"four",3]""")]
    [InlineData("[2,null,3]")]
    public void ANumberListItemThatIsNotANumber_IsRefusedAtItsIndex(string yData)
    {
        var refusal = Assert.Throws<ToolRefusalException>(() =>
            Reader.Read(new ChartSpec(Spec($$"""{"type":"line","xData":[1,2,3],"yData":{{yData}}}"""))));

        Assert.Contains("$.subPlots[0].series[0].yData[1]", refusal.Message);
    }

    // ── The colour spelling: a CSS4 name becomes the hex the serializer reads, a hex of three, four, six or eight digits is kept as written (core's Color.FromHex reads all four), wherever the colour sits, and anything else is refused with the path it stands at ──

    private static JsonNode Normalised(string json, string path = "$")
    {
        var root = JsonNode.Parse(json)!;
        ChartSpecReader.NormaliseColours(root, path);
        return root;
    }

    [Theory]
    [InlineData("red", "#FF0000")]
    [InlineData("CornflowerBlue", "#6495ED")]
    [InlineData("#f00", "#F00")]
    [InlineData("f00", "#F00")]
    [InlineData("#f008", "#F008")]
    [InlineData("#00ff00", "#00FF00")]
    [InlineData("00ff00", "#00FF00")]
    [InlineData("#aabbccdd", "#AABBCCDD")]
    public void AColour_IsSpeltAsTheSerializerReadsIt(string written, string spelt)
    {
        var root = Normalised($$"""{"color":"{{written}}"}""");

        Assert.Equal(spelt, (string?)root["color"]);
    }

    [Theory]
    [InlineData("color")]
    [InlineData("edgeColor")]
    [InlineData("faceColor")]
    [InlineData("background")]
    [InlineData("foregroundText")]
    [InlineData("axesBackground")]
    public void AFieldThatHoldsOneColour_IsNormalised(string field)
    {
        var root = Normalised($$"""{"{{field}}":"white"}""");

        Assert.Equal("#FFFFFF", (string?)root[field]);
    }

    [Theory]
    [InlineData("stateSegmentColors")]
    [InlineData("bandColors")]
    [InlineData("colors")]
    [InlineData("edgeColors")]
    [InlineData("cycleColors")]
    public void AFieldThatHoldsAListOfColours_IsNormalisedEntryByEntry(string field)
    {
        var root = Normalised($$"""{"{{field}}":["red","#0f0",7]}""");

        var list = Assert.IsType<JsonArray>(root[field]);
        Assert.Equal("#FF0000", (string?)list[0]);
        Assert.Equal("#0F0", (string?)list[1]);
        Assert.Equal(7, (int?)list[2]);
    }

    [Fact]
    public void EveryFieldOfAThemesAlarmPalette_IsAColour()
    {
        var root = Normalised("""{"theme":{"alarm":{"critical":"red","warning":"#fa0"},"other":{"critical":"red"}}}""");

        var alarm = root["theme"]!["alarm"]!;
        Assert.Equal("#FF0000", (string?)alarm["critical"]);
        Assert.Equal("#FA0", (string?)alarm["warning"]);
        Assert.Equal("red", (string?)root["theme"]!["other"]!["critical"]);
    }

    [Fact]
    public void AColourSittingInAnArrayOfObjects_IsReachedThroughTheArray()
    {
        var root = Normalised("""{"subPlots":[{"series":[{"type":"line","color":"red"},{"type":"line","color":"blue"}]}]}""");

        var series = root["subPlots"]![0]!["series"]!;
        Assert.Equal("#FF0000", (string?)series[0]!["color"]);
        Assert.Equal("#0000FF", (string?)series[1]!["color"]);
    }

    [Fact]
    public void AnArrayDirectlyInAnArray_IsWalkedToo()
    {
        var root = Normalised("""{"grid":[[{"color":"red"}]]}""");

        Assert.Equal("#FF0000", (string?)root["grid"]![0]![0]!["color"]);
    }

    [Fact]
    public void AFieldThatIsNotAColour_KeepsItsWords()
    {
        var root = Normalised("""{"title":"red","label":"#f00","colorMap":"viridis","colorful":"red"}""");

        Assert.Equal("red", (string?)root["title"]);
        Assert.Equal("#f00", (string?)root["label"]);
        Assert.Equal("viridis", (string?)root["colorMap"]);
        Assert.Equal("red", (string?)root["colorful"]);
    }

    [Fact]
    public void AColourFieldThatHoldsAnotherKindOfValue_IsLeftForTheStrictPass()
    {
        var root = Normalised("""{"color":7,"edgeColor":null,"background":true,"colors":"red","faceColor":{"color":"red"}}""");

        Assert.Equal(7, (int?)root["color"]);
        Assert.Null(root["edgeColor"]);
        Assert.True((bool?)root["background"]);
        Assert.Equal("red", (string?)root["colors"]);
        Assert.Equal("#FF0000", (string?)root["faceColor"]!["color"]);
    }

    [Fact]
    public void ANodeThatIsNotAnObjectOrAnArray_IsLeftAlone()
    {
        JsonNode? nothing = null;
        ChartSpecReader.NormaliseColours(nothing, "$");
        var text = JsonValue.Create("red")!;
        ChartSpecReader.NormaliseColours(text, "$");

        Assert.Null(nothing);
        Assert.Equal("red", (string?)text);
    }

    [Theory]
    [InlineData("reddish")]
    [InlineData("#12")]
    [InlineData("#12345")]
    [InlineData("#1234567")]
    [InlineData("#123456789")]
    [InlineData("#ggg")]
    [InlineData("")]
    public void AColourTheLibraryDoesNotKnow_IsRefusedByNameAndPath(string written)
    {
        var refusal = Assert.Throws<ToolRefusalException>(() =>
            Normalised($$"""{"subPlots":[{"series":[{"color":"{{written}}"}]}]}"""));

        Assert.Equal($"'{written}' at $.subPlots[0].series[0].color is not a colour the library knows: use a CSS4 colour name or #RRGGBB.", refusal.Message);
    }

    [Fact]
    public void ABadEntryOfAColourList_IsRefusedWithItsIndex()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Normalised("""{"bandColors":["red","nope"]}""", path: "$.axes"));

        Assert.Contains("'nope' at $.axes.bandColors[1]", refusal.Message);
    }

    [Fact]
    public void ABadColourInAnAlarmPalette_IsRefusedWithItsField()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Normalised("""{"theme":{"alarm":{"critical":"nope"}}}"""));

        Assert.Contains("'nope' at $.theme.alarm.critical", refusal.Message);
    }
}
