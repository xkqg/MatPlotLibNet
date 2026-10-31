// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>What the figure draws on itself, outside every subplot, travels through the figure JSON: its texts (matplotlib's
/// <c>figtext</c>), its legend (<c>figlegend</c>) and its images (<c>figimage</c>). A figure read back from JSON draws the same
/// picture as the one written, and a figure that has none of them writes the bytes it wrote before they existed, so a JSON
/// written by an earlier release reads at their defaults: no text, no legend, no image.
/// <para>The reflected rows live with the axes parts (<see cref="DrawnAxesFixtures"/>, parts "FigureLegend", "FigureText" and
/// "FigureImage"), which set every settable property; this class holds what a row cannot state: the values a text and an image
/// are given when they are made (their words, places and pixels), the drawn picture, the bytes at rest, a gap in an image, and
/// the guard over every list of the figure.</para>
/// <para>Tagged into the <c>ChartSerializerGlobalState</c> collection: a figure is read back through the process-global
/// series registry, which <see cref="SeriesRegistryTests"/> briefly rewrites.</para></summary>
[Collection("ChartSerializerGlobalState")]
public class FigureOverlayRoundTripTests
{
    private static readonly ChartSerializer Serializer = new();

    private static Figure RoundTrip(Figure figure) => Serializer.FromJson(Serializer.ToJson(figure));

    /// <summary>The names of the fields at the top level of the figure's JSON.</summary>
    private static List<string> RootFields(Figure figure)
    {
        using var document = JsonDocument.Parse(Serializer.ToJson(figure));
        return [.. document.RootElement.EnumerateObject().Select(property => property.Name)];
    }

    /// <summary>Two subplots side by side, each with a labelled line. Placed through a grid specification, which the JSON
    /// carries, so the layout of the figure read back is the layout of the figure written.</summary>
    private static FigureBuilder TwoLines() => Plt.Create().WithSize(600, 400).WithGridSpec(1, 2)
        .AddSubPlot(new GridPosition(0, 1, 0, 1), ax => ax.Plot([1.0, 2, 3], [3.0, 1, 2], s => s.Label = "north"))
        .AddSubPlot(new GridPosition(0, 1, 1, 2), ax => ax.Plot([1.0, 2, 3], [2.0, 3, 1], s => s.Label = "south"));

    // ---- the texts -----------------------------------------------------------------------------------------------

    [Fact]
    public void AFigureText_ReadBackFromJson_KeepsItsWordsPlaceAndLook()
    {
        var figure = TwoLines().FigText("Source: ECB", 0.02, 0.03, text =>
        {
            text.Font = new Font { Size = 11, Weight = FontWeight.Bold, Slant = FontSlant.Italic, Color = Color.FromHex("#112233") };
            text.Color = Color.FromHex("#AA3355");
            text.Alignment = TextAlignment.Center;
            text.VerticalAlignment = TextVerticalAlignment.Top;
            text.Rotation = 30;
        }).Build();

        var read = Assert.Single(RoundTrip(figure).Texts);

        var written = figure.Texts[0];
        Assert.Equal(written.Text, read.Text);
        Assert.Equal(written.X, read.X);
        Assert.Equal(written.Y, read.Y);
        Assert.Equal(written.Font, read.Font);
        Assert.Equal(written.Color, read.Color);
        Assert.Equal(written.Alignment, read.Alignment);
        Assert.Equal(written.VerticalAlignment, read.VerticalAlignment);
        Assert.Equal(written.Rotation, read.Rotation);
    }

    [Fact]
    public void FigureTexts_ReadBackFromJson_KeepTheOrderTheyWereAdded()
    {
        var figure = TwoLines().FigText("first", 0.1, 0.1).FigText("second", 0.2, 0.2).FigText("third", 0.3, 0.3).Build();

        var read = RoundTrip(figure);

        Assert.Equal(["first", "second", "third"], read.Texts.Select(text => text.Text));
    }

