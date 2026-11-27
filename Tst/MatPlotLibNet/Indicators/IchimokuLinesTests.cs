// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>What an Ichimoku draws and what its caller can say about it: its five lines (the conversion line, the base line, the two
/// spans that bound the cloud, and the lagging close) and the cloud between the spans; the colour, width and dash pattern its caller
/// gave reach the lines they are meant for, and a part given none keeps the colour the indicator chose for it. pandas-ta's <c>ichimoku</c>
/// (<c>ISA_9, ISB_26, ITS_9, IKS_26, ICS_26</c>) returns the same five lines.</summary>
public sealed class IchimokuLinesTests
{
    private static readonly double[] Close = [.. Enumerable.Range(0, 80).Select(i => 100 + 5 * Math.Sin(2 * Math.PI * i / 25) + 3 * Math.Sin(2 * Math.PI * i / 7))];
    private static readonly double[] High = [.. Close.Select(c => c + 1)];
    private static readonly double[] Low = [.. Close.Select(c => c - 1)];

    private static Ichimoku New() => new(OhlcvBars.Hlc(High, Low, Close));

    private static Axes Applied(Ichimoku ichimoku)
    {
        var axes = new Axes();
        ichimoku.Apply(axes);
        return axes;
    }

    // pandas_ta.ichimoku(high, low, close) on these 80 bars (the closed-form ones of the Fidelity suite, high = close + 1 + 0.5 |sin(0.5 i)|, low =
    // close - 1 - 0.5 |cos(0.5 i)|): ISA_9 first = 97.84821238726701, ISB_26 first = 99.7958533906574, ITS_9 first = 103.37401547869408, IKS_26
    // first = 99.42630577623598, ICS_26 first = 98.69834878264733. pandas-ta places the two spans 25 bars ahead of the bar they are worked out at,
    // this library the displacement (26): the values are the same, one bar apart.
    private static readonly double[] FidelityClose = [.. Enumerable.Range(0, 80).Select(i => 100.0 + 5.0 * Math.Sin(2 * Math.PI * i / 25) + 3.0 * Math.Sin(2 * Math.PI * i / 7))];
    private static readonly double[] FidelityHigh = [.. FidelityClose.Select((c, i) => c + 1.0 + 0.5 * Math.Abs(Math.Sin(i * 0.5)))];
    private static readonly double[] FidelityLow = [.. FidelityClose.Select((c, i) => c - 1.0 - 0.5 * Math.Abs(Math.Cos(i * 0.5)))];

    [Fact]
    public void Apply_DrawsTheTwoSpans_TheConversionAndBaseLines_TheLaggingClose_AndTheCloud_InTheOrderMatplotlibDrawsThem()
    {
        var axes = Applied(New());

        Assert.Equal(["Span A", "Span B", "Tenkan", "Kijun", "Chikou", "Cloud"], axes.Series.Select(s => s.Label));
        Assert.IsType<AreaSeries>(axes.Series[5]);
        Assert.All(new[] { 0, 1, 2, 3, 4 }, i => Assert.IsType<LineSeries>(axes.Series[i]));
    }

    [Fact]
    public void EachSpan_StandsWhereItsOwnFirstValueIs_PlusTheDisplacement_AsPandasTaHasIt()
    {
        var axes = Applied(new Ichimoku(OhlcvBars.Hlc(FidelityHigh, FidelityLow, FidelityClose)));
        var spanA = (LineSeries)axes.Series[0];
        var spanB = (LineSeries)axes.Series[1];

        Assert.Equal(25 + 26, spanA.XData[0]);     // the base line's first bar plus the displacement
        Assert.Equal(51 + 26, spanB.XData[0]);     // the 52-bar span's first bar plus the displacement
        Assert.Equal(97.84821238726701, spanA.YData[0], 9);
        Assert.Equal(99.7958533906574, spanB.YData[0], 9);
        Assert.Equal(FidelityClose.Length - 25, spanA.XData.Length);   // a value for every bar the base line has
        Assert.Equal(FidelityClose.Length - 51, spanB.XData.Length);
    }

