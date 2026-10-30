// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>A ternary series is a list of compositions of three parts, drawn as points or as a line in a triangle. The amounts
/// are kept as they were given (a count, a percentage, a mass) and closed to fractions only to be placed, so the table shows
/// what the author knows and the picture shows the proportions. A sample that cannot be placed is refused where the series is
/// built, by the row and the part, and not drawn at some corner by a division by zero.</summary>
public class TernarySeriesTests
{
    private static readonly double[] Top = [0.2, 2, 20];
    private static readonly double[] Left = [0.5, 5, 50];
    private static readonly double[] Right = [0.3, 3, 30];

    [Fact]
    public void TheVerbs_PutTheAxesOnTheTernaryCoordinateSystem()
    {
        var plot = new Axes();
        var scatter = new Axes();

        plot.TernaryPlot(Top, Left, Right);
        scatter.TernaryScatter(Top, Left, Right);

        Assert.Equal(CoordinateSystem.Ternary, plot.CoordinateSystem);
        Assert.Equal(CoordinateSystem.Ternary, scatter.CoordinateSystem);
        Assert.IsType<TernaryLineSeries>(Assert.Single(plot.Series));
        Assert.IsType<TernaryScatterSeries>(Assert.Single(scatter.Series));
    }

    [Fact]
    public void TheAmounts_AreKeptAsGiven_AndPlacedAsFractions()
    {
        var series = new Axes().TernaryScatter(Top, Left, Right);

        Assert.Equal(20.0, series.Top[2]);   // the table and the JSON hold what the author knew
        var placed = series.FramePoints();

        Assert.Equal(3, placed.Length);
        // T3: (0.2, 0.5, 0.3), (2, 5, 3) and (20, 50, 30) are one place, T1 of the vectors.
        foreach (var at in placed)
        {
            Assert.Equal(0.4, at.Position.X, 6);
            Assert.Equal(0.173205, at.Position.Y, 6);
        }

        Assert.Equal([0, 1, 2], placed.Select(p => p.Row));
    }

