// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>The schema a model reads has to be the schema the reader enforces, or the server documents one thing
/// and accepts another. Both come off the same DTO records, and the example is run through the real renderer.</summary>
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

    [Fact]
    public void AChartTypeThisServerDoesNotList_IsRefusedToo() =>
        Assert.Throws<ToolRefusalException>(() => Description.Describe("sankey"));

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
    public void TheFieldNames_AreTheWireNames_NotTheClrNames()
    {
        string schema = Description.Describe(null);

        Assert.Contains("altText", schema);
        Assert.DoesNotContain("AltText", schema);
    }
}