    [Fact]
    public void AFigureTextThatSetsNothingButItsPlace_WritesNothingElse()
    {
        var figure = TwoLines().FigText("plain", 0.5, 0.5).Build();

        using var document = JsonDocument.Parse(Serializer.ToJson(figure));
        var written = document.RootElement.GetProperty("texts")[0];

        Assert.Equal(["text", "x", "y"], written.EnumerateObject().Select(property => property.Name));
    }

    [Fact]
    public void AFigureTextWithEveryValueAtItsDefault_ReadsBackAtTheDefaults()
    {
        var figure = TwoLines().FigText("plain", 0.5, 0.5).Build();

        var read = Assert.Single(RoundTrip(figure).Texts);

        Assert.Null(read.Font);
        Assert.Null(read.Color);
        Assert.Equal(TextAlignment.Left, read.Alignment);
        Assert.Equal(TextVerticalAlignment.Baseline, read.VerticalAlignment);
        Assert.Equal(0, read.Rotation);
    }

    /// <summary>A position off the figure, or no number at all, is kept by the model and not drawn: the JSON keeps it as it
    /// keeps every other value, so editing the figure later finds what was written.</summary>
    [Theory]
    [InlineData(1.5, 0.5)]
    [InlineData(0.5, -0.25)]
    [InlineData(double.NaN, 0.5)]
    [InlineData(0.5, double.PositiveInfinity)]
    public void AFigureTextAtAPlaceItIsNotDrawnAt_ReadBackFromJson_KeepsThatPlace(double x, double y)
    {
        var figure = TwoLines().FigText("nowhere", x, y).Build();

        var read = Assert.Single(RoundTrip(figure).Texts);

        Assert.Equal(x, read.X);
        Assert.Equal(y, read.Y);
    }

    [Fact]
    public void AFigureTextReadBackFromJson_DrawsTheSameSvg()
    {
        var figure = TwoLines().WithTitle("Revenue")
            .FigText("Source: ECB", 0.02, 0.03)
            .FigText("(a)", 0.5, 0.97, text => { text.Alignment = TextAlignment.Center; text.Rotation = 10; text.Color = Color.FromHex("#C0392B"); })
            .Build();

        string drawn = figure.ToSvg();

        Assert.Contains(">Source: ECB<", drawn);
        Assert.Contains(">(a)<", drawn);
        Assert.Equal(drawn, RoundTrip(figure).ToSvg());
    }

    [Fact]
    public void AFigureTextOnAFigureWithNoSubplot_ReadBackFromJson_DrawsTheSameSvg()
    {
        var figure = Plt.Create().WithSize(300, 200).FigText("only a note", 0.5, 0.5).Build();

        string drawn = figure.ToSvg();

        Assert.Contains(">only a note<", drawn);
        Assert.Equal(drawn, RoundTrip(figure).ToSvg());
    }

    // ---- the legend ----------------------------------------------------------------------------------------------

    [Fact]
    public void AFigureLegend_ReadBackFromJson_KeepsEveryValueItWasGiven()
    {
        var legend = new Legend
        {
            Visible = true,
            Position = LegendPosition.LowerCenter,
            NCols = 2,
            FontSize = 9,
            Title = "Series",
            TitleFontSize = 11,
            FrameOn = false,
            FrameAlpha = 0.5,
            FancyBox = true,
            Shadow = true,
            EdgeColor = Color.FromHex("#445566"),
            FaceColor = Color.FromHex("#EEEEEE"),
            MarkerScale = 1.5,
            LabelSpacing = 0.75,
            ColumnSpacing = 3,
            LegendValues = true,
        };
        var figure = TwoLines().FigLegend(_ => legend).Build();

        var read = RoundTrip(figure).Legend;

        Assert.Equal(legend, read);
    }

    /// <summary>A figure with no legend and a figure with a plain one are different figures: the first draws none, the second
    /// lists the labelled series of every subplot. So the plain legend, which an axes' legend leaves out of the JSON, is
    /// written here as an empty object, and comes back as a legend.</summary>
    [Fact]
    public void APlainFigureLegend_ReadBackFromJson_IsStillALegend()
    {
        var figure = TwoLines().FigLegend().Build();

        string json = Serializer.ToJson(figure);
        var read = RoundTrip(figure);

        Assert.Equal(new Legend(), read.Legend);
        using var document = JsonDocument.Parse(json);
        var written = document.RootElement.GetProperty("legend");
        Assert.Equal(JsonValueKind.Object, written.ValueKind);
        Assert.Empty(written.EnumerateObject());
    }

