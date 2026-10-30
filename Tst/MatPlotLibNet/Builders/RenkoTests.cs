// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The <c>Renko</c> verb: the bricks themselves are worked out by <see cref="Numerics.RenkoExtensions"/> (held to a
/// hand trace in <c>RenkoBrickTests</c>); this holds what the verb draws with them. Two floating bar series, one for the bricks
/// that rose and one for those that fell, each brick a slot wide on the position of its number so that neighbours touch, and
/// the verb draws exactly the picture the hand chain of those series draws.</summary>
public class RenkoTests
{
    private static readonly double[] Closes = [10, 10.4, 11.0, 13.2, 12.5, 11.0, 10.4, 7.8, 8.9, 10.0, 9.0, 15.9];

    private static Axes Chart(double[] closes, double brick = 1, Action<RenkoParts>? configure = null) =>
        Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Renko(closes, brick, configure)).Build().SubPlots[0];

    private static Figure Drawn(double[] closes, double brick = 1, Action<RenkoParts>? configure = null) =>
        Plt.Create().WithSize(800, 450).AddSubPlot(1, 1, 1, ax => ax.Renko(closes, brick, configure).WithLegend()).Build();

    [Fact]
    public void TheVerb_DrawsExactlyWhatTheHandRecipeDraws()
    {
        // Thirteen bricks (the vector of RenkoBrickTests), worked out by hand: bricks 0-2 and 7-12 rose, 3-6 fell, each brick
        // a unit tall standing on its bottom, centred on its number plus a half.
        var byHand = Plt.Create().WithSize(800, 450).AddSubPlot(1, 1, 1, ax =>
        {
            ax.AddSeries(new BarSeries(["0", "1", "2", "7", "8", "9", "10", "11", "12"], [1.0, 1, 1, 1, 1, 1, 1, 1, 1])
            {
                XCoordinate = [0.5, 1.5, 2.5, 7.5, 8.5, 9.5, 10.5, 11.5, 12.5],
                StackBaseline = [10.0, 11, 12, 9, 10, 11, 12, 13, 14],
                BarWidth = 1, Color = Colors.Green, Label = "up",
            });
            ax.AddSeries(new BarSeries(["3", "4", "5", "6"], [1.0, 1, 1, 1])
            {
                XCoordinate = [3.5, 4.5, 5.5, 6.5],
                StackBaseline = [11.0, 10, 9, 8],
                BarWidth = 1, Color = Colors.Red, Label = "down",
            });
            ax.SetXTickLocator(new FixedLocator([]));
            ax.WithLegend();
        }).Build();

        Assert.Equal(byHand.ToSvg(), Drawn(Closes).ToSvg());
    }

    [Fact]
    public void TheParts_AreTheTwoSeriesOnTheAxes_TheRisingBricksAndTheFallingOnes()
    {
        RenkoParts parts = default;
        var axes = Chart(Closes, configure: p => parts = p);

        Assert.Equal([parts.Up, parts.Down], axes.Series.Cast<BarSeries>());
        Assert.Equal(("up", "down"), (parts.Up.Label, parts.Down.Label));
        Assert.Equal((Colors.Green, Colors.Red), (parts.Up.Color, parts.Down.Color));
        Assert.Equal(9, parts.Up.Values.Length);
        Assert.Equal(4, parts.Down.Values.Length);
    }

    [Fact]
    public void WhatTheCallbackSets_WinsOverWhatTheVerbDid()
    {
        var axes = Chart(Closes, configure: p => { p.Up.Color = Colors.Blue; p.Down.Label = "fell"; });

        Assert.Equal(Colors.Blue, ((BarSeries)axes.Series[0]).Color);
        Assert.Equal("fell", ((BarSeries)axes.Series[1]).Label);
    }

    [Fact]
    public void TheBricksOfAChart_TouchCornerToCorner_AtAnyBrickSize()
    {
        // Bricks of 0.25 on the same closes scaled down: the same picture, the corners still touching.
        double[] scaled = [.. Closes.Select(price => price * 0.25)];
        RenkoParts parts = default;
        Chart(scaled, 0.25, p => parts = p);

        var bricks = parts.Up.XCoordinate!.Zip(parts.Up.StackBaseline!, (x, foot) => (X: x, Bottom: foot, Top: foot + 0.25))
            .Concat(parts.Down.XCoordinate!.Zip(parts.Down.StackBaseline!, (x, foot) => (X: x, Bottom: foot, Top: foot + 0.25)))
            .OrderBy(brick => brick.X).ToList();
        bool[] rising = [.. bricks.Select(brick => parts.Up.XCoordinate!.Contains(brick.X))];

        Assert.Equal(13, bricks.Count);
        for (int i = 1; i < bricks.Count; i++)
        {
            double touch = rising[i] ? bricks[i - 1].Top : bricks[i - 1].Bottom;
            Assert.Equal(touch, rising[i] ? bricks[i].Bottom : bricks[i].Top, 9);
            Assert.Equal(1.0, bricks[i].X - bricks[i - 1].X, 12);
        }
    }

    [Fact]
    public void UnderAStackedBarMode_TheBricksStillStandOnTheirOwnBottom()
    {
        var stacked = Plt.Create().WithSize(800, 450).AddSubPlot(1, 1, 1, ax => ax.Renko(Closes, 1).SetBarMode(BarMode.Stacked)).Build();
        var plain = Plt.Create().WithSize(800, 450).AddSubPlot(1, 1, 1, ax => ax.Renko(Closes, 1)).Build();

        Assert.Equal(plain.ToSvg(), stacked.ToSvg());
    }

    [Fact]
    public void TheXAxis_CarriesNoTicks_BecauseItIsOnlyTheOrderOfTheBricks()
    {
        var axes = Chart(Closes);

        Assert.IsType<FixedLocator>(axes.XAxis.TickLocator);
        Assert.Empty(axes.XAxis.TickLocator!.Locate(0, 13));
    }

    [Fact]
    public void TheTable_IsOneRowPerBrick_ItsNumberAndWhereItStartsAndEnds()
    {
        RenkoParts parts = default;
        Chart(Closes, configure: p => parts = p);

        var down = parts.Down.ToDataTable()!;

        Assert.Equal(["category", "from", "to"], down.Columns.Select(c => c.Header));
        Assert.Equal(["3", "4", "5", "6"], down.Rows.Select(row => row[0].Text));
        Assert.Equal([11.0, 10.0, 9.0, 8.0], down.Rows.Select(row => row[1].Number));
        Assert.Equal([12.0, 11.0, 10.0, 9.0], down.Rows.Select(row => row[2].Number));
    }

    [Fact]
    public void BothSeriesExist_EvenWhenThePricesDrawNoBrickOfOneKind()
    {
        RenkoParts parts = default;
        Chart([10, 11, 12], configure: p => parts = p);

        Assert.Equal(2, parts.Up.Values.Length);
        Assert.Empty(parts.Down.Values);
    }

    [Fact]
    public void PricesThatDrawNoBrick_LeaveTwoEmptySeries_AndDoNotBreakTheFigure()
    {
        var figure = Drawn([10, 10.4, 9.8]);

        Assert.All(figure.SubPlots[0].Series.Cast<BarSeries>(), series => Assert.Empty(series.Values));
        Assert.NotEmpty(figure.ToSvg());
    }

    [Fact]
    public void TheChart_SurvivesTheJson_AndDrawsTheSamePicture()
    {
        var figure = Drawn(Closes);

        var back = ChartSerializerDefault.Current.FromJson(figure.ToJson());

        Assert.Equal(figure.ToSvg(), back.ToSvg());
        Assert.Equal(2, back.SubPlots[0].Series.Count);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ABrickThatCoversNoPrice_IsRefusedByName(double brick)
    {
        Assert.Equal("brickSize", Assert.Throws<ArgumentOutOfRangeException>(() => Chart(Closes, brick)).ParamName);
    }

    [Fact]
    public void NoPrices_AreRefusedByName()
    {
        Assert.Equal("prices", Assert.Throws<ArgumentNullException>(() => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Renko(null!, 1)).Build()).ParamName);
    }
}
