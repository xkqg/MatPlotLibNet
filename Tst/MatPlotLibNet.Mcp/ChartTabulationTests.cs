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
        string text = _tabulation.Tabulate(Line).Markdown;

        Assert.Contains("**Revenue**", text);
        Assert.Contains("| Quarter | 2026 |", text);
        Assert.Contains("| 3 | 15 |", text);
    }

    [Fact]
    public void AChartWithNothingTabular_SaysSo_RatherThanReturningNothing()
    {
        // A quiver key is a reference arrow — an annotation, not data. The chart draws; the table has nothing.
        string text = _tabulation.Tabulate(Spec(
            """{"width":200,"height":200,"subPlots":[{"series":[{"type":"quiverkey","quiverKeyX":0.5,"quiverKeyY":0.9,"quiverKeyU":1,"quiverKeyLabel":"1 m/s"}]}]}""")).Markdown;

        Assert.Contains("no tabular", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TwoSubplots_AreTwoTables()
    {
        string text = _tabulation.Tabulate(Spec(
            """{"width":400,"height":300,"subPlots":[{"title":"A","series":[{"type":"line","xData":[1],"yData":[2]}]},{"title":"B","series":[{"type":"line","xData":[3],"yData":[4]}]}]}""")).Markdown;

        Assert.Contains("**A**", text);
        Assert.Contains("**B**", text);
    }

    [Fact]
    public void ATableBeyondTheCeiling_IsRefused_WithTheCeilingInTheMessage()
    {
        string xs = string.Join(',', Enumerable.Range(0, 600));
        var spec = Spec($$"""{"width":400,"height":300,"subPlots":[{"series":[{"type":"line","xData":[{{xs}}],"yData":[{{xs}}]}]}]}""");

        var refusal = Assert.Throws<ToolRefusalException>(() => _tabulation.Tabulate(spec));

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

        Assert.Contains("| 0 | 0 |", _tabulation.Tabulate(spec).Markdown);
    }

    [Fact]
    public void ABadSpec_IsRefused_TheSameWayEveryOtherToolRefusesIt()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() =>
            _tabulation.Tabulate(Spec("""{"width":400,"height":300,"subPlots":[{"series":[{"type":"lien","xData":[1],"yData":[2]}]}]}""")));

        Assert.Contains("'lien'", refusal.Message);
    }

    // ── the same data as values, beside the markdown ───────────────────────────
    //
    // Markdown is what a model reads; it is not what a model computes with. Summing a column meant parsing a
    // pipe table back into numbers, which is a step that can go wrong silently. The tool now carries both: the
    // markdown in the text it always returned, and the same values as structured content beside it.

    [Fact]
    public void TheValues_ComeBackAsNumbers_NotAsText()
    {
        var table = Assert.Single(_tabulation.Tabulate(Line).Tables);

        Assert.Equal("Revenue", table.Caption);
        Assert.Equal(3, table.Rows.Count);
        Assert.Equal(3.0, table.Rows[2][0]);
        Assert.Equal(15.0, table.Rows[2][1]);
    }

    [Fact]
    public void EveryColumn_SaysWhatItHoldsAndWhichAxisItIsOn()
    {
        var table = Assert.Single(_tabulation.Tabulate(Line).Tables);

        Assert.Equal(["Quarter", "2026"], table.Columns.Select(c => c.Header));
        Assert.Equal(["number", "number"], table.Columns.Select(c => c.Kind));
        Assert.Equal(["x", "y"], table.Columns.Select(c => c.Axis));
    }

    [Fact]
    public void ADateAxis_ComesBackAsADate_NotAsTheNumberADateIsStoredAs()
    {
        // A date axis carries OLE Automation numbers. 46277.58 means nothing to a reader of the JSON, so a date
        // column is written out in full, to the millisecond, in the one order that sorts correctly.
        double noon = new DateTime(2026, 9, 12, 12, 0, 0, DateTimeKind.Unspecified).ToOADate();
        var spec = Spec($$"""{"width":400,"height":300,"subPlots":[{"xAxis":{"scale":"date"},"series":[{"type":"line","xData":[{{noon.ToString(System.Globalization.CultureInfo.InvariantCulture)}}],"yData":[7]}]}]}""");

        var table = Assert.Single(_tabulation.Tabulate(spec).Tables);

        Assert.Equal("date", table.Columns[0].Kind);
        Assert.Equal("2026-09-12T12:00:00.000", table.Rows[0][0]);
        Assert.Equal(7.0, table.Rows[0][1]);
    }

    [Fact]
    public void ANumberNoDateCouldEverBe_StaysANumber()
    {
        // The range a date can be written in is finite. A value outside it is reported as the number it is,
        // rather than throwing away a whole table over one cell.
        var spec = Spec("""{"width":400,"height":300,"subPlots":[{"xAxis":{"scale":"date"},"series":[{"type":"line","xData":[9e9],"yData":[1]}]}]}""");

        var table = Assert.Single(_tabulation.Tabulate(spec).Tables);

        Assert.Equal("date", table.Columns[0].Kind);
        Assert.Equal(9e9, table.Rows[0][0]);
    }

    [Fact]
    public void ATextColumn_ComesBackAsText()
    {
        var spec = Spec("""{"width":400,"height":300,"subPlots":[{"series":[{"type":"bar","categories":["north","south"],"values":[3,4]}]}]}""");

        var table = Assert.Single(_tabulation.Tabulate(spec).Tables);

        Assert.Equal("text", table.Columns[0].Kind);
        Assert.Equal("north", table.Rows[0][0]);
        Assert.Equal(3.0, table.Rows[0][1]);
    }

    [Fact]
    public void AChartWithNothingTabular_HasNoTablesEither()
    {
        var result = _tabulation.Tabulate(Spec(
            """{"width":200,"height":200,"subPlots":[{"series":[{"type":"quiverkey","quiverKeyX":0.5,"quiverKeyY":0.9,"quiverKeyU":1,"quiverKeyLabel":"1 m/s"}]}]}"""));

        Assert.Empty(result.Tables);
        Assert.Contains("no tabular", result.Markdown, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TwoSubplots_AreTwoTablesInTheValuesToo()
    {
        var result = _tabulation.Tabulate(Spec(
            """{"width":400,"height":300,"subPlots":[{"title":"A","series":[{"type":"line","xData":[1],"yData":[2]}]},{"title":"B","series":[{"type":"line","xData":[3],"yData":[4]}]}]}"""));

        Assert.Equal(["A", "B"], result.Tables.Select(t => t.Caption));
    }
}
