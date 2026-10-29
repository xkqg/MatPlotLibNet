// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>The schema a model reads has to be the schema the reader enforces, or the server documents one thing
/// and accepts another. Both come off the same DTO records, and the example is run through the real renderer.</summary>
[Collection("ChartServicesGlobalState")]
public class ChartSchemaDescriptionTests
{
    private static readonly ChartTypeCatalog Catalog = new();
    private static readonly ChartSchemaDescription Description = new(Catalog);

    [Fact]
    public void TheSchema_ShowsTheFieldsAFigureSpecNeeds()
    {
        string schema = Description.Describe(null);

        Assert.Contains("width: number", schema);
        Assert.Contains("height: number", schema);
        Assert.Contains("subPlots", schema);
        Assert.Contains("series", schema);
        Assert.Contains("xData: number[]", schema);
    }

    [Fact]
    public void AskingAboutOneChartType_DescribesThatOne()
    {
        string schema = Description.Describe("candlestick");

        Assert.Contains("'candlestick'", schema);
        Assert.Contains("open: number[]", schema);
        Assert.Contains("close: number[]", schema);
    }

    [Fact]
    public void AskingAboutAnUnknownChartType_IsRefusedByName_WithTheNearest()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Description.Describe("candlestik"));

        Assert.Contains("'candlestik'", refusal.Message);
        Assert.Contains("'candlestick'", refusal.Message);
    }

    [Theory]
    [InlineData("geo_polygon")] // MatPlotLibNet.Geo's own discriminator, but nothing registers a factory for it
    [InlineData("nope")]
    public void AChartTypeThisServerDoesNotList_IsRefusedToo(string type) =>
        Assert.Throws<ToolRefusalException>(() => Description.Describe(type));

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

        var label = schema.Split(Environment.NewLine).Single(line => line.TrimStart().StartsWith("label:", StringComparison.Ordinal));
        Assert.Contains("legend", label, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("labelPlacement", label);
        Assert.Contains("lineEnd", label, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ThePlacementField_IsListedWithItsAcceptedValues()
    {
        string schema = Description.Describe("line");

        var placement = schema.Split(Environment.NewLine).Single(line => line.TrimStart().StartsWith("labelPlacement:", StringComparison.Ordinal));
        Assert.Contains("Legend", placement);
        Assert.Contains("LineEnd", placement);
        Assert.Contains("line", placement, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A field whose wire name says little is explained where it is listed: <c>c</c> is matplotlib's name for
    /// the values a colour map colours, an area's step mode travels in <c>drawStyle</c>, and <c>fill</c> is a switch,
    /// not a colour.</summary>
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
    public void AFieldWhoseNameSaysLittle_IsExplainedWhereItIsListed(string type, string field, string explanation)
    {
        string schema = Description.Describe(type);

        var line = schema.Split(Environment.NewLine).Single(l => l.TrimStart().StartsWith(field, StringComparison.Ordinal));
        Assert.Contains(explanation, line, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheFieldNames_AreTheWireNames_NotTheClrNames()
    {
        string schema = Description.Describe(null);

        Assert.Contains("altText", schema);
        Assert.DoesNotContain("AltText", schema);
    }

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

        Assert.Contains(schema.Split(Environment.NewLine), line => line.Trim() == kind);
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

        var line = schema.Split(Environment.NewLine).First(l => l.TrimStart().StartsWith(field, StringComparison.Ordinal));
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
        string[] section = schema[at..].Split(Environment.NewLine);
        Assert.All(AxesSeatShape.OfSubplot.SlotLists, list => Assert.Contains($".{list}", section[0]));
        Assert.Contains(section.Skip(1).Take(2), line => line.Trim() == "axis: object");
        Assert.Contains(section.Skip(1).Take(2), line => line.Trim().StartsWith("series: object[]", StringComparison.Ordinal));
    }

    /// <summary>The lists of Y axes and the gap between their titles carry little in their names: the schema says what an
    /// entry is, which end of the plot it stands at, and which list continues which.</summary>
    [Theory]
    [InlineData("extraRightYAxes:", "secondaryYAxis")]
    [InlineData("rightYAxesOverflow:", "extraRightYAxes")]
    [InlineData("leftYAxes:", "yAxis")]
    [InlineData("leftYAxesOverflow:", "continues")]
    [InlineData("yAxisTitleGap:", "pixels")]
    public void AFieldOfTheYAxisLists_IsExplainedWhereItIsListed(string field, string explanation)
    {
        string schema = Description.Describe(null);

        var line = schema.Split(Environment.NewLine).Single(l => l.TrimStart().StartsWith(field, StringComparison.Ordinal));
        Assert.Contains(explanation, line);
    }

    [Fact]
    public void TheDescriptionOfOneChartType_NamesEverySeatASeriesMayStandAt()
    {
        string schema = Description.Describe("line");

        Assert.Contains("leftYAxes[].series[]", schema);
    }

    /// <summary>The normaliser is not a public member of the series record, so its line is written by hand: one chart
    /// type's description still names it.</summary>
    [Fact]
    public void AHeatmapsDescription_NamesItsNormaliser()
    {
        string schema = Description.Describe("heatmap");

        Assert.Contains(schema.Split(Environment.NewLine), line => line.TrimStart().StartsWith("normalizer:", StringComparison.Ordinal));
    }
}