    [Fact]
    public void AnInvisibleFigureLegend_ReadBackFromJson_IsStillThere()
    {
        var figure = TwoLines().FigLegend(legend => legend with { Visible = false }).Build();

        var read = RoundTrip(figure).Legend;

        Assert.NotNull(read);
        Assert.False(read.Visible);
    }

    [Fact]
    public void AFigureLegendReadBackFromJson_DrawsTheSameSvg()
    {
        var figure = TwoLines().FigLegend(legend => legend with { Position = LegendPosition.LowerLeft, Title = "Series", NCols = 2 }).Build();

        string drawn = figure.ToSvg();

        Assert.Contains(">north<", drawn);
        Assert.Contains(">Series<", drawn);
        Assert.Equal(drawn, RoundTrip(figure).ToSvg());
    }

    // ---- the images ----------------------------------------------------------------------------------------------

    [Fact]
    public void AFigureImage_ReadBackFromJson_KeepsItsPixelsPlaceAndColouring()
    {
        double[,] pixels = { { 0, 1, 2 }, { 3, 4, 5 } };
        var colorMap = new LinearColorMap("ink", [Color.FromHex("#000000"), Color.FromHex("#3366CC"), Color.FromHex("#FFFFFF")]) { BadColor = Color.FromHex("#FF00FF") };
        var figure = TwoLines().FigImage(pixels, 12, 34, image =>
        {
            image.ColorMap = colorMap;
            image.Normalizer = new PowerNormNormalizer(0.5);
            image.VMin = -1;
            image.VMax = 9;
            image.Alpha = 0.6;
        }).Build();

        var read = Assert.Single(RoundTrip(figure).Images);

        var written = figure.Images[0];
        Assert.Equal(2, read.Data.GetLength(0));
        Assert.Equal(3, read.Data.GetLength(1));
        Assert.Equal(pixels.Cast<double>(), read.Data.Cast<double>());
        Assert.Equal(12, read.XOffset);
        Assert.Equal(34, read.YOffset);
        Assert.True(DrawnSeriesFixtures.SameValue(written.ColorMap, read.ColorMap));
        Assert.True(DrawnSeriesFixtures.SameValue(written.Normalizer, read.Normalizer));
        Assert.Equal(-1, read.VMin);
        Assert.Equal(9, read.VMax);
        Assert.Equal(0.6, read.Alpha);
    }

    [Fact]
    public void AFigureImageWithAShippedColourMap_ReadBackFromJson_NamesThatMap()
    {
        var figure = TwoLines().FigImage(new double[,] { { 0, 1 } }, 0, 0, image => image.ColorMap = ColorMaps.Plasma).Build();

        var read = Assert.Single(RoundTrip(figure).Images);

        Assert.Equal(ColorMaps.Plasma.Name, read.ColorMap!.Name);
        Assert.True(DrawnSeriesFixtures.SameValue(ColorMaps.Plasma, read.ColorMap));
    }

    [Fact]
    public void AFigureImageThatSetsNothingButItsPixelsAndPlace_WritesNothingElse()
    {
        var figure = TwoLines().FigImage(new double[,] { { 0, 1 } }, 3, 4).Build();

        using var document = JsonDocument.Parse(Serializer.ToJson(figure));
        var written = document.RootElement.GetProperty("images")[0];

        Assert.Equal(["data", "xOffset", "yOffset"], written.EnumerateObject().Select(property => property.Name));
    }

    [Fact]
    public void AFigureImageWithEveryValueAtItsDefault_ReadsBackAtTheDefaults()
    {
        var figure = TwoLines().FigImage(new double[,] { { 0, 1 } }, 3, 4).Build();

        var read = Assert.Single(RoundTrip(figure).Images);

        Assert.Null(read.ColorMap);
        Assert.Null(read.Normalizer);
        Assert.Null(read.VMin);
        Assert.Null(read.VMax);
        Assert.Equal(1.0, read.Alpha);
    }

