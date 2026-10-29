// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>The reader is the one door between a model-authored document and the library's serializer.
/// <c>ChartSerializer.FromJson</c> is a round-trip reader for its own writer: it drops an unknown series type
/// leniently, skips an unknown property, ignores a misspelled enum value and overwrites the figure defaults
/// with zero when a size is absent. Handed to a model every one of those turns a typo into a blank picture
/// reported as success — so the reader refuses first, and names the field it refused.</summary>
public class ChartSpecReaderTests
{
    private static readonly ChartSpecReader Reader = new(new ChartTypeCatalog(), RenderLimits.Default);

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

    [Fact]
    public void AnUnknownFigureField_IsRefusedByName()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() =>
            Reader.Read(new ChartSpec($$"""{"widht":400,"height":300,"subPlots":[{"series":[{{Line}}]}]}""")));

        Assert.Contains("'widht'", refusal.Message);
    }

    [Fact]
    public void AnUnknownSeriesField_IsRefusedByNameAndPath()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() =>
            Reader.Read(new ChartSpec(Spec("""{"type":"line","x":[1,2],"y":[1,2]}"""))));

        Assert.Contains("'x'", refusal.Message);
        Assert.Contains("subPlots[0].series[0]", refusal.Message);
    }

    [Fact]
    public void AnUnknownSeriesType_IsRefusedByName_WithTheNearestListedType()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() =>
            Reader.Read(new ChartSpec(Spec("""{"type":"lien","xData":[1,2],"yData":[1,2]}"""))));

        Assert.Contains("'lien'", refusal.Message);
        Assert.Contains("'line'", refusal.Message);
    }

    [Fact]
    public void ASeriesTypeInTheWrongCase_IsRefused_WithTheExactName()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() =>
            Reader.Read(new ChartSpec(Spec("""{"type":"Line","xData":[1,2],"yData":[1,2]}"""))));

        Assert.Contains("'Line'", refusal.Message);
        Assert.Contains("'line'", refusal.Message);
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

    [Fact]
    public void AnUnknownColour_IsRefusedByNameAndPath()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() =>
            Reader.Read(new ChartSpec(Spec("""{"type":"line","xData":[1,2],"yData":[1,2],"color":"reddish"}"""))));

        Assert.Contains("'reddish'", refusal.Message);
        Assert.Contains("subPlots[0].series[0].color", refusal.Message);
    }

    [Fact]
    public void MismatchedArrayLengths_AreRefused_NamingTheSeriesAndTheLengths()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() =>
            Reader.Read(new ChartSpec(Spec("""{"type":"line","xData":[1,2,3],"yData":[1]}"""))));

        Assert.Equal("The 'line' series at $.subPlots[0].series[0] was refused: Array lengths must match. Got 3 and 1.", refusal.Message);
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
        Assert.Contains("Array lengths must match", refusal.Message);
        Assert.Contains("'surface'", refusal.Message);
    }

    [Fact]
    public void AnAbsentSeriesArray_IsRefused_NamingTheSeriesAndTheLengths()
    {
        // The library substitutes an empty array for the absent half and refuses the pair; the reader adds the place.
        var refusal = Assert.Throws<ToolRefusalException>(() =>
            Reader.Read(new ChartSpec(Spec("""{"type":"line","xData":[1,2,3]}"""))));

        Assert.Equal("The 'line' series at $.subPlots[0].series[0] was refused: Array lengths must match. Got 3 and 0.", refusal.Message);
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

        Assert.StartsWith("The 'stackedarea' series at $.subPlots[0].series[0] was refused: Array lengths must match.", refusal.Message);
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

    /// <summary>Only a type whose reader derives the other half may leave it out: a line with y values alone is
    /// still refused, naming the series and the lengths.</summary>
    [Fact]
    public void ALineWithYDataAlone_IsStillRefused_NamingTheSeriesAndTheLengths()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() =>
            Reader.Read(new ChartSpec(Spec("""{"type":"line","yData":[1,2,3]}"""))));

        Assert.Equal("The 'line' series at $.subPlots[0].series[0] was refused: Array lengths must match. Got 0 and 3.", refusal.Message);
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
}