    [Theory]
    [InlineData(new[] { 1.0, 2.0 }, new[] { 1.0, -2.0 }, new[] { 1.0, 1.0 }, "Row 1", "Left")]
    [InlineData(new[] { double.NaN, 2.0 }, new[] { 1.0, 2.0 }, new[] { 1.0, 1.0 }, "Row 0", "Top")]
    [InlineData(new[] { 1.0, 2.0 }, new[] { 1.0, 2.0 }, new[] { 1.0, double.PositiveInfinity }, "Row 1", "Right")]
    [InlineData(new[] { 1.0, 0.0 }, new[] { 1.0, 0.0 }, new[] { 1.0, 0.0 }, "Row 1", "all three parts are zero")]
    public void ASampleThatCannotBePlaced_IsRefusedByItsRowAndItsPart(double[] top, double[] left, double[] right, string row, string what)
    {
        var scatterRefusal = Assert.Throws<ArgumentException>(() => new Axes().TernaryScatter(top, left, right));
        var lineRefusal = Assert.Throws<ArgumentException>(() => new Axes().TernaryPlot(top, left, right));

        foreach (var refusal in new[] { scatterRefusal, lineRefusal })
        {
            Assert.Contains(row, refusal.Message, StringComparison.Ordinal);
            Assert.Contains(what, refusal.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ArraysOfDifferentLengths_AreRefusedLikeEveryOtherSeriesDoes()
    {
        Assert.Throws<ArgumentException>(() => new Axes().TernaryScatter([1.0, 2.0], [1.0], [1.0, 2.0]));
        Assert.Throws<ArgumentException>(() => new Axes().TernaryPlot([1.0], [1.0, 2.0], [1.0]));
    }

    [Fact]
    public void ASeriesWithNoSamples_IsAllowed_AndPlacesNothing()
    {
        var series = new Axes().TernaryScatter([], [], []);

        Assert.Empty(series.FramePoints());
        Assert.Empty(series.ToDataTable()!.Rows);
    }

    [Fact]
    public void ASampleBuiltWithoutTheVerbThatCannotBePlaced_IsLeftOutOfWhatIsPlaced_NotThrown()
    {
        // Only the verbs refuse, as with every series; one built directly and then drawn must not take the figure down.
        var series = new TernaryScatterSeries([1.0, -1.0, 1.0], [1.0, 1.0, 0.0], [1.0, 1.0, 0.0]);

        Assert.Equal([0, 2], series.FramePoints().Select(p => p.Row));
    }

    [Fact]
    public void TheTable_HasTheRawAmountsAndTheirSum()
    {
        var table = new Axes().TernaryScatter(Top, Left, Right).ToDataTable();

        Assert.NotNull(table);
        Assert.Equal(["top", "left", "right", "sum"], table!.Columns.Select(c => c.Header));
        Assert.Equal(3, table.Rows.Count);
        Assert.Equal(20.0, table.Rows[2][0].Number);
        Assert.Equal(100.0, table.Rows[2][3].Number);
        Assert.Equal(1.0, table.Rows[0][3].Number, 12);
    }

    [Fact]
    public void ARangeIsContributedToNoAxis_BecauseTheFrameIsFixed()
    {
        var contribution = new Axes().TernaryScatter(Top, Left, Right).ComputeDataRange(new NoLimits());

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

    // ── the picture, the JSON ───────────────────────────────────────────────────

    [Fact]
    public void AScatter_RoundTripsThroughTheJson_WithWhatItDraws()
    {
        var figure = new Figure { Width = 500, Height = 400 };
        var axes = figure.AddSubPlot(1, 1, 1);
        axes.XAxis.Label = "Sand";
        var scatter = axes.TernaryScatter(Top, Left, Right);
        scatter.Color = Color.FromHex("#C03030");
        scatter.MarkerSize = 9;
        scatter.Label = "samples";

        var read = new ChartSerializer().FromJson(figure.ToJson());

        var back = Assert.IsType<TernaryScatterSeries>(Assert.Single(read.SubPlots[0].Series));
        Assert.Equal(CoordinateSystem.Ternary, read.SubPlots[0].CoordinateSystem);
        Assert.Equal(Top, back.Top);
        Assert.Equal(Left, back.Left);
        Assert.Equal(Right, back.Right);
        Assert.Equal(Color.FromHex("#C03030"), back.Color);
        Assert.Equal(9.0, back.MarkerSize);
        Assert.Equal("samples", back.Label);
        Assert.Equal("Sand", read.SubPlots[0].XAxis.Label);
    }

    [Fact]
    public void ALine_RoundTripsThroughTheJson_WithItsStyle()
    {
        var figure = new Figure { Width = 500, Height = 400 };
        var line = figure.AddSubPlot(1, 1, 1).TernaryPlot(Top, Left, Right);
        line.LineStyle = LineStyle.Dashed;
        line.LineWidth = 3;

        var read = new ChartSerializer().FromJson(figure.ToJson());

        var back = Assert.IsType<TernaryLineSeries>(Assert.Single(read.SubPlots[0].Series));
        Assert.Equal(LineStyle.Dashed, back.LineStyle);
        Assert.Equal(3.0, back.LineWidth);
    }

    [Fact]
    public void WhatIsAtRest_IsLeftOutOfTheJson()
    {
        var dto = new Axes().TernaryScatter(Top, Left, Right).ToSeriesDto();
        var lineDto = new Axes().TernaryPlot(Top, Left, Right).ToSeriesDto();

        Assert.Equal("ternaryscatter", dto.Type);
        Assert.Null(dto.MarkerSize);
        Assert.Equal("ternaryline", lineDto.Type);
        Assert.Null(lineDto.LineStyle);
        Assert.Null(lineDto.LineWidth);
    }

    [Fact]
    public void BothKinds_AreInTheRegistry()
    {
        Assert.Contains("ternaryscatter", SeriesRegistry.Discriminators);
        Assert.Contains("ternaryline", SeriesRegistry.Discriminators);
    }

    [Fact]
    public void ASeriesWithALabel_HasALegendEntry()
    {
        var scatter = new Axes().TernaryScatter(Top, Left, Right);
        scatter.Label = "samples";

        Assert.Equal("samples", Assert.Single(scatter.LegendParts).Label);
    }
}
