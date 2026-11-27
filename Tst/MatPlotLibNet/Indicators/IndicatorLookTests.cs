// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>The colours the indicators choose for a part their caller gave none are one look, <see cref="IndicatorLook.Library"/>,
/// and an indicator that draws in one hands it to the series as its verb's colour (the source scan that keeps an indicator from naming
/// a colour of its own is a row of <c>OneOwnerSourceContractTests</c>).</summary>
public class IndicatorLookTests
{
    private static readonly double[] Close = [10, 11, 12, 11, 13, 12, 14, 13, 15, 14, 16, 15, 17, 16, 18, 17, 19, 18, 20, 19, 21, 20, 22, 21, 23, 22, 24, 23, 25, 24, 26, 25, 27, 26, 28, 27, 29, 28, 30, 29, 31, 30, 32, 31, 33, 32, 34, 33, 35, 34, 36, 35, 37, 36, 38, 37];

    /// <summary>The look holds the pairs it reads as the values they are, no second name for either: its lines the standard cycle's
    /// first two colours (<see cref="CyclePair.Standard"/>), its rise and fall the library's pair (<see cref="UpDownColors.Standard"/>).</summary>
    [Fact]
    public void TheLook_HoldsTheStandardCyclesPairAndTheRiseAndFallPair_AsValues()
    {
        Assert.Equal(CyclePair.Standard, IndicatorLook.Library.Lines);
        Assert.Equal(QualitativeColorMaps.Tab10At(0), IndicatorLook.Library.Lines.First);
        Assert.Equal(QualitativeColorMaps.Tab10At(1), IndicatorLook.Library.Lines.Second);
        Assert.Equal(UpDownColors.Standard, IndicatorLook.Library.Moves);
    }

    [Fact]
    public void AnIndicatorThatDrawsARiseAndAFall_StatesTheLooksPair()
    {
        var axes = new Axes();
        new ParabolicSar(Close, Close).Apply(axes);

        var chosen = axes.Series.OfType<ChartSeries>().Select(s => s.VerbColor).ToArray();

        Assert.Contains(IndicatorLook.Library.Moves.Up, chosen);
        Assert.Contains(IndicatorLook.Library.Moves.Down, chosen);
    }

    [Fact]
    public void TheIchimokuSpansAndTheLaggingClose_AreColoursOfTheStandardCycle()
    {
        var ichimoku = IndicatorLook.Library.Ichimoku;

        Assert.Equal(QualitativeColorMaps.Tab10At(2), ichimoku.SpanA);
        Assert.Equal(QualitativeColorMaps.Tab10At(3), ichimoku.SpanB);
        Assert.Equal(QualitativeColorMaps.Tab10At(4), ichimoku.Chikou);
    }

    [Fact]
    public void TheIchimokuLines_AreTwoColoursOfTheirOwn_NotOfTheCycle()
    {
        var ichimoku = IndicatorLook.Library.Ichimoku;

        Assert.Equal(Color.FromHex("#0496ff"), ichimoku.Tenkan);
        Assert.Equal(Color.FromHex("#991515"), ichimoku.Kijun);
        Assert.DoesNotContain(ichimoku.Tenkan, QualitativeColorMaps.Tab10Cycle());
        Assert.DoesNotContain(ichimoku.Kijun, QualitativeColorMaps.Tab10Cycle());
    }

    [Fact]
    public void AnIchimokuStatesTheLooksColoursAsTheChoiceOfEachPart()
    {
        var axes = new Axes();
        new Ichimoku(OhlcvBars.Hlc(Close, Close, Close)).Apply(axes);

        var chosen = axes.Series.OfType<ChartSeries>().ToDictionary(s => s.Label ?? "", s => s.VerbColor);

        var look = IndicatorLook.Library.Ichimoku;
        Assert.Equal(look.SpanA, chosen["Span A"]);
        Assert.Equal(look.SpanB, chosen["Span B"]);
        Assert.Equal(look.Tenkan, chosen["Tenkan"]);
        Assert.Equal(look.Kijun, chosen["Kijun"]);
        Assert.Equal(look.Chikou, chosen["Chikou"]);
        Assert.Equal(look.SpanA, chosen["Cloud"]);
    }

    [Fact]
    public void ABandIndicator_StatesThePrimaryColourForItsBandAndItsMidline()
    {
        var axes = new Axes();
        new BollingerBands(Close).Apply(axes);

        var colours = axes.Series.OfType<ChartSeries>().Select(s => s.VerbColor).ToArray();

        Assert.NotEmpty(colours);
        Assert.All(colours, c => Assert.Equal(IndicatorLook.Library.Lines.First, c));
    }

    [Fact]
    public void ASignalLine_StatesTheSecondaryColour()
    {
        var axes = new Axes();
        new Macd(Close).Apply(axes);

        var signal = axes.Series.OfType<ChartSeries>().Single(s => s.Label is { } l && l.StartsWith("Signal", StringComparison.Ordinal));

        Assert.Equal(IndicatorLook.Library.Lines.Second, signal.VerbColor);
    }
}
