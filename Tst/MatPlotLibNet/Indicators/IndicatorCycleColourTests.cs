// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>An indicator that names no colour takes the next colours of the cycle under the matplotlib themes, as matplotlib draws an
/// artist that names none, and keeps the colour the library chose for it under the library's own themes. A Bollinger or Keltner band
/// (the filled channel and its middle line) is one unit and takes one colour; the long and the short dots of a parabolic SAR take one
/// each. The band was drawn in a fixed <c>#1f77b4</c> under every theme, the colour of the price line it stands over under v2, and the
/// dots in a fixed green and red.
/// <para>matplotlib 3.11.2, printed with python: under the default style <c>ax.plot(close)</c> then <c>ax.plot(middle).get_color()</c>
/// is <c>'#ff7f0e'</c> (the second colour of <c>axes.prop_cycle</c>), and under the classic style <c>'g'</c> (<c>#008000</c>); the
/// fidelity reference draws the band's <c>fill_between</c> in that line's colour. <c>ax.scatter</c> twice on an empty axes gives
/// <c>#1f77b4</c> then <c>#ff7f0e</c> under the default style and <c>#0000ff</c> both times under the classic one
/// (<c>_internal.classic_mode</c>: a scatter that names no colour takes <c>'b'</c> and leaves the cycle alone).</para></summary>
public sealed class IndicatorCycleColourTests
{
    private const string Blue = "#1F77B4";     // C0 under v2
    private const string Orange = "#FF7F0E";   // C1 under v2
    private const string Green2 = "#2CA02C";   // C2 under v2
    private const string Red2 = "#D62728";     // C3 under v2
    private const string ClassicGreen = "#008000";
    private const string ClassicBlue = "#0000FF";

    private static readonly double[] X = [.. Enumerable.Range(0, 40).Select(i => (double)i)];
    private static readonly double[] Close = [.. Enumerable.Range(0, 40).Select(i => 100 + 5 * Math.Sin(2 * Math.PI * i / 25) + 3 * Math.Sin(2 * Math.PI * i / 7))];
    private static readonly double[] High = [.. Close.Select((c, i) => c + 1 + 0.5 * Math.Abs(Math.Sin(i * 0.5)))];
    private static readonly double[] Low = [.. Close.Select((c, i) => c - 1 - 0.5 * Math.Abs(Math.Cos(i * 0.5)))];

    private static string Svg(Theme theme, Action<AxesBuilder> axes)
    {
        var figure = Plt.Create().WithSize(500, 400).WithTheme(theme).AddSubPlot(1, 1, 1, axes).Build();
        figure.EnableLegendToggle = true;   // gives every series group its data-series-index
        return figure.ToSvg();
    }

    private static void AssertWears(string svg, int seriesIndex, string colour, params string[] not)
    {
        var group = svg.SeriesGroup(seriesIndex);
        Assert.Contains(colour, group, StringComparison.OrdinalIgnoreCase);
        foreach (var other in not) Assert.DoesNotContain(other, group, StringComparison.OrdinalIgnoreCase);
    }

    // ── a band: one colour of the cycle for the channel and its middle line ───────────────────────────────────────

    [Fact]
    public void V2_ABollingerBandAfterTheClose_WearsTheSecondCycleColour()
    {
        var svg = Svg(Theme.MatplotlibV2, ax => ax.Plot(X, Close).BollingerBands(5));

        AssertWears(svg, 1, Orange, Blue);   // the channel
        AssertWears(svg, 2, Orange, Blue);   // the middle line
    }

    [Fact]
    public void Classic_ABollingerBandAfterTheClose_WearsTheClassicSecondColour()
    {
        var svg = Svg(Theme.MatplotlibClassic, ax => ax.Plot(X, Close).BollingerBands(5));

        AssertWears(svg, 1, ClassicGreen, ClassicBlue);
        AssertWears(svg, 2, ClassicGreen, ClassicBlue);
    }

    [Fact]
    public void V2_AKeltnerChannel_WearsTheNextCycleColour()
    {
        var svg = Svg(Theme.MatplotlibV2, ax => ax.Plot(X, Close).Indicator(new KeltnerChannels(OhlcvBars.Hlc(High, Low, Close), 5, 2)));

        AssertWears(svg, 1, Orange, Blue);
        AssertWears(svg, 2, Orange, Blue);
    }

