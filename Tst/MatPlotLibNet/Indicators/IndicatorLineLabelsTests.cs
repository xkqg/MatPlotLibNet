// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Indicators;
using MatPlotLibNet.Indicators.Streaming;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>A line an indicator draws carries one label, whichever form draws it: the main line wears the indicator's
/// <see cref="Indicator.Label"/> and a second line (a signal, a %D, a +DI) the same text in the batch picture and in the streaming
/// form's output. The numbers of a label are written the same in every culture.</summary>
public sealed class IndicatorLineLabelsTests
{
    private const int Bars = 120;

    private static double[] Close { get; } = [.. Enumerable.Range(0, Bars).Select(i => 100 + (i % 7) - (i % 3) * 0.5 + i * 0.1)];

    private static double[] High { get; } = [.. Close.Select(c => c + 1)];

    private static double[] Low { get; } = [.. Close.Select(c => c - 1)];

    private static string?[] Drawn(Indicator indicator)
    {
        var axes = new Axes();
        indicator.Apply(axes);
        return [.. axes.Series.OfType<ChartSeries>().Where(s => s is LineSeries).Select(s => s.Label)];
    }

    private static string?[] Streamed(IStreamingIndicator form) => [.. form.OutputSeries.Select(s => s.Label)];

    [Fact]
    public void Ichimoku_NamesItsSpansAlikeInBothForms()
    {
        Assert.Equal(["Span A", "Span B", "Tenkan", "Kijun", "Chikou"], Drawn(new Ichimoku(OhlcvBars.Hlc(High, Low, Close))));
        Assert.Equal(["Tenkan", "Kijun", "Span A", "Span B", "Chikou"], Streamed(new StreamingIchimoku()));
    }

    [Fact]
    public void Bollinger_NamesItsMultiplierInBothForms()
    {
        Assert.Equal(["BB(20,2)"], Drawn(new BollingerBands(Close)));
        Assert.Equal(["BB(20,2)", "BB(20,2) Upper", "BB(20,2) Lower"], Streamed(new StreamingBollinger()));
    }

    [Fact]
    public void Squeeze_NamesItsMomentumLineByItsLabelInBothForms()
    {
        Assert.Equal(["Squeeze(20)"], Drawn(new SqueezeMomentum(OhlcvBars.Hlc(High, Low, Close))));
        Assert.Equal(["Squeeze(20)", "Squeeze(20) On"], Streamed(new StreamingSqueezeMomentum()));
    }

    [Fact]
    public void Dispersion_NamesTheNumberOfItsSeriesInBothForms_OnceTheStreamingFormHasSeenARow()
    {
        double[][] rows = [[1, 2, 3], [2, 3, 4]];
        Assert.Equal(["Dispersion(3)"], Drawn(new DispersionIndex(rows)));
        var form = new StreamingDispersionIndex();
        form.Append(rows[0]);
        Assert.Equal(["Dispersion(3)"], Streamed(form));
    }

    [Theory]
    [InlineData("nl-NL")]
    [InlineData("de-DE")]
    public void TheNumbersOfALabel_AreWrittenTheSameInEveryCulture(string name)
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);
            var bars = OhlcvBars.Hlc(High, Low, Close);
            Assert.Equal("BB(20,2.5)", new BollingerBands(Close, 20, 2.5).Label);
            Assert.Equal("BB(20,2.5)", new StreamingBollinger(20, 2.5).Label);
            Assert.Equal("KC(20,1.5)", new KeltnerChannels(bars).Label);
            Assert.Equal("KC(20,1.5)", new StreamingKeltnerChannels().Label);
            Assert.Equal("ST(10,2.5)", new Supertrend(bars, 10, 2.5).Label);
            Assert.Equal("ST(10,2.5)", new StreamingSupertrend(10, 2.5).Label);
            Assert.Equal("IFT(scale=1.5)", new InverseFisherTransform(Close, 1.5).Label);
            Assert.Equal("IFT(scale=1.5)", new StreamingInverseFisherTransform(1.5).Label);
            var axes = new Axes();
            new FibonacciRetracement(90, 110).Apply(axes);
            Assert.Equal(["23.6%", "38.2%", "50.0%", "61.8%", "78.6%"], axes.ReferenceLines.Select(line => line.Label));
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }
}
