// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>The colours of a rise and a fall are one pair, <see cref="UpDownColors.Standard"/>, and every part that draws a rise and a
/// fall without being given a colour reads it (the source scan that holds it to one spelling is a row of
/// <c>OneOwnerSourceContractTests</c>).</summary>
public class UpDownColorsTests
{
    [Fact]
    public void TheStandardPair_IsTheCssGreenAndRed_NotTheCyclesGreenAndRed()
    {
        Assert.Equal(Css4Colors.Green, UpDownColors.Standard.Up);
        Assert.Equal(Css4Colors.Red, UpDownColors.Standard.Down);
        Assert.NotEqual(Colors.Tab10Green, UpDownColors.Standard.Up);
        Assert.NotEqual(Colors.Tab10Red, UpDownColors.Standard.Down);
    }

    [Fact]
    public void ARecordStruct_HoldsItsTwoColoursAsGiven()
    {
        var pair = new UpDownColors(Colors.Blue, Colors.Orange);

        var (up, down) = pair;

        Assert.Equal(Colors.Blue, up);
        Assert.Equal(Colors.Orange, down);
    }

    [Fact]
    public void ACandlestickAndAnOhlcBar_RestAtTheStandardPair()
    {
        double[] v = [1, 2, 3];
        var candles = new CandlestickSeries(v, v, v, v);

        Assert.Equal(UpDownColors.Standard.Up, candles.UpColor);
        Assert.Equal(UpDownColors.Standard.Down, candles.DownColor);
    }

    [Fact]
    public void APointAndFigureChart_RestsAtTheStandardPair()
    {
        var chart = new PointAndFigureSeries([1.0, 2.0, 3.0], 1);

        Assert.Equal(UpDownColors.Standard.Up, chart.UpColor);
        Assert.Equal(UpDownColors.Standard.Down, chart.DownColor);
    }

    [Fact]
    public void AWaterfall_RestsItsRiseAndFallAtTheStandardPair()
    {
        var waterfall = new WaterfallSeries(["a", "b"], [1.0, -1.0]);

        Assert.Equal(UpDownColors.Standard.Up, waterfall.IncreaseColor);
        Assert.Equal(UpDownColors.Standard.Down, waterfall.DecreaseColor);
    }

    [Fact]
    public void ARenkoChart_DrawsItsBricksInTheStandardPair()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Renko([10.0, 12, 14, 12, 10, 8], 1)).Build();

        var colours = figure.SubPlots[0].Series.OfType<ChartSeries>().Select(s => ((IHasColor)s).Color).ToArray();

        Assert.Contains(UpDownColors.Standard.Up, colours);
        Assert.Contains(UpDownColors.Standard.Down, colours);
    }

    /// <summary>The markup of a line with one signal marker in the given direction.</summary>
    private static string SignalSvg(SignalDirection direction)
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1.0], [0.0, 1.0])).Build();
        figure.SubPlots[0].AddSignal(0.5, 0.5, direction);
        return figure.ToSvg();
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("ASignalMarker_RestsAtTheStandardPair_ByItsDirection (#1)", () => SignalSvg(SignalDirection.Buy), UpDownColors.Standard.Up.ToHex()) { Comparison = StringComparison.OrdinalIgnoreCase },
        new DrawnToken("ASignalMarker_RestsAtTheStandardPair_ByItsDirection (#2)", () => SignalSvg(SignalDirection.Sell), UpDownColors.Standard.Down.ToHex()) { Comparison = StringComparison.OrdinalIgnoreCase },
        new DrawnToken("AStreamingCandlestick_FallsBackToTheStandardPair_WhenGivenNone", () =>
        {
            var series = new StreamingCandlestickSeries(8);
            series.AppendBar(1, 2, 0.5, 1.5);
            series.AppendBar(1.5, 2.5, 1, 1.2);
            var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.AddSeries(series)).Build();
            return figure.ToSvg();
        }, UpDownColors.Standard.Up.ToHex(), UpDownColors.Standard.Down.ToHex()) { Comparison = StringComparison.OrdinalIgnoreCase },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    [Fact]
    public void AnIndicatorGivenNoColour_ChoosesTheStandardPair()
    {
        var sar = new ParabolicSar([1.0, 2, 3], [0.5, 1, 2]);
        var profit = new ProfitLoss([1.0, -1.0]);

        Assert.Equal(UpDownColors.Standard.Up, sar.LongColor);
        Assert.Equal(UpDownColors.Standard.Down, sar.ShortColor);
        Assert.Equal(UpDownColors.Standard.Up, profit.ProfitColor);
        Assert.Equal(UpDownColors.Standard.Down, profit.LossColor);
    }
}