    [Fact]
    public void V2_TheBandTakesOneSlot_TheSeriesAfterItWearsTheThirdColour()
    {
        var svg = Svg(Theme.MatplotlibV2, ax => ax.Plot(X, Close).BollingerBands(5).Plot(X, Low));

        AssertWears(svg, 3, Green2, Orange);
    }

    [Fact]
    public void TheLibrarysOwnTheme_KeepsTheBandInItsOwnBlue()
    {
        var svg = Svg(Theme.Dark, ax => ax.Plot(X, Close).BollingerBands(5));

        AssertWears(svg, 1, Blue);
        AssertWears(svg, 2, Blue);
    }

    [Fact]
    public void ABandGivenAColour_WearsIt_UnderTheMatplotlibThemesToo()
    {
        var svg = Svg(Theme.MatplotlibV2, ax => ax.Plot(X, Close).BollingerBands(5, configure: b => b.Color = Colors.RebeccaPurple));

        AssertWears(svg, 1, Colors.RebeccaPurple.ToHex(), Orange);
        AssertWears(svg, 2, Colors.RebeccaPurple.ToHex(), Orange);
    }

    [Fact]
    public void TheBand_IsOneColourGroup_ItsChannelAndMiddleLine()
    {
        var axes = new Axes();
        axes.Plot(X, Close);
        new BollingerBands(Close, 5).Apply(axes);

        var channel = (ChartSeries)axes.Series[1];
        var middle = (ChartSeries)axes.Series[2];
        Assert.NotNull(channel.ColorGroup);
        Assert.Equal(channel.ColorGroup, middle.ColorGroup);
        Assert.Null(((IHasColor)channel).Color);
        Assert.Null(((IHasColor)middle).Color);
    }

    // ── a parabolic SAR: the long and the short dots each take a colour ──────────────────────────────────────────

    [Fact]
    public void V2_TheDotsOfAParabolicSar_TakeTheCycleInTurn()
    {
        var svg = Svg(Theme.MatplotlibV2, ax => ax.ParabolicSar(High, Low));

        AssertWears(svg, 0, Blue, Orange);
        AssertWears(svg, 1, Orange, Blue);
    }

    [Fact]
    public void Classic_TheDotsOfAParabolicSar_AreTheClassicScatterBlue()
    {
        var svg = Svg(Theme.MatplotlibClassic, ax => ax.ParabolicSar(High, Low));

        AssertWears(svg, 0, ClassicBlue);
        AssertWears(svg, 1, ClassicBlue);
    }

    [Fact]
    public void TheLibrarysOwnTheme_KeepsTheDotsGreenAndRed()
    {
        var svg = Svg(Theme.Default, ax => ax.ParabolicSar(High, Low));

        AssertWears(svg, 0, Colors.Green.ToHex());
        AssertWears(svg, 1, Colors.Red.ToHex());
    }

    [Fact]
    public void DotsGivenColours_WearThem_UnderTheMatplotlibThemesToo()
    {
        var svg = Svg(Theme.MatplotlibV2, ax => ax.ParabolicSar(High, Low, configure: s => { s.LongColor = Colors.RebeccaPurple; s.ShortColor = Colors.Teal; }));

        AssertWears(svg, 0, Colors.RebeccaPurple.ToHex(), Blue);
        AssertWears(svg, 1, Colors.Teal.ToHex(), Orange);
    }

    [Fact]
    public void TheDotColours_ReadGreenAndRed_UntilGiven()
    {
        var sar = new ParabolicSar(High, Low);
        Assert.Equal(Colors.Green, sar.LongColor);
        Assert.Equal(Colors.Red, sar.ShortColor);

        sar.LongColor = Colors.Green;   // given, even though it is the resting value: drawn as given
        sar.ShortColor = Colors.Teal;
        Assert.Equal(Colors.Green, sar.LongColor);
        Assert.Equal(Colors.Teal, sar.ShortColor);
        sar = new ParabolicSar(High, Low) { LongColor = Colors.Green };
        var axes = new Axes();
        sar.Apply(axes);
        Assert.Equal(Colors.Green, ((ScatterSeries)axes.Series[0]).Color);
        Assert.Null(((ScatterSeries)axes.Series[1]).Color);
    }

    // ── R67: every indicator line, fill or bar the verb coloured where its caller named none ───────────────────────
    // The references draw each part with no colour (generate.py: fig_ind_macd plot, plot, bar; fig_ind_stoch plot, plot; fig_ind_adx
    // plot, plot, plot), so each takes the next colour of the cycle its call asks: under the classic style plot -> b, g, r and
    // bar -> b (the patch cycle), under the default style C0, C1, C2 and C0.

