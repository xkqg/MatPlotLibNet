// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>A chord diagram shows the flows between the groups of a square matrix: each group an arc of a circle, each pair with
/// a flow between them a ribbon. It fills its plot without axes around it, takes a colour for each group and names each group in
/// the legend; its table is the matrix as source, target and value. What cannot be drawn (a matrix that is not square or holds
/// nothing, a name missing for a group) is refused where the series is built.</summary>
public class ChordSeriesTests
{
    private static readonly string[] Names = ["North", "South", "East"];

    private static readonly double[,] Flows =
    {
        { 0, 5, 2 },
        { 3, 0, 0 },
        { 0, 4, 6 },
    };

    [Fact]
    public void TheVerb_AddsTheSeries_AndLeavesTheAxesCartesianWithoutAxesAroundIt()
    {
        var axes = new Axes();

        var series = axes.Chord(Names, Flows);

        Assert.Same(series, Assert.Single(axes.Series));
        Assert.Equal(CoordinateSystem.Cartesian, axes.CoordinateSystem);
        Assert.True(series.DrawsWithoutAxes);
        Assert.Equal(3, series.ColorSlotSpan);
        Assert.Equal(Names, series.Names);
        Assert.Equal(3, series.Flows.GetLength(0));
    }

    [Fact]
    public void ANameMissingForAGroup_IsRefusedByName()
    {
        var refusal = Assert.Throws<ArgumentException>(() => new Axes().Chord(["North", "South"], Flows));

        Assert.Contains("names", refusal.Message, StringComparison.Ordinal);
        Assert.Equal("names", refusal.ParamName);
    }

    [Fact]
    public void AMatrixThatCannotBeDrawn_IsRefusedWhereTheSeriesIsBuilt()
    {
        Assert.Throws<ArgumentException>(() => new Axes().Chord(["A", "B"], new double[2, 3]));
        Assert.Throws<ArgumentException>(() => new Axes().Chord(["A", "B"], new double[2, 2]));   // all zero
        Assert.Throws<ArgumentException>(() => new Axes().Chord(["A", "B"], new double[,] { { 1, -1 }, { 0, 1 } }));
    }

    [Fact]
    public void TheLegend_HasAnEntryForEachGroup_InTheColourItsArcWears()
    {
        var parts = new Axes().Chord(["North", "", "East"], Flows).LegendParts;

        Assert.Equal(["North", "East"], parts.Select(p => p.Label));   // a group with no name has no entry
        Assert.Equal([0, 2], parts.Select(p => p.ColorSlotOffset));
    }

    [Fact]
    public void TheTable_IsTheMatrixAsSourceTargetAndValue_RowByRow_WithoutTheEmptyCells()
    {
        var table = new Axes().Chord(Names, Flows).ToDataTable();

        Assert.NotNull(table);
        Assert.Equal(["source", "target", "value"], table!.Columns.Select(c => c.Header));
        Assert.Equal(
            [("North", "South", 5.0), ("North", "East", 2.0), ("South", "North", 3.0), ("East", "South", 4.0), ("East", "East", 6.0)],
            table.Rows.Select(r => (r[0].Text!, r[1].Text!, r[2].Number)));
    }

    [Fact]
    public void ARangeIsContributedToNoAxis()
    {
        var contribution = new Axes().Chord(Names, Flows).ComputeDataRange(new NoLimits());

        Assert.Equal(new DataRangeContribution(null, null, null, null), contribution);
    }

    private sealed class NoLimits : IAxesContext
    {
        public double? XAxisMin => null;
        public double? XAxisMax => null;
        public double? YAxisMin => null;
        public double? YAxisMax => null;
        public BarMode BarMode => default;
        public IReadOnlyList<ISeries> AllSeries => [];
    }

    // ── what can be set ─────────────────────────────────────────────────────────

    [Fact]
    public void ThePaddingAndTheRibbonOpacity_HaveTheirRestingValues_AndRefuseWhatCannotBeDrawn()
    {
        var series = new Axes().Chord(Names, Flows);

        Assert.Equal(0.05, series.Padding);
        Assert.Equal(0.67, series.RibbonAlpha);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.Padding = -0.1);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.Padding = 2.1);   // three groups: 3 * 2.1 is more than the circle
        Assert.Throws<ArgumentOutOfRangeException>(() => series.RibbonAlpha = 1.5);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.RibbonAlpha = double.NaN);
        series.Padding = 0.2;
        series.RibbonAlpha = 0;
        Assert.Equal(0.2, series.Padding);
        Assert.Equal(0.0, series.RibbonAlpha);
    }

    // ── the JSON ────────────────────────────────────────────────────────────────

    [Fact]
    public void TheSeries_RoundTripsThroughTheJson_WithWhatItDraws()
    {
        var figure = new Figure { Width = 500, Height = 500 };
        var series = figure.AddSubPlot(1, 1, 1).Chord(Names, Flows);
        series.Padding = 0.1;
        series.RibbonAlpha = 0.5;
        series.Colors = [Color.FromHex("#C03030"), Color.FromHex("#30C030")];
        series.Label = "traffic";

        var read = new ChartSerializer().FromJson(figure.ToJson());

        var back = Assert.IsType<ChordSeries>(Assert.Single(read.SubPlots[0].Series));
        Assert.Equal(Names, back.Names);
        Assert.Equal(Flows, back.Flows);
        Assert.Equal(0.1, back.Padding);
        Assert.Equal(0.5, back.RibbonAlpha);
        Assert.Equal(series.Colors, back.Colors);
        Assert.Equal("traffic", back.Label);
    }

    [Fact]
    public void WhatIsAtRest_IsLeftOutOfTheJson()
    {
        var dto = new Axes().Chord(Names, Flows).ToSeriesDto();

        Assert.Equal("chord", dto.Type);
        Assert.Null(dto.ChordPadding);
        Assert.Null(dto.Alpha);
        Assert.Null(dto.Colors);
        Assert.Equal(Names, dto.Categories);
        Assert.Equal(3, dto.ChordFlows!.Count);
    }

    [Fact]
    public void TheChordKind_IsInTheRegistry() => Assert.Contains("chord", SeriesRegistry.Discriminators);
}
