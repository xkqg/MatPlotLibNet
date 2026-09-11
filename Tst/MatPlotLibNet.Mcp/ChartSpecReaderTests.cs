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
        var refusal = Assert.Throws<ToolRefusalException>(() =>
            Reader.Read(new ChartSpec(Spec("""{"type":"sankey"}"""))));

        Assert.Contains("'sankey'", refusal.Message);
        Assert.Contains("cannot", refusal.Message);
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
    public void AMisspelledSeriesEnumValue_IsRefusedWithTheAcceptedValues(string series, string wrong, string accepted)
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(Spec(series))));

        Assert.Contains($"'{wrong}'", refusal.Message);
        Assert.Contains(accepted, refusal.Message);
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

        Assert.Contains("subPlots[0].series[0]", refusal.Message);
        Assert.Contains("xData", refusal.Message);
        Assert.Contains("yData", refusal.Message);
        Assert.Contains("3", refusal.Message);
        Assert.Contains("1", refusal.Message);
    }

    [Fact]
    public void AnAbsentSeriesArray_IsRefused_NamingTheMissingField()
    {
        // The library substitutes an empty array and then throws "Array lengths must match. Got 3 and 0." with
        // no field name; the reader says which array is missing before the library ever sees the document.
        var refusal = Assert.Throws<ToolRefusalException>(() =>
            Reader.Read(new ChartSpec(Spec("""{"type":"line","xData":[1,2,3]}"""))));

        Assert.Contains("yData", refusal.Message);
        Assert.Contains("subPlots[0].series[0]", refusal.Message);
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
    public void ASecondarySeriesOfAnUnsupportedType_IsRefused_WithTheSupportedTypes()
    {
        // Core routes secondary-axis series through a switch that ends in "_ => null" with no diagnostic:
        // a bar on the secondary axis vanishes without a word. The reader refuses it by name.
        var refusal = Assert.Throws<ToolRefusalException>(() =>
            Reader.Read(new ChartSpec(Spec(Line,
                axesExtras: ""","secondaryYAxis":{},"secondarySeries":[{"type":"bar","categories":["a"],"values":[1]}]"""))));

        Assert.Contains("'bar'", refusal.Message);
        Assert.Contains("secondary", refusal.Message);
        Assert.Contains("'line'", refusal.Message);
    }

    [Fact]
    public void ASecondaryLineSeries_IsAccepted()
    {
        var figure = Reader.Read(new ChartSpec(Spec(Line,
            axesExtras: ""","secondaryYAxis":{},"secondarySeries":[{"type":"line","xData":[1,2],"yData":[5,6]}]""")));

        Assert.NotNull(figure.SubPlots[0].SecondaryYAxis);
    }
}