    private const string ClassicRed = "#FF0000";

    [Fact]
    public void Classic_AMacdsSignalIsGreen_AndItsHistogramBlue_AsTheReferenceDrawsThem()
    {
        var svg = Svg(Theme.MatplotlibClassic, ax => ax.Indicator(new Macd(Close, 5, 10, 4)));

        AssertWears(svg, 0, ClassicBlue);                 // the MACD line: plot, C0
        AssertWears(svg, 1, ClassicGreen, Orange);        // the signal: plot, C1
        AssertWears(svg, 2, ClassicBlue, ClassicRed);     // the histogram: bar, the patch cycle's C0
    }

    [Fact]
    public void Classic_AStochasticsD_IsGreen()
    {
        var svg = Svg(Theme.MatplotlibClassic, ax => ax.Indicator(new Stochastic(OhlcvBars.Hlc(High, Low, Close), 5, 3)));

        AssertWears(svg, 1, ClassicGreen, Orange);
    }

    [Fact]
    public void V2_AnAdxsDirectionalLines_TakeTheNextTwoColours()
    {
        var svg = Svg(Theme.MatplotlibV2, ax => ax.Indicator(new Adx(OhlcvBars.Hlc(High, Low, Close), 5)));

        AssertWears(svg, 1, Orange, Colors.Green.ToHex());
        AssertWears(svg, 2, Green2, Colors.Red.ToHex());
    }

    [Fact]
    public void Classic_TheSignalOfAKlingerOscillatorAndTheLeadOfASineWave_TakeTheSecondColour()
    {
        double[] volume = [.. Close.Select((_, i) => 1000.0 + 100 * (i % 5))];
        var klinger = Svg(Theme.MatplotlibClassic, ax => ax.Indicator(new KlingerVolumeOscillator(OhlcvBars.Hlcv(High, Low, Close, volume), 5, 10, 4)));
        var sine = Svg(Theme.MatplotlibClassic, ax => ax.Indicator(new EhlersSineWave(Close)));

        AssertWears(klinger, 1, ClassicGreen, Orange);
        AssertWears(sine, 1, ClassicGreen, Orange);
    }

    [Fact]
    public void V2_ADrawdownAndAnIchimokuCloud_TakeThePatchCycle_AndIchimokusLinesTheLinesCycle()
    {
        double[] equity = [100.0, 104, 101, 97, 103, 108, 99, 110];
        var drawdown = Svg(Theme.MatplotlibV2, ax => ax.Indicator(new DrawDown(equity)));
        AssertWears(drawdown, 0, Blue, Colors.Red.ToHex());

        double[] x = [.. Enumerable.Range(0, 80).Select(i => (double)i)];
        double[] close = [.. x.Select(i => 100 + 5 * Math.Sin(2 * Math.PI * i / 25) + 3 * Math.Sin(2 * Math.PI * i / 7))];
        double[] high = [.. close.Select(c => c + 1)], low = [.. close.Select(c => c - 1)];
        var ichimoku = Svg(Theme.MatplotlibV2, ax => ax.Plot(x, close).Indicator(new Ichimoku(OhlcvBars.Hlc(high, low, close))));
        AssertWears(ichimoku, 1, Orange, Colors.Tab10Green.ToHex());   // Span A: plot, C1 after the close, as ax.plot draws the five lines of pandas-ta's ichimoku
        AssertWears(ichimoku, 2, Green2, "#D62728");                   // Span B: C2
        AssertWears(ichimoku, 3, Red2, "#0496FF");                     // Tenkan: C3
        AssertWears(ichimoku, 4, "#9467BD", "#991515");                // Kijun: C4
        AssertWears(ichimoku, 5, "#8C564B", "#9467BD");                // Chikou: C5
        AssertWears(ichimoku, 6, Blue, Colors.Tab10Green.ToHex());     // the cloud: fill_between, the patch cycle's C0
    }

