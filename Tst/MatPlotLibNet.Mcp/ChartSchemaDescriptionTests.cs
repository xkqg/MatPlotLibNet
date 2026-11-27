// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>The schema a model reads has to be the schema the reader enforces, or the server documents one thing
/// and accepts another. Both come off the same DTO records, and the example is run through the real renderer.</summary>
[Collection("ChartServicesGlobalState")]
public class ChartSchemaDescriptionTests
{
    private static readonly ChartTypeCatalog Catalog = new();
    private static readonly ChartSchemaDescription Description = new(Catalog);

    private static readonly MatPlotLibNet.Models.Figure Fresh = new();

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("TheSchema_ShowsTheFieldsAFigureSpecNeeds", () => Description.Describe(null), "width: number", "height: number", "subPlots", "series", "xData: number[]"),
        new DrawnToken("AskingAboutOneChartType_DescribesThatOne", () => Description.Describe("candlestick"), "'candlestick'", "open: number[]", "close: number[]"),
        new DrawnToken("TheFieldNames_AreTheWireNames_NotTheClrNames", () => Description.Describe(null), "altText") { Absent = ["AltText"] },
        new DrawnToken("TheDescriptionOfOneChartType_NamesEverySeatASeriesMayStandAt", () => Description.Describe("line"), "leftYAxes[].series[]"),
        new DrawnToken("TheNotesOfTheFiguresSizeAndResolution_SayWhatAFreshFigureStartsAt", () => Description.Describe(null),
            FormattableString.Invariant($"width: number  — the width of the figure in pixels; absent for {Fresh.Width}"),
            FormattableString.Invariant($"height: number  — the height of the figure in pixels; absent for {Fresh.Height}"),
            FormattableString.Invariant($"the resolution, {Fresh.Dpi} by default: a PNG is drawn with dpi / {Fresh.Dpi} pixels")),
        //  The legend section says it serves the figure's legend as well as the subplots'.
        new DrawnToken("TheLegendSection_NamesTheFiguresLegendToo", LegendHeading, "subPlots[].legend"),
        new DrawnToken("TheLegendSection_NamesTheFiguresLegendToo_InAnyCase", LegendHeading, "figure") { Comparison = StringComparison.OrdinalIgnoreCase },
    };

    private static string LegendHeading() => Description.Describe(null).HeadingLine("Legend fields");

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    [Fact]
    public void AskingAboutAnUnknownChartType_IsRefusedByName_WithTheNearest()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Description.Describe("candlestik"));

        Assert.Contains("'candlestik'", refusal.Message);
        Assert.Contains("'candlestick'", refusal.Message);
    }

    [Fact]
    public void AChartTypeThisServerDoesNotList_IsRefusedToo() =>
        Assert.Throws<ToolRefusalException>(() => Description.Describe("nope"));

    /// <summary>A map keeps its projection and its features in the package data of its series: the schema says what that holds,
    /// with the names of the projections, the numbers a projection takes and the built-in feature sets, read off the Geo package.</summary>
    [Fact]
    public void AMap_IsDescribed_WithWhatItsPackageDataHolds()
    {
        string schema = Description.Describe("geo_polygon");

        var packageData = schema.FieldLine("packageData:");
        Assert.Contains("Robinson", packageData);
        Assert.Contains("Orthographic (lat_0, lon_0)", packageData);
        Assert.Contains("ne_110m_admin_0_countries", packageData);
        Assert.Contains("ne_110m_coastline", packageData);
        Assert.Contains("FeatureCollection", packageData);
        Assert.DoesNotContain("reads none", packageData);
    }

    [Fact]
    public void TheExampleInTheSchema_IsItselfARenderableSpec()
    {
        // The one test that keeps the documentation honest: what the model is shown, the renderer accepts.
        var rendering = new ChartRendering(new ChartSpecReader(Catalog, RenderLimits.Default), new ChartSummarizer());

        var result = rendering.Render(new ChartSpec(ChartSchemaDescription.Example), ChartFormat.Png);

        Assert.Equal(0x89, result.Bytes[0]);
        Assert.Equal("Revenue", result.Summary.Title);
    }

    [Fact]
    public void TheLabelNote_SaysALabelIsALegendEntryUnlessThePlacementSaysLineEnd()
    {
        string schema = Description.Describe("line");

        var label = schema.FieldLine("label:");
        Assert.Contains("legend", label, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("labelPlacement", label);
        Assert.Contains("lineEnd", label, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ThePlacementField_IsListedWithItsAcceptedValues()
    {
        string schema = Description.Describe("line");

        var placement = schema.FieldLine("labelPlacement:");
        Assert.Contains("Legend", placement);
        Assert.Contains("LineEnd", placement);
        Assert.Contains("line", placement, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A field whose wire name says little is explained where it is listed: <c>c</c> is matplotlib's name for
    /// the values a colour map colours, an area's step mode travels in <c>drawStyle</c>, and <c>fill</c> is a switch,
    /// not a colour. The lists of Y axes and the gap between their titles (no chart type: the overview) carry little in their names too:
    /// the schema says what an entry is, which end of the plot it stands at, and which list continues which.</summary>
    [Theory]
    [InlineData("scatter", "c:", "colour map")]
    [InlineData("area", "drawStyle:", "area")]
    [InlineData("bar", "stackBaseline:", "starts")]
    [InlineData("kde", "fill:", "area under")]
    [InlineData("contourf", "hatches:", "band")]
    [InlineData("polarheatmap", "rMax:", "radius")]
    [InlineData("waterfall", "totalIndices:", "zero")]
    [InlineData("histogram", "density:", "area")]
    [InlineData("streamplot", "streamDensity:", "streamlines")]
    [InlineData("histogram", "rWidth:", "bin")]
    [InlineData("pie", "autoPct:", "percentage")]
    [InlineData("pie", "explode:", "slice")]
    [InlineData("pie", "colors:", "slice")]
    [InlineData("table", "fontSize:", "contour")]
    [InlineData("heatmap", "colorMap:", "built in code")]
    [InlineData("heatmap", "colorMapName:", "ships")]
    [InlineData("pairgrid", "pairGridOffDiagonalColorMapDescription:", "built in code")]
    [InlineData(null, "extraRightYAxes:", "secondaryYAxis")]
    [InlineData(null, "rightYAxesOverflow:", "extraRightYAxes")]
    [InlineData(null, "leftYAxes:", "yAxis")]
    [InlineData(null, "leftYAxesOverflow:", "continues")]
    [InlineData(null, "yAxisTitleGap:", "pixels")]
    [InlineData(null, "bottomXAxes:", "below the plot")]
    public void AFieldWhoseNameSaysLittle_IsExplainedWhereItIsListed(string? type, string field, string explanation) =>
        Assert.Contains(explanation, Description.Describe(type).FieldLine(field), StringComparison.OrdinalIgnoreCase);

    /// <summary>A tick locator, a tick formatter and a normaliser are written by kind: the schema names every kind the
    /// reader accepts, with the fields that kind takes, read off the same records the serializer reads.</summary>
    [Theory]
    [InlineData("multiple: base: number, offset: number")]
    [InlineData("fixed: positions: number[]")]
    [InlineData("log")]
    [InlineData("category: labels: string[], reversed: boolean, positions: number[]")]
    [InlineData("autoDate")]
    [InlineData("twoSlope: center: number")]
    [InlineData("centered: vcenter: number, halfrange: number")]
    [InlineData("linear: name: string, colors: string (colour)[], positions: number[], underColor: string (colour), overColor: string (colour), badColor: string (colour)")]
    [InlineData("listed: name: string, colors: string (colour)[], underColor: string (colour), overColor: string (colour), badColor: string (colour)")]
    [InlineData("reversed: map: object")]
    [InlineData("named: name: string")]
    public void TheSchema_NamesEveryKindOfLocatorFormatterAndNormaliser_WithItsFields(string kind)
    {
        string schema = Description.Describe(null);

        Assert.Contains(SourceFile.LinesOf(schema), line => line.Trim() == kind);
    }

    /// <summary>The parts an axes draws besides its series are listed with their fields, wire names and all.</summary>
    [Theory]
    [InlineData("Legend fields", "position: string (enum)")]
    [InlineData("Colour bar fields", "orientation: string (enum)")]
    [InlineData("Grid fields", "axis: string (enum)")]
    [InlineData("Grid change fields", "axis: string (enum)")]
    [InlineData("Grid change fields", "alpha: number")]
    [InlineData("Grid change fields", "visible: boolean")]
    [InlineData("Tick fields", "direction: string (enum)")]
    public void TheSchema_ListsThePartsAnAxesDraws(string section, string field)
    {
        string schema = Description.Describe(null);

        int at = schema.IndexOf(section, StringComparison.Ordinal);
        Assert.True(at >= 0, $"'{section}' is not in the schema.");
        Assert.Contains(field, schema[at..]);
    }

    [Theory]
    [InlineData("tickLocator:", "type")]
    [InlineData("tickFormatter:", "type")]
    public void ADescribedAxisField_SaysItIsWrittenByKind(string field, string explanation)
    {
        string schema = Description.Describe(null);

        var line = SourceFile.LinesOf(schema).First(l => l.TrimStart().StartsWith(field, StringComparison.Ordinal));
        Assert.Contains(explanation, line);
    }

    /// <summary>The reader walks every seat of a subplot, so the schema names every one: the heading of the axis fields and
    /// the heading of the series fields come off the same seat shape the reader walks.</summary>
    [Fact]
    public void TheSchema_NamesEverySeatOfASubplot_AxesAndSeriesAlike()
    {
        string schema = Description.Describe(null);

        Assert.All(AxesSeatShape.OfSubplot.Names, seat => Assert.Contains($".{seat.Path}", schema));
    }

    [Fact]
    public void TheSchema_ListsTheFieldsOfAYAxisSlot_UnderTheListsThatHoldThem()
    {
        string schema = Description.Describe(null);

        int at = schema.IndexOf("Y axis slot fields", StringComparison.Ordinal);
        Assert.True(at >= 0, "'Y axis slot fields' is not in the schema.");
        string[] section = SourceFile.LinesOf(schema[at..]);
        var slotRecord = typeof(SeriesDto).Assembly.GetType("MatPlotLibNet.Serialization.SecondaryAxisSlotDto", throwOnError: true)!;
        Assert.All(AxesSeatShape.OfSubplot.SlotListsOf(slotRecord), list => Assert.Contains($".{list}", section[0]));
        Assert.DoesNotContain(".bottomXAxes", section[0]);
        Assert.Contains(section.Skip(1).Take(2), line => line.Trim() == "axis: object");
        Assert.Contains(section.Skip(1).Take(2), line => line.Trim().StartsWith("series: object[]", StringComparison.Ordinal));
        Assert.True(section[3].Trim().Length == 0, $"a Y axis slot holds an axis and its series only, not '{section[3]}'.");
    }

    /// <summary>An X axis below the plot is its own record: the axis, linked to the primary X axis, and where it stands. It holds
    /// no series, so the schema neither lists a series field for it nor names its list among the Y axis slots.</summary>
    [Fact]
    public void TheSchema_ListsTheFieldsOfAnXAxisBelowThePlot_UnderItsList()
    {
        string schema = Description.Describe(null);

        int at = schema.IndexOf("X axis below the plot fields", StringComparison.Ordinal);
        Assert.True(at >= 0, "'X axis below the plot fields' is not in the schema.");
        string[] section = SourceFile.LinesOf(schema[at..]);
        Assert.Contains("subPlots[].bottomXAxes", section[0]);
        Assert.Contains(section.Skip(1).Take(2), line => line.Trim() == "axis: object");
        Assert.Contains(section.Skip(1).Take(2), line => line.Trim().StartsWith("location: number  — ", StringComparison.Ordinal)
            && line.Contains("bottom spine", StringComparison.Ordinal));
        Assert.True(section[3].Trim().Length == 0, $"an X axis below the plot holds an axis and a location only, not '{section[3]}'.");
    }

    /// <summary>A link is drawn on every linked axis the wire carries, the axes below the plot among them.</summary>
    [Fact]
    public void TheLinkNoteAndTheLinkKinds_NameTheXAxesBelowThePlot()
    {
        string schema = Description.Describe(null);

        Assert.Contains("bottomXAxes", SourceFile.LinesOf(schema).First(l => l.TrimStart().StartsWith("link:", StringComparison.Ordinal)));
        Assert.Contains("bottomXAxes", schema.HeadingLine("Axis link kinds"));
    }

    /// <summary>What the figure draws on itself — its texts, its legend, its images — is listed with the figure's own fields,
    /// each with the section that says what an item of it holds.</summary>
    [Theory]
    [InlineData("texts: object[]")]
    [InlineData("images: object[]")]
    [InlineData("legend: object")]
    public void TheFigureFields_ListTheTextsTheLegendAndTheImages(string field)
    {
        var figureFields = Description.SectionFields("Figure fields:");

        Assert.Contains(figureFields, line => line.StartsWith(field, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Figure text fields", "text: string")]
    [InlineData("Figure text fields", "x: number")]
    [InlineData("Figure text fields", "y: number")]
    [InlineData("Figure text fields", "font: object")]
    [InlineData("Figure text fields", "color: string (colour)")]
    [InlineData("Figure text fields", "alignment: string (enum)")]
    [InlineData("Figure text fields", "verticalAlignment: string (enum)")]
    [InlineData("Figure text fields", "rotation: number")]
    [InlineData("Figure image fields", "data: number[][]")]
    [InlineData("Figure image fields", "xOffset: number")]
    [InlineData("Figure image fields", "yOffset: number")]
    [InlineData("Figure image fields", "colorMapName: string")]
    [InlineData("Figure image fields", "colorMap: object")]
    [InlineData("Figure image fields", "normalizer: object")]
    [InlineData("Figure image fields", "vMin: number")]
    [InlineData("Figure image fields", "vMax: number")]
    [InlineData("Figure image fields", "alpha: number")]
    public void TheSchema_ListsTheFieldsOfAFigureTextAndOfAFigureImage(string section, string field)
    {
        var fields = Description.SectionFields(section);

        Assert.Contains(fields, line => line.StartsWith(field, StringComparison.Ordinal));
    }

    /// <summary>The fields whose names say little are explained where they are listed, with the enum names the reader accepts:
    /// the alignments, the offsets (pixels from the bottom-left corner), the place as fractions, and the legend that lists
    /// every subplot.</summary>
    [Theory]
    [InlineData("Figure text fields", "x:", "fraction")]
    [InlineData("Figure text fields", "y:", "bottom")]
    [InlineData("Figure text fields", "alignment:", "Center")]
    [InlineData("Figure text fields", "verticalAlignment:", "Baseline")]
    [InlineData("Figure text fields", "verticalAlignment:", "Bottom")]
    [InlineData("Figure text fields", "rotation:", "degrees")]
    [InlineData("Figure image fields", "data:", "row")]
    [InlineData("Figure image fields", "xOffset:", "pixels")]
    [InlineData("Figure image fields", "yOffset:", "bottom")]
    public void AFieldOfAFigureTextOrImage_IsExplainedWhereItIsListed(string section, string field, string explanation)
    {
        var fields = Description.SectionFields(section);

        var line = fields.Single(l => l.StartsWith(field, StringComparison.Ordinal));
        Assert.Contains(explanation, line, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("texts:", "figtext")]
    [InlineData("images:", "figimage")]
    [InlineData("legends:", "every subplot")]
    [InlineData("legends:", "fig.legend()")]
    [InlineData("legend:", "write legends instead")]
    public void TheFiguresTextsImagesAndLegends_AreExplainedWhereTheyAreListed(string field, string explanation)
    {
        var figureFields = Description.SectionFields("Figure fields:");

        var line = figureFields.Single(l => l.StartsWith(field, StringComparison.Ordinal));
        Assert.Contains(explanation, line, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The cell of a grid a subplot was added to, and its buy and sell signals, are listed with the subplot's fields and
    /// say what they hold: the cell as matplotlib's add_subplot names it, a signal with the directions the reader accepts.</summary>
    [Theory]
    [InlineData("subplot: object", "add_subplot")]
    [InlineData("subplot: object", "index")]
    [InlineData("signals: object[]", "Buy")]
    [InlineData("signals: object[]", "Sell")]
    public void TheSubplotFields_ListTheCellOfAGridAndTheSignals_AndSayWhatTheyHold(string field, string explanation)
    {
        var subplotFields = Description.SectionFields("Subplot fields");

        var line = subplotFields.Single(l => l.StartsWith(field, StringComparison.Ordinal));
        Assert.Contains(explanation, line, StringComparison.Ordinal);
    }

    /// <summary>The zoom an inset indicates on its parent is listed with the subplot's fields, said to work on an inset only,
    /// and has a section of its own whose fields say what they draw.</summary>
    [Fact]
    public void TheSubplotFields_ListTheZoomIndicator_AsAnInsetsSetting()
    {
        var subplotFields = Description.SectionFields("Subplot fields");

        var line = subplotFields.Single(l => l.StartsWith("zoomIndicator: object", StringComparison.Ordinal));
        Assert.Contains("inset", line, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("indicate_inset_zoom", line, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("color: string (colour)", "neutral grey")]
    [InlineData("alpha: number", "0.5")]
    [InlineData("lineWidth: number", "pixels")]
    [InlineData("lineStyle: string (enum)", "Dashed")]
    [InlineData("connectors: boolean", "two")]
    public void TheZoomIndicatorFields_AreListedAndExplained(string field, string explanation)
    {
        var fields = Description.SectionFields("Zoom indicator fields");

        var line = fields.Single(l => l.StartsWith(field, StringComparison.Ordinal));
        Assert.Contains(explanation, line, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The normaliser is not a public member of the series record, so its line is written by hand: one chart
    /// type's description still names it.</summary>
    [Fact]
    public void AHeatmapsDescription_NamesItsNormaliser()
    {
        string schema = Description.Describe("heatmap");

        Assert.Contains(SourceFile.LinesOf(schema), line => line.TrimStart().StartsWith("normalizer:", StringComparison.Ordinal));
    }
}
