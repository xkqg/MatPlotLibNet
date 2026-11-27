// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Indicators.Streaming;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Tests.Indicators.Streaming;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>The smoothing of the stochastic's %K: 3 raw values unless the caller names another count, 1 leaving %K the raw (fast) one.
/// pandas-ta and TA-Lib draw the smoothed one, so that is the default.</summary>
public sealed class StochasticSmoothingTests
{
    private static readonly OracleBars Bars = OracleBars.Walk(120);

    private static double Raw(int bar, int k) =>
        (Bars.Close[bar] - Bars.Low[(bar - k + 1)..(bar + 1)].Min()) / (Bars.High[(bar - k + 1)..(bar + 1)].Max() - Bars.Low[(bar - k + 1)..(bar + 1)].Min()) * 100;

    [Fact]
    public void WithASmoothingOfOne_KIsTheRawPercentK_AsTheLibraryDrewItBeforeTheDefaultWasTheSlowOne()
    {
        var result = new Stochastic(OhlcvBars.Hlc(Bars.High, Bars.Low, Bars.Close), 14, 3, 1).Compute();

        Assert.Equal(120 - 13, result.K.Length);
        Assert.Equal(120 - 15, result.D.Length);
        Assert.Equal(Raw(50, 14), result.K[50 - 13], 9);
        Assert.Equal((Raw(50, 14) + Raw(49, 14) + Raw(48, 14)) / 3, result.D[50 - 15], 9);
    }

    [Fact]
    public void ByDefault_KIsTheMeanOfThreeRawValues_AndComesTwoBarsLater()
    {
        var result = new Stochastic(OhlcvBars.Hlc(Bars.High, Bars.Low, Bars.Close), 14, 3).Compute();

        Assert.Equal(120 - 15, result.K.Length);
        Assert.Equal(120 - 17, result.D.Length);
        Assert.Equal((Raw(50, 14) + Raw(49, 14) + Raw(48, 14)) / 3, result.K[50 - 15], 9);
    }

    [Fact]
    public void TooFewBarsForTheSmoothing_GiveNoK_AndOneMoreGivesOne()
    {
        var few = new Stochastic(OhlcvBars.Hlc(Bars.High[..15], Bars.Low[..15], Bars.Close[..15]), 14, 3).Compute();
        var enough = new Stochastic(OhlcvBars.Hlc(Bars.High[..16], Bars.Low[..16], Bars.Close[..16]), 14, 3).Compute();

        Assert.Empty(few.K);
        Assert.Empty(few.D);
        Assert.Single(enough.K);
        Assert.Empty(enough.D);
    }

    [Fact]
    public void Apply_PutsTheSmoothedKAtTheBarItFirstHasAValue_AndDTwoBarsLater()
    {
        var axes = new Axes();
        new Stochastic(OhlcvBars.Hlc(Bars.High, Bars.Low, Bars.Close), 14, 3).Apply(axes);

        Assert.Equal(15.0, ((LineSeries)axes.Series[0]).XData[0]);
        Assert.Equal(17.0, ((LineSeries)axes.Series[1]).XData[0]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void TheStreamingForm_SaysWhatTheBatchSaysOverEveryPrefix_ForAnySmoothing(int smoothK) =>
        StreamingEquality.AssertAgrees(new("Stochastic", () => new StreamingStochastic(5, 3, smoothK, 10_000),
            b =>
            {
                var stochastic = new Stochastic(OhlcvBars.Hlc(b.High, b.Low, b.Close), 5, 3, smoothK).Compute();
                return [StreamingRow.Last(stochastic.K), StreamingRow.Last(stochastic.D)];
            }));
}