    [Fact]
    public void TheCloud_IsTheRegionBetweenTheSpansWhereBothHaveAValue()
    {
        var axes = Applied(new Ichimoku(OhlcvBars.Hlc(FidelityHigh, FidelityLow, FidelityClose)));
        var spanA = (LineSeries)axes.Series[0];
        var spanB = (LineSeries)axes.Series[1];
        var cloud = (AreaSeries)axes.Series[5];

        Assert.Equal(spanB.XData, cloud.XData);
        int at = Array.IndexOf(spanA.XData, cloud.XData[0]);
        Assert.Equal(spanA.YData[at], cloud.YData[0]);
        Assert.Equal(spanB.YData[0], cloud.YData2![0]);
    }

    [Fact]
    public void TheLaggingClose_IsTheCloseShiftedBackByTheDisplacement()
    {
        var chikou = (LineSeries)Applied(New()).Series[4];

        Assert.Equal(0.0, chikou.XData[0]);
        Assert.Equal(Close.Length - 26, chikou.XData.Length);
        Assert.Equal(Close[26], chikou.YData[0]);
        Assert.Equal(Close[^1], chikou.YData[^1]);
    }

    [Fact]
    public void Compute_GivesSpanAForEveryBarTheBaseLineHasOne()
    {
        var result = new Ichimoku(OhlcvBars.Hlc(FidelityHigh, FidelityLow, FidelityClose)).Compute();

        Assert.Equal(result.KijunSen.Length, result.SenkouSpanA.Length);
        Assert.Equal(97.84821238726701, result.SenkouSpanA[0], 9);
    }

    [Fact]
    public void ACloseTooShortForTheDisplacement_DrawsNoLaggingLine()
    {
        var bars = OhlcvBars.Hlc(High[..60], Low[..60], Close[..60]);
        var axes = Applied(new Ichimoku(bars, displacement: 60));

        Assert.DoesNotContain(axes.Series, s => s.Label == "Chikou");
    }

    [Fact]
    public void TheColourTheCallerGave_IsTheConversionLines_AndTheOtherPartsHaveTheirOwnColoursToGive()
    {
        var axes = Applied(new Ichimoku(OhlcvBars.Hlc(High, Low, Close))
        {
            Color = Colors.Red,
            KijunColor = Colors.Blue,
            CloudColor = Colors.Green,
            ChikouColor = Colors.Orange,
        });

        Assert.Equal(Colors.Red, ((LineSeries)axes.Series[2]).Color);
        Assert.Equal(Colors.Blue, ((LineSeries)axes.Series[3]).Color);
        Assert.All(new[] { 0, 1, 5 }, i => Assert.Equal(Colors.Green, ((IHasColor)axes.Series[i]).Color));
        Assert.Equal(Colors.Orange, ((LineSeries)axes.Series[4]).Color);
    }

    [Fact]
    public void WithNoColourGiven_EveryPartKeepsTheColourTheIndicatorChose_ForTheThemeToDecide()
    {
        var axes = Applied(New());

        Assert.All(axes.Series.Cast<ChartSeries>(), s => Assert.NotNull(s.VerbColor));
        Assert.All(axes.Series.Cast<IHasColor>(), s => Assert.Null(s.Color));
        Assert.Equal(Color.FromHex("#0496ff"), ((ChartSeries)axes.Series[2]).VerbColor);
        Assert.Equal(Color.FromHex("#991515"), ((ChartSeries)axes.Series[3]).VerbColor);
        Assert.Equal(Colors.Tab10Green, ((ChartSeries)axes.Series[0]).VerbColor);
        Assert.Equal(Colors.Tab10Green, ((ChartSeries)axes.Series[5]).VerbColor);
    }

    [Fact]
    public void AWidthAndADashPatternTheCallerGave_ReachEveryLine_AndNoneGivenLeavesThemToTheTheme()
    {
        var plain = Applied(New());
        Assert.All(plain.Series.OfType<LineSeries>(), line =>
        {
            Assert.Null(line.Stroke.Width);
            Assert.Null(line.Stroke.Style);
        });

        var styled = Applied(new Ichimoku(OhlcvBars.Hlc(High, Low, Close)) { LineWidth = 3, LineStyle = LineStyle.Dotted });
        var lines = styled.Series.OfType<LineSeries>().ToArray();
        Assert.Equal(5, lines.Length);
        Assert.All(lines, line =>
        {
            Assert.Equal(3.0, line.Stroke.Width);
            Assert.Equal(LineStyle.Dotted, line.Stroke.Style);
        });
    }
}
