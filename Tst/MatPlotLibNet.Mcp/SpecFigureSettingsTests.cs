// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Skia;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Tests.Serialization;

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>The figure's own settings travel through the spec reader as its axes do: a theme, a spacing, the interaction
/// settings. An agent writes a theme by hand, so its colours may be names and its enum values may be misspelt; the
/// reader takes the first and refuses the second with what it accepts, as it does for every other part.</summary>
[Collection("ChartServicesGlobalState")]
public class SpecFigureSettingsTests
{
    private static readonly ChartSpecReader Reader = new(new ChartTypeCatalog(), RenderLimits.Default);

    /// <summary>A spec with <paramref name="theme"/> (a JSON object) as its theme, and one line.</summary>
    private static string Spec(string theme) =>
        $$"""{"width":400,"height":300,"theme":{{theme}},"subPlots":[{"series":[{"type":"line","xData":[1,2],"yData":[1,2]}]}]}""";

    [Theory]
    [InlineData("""{"defaultFont":{"weight":"heavy"}}""", "heavy", "Bold")]
    [InlineData("""{"defaultFont":{"slant":"slanted"}}""", "slanted", "Oblique")]
    [InlineData("""{"defaultGrid":{"visible":true,"lineStyle":"wavy"}}""", "wavy", "Dotted")]
    [InlineData("""{"defaultGrid":{"visible":true,"axis":"z"}}""", "z", "Both")]
    [InlineData("""{"propCycler":{"lineStyles":["solid","wavy"]}}""", "wavy", "Dashed")]
    [InlineData("""{"propCycler":{"markerStyles":["circle","blob"]}}""", "blob", "Diamond")]
    public void AMisspelledValueInATheme_IsRefusedWithTheAcceptedValues(string theme, string wrong, string accepted)
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(Spec(theme))));

        Assert.Contains($"'{wrong}'", refusal.Message);
        Assert.Contains(accepted, refusal.Message);
    }

    [Fact]
    public void AMisspelledValueInACyclerList_IsRefusedByPath()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(Spec(
            """{"propCycler":{"markerStyles":["circle","blob"]}}"""))));

        Assert.Contains("theme.propCycler.markerStyles[1]", refusal.Message);
    }

    /// <summary>An item that is not a string is left to the strict pass, as a single value of the wrong shape is: it is
    /// refused there by path.</summary>
    [Fact]
    public void ACyclerListItemThatIsNotAString_IsRefusedByPath()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(Spec(
            """{"propCycler":{"markerStyles":["circle",true]}}"""))));

        Assert.Contains("theme.propCycler.markerStyles[1]", refusal.Message);
    }

    [Fact]
    public void AcceptedCyclerValues_ReachTheTheme_InAnyCase()
    {
        var figure = Reader.Read(new ChartSpec(Spec("""{"propCycler":{"lineStyles":["DASHED","dotted"],"markerStyles":["square"]}}""")));

        var cycler = figure.Theme.PropCycler!;
        Assert.Equal(LineStyle.Dashed, cycler[0].LineStyle);
        Assert.Equal(LineStyle.Dotted, cycler[1].LineStyle);
        Assert.Equal(MarkerStyle.Square, cycler[0].MarkerStyle);
    }

    /// <summary>Every colour of a theme takes a CSS4 name or a short hex, wherever it sits: the theme's own grounds, its
    /// cycle, its alarm palette.</summary>
    [Fact]
    public void ColourNamesInATheme_AreNormalised()
    {
        var figure = Reader.Read(new ChartSpec(Spec(
            """{"background":"black","foregroundText":"white","axesBackground":"#333","cycleColors":["red","#00f"],"alarm":{"resting":"grey","warning":"orange","critical":"red","unknown":"silver"}}""")));

        var theme = figure.Theme;
        Assert.Equal(Color.FromHex("#000000"), theme.Background);
        Assert.Equal(Color.FromHex("#FFFFFF"), theme.ForegroundText);
        Assert.Equal(Color.FromHex("#333333"), theme.AxesBackground);
        Assert.Equal([Color.FromHex("#FF0000"), Color.FromHex("#0000FF")], theme.CycleColors);
        Assert.Equal(Color.FromHex("#FFA500"), theme.Alarm.Warning);
        Assert.Equal(Color.FromHex("#C0C0C0"), theme.Alarm.Unknown);
    }

    [Fact]
    public void AnUnknownColourInAThemesCycle_IsRefusedByPath()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(Spec(
            """{"cycleColors":["red","reddish"]}"""))));

        Assert.Contains("'reddish'", refusal.Message);
        Assert.Contains("theme.cycleColors[1]", refusal.Message);
    }

    /// <summary>A built-in theme travels as the data it is: what a figure under it writes, the reader reads back to the
    /// same theme.</summary>
    [Fact]
    public void ABuiltInThemeWrittenByAFigure_PassesTheSpecReader()
    {
        var figure = Plt.Create().WithTheme(Theme.Ggplot).Plot([1.0, 2], [1.0, 2]).Build();

        var read = Reader.Read(new ChartSpec(figure.ToJson()));

        Assert.Equal(figure.ToSvg(), read.ToSvg());
    }

    [Fact]
    public void TheSchema_ListsTheThemeAndSpacingFields()
    {
        string schema = new ChartSchemaDescription(new ChartTypeCatalog()).Describe(null);

        foreach (var (section, field) in new[]
                 {
                     ("Theme fields", "cycleColors: string (colour)[]"),
                     ("Theme fields", "propCycler: object"),
                     ("Spacing fields", "tightLayout: boolean"),
                     ("Interaction theme fields", "highlightOpacity: number"),
                 })
        {
            int at = schema.IndexOf(section, StringComparison.Ordinal);
            Assert.True(at >= 0, $"'{section}' is not in the schema.");
            Assert.Contains(field, schema[at..]);
        }

        Assert.Contains(schema.Split(Environment.NewLine), line => line.TrimStart().StartsWith("theme:", StringComparison.Ordinal) && line.Contains("default", StringComparison.Ordinal));
    }

    /// <summary>The fixture this project links is the core contract's: its theme rows reach this reader too.</summary>
    [Fact]
    public void TheLinkedContract_HoldsTheFigureAndItsTheme()
    {
        Assert.Contains(DrawnAxesFixtures.Parts, part => part.Name == "Figure");
        Assert.Contains(DrawnAxesFixtures.Parts, part => part.Name == "Theme");
    }

    // ---- what the figure draws on itself: texts, legend, images ---------------------------------------------------

    /// <summary>A spec with the figure-level <paramref name="fields"/> (JSON members, comma separated) and one line.</summary>
    private static string FigureSpec(string fields) =>
        $$"""{"width":400,"height":300,{{fields}},"subPlots":[{"series":[{"type":"line","xData":[1,2],"yData":[1,2]}]}]}""";

    /// <summary>The fixture this project links holds the figure's texts, legend and images too.</summary>
    [Fact]
    public void TheLinkedContract_HoldsTheFiguresLegendTextsAndImages()
    {
        Assert.Contains(DrawnAxesFixtures.Parts, part => part.Name == "FigureLegend");
        Assert.Contains(DrawnAxesFixtures.Parts, part => part.Name == "FigureText");
        Assert.Contains(DrawnAxesFixtures.Parts, part => part.Name == "FigureImage");
        Assert.NotEmpty(DrawnAxesFixtures.FigureLists);
    }

    /// <summary>Every value of every item of the figure's lists, the ones a text and an image are made with included (their
    /// words, places and pixels), passes the reader as written; the rows of the settable properties are the axes parts'.</summary>
    [Theory]
    [MemberData(nameof(DrawnAxesFixtures.FigureListValueKeys), MemberType = typeof(DrawnAxesFixtures))]
    public void AFigureListValue_PassesTheSpecReader_AsItWasWritten(string key)
    {
        var figure = DrawnAxesFixtures.Build(everyPropertySet: true);
        object? written = DrawnAxesFixtures.FigureListValues(figure)[key];

        var read = DrawnAxesFixtures.FigureListValues(Reader.Read(new ChartSpec(figure.ToJson())));

        Assert.True(read.ContainsKey(key), $"Figure.{key}: written as {DrawnSeriesFixtures.Show(written)}, and nothing of it came through the spec reader.");
        Assert.True(DrawnSeriesFixtures.SameValue(written, read[key]),
            $"Figure.{key}: written as {DrawnSeriesFixtures.Show(written)}, read back through the spec reader as {DrawnSeriesFixtures.Show(read[key])}.");
    }

    [Fact]
    public void AFigureText_PassesTheSpecReader_WithColourNamesAndEnumValuesInAnyCase()
    {
        var figure = Reader.Read(new ChartSpec(FigureSpec(
            """
            "texts":[{"text":"Source: ECB","x":0.05,"y":0.04,"color":"red","alignment":"CENTER","verticalAlignment":"top","rotation":15,"font":{"weight":"BOLD","slant":"italic","color":"blue"}}]
            """)));

        var text = Assert.Single(figure.Texts);
        Assert.Equal("Source: ECB", text.Text);
        Assert.Equal(0.05, text.X);
        Assert.Equal(0.04, text.Y);
        Assert.Equal(Color.FromHex("#FF0000"), text.Color);
        Assert.Equal(MatPlotLibNet.Rendering.TextAlignment.Center, text.Alignment);
        Assert.Equal(MatPlotLibNet.Rendering.TextVerticalAlignment.Top, text.VerticalAlignment);
        Assert.Equal(15, text.Rotation);
        Assert.Equal(FontWeight.Bold, text.Font!.Weight);
        Assert.Equal(FontSlant.Italic, text.Font.Slant);
        Assert.Equal(Color.FromHex("#0000FF"), text.Font.Color);
    }

    [Theory]
    [InlineData("""{"text":"a","x":0.5,"y":0.5,"alignment":"middle"}""", "middle", "Center", "$.texts[0].alignment")]
    [InlineData("""{"text":"a","x":0.5,"y":0.5,"verticalAlignment":"middle"}""", "middle", "Baseline", "$.texts[0].verticalAlignment")]
    [InlineData("""{"text":"a","x":0.5,"y":0.5,"font":{"weight":"heavy"}}""", "heavy", "Bold", "$.texts[0].font.weight")]
    [InlineData("""{"text":"a","x":0.5,"y":0.5,"font":{"slant":"slanted"}}""", "slanted", "Oblique", "$.texts[0].font.slant")]
    public void AMisspelledValueOfAFigureText_IsRefusedWithTheAcceptedValues(string text, string wrong, string accepted, string path)
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(FigureSpec($$"""
            "texts":[{{text}}]
            """))));

        Assert.Contains($"'{wrong}'", refusal.Message);
        Assert.Contains(accepted, refusal.Message);
        Assert.Contains(path, refusal.Message);
    }

    [Fact]
    public void AnUnknownColourInAFigureText_IsRefusedByPath()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(FigureSpec(
            """
            "texts":[{"text":"a","x":0.5,"y":0.5,"color":"reddish"}]
            """))));

        Assert.Contains("'reddish'", refusal.Message);
        Assert.Contains("$.texts[0].color", refusal.Message);
    }

    [Fact]
    public void AFigureTextThatIsTooLong_IsRefusedByPath()
    {
        string longText = new('x', RenderLimits.Default.MaxTextLength + 1);

        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(FigureSpec(
            $$"""
            "texts":[{"text":"{{longText}}","x":0.5,"y":0.5}]
            """))));

        Assert.Contains("'text' at $.texts[0]", refusal.Message);
        Assert.Contains("limit", refusal.Message);
    }

    [Fact]
    public void AFigureTextWithAFieldItDoesNotHave_IsRefusedByName()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(FigureSpec(
            """
            "texts":[{"text":"a","x":0.5,"y":0.5,"size":3}]
            """))));

        Assert.Contains("'size'", refusal.Message);
    }

    [Fact]
    public void ANullEntryInTheFigureTexts_IsRefusedByPath()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(FigureSpec(
            """
            "texts":[null]
            """))));

        Assert.Contains("$.texts[0]", refusal.Message);
        Assert.Contains("null", refusal.Message);
    }

    [Fact]
    public void AFigureLegend_PassesTheSpecReader_WithColourNamesAndThePositionInAnyCase()
    {
        var figure = Reader.Read(new ChartSpec(FigureSpec(
            """
            "legend":{"position":"lowercenter","title":"Series","nCols":2,"edgeColor":"red","faceColor":"#eee"}
            """)));

        var legend = figure.Legend!;
        Assert.Equal(LegendPosition.LowerCenter, legend.Position);
        Assert.Equal("Series", legend.Title);
        Assert.Equal(2, legend.NCols);
        Assert.Equal(Color.FromHex("#FF0000"), legend.EdgeColor);
        Assert.Equal(Color.FromHex("#EEEEEE"), legend.FaceColor);
    }

    /// <summary>The figure's legend is there or it is not: an empty object is a legend at its defaults, which draws, and
    /// no field at all is no legend, which does not. The subplot's own legend is a different field of a different object.</summary>
    [Fact]
    public void AnEmptyFigureLegend_IsALegendAtItsDefaults_AndNoFieldIsNone()
    {
        var with = Reader.Read(new ChartSpec(FigureSpec("\"legend\":{}")));
        var without = Reader.Read(new ChartSpec("""{"width":400,"height":300,"subPlots":[{"series":[{"type":"line","xData":[1,2],"yData":[1,2],"label":"a"}]}]}"""));

        Assert.Equal(new Legend(), with.Legend);
        Assert.Null(without.Legend);
    }

    [Fact]
    public void AMisspelledPositionOfTheFigureLegend_IsRefusedWithTheAcceptedValues()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(FigureSpec(
            """
            "legend":{"position":"topRight"}
            """))));

        Assert.Contains("'topRight'", refusal.Message);
        Assert.Contains("UpperRight", refusal.Message);
        Assert.Contains("$.legend.position", refusal.Message);
    }

    [Fact]
    public void AFigureLegendAndASubplotLegend_AreTwoFields()
    {
        var figure = Reader.Read(new ChartSpec(
            """
            {"width":400,"height":300,"legend":{"position":"lowerRight"},
             "subPlots":[{"legend":{"position":"upperLeft"},"series":[{"type":"line","xData":[1,2],"yData":[1,2],"label":"a"}]}]}
            """));

        Assert.Equal(LegendPosition.LowerRight, figure.Legend!.Position);
        Assert.Equal(LegendPosition.UpperLeft, figure.SubPlots[0].Legend.Position);
    }

    [Fact]
    public void AFigureImage_PassesTheSpecReader_WithItsColouringWrittenByKind()
    {
        var figure = Reader.Read(new ChartSpec(FigureSpec(
            """
            "images":[{"data":[[1,2,3],[4,5,6]],"xOffset":12,"yOffset":34,"vMin":0,"vMax":9,"alpha":0.5,
                       "colorMap":{"type":"linear","name":"ink","colors":["red","blue"]},"normalizer":{"center":3,"type":"twoSlope"}}]
            """)));

        var image = Assert.Single(figure.Images);
        Assert.Equal(2, image.Data.GetLength(0));
        Assert.Equal(3, image.Data.GetLength(1));
        Assert.Equal(6, image.Data[1, 2]);
        Assert.Equal(12, image.XOffset);
        Assert.Equal(34, image.YOffset);
        Assert.Equal(0, image.VMin);
        Assert.Equal(9, image.VMax);
        Assert.Equal(0.5, image.Alpha);
        Assert.True(DrawnSeriesFixtures.SameValue(new LinearColorMap("ink", [Color.FromHex("#FF0000"), Color.FromHex("#0000FF")]), image.ColorMap));
        Assert.True(DrawnSeriesFixtures.SameValue(new TwoSlopeNormalizer(3), image.Normalizer));
    }

    [Fact]
    public void AFigureImage_PassesTheSpecReader_WithAShippedColourMapByName()
    {
        var figure = Reader.Read(new ChartSpec(FigureSpec(
            """
            "images":[{"data":[[1,2]],"colorMapName":"plasma"}]
            """)));

        Assert.True(DrawnSeriesFixtures.SameValue(ColorMaps.Plasma, Assert.Single(figure.Images).ColorMap));
    }

    [Fact]
    public void AMissingCellOfAFigureImage_PassesTheSpecReader_AsTheMissingValue()
    {
        var figure = Reader.Read(new ChartSpec(FigureSpec(
            """
            "images":[{"data":[[1,"NaN"],[3,4]]}]
            """)));

        var image = Assert.Single(figure.Images);
        Assert.True(double.IsNaN(image.Data[0, 1]));
        Assert.Equal(3, image.Data[1, 0]);
    }

    /// <summary>The kind of a normaliser or a colour map is written first or last, in any case, as everywhere else in a spec.</summary>
    [Fact]
    public void TheKindOfAFigureImagesNormaliser_IsTakenAsWritten_WhereverItStands()
    {
        var figure = Reader.Read(new ChartSpec(FigureSpec(
            """
            "images":[{"data":[[1,2]],"normalizer":{"center":1.5,"type":"TWOSLOPE"}}]
            """)));

        Assert.True(DrawnSeriesFixtures.SameValue(new TwoSlopeNormalizer(1.5), Assert.Single(figure.Images).Normalizer));
    }

    [Theory]
    [InlineData("""{"data":[[1,2]],"normalizer":{"type":"cubic"}}""", "cubic", "twoSlope", "$.images[0].normalizer")]
    [InlineData("""{"data":[[1,2]],"colorMap":{"type":"spiral"}}""", "spiral", "linear", "$.images[0].colorMap")]
    public void AMisspelledKindOfAFigureImage_IsRefusedWithTheAcceptedKinds(string image, string wrong, string accepted, string path)
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(FigureSpec($$"""
            "images":[{{image}}]
            """))));

        Assert.Contains($"'{wrong}'", refusal.Message);
        Assert.Contains(accepted, refusal.Message);
        Assert.Contains(path, refusal.Message);
    }

    [Fact]
    public void AnUnknownColourInAFigureImagesColourMap_IsRefusedByPath()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(FigureSpec(
            """
            "images":[{"data":[[1,2]],"colorMap":{"type":"linear","colors":["red","reddish"]}}]
            """))));

        Assert.Contains("'reddish'", refusal.Message);
        Assert.Contains("$.images[0].colorMap.colors[1]", refusal.Message);
    }

    /// <summary>Rows of different lengths are no image: the reader says so and names the field, rather than keeping the first
    /// row's width and dropping the rest of the longer ones.</summary>
    [Fact]
    public void AFigureImageWhoseRowsDiffer_IsRefusedByFieldName()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(FigureSpec(
            """
            "images":[{"data":[[1,2,3],[4,5]]}]
            """))));

        Assert.Contains("data", refusal.Message);
        Assert.Contains("same number", refusal.Message);
    }

    /// <summary>An image is cut at the figure's edge and never makes the picture bigger, so the canvas ceiling that bounds the
    /// picture does not bound its offsets: one far outside the figure is accepted and draws nothing there.</summary>
    [Fact]
    public void AFigureImageFarOutsideTheFigure_PassesTheSpecReader()
    {
        var figure = Reader.Read(new ChartSpec(FigureSpec(
            """
            "images":[{"data":[[1]],"xOffset":100000,"yOffset":-100000}]
            """)));

        Assert.Equal(100000, Assert.Single(figure.Images).XOffset);
    }

    /// <summary>What the reader accepts the writer writes: a figure with texts, a legend and images, written by the library and
    /// handed back to the reader, draws the same picture.</summary>
    [Fact]
    public void AFigureWithTextsALegendAndImages_WrittenByTheLibrary_PassesTheSpecReader_AndDrawsTheSame()
    {
        var figure = Plt.Create().WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 3, 2], s => s.Label = "north"))
            .FigText("Source: ECB", 0.02, 0.03, text => { text.Alignment = MatPlotLibNet.Rendering.TextAlignment.Left; text.Color = Color.FromHex("#C0392B"); })
            .FigLegend(legend => legend with { Position = LegendPosition.LowerLeft })
            .FigImage(new double[,] { { 1, double.NaN }, { 3, 4 } }, 10, 10, image => image.ColorMap = ColorMaps.Plasma)
            .Build();

        var read = Reader.Read(new ChartSpec(figure.ToJson()));

        Assert.Equal(figure.ToSvg(), read.ToSvg());
    }

    // ---- a figure that draws something of its own: a text or an image, and no series -----------------------------

    /// <summary>A spec with the figure-level <paramref name="fields"/> (JSON members, comma separated) and no subplot at all.</summary>
    private static string SpecWithoutSeries(string fields) => $$"""{"width":400,"height":300,{{fields}}}""";

    private static byte[] PngOf(ChartSpec spec) =>
        new ChartRendering(Reader, new ChartSummarizer()).Render(spec, ChartFormat.Png).Bytes;

    /// <summary>The library draws a figure with a text and nothing else (core's overlay pass runs for a figure without subplots),
    /// so a spec that holds only a text is a picture, not "nothing to draw": the reader takes it and the picture is the one the
    /// library draws for the same figure.</summary>
    [Fact]
    public void ASpecWithOnlyAFigureText_IsRead_AndDrawsTheTextTheLibraryDraws()
    {
        var spec = new ChartSpec(SpecWithoutSeries("""
            "texts":[{"text":"Source: ECB","x":0.5,"y":0.5}]
            """));
        var library = Plt.Create().WithSize(400, 300).FigText("Source: ECB", 0.5, 0.5).Build();

        var figure = Reader.Read(spec);

        Assert.Empty(figure.SubPlots);
        Assert.Equal("Source: ECB", Assert.Single(figure.Texts).Text);
        Assert.Equal(library.ToPng(), PngOf(spec));
        Assert.NotEqual(Plt.Create().WithSize(400, 300).Build().ToPng(), PngOf(spec));
    }

    [Fact]
    public void ASpecWithOnlyAFigureImage_IsRead_AndDrawsTheImageTheLibraryDraws()
    {
        var spec = new ChartSpec(SpecWithoutSeries("""
            "images":[{"data":[[1,2,3],[4,5,6]],"xOffset":10,"yOffset":20}]
            """));
        var library = Plt.Create().WithSize(400, 300).FigImage(new double[,] { { 1, 2, 3 }, { 4, 5, 6 } }, 10, 20).Build();

        var figure = Reader.Read(spec);

        Assert.Empty(figure.SubPlots);
        Assert.Single(figure.Images);
        Assert.Equal(library.ToPng(), PngOf(spec));
        Assert.NotEqual(Plt.Create().WithSize(400, 300).Build().ToPng(), PngOf(spec));
    }

    /// <summary>The refusal asks core's rule for what shows, not a count of entries: a text outside the figure, a text of blanks, an
    /// image with no place or with no pixel leave the picture as blank as no entry at all.</summary>
    [Theory]
    [InlineData("""{"text":"outside","x":1.5,"y":0.5}""", "texts")]
    [InlineData("""{"text":"below","x":0.5,"y":-0.1}""", "texts")]
    [InlineData("""{"text":"   ","x":0.5,"y":0.5}""", "texts")]
    [InlineData("""{"text":"","x":0.5,"y":0.5}""", "texts")]
    [InlineData("""{"data":[[1,2]],"xOffset":"NaN"}""", "images")]
    [InlineData("""{"data":[]}""", "images")]
    public void ASpecWhoseOnlyTextOrImageShowsNothing_IsRefusedAsNothingToDraw(string entry, string list)
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(SpecWithoutSeries($"\"{list}\":[{entry}]"))));

        Assert.Contains("Nothing to draw", refusal.Message);
    }

    /// <summary>A legend and a colour bar say what other things draw: with no series they describe nothing, so they alone are no
    /// picture. The refusal names what would do.</summary>
    [Theory]
    [InlineData("\"legend\":{}")]
    [InlineData("\"figureColorBar\":{}")]
    [InlineData("\"legend\":{},\"figureColorBar\":{}")]
    public void ASpecWithOnlyAFigureLegendOrColourBar_IsRefusedAsNothingToDraw(string fields)
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(SpecWithoutSeries(fields))));

        Assert.Contains("Nothing to draw", refusal.Message);
        Assert.Contains("series", refusal.Message);
        Assert.Contains("texts", refusal.Message);
        Assert.Contains("images", refusal.Message);
    }

    /// <summary>A text that shows next to a text that does not: the one that shows is enough.</summary>
    [Fact]
    public void ASpecWithOneTextThatShowsAndOneThatDoesNot_IsRead()
    {
        var figure = Reader.Read(new ChartSpec(SpecWithoutSeries("""
            "texts":[{"text":"outside","x":2,"y":2},{"text":"inside","x":0.5,"y":0.5}]
            """)));

        Assert.Equal(2, figure.Texts.Count);
    }
}
