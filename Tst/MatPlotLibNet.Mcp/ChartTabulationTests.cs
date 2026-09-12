// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>A model can look at the PNG, but it cannot read a number off it. The fifth tool answers the question
/// the picture cannot: what are the values. It is markdown, because that is the form a model reads without a
/// parser, and it is capped, because a ten-thousand-row table is a context window spent on one chart.</summary>
public class ChartTabulationTests
{
    private readonly ChartTabulation _tabulation = new(
        new ChartSpecReader(new ChartTypeCatalog(), RenderLimits.Default), RenderLimits.Default);

    private static ChartSpec Spec(string json) => new(json);

    private static readonly ChartSpec Line = Spec(
        """{"width":400,"height":300,"title":"Revenue","subPlots":[{"xAxis":{"label":"Quarter"},"series":[{"type":"line","xData":[1,2,3],"yData":[12,18,15],"label":"2026"}]}]}""");

    [Fact]
    public void TheTable_IsMarkdownUnderTheChartsOwnName()
    {
        string text = _tabulation.Describe(Line);

        Assert.Contains("**Revenue**", text);
        Assert.Contains("| Quarter | 2026 |", text);
        Assert.Contains("| 3 | 15 |", text);
    }

    [Fact]
    public void AChartWithNothingTabular_SaysSo_RatherThanReturningNothing()
    {
        // A quiver key is a reference arrow — an annotation, not data. The chart draws; the table has nothing.
        string text = _tabulation.Describe(Spec(
            """{"width":200,"height":200,"subPlots":[{"series":[{"type":"quiverkey","quiverKeyX":0.5,"quiverKeyY":0.9,"quiverKeyU":1,"quiverKeyLabel":"1 m/s"}]}]}"""));

        Assert.Contains("no tabular", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TwoSubplots_AreTwoTables()
    {
        string text = _tabulation.Describe(Spec(
            """{"width":400,"height":300,"subPlots":[{"title":"A","series":[{"type":"line","xData":[1],"yData":[2]}]},{"title":"B","series":[{"type":"line","xData":[3],"yData":[4]}]}]}"""));

        Assert.Contains("**A**", text);
        Assert.Contains("**B**", text);
    }

    [Fact]
    public void ATableBeyondTheCeiling_IsRefused_WithTheCeilingInTheMessage()
    {
        string xs = string.Join(',', Enumerable.Range(0, 600));
        var spec = Spec($$"""{"width":400,"height":300,"subPlots":[{"series":[{"type":"line","xData":[{{xs}}],"yData":[{{xs}}]}]}]}""");

        var refusal = Assert.Throws<ToolRefusalException>(() => _tabulation.Describe(spec));

        Assert.Contains("600", refusal.Message);
        Assert.Contains(RenderLimits.Default.MaxTableRows.ToString(System.Globalization.CultureInfo.InvariantCulture), refusal.Message);
        Assert.Contains("save_chart", refusal.Message);
    }

    [Fact]
    public void ATableExactlyAtTheCeiling_IsAllowed()
    {
        int rows = RenderLimits.Default.MaxTableRows;
        string xs = string.Join(',', Enumerable.Range(0, rows));
        var spec = Spec($$"""{"width":400,"height":300,"subPlots":[{"series":[{"type":"line","xData":[{{xs}}],"yData":[{{xs}}]}]}]}""");

        Assert.Contains("| 0 | 0 |", _tabulation.Describe(spec));
    }

    [Fact]
    public void ABadSpec_IsRefused_TheSameWayEveryOtherToolRefusesIt()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() =>
            _tabulation.Describe(Spec("""{"width":400,"height":300,"subPlots":[{"series":[{"type":"lien","xData":[1],"yData":[2]}]}]}""")));

        Assert.Contains("'lien'", refusal.Message);
    }
}