    /// <summary>A missing cell is NaN, which JSON has no number for: it is written as the string <c>"NaN"</c> and read back as
    /// NaN, not as a zero (a dark pixel) and not dropped (which would shift every cell after it).</summary>
    [Fact]
    public void AFigureImageWithMissingCells_ReadBackFromJson_KeepsThemWhereTheyWere()
    {
        double[,] pixels = { { 1, double.NaN, 3 }, { double.NaN, 5, 6 } };
        var figure = TwoLines().FigImage(pixels, 5, 5).Build();

        string json = Serializer.ToJson(figure);
        var read = Assert.Single(RoundTrip(figure).Images);

        Assert.Contains("\"NaN\"", json);
        Assert.Equal([1, double.NaN, 3, double.NaN, 5, 6], read.Data.Cast<double>());
        Assert.Equal(2, read.Data.GetLength(0));
    }

    [Fact]
    public void AFigureImageWithMissingCells_ReadBackFromJson_DrawsTheSameSvg()
    {
        double[,] pixels = { { 1, double.NaN, 3 }, { double.NaN, 5, 6 } };
        var withBadColour = new LinearColorMap("ink", [Color.FromHex("#000000"), Color.FromHex("#FFFFFF")]) { BadColor = Color.FromHex("#FF00FF") };
        var plain = TwoLines().FigImage(pixels, 5, 5).Build();
        var coloured = TwoLines().FigImage(pixels, 5, 5, image => image.ColorMap = withBadColour).Build();

        Assert.Equal(plain.ToSvg(), RoundTrip(plain).ToSvg());
        Assert.Equal(coloured.ToSvg(), RoundTrip(coloured).ToSvg());
        Assert.Contains("#FF00FF", coloured.ToSvg());
    }

    [Fact]
    public void AFigureImageReadBackFromJson_DrawsTheSameSvg()
    {
        double[,] pixels = { { 0, 1, 2, 3 }, { 4, 5, 6, 7 }, { 8, 9, 10, 11 } };
        var figure = TwoLines().FigImage(pixels, 20, 15, image => { image.ColorMap = ColorMaps.Plasma; image.Alpha = 0.7; image.VMax = 20; }).Build();

        Assert.Equal(figure.ToSvg(), RoundTrip(figure).ToSvg());
    }

    [Fact]
    public void FigureImages_ReadBackFromJson_KeepTheOrderTheyWereAdded()
    {
        var figure = TwoLines().FigImage(new double[,] { { 1 } }, 1, 1).FigImage(new double[,] { { 2 } }, 2, 2).Build();

        var read = RoundTrip(figure);

        Assert.Equal([1.0, 2.0], read.Images.Select(image => image.Data[0, 0]));
        Assert.Equal([1.0, 2.0], read.Images.Select(image => image.XOffset));
    }

    [Fact]
    public void AFigureImageWithNoPixels_ReadBackFromJson_IsStillThere()
    {
        var figure = TwoLines().FigImage(new double[0, 0], 1, 2).Build();

        var read = Assert.Single(RoundTrip(figure).Images);

        Assert.Equal(0, read.Data.GetLength(0));
        Assert.Equal(1, read.XOffset);
    }

    /// <summary>An offset that is no number gives the image no place: the model keeps it and does not draw it, and the JSON
    /// keeps it too.</summary>
    [Fact]
    public void AFigureImageWithNoPlace_ReadBackFromJson_KeepsThatOffset()
    {
        var figure = TwoLines().FigImage(new double[,] { { 1 } }, double.NaN, double.NegativeInfinity).Build();

        var read = Assert.Single(RoundTrip(figure).Images);

        Assert.True(double.IsNaN(read.XOffset));
        Assert.True(double.IsNegativeInfinity(read.YOffset));
    }

    // ---- JSON written by hand -------------------------------------------------------------------------------------