    [Fact]
    public void V2_AProfitAndLossTakesTheFirstTwoColoursOfThePatchCycle_AndKeepsAGivenColour()
    {
        double[] returns = [0.02, -0.01, 0.03, -0.02];
        var svg = Svg(Theme.MatplotlibV2, ax => ax.Indicator(new ProfitLoss(returns)));
        var given = Svg(Theme.MatplotlibV2, ax => ax.Indicator(new ProfitLoss(returns) { ProfitColor = Colors.Green }));

        AssertWears(svg, 0, Blue, Colors.Green.ToHex());
        AssertWears(svg, 1, Orange, Colors.Red.ToHex());
        AssertWears(given, 0, Colors.Green.ToHex());
        Assert.Equal(Colors.Green, new ProfitLoss(returns).ProfitColor);
        Assert.Equal(Colors.Red, new ProfitLoss(returns).LossColor);
    }

    [Fact]
    public void TheLibrarysOwnTheme_KeepsEachIndicatorsOwnColours()
    {
        double[] equity = [100.0, 104, 101, 97, 103, 108, 99, 110];
        var macd = Svg(Theme.Default, ax => ax.Indicator(new Macd(Close, 5, 10, 4)));
        var adx = Svg(Theme.Default, ax => ax.Indicator(new Adx(OhlcvBars.Hlc(High, Low, Close), 5)));
        var drawdown = Svg(Theme.Default, ax => ax.Indicator(new DrawDown(equity)));
        var pnl = Svg(Theme.Default, ax => ax.Indicator(new ProfitLoss([0.02, -0.01])));

        AssertWears(macd, 1, Orange);
        AssertWears(adx, 1, Colors.Green.ToHex());
        AssertWears(adx, 2, Colors.Red.ToHex());
        AssertWears(drawdown, 0, Colors.Red.ToHex());
        AssertWears(pnl, 0, Colors.Green.ToHex());
        AssertWears(pnl, 1, Colors.Red.ToHex());
    }

    // ── the slot decides; the JSON carries the verb's colour ─────────────────────────────────────────────────────

    [Fact]
    public void TheSlot_DrawsAVerbsColourUnderTheLibrarysThemes_AndTheCycleUnderTheMatplotlibThemes()
    {
        var line = new LineSeries([0.0, 1], [0.0, 1]) { VerbColor = Colors.RebeccaPurple };

        Assert.Equal(Colors.RebeccaPurple, Theme.Default.SeriesSlot(1).ColorOf(line));
        Assert.Equal(Colors.RebeccaPurple, Theme.Default.SeriesSlot(1).UnnamedColorOf(line));
        Assert.Equal(Colors.Tab10Orange, Theme.MatplotlibV2.SeriesSlot(1).ColorOf(line));
        Assert.Equal(Colors.Tab10Orange, Theme.MatplotlibV2.SeriesSlot(1).UnnamedColorOf(line));

        line.Color = Colors.Teal;
        Assert.Equal(Colors.Teal, Theme.Default.SeriesSlot(1).ColorOf(line));
        Assert.Equal(Colors.Teal, Theme.MatplotlibV2.SeriesSlot(1).ColorOf(line));
    }

    [Fact]
    public void ASeriesNoVerbColoured_TakesTheSlotsColour()
    {
        var line = new LineSeries([0.0, 1], [0.0, 1]);

        Assert.Equal(Theme.Default.SeriesSlot(2).CycleColor, Theme.Default.SeriesSlot(2).UnnamedColorOf(line));
        Assert.Equal(Theme.Default.SeriesSlot(2).CycleColor, Theme.Default.SeriesSlot(2).UnnamedColorOf(new PieSeries([1.0])));
        Assert.Equal(Theme.Default.SeriesSlot(2).CycleColor, Theme.Default.SeriesSlot(2).UnnamedColorOf(new ForeignSeries()));
    }

    [Theory]
    [InlineData("default")]
    [InlineData("v2")]
    [InlineData("classic")]
    public void TheVerbsColour_SurvivesTheJson_SoTheFigureDrawsTheSameAfterARoundTrip(string themeId)
    {
        var theme = themeId.AsTheme();
        var figure = Plt.Create().WithSize(500, 400).WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax => ax.Plot(X, Close).BollingerBands(5).ParabolicSar(High, Low).Indicator(new Ichimoku(OhlcvBars.Hlc(High, Low, Close), 5, 8, 12, 4))).Build();

        string json = figure.ToJson();
        var restored = ChartSerializerDefault.Current.FromJson(json);

        Assert.Contains("\"verbColor\"", json, StringComparison.Ordinal);
        Assert.Equal(figure.ToSvg(), restored.ToSvg());
        Assert.Equal(json, restored.ToJson());
    }
}