    /// <summary>A text written by hand may leave out whatever it does not care for: no words is a text of none (which draws
    /// nothing), no place is the bottom-left corner.</summary>
    [Fact]
    public void AFigureTextWithOnlyItsPlace_ReadsAsATextOfNoWords()
    {
        var read = Serializer.FromJson("""{"width":400,"height":300,"texts":[{"x":0.25,"y":0.75}]}""");

        var text = Assert.Single(read.Texts);
        Assert.Equal("", text.Text);
        Assert.Equal(0.25, text.X);
        Assert.Equal(0.75, text.Y);
    }

    [Fact]
    public void AFigureImageWithNoPixels_WrittenByHand_ReadsAsAnImageOfNone()
    {
        var read = Serializer.FromJson("""{"width":400,"height":300,"images":[{"xOffset":3,"yOffset":4}]}""");

        var image = Assert.Single(read.Images);
        Assert.Empty(image.Data);
        Assert.Equal(3, image.XOffset);
        Assert.Equal(4, image.YOffset);
    }

    /// <summary>Rows of different lengths are no rectangle of pixels. Reading them would keep the first row's width and either
    /// drop the rest of a longer row or fail with an index error on a shorter one, so the reader says what is wrong and which
    /// field it is.</summary>
    [Fact]
    public void AFigureImageWhoseRowsDiffer_IsRefusedByTheFieldThatHoldsThem()
    {
        var refusal = Assert.Throws<ArgumentException>(() =>
            Serializer.FromJson("""{"width":400,"height":300,"images":[{"data":[[1,2,3],[4,5]]}]}"""));

        Assert.Equal("data", refusal.ParamName);
        Assert.Contains("same number", refusal.Message);
    }

    [Fact]
    public void AFigureImageWithATallerRowFirst_IsRefusedToo()
    {
        var refusal = Assert.Throws<ArgumentException>(() =>
            Serializer.FromJson("""{"width":400,"height":300,"images":[{"data":[[1],[4,5]]}]}"""));

        Assert.Equal("data", refusal.ParamName);
    }

    [Fact]
    public void AFigureImageWithAColourMapThatCannotBeMade_IsRefusedByTheFieldThatHoldsIt()
    {
        var refusal = Assert.Throws<ArgumentException>(() =>
            Serializer.FromJson("""{"width":400,"height":300,"images":[{"data":[[1]],"colorMap":{"type":"linear","name":"empty","colors":[]}}]}"""));

        Assert.Equal("colors", refusal.ParamName);
    }

    // ---- at rest, and older JSON ---------------------------------------------------------------------------------

    /// <summary>A figure that carries no text, no legend and no image writes none of their fields: the bytes it wrote before
    /// they reached the JSON (the golden corpus holds that for the figures it records).</summary>
    [Fact]
    public void AFigureWithoutOverlays_WritesNoneOfTheirFields()
    {
        var fields = RootFields(TwoLines().Build());

        Assert.DoesNotContain("texts", fields);
        Assert.DoesNotContain("images", fields);
        Assert.DoesNotContain("legend", fields);
    }

    [Fact]
    public void AFigureWithOnlyOneKindOfOverlay_WritesOnlyThatField()
    {
        Assert.Contains("texts", RootFields(TwoLines().FigText("t", 0.5, 0.5).Build()));
        Assert.DoesNotContain("images", RootFields(TwoLines().FigText("t", 0.5, 0.5).Build()));
        Assert.Contains("images", RootFields(TwoLines().FigImage(new double[,] { { 1 } }).Build()));
        Assert.DoesNotContain("texts", RootFields(TwoLines().FigImage(new double[,] { { 1 } }).Build()));
        Assert.Contains("legend", RootFields(TwoLines().FigLegend().Build()));
        Assert.DoesNotContain("texts", RootFields(TwoLines().FigLegend().Build()));
    }

    [Fact]
    public void AJsonWrittenBeforeTheFigureOverlays_ReadsBackWithoutAny()
    {
        const string older = """{"width":400,"height":300,"subPlots":[{"series":[{"type":"line","xData":[1,2],"yData":[1,2]}]}]}""";

        var read = Serializer.FromJson(older);

        Assert.Empty(read.Texts);
        Assert.Empty(read.Images);
        Assert.Null(read.Legend);
    }

    /// <summary>A figure with every overlay set is a fixed point of the round trip: written, read and written again it is the
    /// same JSON.</summary>
    [Fact]
    public void AFigureWithEveryOverlaySet_IsAFixedPointOfTheRoundTrip()
    {
        var figure = DrawnAxesFixtures.Build(everyPropertySet: true);
        string json = Serializer.ToJson(figure);

        Assert.Equal(json, Serializer.ToJson(Serializer.FromJson(json)));
    }

    // ---- the guard over every list of the figure -----------------------------------------------------------------

    /// <summary>Each list the figure has besides its subplots is held by the fixture: it holds an item of it, and a part names
    /// the item's type. A list added to <see cref="Figure"/> tomorrow turns this red, naming it, until the fixture holds an item
    /// of it and a drawn part covers the item's settable properties.</summary>
    [Fact]
    public void EveryGetOnlyListOfTheFigure_HoldsAnItemInTheFixture_AndItsItemTypeIsADrawnPart()
    {
        var figure = DrawnAxesFixtures.Build(everyPropertySet: true);
        var partTypes = DrawnAxesFixtures.Parts.Select(part => part.Type).ToHashSet();

        Assert.NotEmpty(DrawnAxesFixtures.FigureLists);
        foreach (var list in DrawnAxesFixtures.FigureLists)
        {
            int count = ((System.Collections.ICollection)list.GetValue(figure)!).Count;
            Assert.True(count > 0, $"Figure.{list.Name} is empty in DrawnAxesFixtures.Build: add an item of it, or the JSON can drop the whole list unseen.");
            var itemType = list.PropertyType.GetGenericArguments()[0];
            Assert.True(partTypes.Contains(itemType), $"Figure.{list.Name} holds {itemType.Name}, which no part of DrawnAxesFixtures.Parts covers.");
        }
    }

    [Fact]
    public void TheGuard_FindsTheTextsAndTheImagesOfTheFigure()
    {
        Assert.Equal([nameof(Figure.Images), nameof(Figure.Texts)], DrawnAxesFixtures.FigureLists.Select(list => list.Name));
    }

    /// <summary>The length of each list and every value of every item of it, the ones given when it is made (a text's words
    /// and place, an image's pixels and offsets) included, comes back from JSON as it was. One row per value, so a field the
    /// DTO drops is named: <c>Texts[0].Y</c>, <c>Images[0].XOffset</c>.</summary>
    [Theory]
    [MemberData(nameof(DrawnAxesFixtures.FigureListValueKeys), MemberType = typeof(DrawnAxesFixtures))]
    public void AFigureListValue_ComesBackFromJson_AsItWasWritten(string key)
    {
        var figure = DrawnAxesFixtures.Build(everyPropertySet: true);
        object? written = DrawnAxesFixtures.FigureListValues(figure)[key];

        var read = DrawnAxesFixtures.FigureListValues(RoundTrip(figure));

        Assert.True(read.ContainsKey(key), $"Figure.{key}: written as {DrawnSeriesFixtures.Show(written)}, and nothing of it came back from JSON.");
        Assert.True(DrawnSeriesFixtures.SameValue(written, read[key]),
            $"Figure.{key}: written as {DrawnSeriesFixtures.Show(written)}, read back from JSON as {DrawnSeriesFixtures.Show(read[key])}.");
    }

    /// <summary>The guard sees the places a flat comparison of pixels would not: the size of an image is a value of it.</summary>
    [Fact]
    public void TheGuardValues_HoldTheSizeOfAnImage_AndEveryCellOfIt()
    {
        var values = DrawnAxesFixtures.FigureListValues(DrawnAxesFixtures.Build(everyPropertySet: false));

        Assert.Equal(2, values["Images[0].Data.Rows"]);
        Assert.Equal(3, values["Images[0].Data.Columns"]);
        Assert.Equal(6, ((double[])values["Images[0].Data"]!).Length);
        Assert.Equal(1, values["Texts.Count"]);
        Assert.Equal("note", values["Texts[0].Text"]);
    }
}
