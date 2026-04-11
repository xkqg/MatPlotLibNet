// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Indicators;

// ═══════════════════════════════════════════════════════════════
// Phase 0: MakeX / PlotSignal in Indicator base
// ═══════════════════════════════════════════════════════════════

public class IndicatorBaseHelperTests
{
    [Fact]
    public void PlotSignal_AppliesOffset_WhenSet()
    {
        var indicator = new Rsi([100, 101, 102, 103, 104, 105, 106, 107, 108, 109,
                                 110, 111, 112, 113, 114, 115, 116], 14);
        indicator.Offset = 0.5;
        var axes = new Axes();

        indicator.Apply(axes);

        var line = axes.Series.OfType<LineSeries>().First();
        Assert.Equal(14.5, line.XData[0], precision: 10);
    }

    [Fact]
    public void PlotSignal_SetsLabelColorLineWidthLineStyle()
    {
        var indicator = new Sma([10, 20, 30, 40, 50], 3);
        indicator.Label = "TestSMA";
        indicator.Color = Color.FromHex("#FF0000");
        indicator.LineWidth = 2.5;
        indicator.LineStyle = LineStyle.Dashed;
        var axes = new Axes();

        indicator.Apply(axes);

        var line = axes.Series.OfType<LineSeries>().First();
        Assert.Equal("TestSMA", line.Label);
        Assert.Equal(Color.FromHex("#FF0000"), line.Color);
        Assert.Equal(2.5, line.LineWidth);
        Assert.Equal(LineStyle.Dashed, line.LineStyle);
    }
}

// ═══════════════════════════════════════════════════════════════
// Phase A: CandleIndicator<T> base class
// ═══════════════════════════════════════════════════════════════

public class CandleIndicatorTests
{
    private static readonly double[] H = [12, 15, 14, 16, 13, 17, 15, 14, 16, 18, 15, 14, 13, 16, 17];
    private static readonly double[] L = [ 8,  9, 10,  7, 11,  8, 10, 12,  9, 11, 10, 11, 10,  9, 12];
    private static readonly double[] C = [10, 11, 12, 10, 12, 14, 13, 13, 14, 15, 12, 12, 11, 14, 15];

    // ── Hierarchy ──

    [Fact]
    public void AllHlcIndicators_InheritCandleIndicator()
    {
        Assert.IsAssignableFrom<CandleIndicator<SignalResult>>(new Atr(H, L, C, 5));
        Assert.IsAssignableFrom<CandleIndicator<SignalResult>>(new Adx(H, L, C, 5));
        Assert.IsAssignableFrom<CandleIndicator<SignalResult>>(new Cci(H, L, C, 5));
        Assert.IsAssignableFrom<CandleIndicator<SignalResult>>(new WilliamsR(H, L, C, 5));
        Assert.IsAssignableFrom<CandleIndicator<StochasticResult>>(new Stochastic(H, L, C, 5, 3));
        Assert.IsAssignableFrom<CandleIndicator<BandsResult>>(new KeltnerChannels(H, L, C, 5));
        Assert.IsAssignableFrom<CandleIndicator<IchimokuResult>>(new Ichimoku(H, L, C, 5, 5, 10, 5));
    }

    [Fact]
    public void CandleIndicator_IsAlsoIndicatorAndIIndicator()
    {
        var atr = new Atr(H, L, C, 5);
        Assert.IsAssignableFrom<Indicator<SignalResult>>(atr);
        Assert.IsAssignableFrom<Indicator>(atr);
        Assert.IsAssignableFrom<IIndicator>(atr);
    }

    [Fact]
    public void ParabolicSar_DoesNotInheritCandleIndicator()
    {
        var sar = new ParabolicSar(H, L);
        Assert.IsNotAssignableFrom<CandleIndicator<ParabolicSarResult>>(sar);
    }

    // ── Compute unchanged ──

    [Fact]
    public void Atr_Compute_UnchangedAfterRefactor()
    {
        double[] atr = new Atr(H, L, C, 3).Compute();
        Assert.True(atr.Length > 0);
        Assert.All(atr, v => Assert.True(v > 0));
    }

    [Fact]
    public void Adx_Compute_UnchangedAfterRefactor()
    {
        double[] adx = new Adx(H, L, C, 3).Compute();
        Assert.True(adx.Length > 0);
    }

    [Fact]
    public void Cci_Compute_UnchangedAfterRefactor()
    {
        double[] cci = new Cci(H, L, C, 5).Compute();
        Assert.True(cci.Length > 0);
    }

    [Fact]
    public void WilliamsR_Compute_UnchangedAfterRefactor()
    {
        double[] wr = new WilliamsR(H, L, C, 5).Compute();
        Assert.True(wr.Length > 0);
        Assert.All(wr, v => Assert.InRange(v, -100, 0));
    }

    [Fact]
    public void Stochastic_Compute_UnchangedAfterRefactor()
    {
        var result = new Stochastic(H, L, C, 5, 3).Compute();
        Assert.True(result.K.Length > 0);
        Assert.True(result.D.Length > 0);
    }

    [Fact]
    public void KeltnerChannels_Compute_UnchangedAfterRefactor()
    {
        var result = new KeltnerChannels(H, L, C, 5).Compute();
        Assert.True(result.Middle.Length > 0);
    }

    [Fact]
    public void Ichimoku_Compute_UnchangedAfterRefactor()
    {
        var result = new Ichimoku(H, L, C, 3, 5, 8, 3).Compute();
        Assert.True(result.TenkanSen.Length > 0);
        Assert.True(result.KijunSen.Length > 0);
    }

    // ── Apply produces expected series ──

    [Fact]
    public void Atr_Apply_ProducesLineSeries()
    {
        var axes = new Axes();
        new Atr(H, L, C, 5).Apply(axes);
        Assert.Contains(axes.Series, s => s is LineSeries);
    }

    [Fact]
    public void Stochastic_Apply_ProducesTwoLines()
    {
        var axes = new Axes();
        new Stochastic(H, L, C, 5, 3).Apply(axes);
        Assert.True(axes.Series.OfType<LineSeries>().Count() >= 2);
    }
}

// ═══════════════════════════════════════════════════════════════
// Phase B: OhlcSeries base class
// ═══════════════════════════════════════════════════════════════

public class OhlcSeriesTests
{
    private static readonly double[] O = [10, 11, 12, 13];
    private static readonly double[] H = [15, 16, 17, 18];
    private static readonly double[] L = [ 8,  9, 10, 11];
    private static readonly double[] C = [12, 13, 14, 15];

    [Fact]
    public void CandlestickSeries_InheritsOhlcSeries()
    {
        var cs = new CandlestickSeries(O, H, L, C);
        Assert.IsAssignableFrom<OhlcSeries>(cs);
        Assert.IsAssignableFrom<ChartSeries>(cs);
        Assert.IsAssignableFrom<IPriceSeries>(cs);
    }

    [Fact]
    public void OhlcBarSeries_InheritsOhlcSeries()
    {
        var obs = new OhlcBarSeries(O, H, L, C);
        Assert.IsAssignableFrom<OhlcSeries>(obs);
        Assert.IsAssignableFrom<IPriceSeries>(obs);
    }

    [Fact]
    public void OhlcSeries_ExposesSharedProperties()
    {
        var cs = new CandlestickSeries(O, H, L, C);
        var ohlc = Assert.IsAssignableFrom<OhlcSeries>(cs);
        Assert.Same(O, ohlc.Open);
        Assert.Same(H, ohlc.High);
        Assert.Same(L, ohlc.Low);
        Assert.Same(C, ohlc.Close);
        Assert.Equal(Colors.Green, ohlc.UpColor);
        Assert.Equal(Colors.Red, ohlc.DownColor);
    }

    [Fact]
    public void PriceData_ReturnsClose()
    {
        IPriceSeries cs = new CandlestickSeries(O, H, L, C);
        IPriceSeries obs = new OhlcBarSeries(O, H, L, C);
        Assert.Same(C, cs.PriceData);
        Assert.Same(C, obs.PriceData);
    }

    [Fact]
    public void CandlestickSeries_KeepsBodyWidth()
    {
        var cs = new CandlestickSeries(O, H, L, C);
        Assert.Equal(0.6, cs.BodyWidth);
    }

    [Fact]
    public void OhlcBarSeries_KeepsTickWidth()
    {
        var obs = new OhlcBarSeries(O, H, L, C);
        Assert.Equal(0.3, obs.TickWidth);
    }
}

// ═══════════════════════════════════════════════════════════════
// Phase C: DatasetSeries base class
// ═══════════════════════════════════════════════════════════════

public class DatasetSeriesTests
{
    private static readonly double[][] Data = [[1, 2, 3], [4, 5, 6], [7, 8, 9]];

    [Fact]
    public void StripplotSeries_InheritsDatasetSeries()
    {
        Assert.IsAssignableFrom<DatasetSeries>(new StripplotSeries(Data));
    }

    [Fact]
    public void SwarmplotSeries_InheritsDatasetSeries()
    {
        Assert.IsAssignableFrom<DatasetSeries>(new SwarmplotSeries(Data));
    }

    [Fact]
    public void PointplotSeries_InheritsDatasetSeries()
    {
        Assert.IsAssignableFrom<DatasetSeries>(new PointplotSeries(Data));
    }

    [Fact]
    public void BoxSeries_InheritsDatasetSeries()
    {
        Assert.IsAssignableFrom<DatasetSeries>(new BoxSeries(Data));
    }

    [Fact]
    public void ViolinSeries_InheritsDatasetSeries()
    {
        Assert.IsAssignableFrom<DatasetSeries>(new ViolinSeries(Data));
    }

    [Fact]
    public void DatasetSeries_ExposesDatasets()
    {
        var ds = Assert.IsAssignableFrom<DatasetSeries>(new StripplotSeries(Data));
        Assert.Same(Data, ds.Datasets);
    }

    [Fact]
    public void StripplotSeries_ComputeDataRange_InheritedFromBase()
    {
        var series = new StripplotSeries(Data);
        var range = series.ComputeDataRange(new AxesContext());
        Assert.Equal(-0.5, range.XMin);
        Assert.Equal(2.5, range.XMax);  // 3 datasets → Length - 0.5
        Assert.Equal(1, range.YMin);
        Assert.Equal(9, range.YMax);
    }
}

// ═══════════════════════════════════════════════════════════════
// Phase D: PriceIndicator<T> base class
// ═══════════════════════════════════════════════════════════════

public class PriceIndicatorTests
{
    [Fact]
    public void AllPriceIndicators_InheritPriceIndicator()
    {
        double[] prices = [10, 20, 30, 40, 50];
        Assert.IsAssignableFrom<PriceIndicator<SignalResult>>(new Sma(prices, 3));
        Assert.IsAssignableFrom<PriceIndicator<SignalResult>>(new Ema(prices, 3));
        Assert.IsAssignableFrom<PriceIndicator<SignalResult>>(new Rsi(prices, 3));
        Assert.IsAssignableFrom<PriceIndicator<BandsResult>>(new BollingerBands(prices, 3));
        Assert.IsAssignableFrom<PriceIndicator<MacdResult>>(new Macd(prices, 3, 5, 3));
    }

    [Fact]
    public void Vwap_InheritsPriceIndicator()
    {
        double[] prices = [10, 20, 30];
        double[] volumes = [100, 200, 300];
        Assert.IsAssignableFrom<PriceIndicator<SignalResult>>(new Vwap(prices, volumes));
    }

    [Fact]
    public void PriceIndicator_IsAlsoIndicator()
    {
        var sma = new Sma([10, 20, 30, 40, 50], 3);
        Assert.IsAssignableFrom<Indicator<SignalResult>>(sma);
        Assert.IsAssignableFrom<Indicator>(sma);
        Assert.IsAssignableFrom<IIndicator>(sma);
    }

    [Fact]
    public void Sma_Compute_UnchangedAfterRefactor()
    {
        double[] result = new Sma([10, 20, 30, 40, 50], 3).Compute();
        Assert.Equal(3, result.Length);
        Assert.Equal(20, result[0]);
        Assert.Equal(30, result[1]);
        Assert.Equal(40, result[2]);
    }

    [Fact]
    public void Ema_Compute_UnchangedAfterRefactor()
    {
        double[] result = new Ema([10, 20, 30, 40, 50], 3).Compute();
        Assert.Equal(5, result.Length);
    }

    [Fact]
    public void Rsi_Compute_UnchangedAfterRefactor()
    {
        double[] prices = Enumerable.Range(0, 20).Select(i => (double)(100 + i)).ToArray();
        double[] result = new Rsi(prices, 14).Compute();
        Assert.True(result.Length > 0);
    }

    [Fact]
    public void BollingerBands_Compute_UnchangedAfterRefactor()
    {
        double[] prices = Enumerable.Range(0, 30).Select(i => (double)(100 + i % 10)).ToArray();
        var result = new BollingerBands(prices, 10).Compute();
        Assert.True(result.Middle.Length > 0);
        Assert.True(result.Upper.Length > 0);
        Assert.True(result.Lower.Length > 0);
    }
}

// ═══════════════════════════════════════════════════════════════
// Phase G: PlotBands() in Indicator base
// ═══════════════════════════════════════════════════════════════

public class PlotBandsTests
{
    [Fact]
    public void BollingerBands_Apply_ProducesFillBetweenAndLine()
    {
        double[] prices = Enumerable.Range(0, 30).Select(i => 100.0 + i % 10).ToArray();
        var axes = new Axes();
        new BollingerBands(prices, 10).Apply(axes);
        Assert.Contains(axes.Series, s => s is LineSeries);
        Assert.Contains(axes.Series, s => s is AreaSeries);
    }

    [Fact]
    public void KeltnerChannels_Apply_ProducesFillBetweenAndLine()
    {
        double[] h = Enumerable.Range(0, 30).Select(i => 110.0 + i % 10).ToArray();
        double[] l = Enumerable.Range(0, 30).Select(i => 90.0 + i % 10).ToArray();
        double[] c = Enumerable.Range(0, 30).Select(i => 100.0 + i % 10).ToArray();
        var axes = new Axes();
        new KeltnerChannels(h, l, c, 10).Apply(axes);
        Assert.Contains(axes.Series, s => s is LineSeries);
        Assert.Contains(axes.Series, s => s is AreaSeries);
    }

    [Fact]
    public void BollingerBands_PlotBands_SetsAlpha()
    {
        double[] prices = Enumerable.Range(0, 30).Select(i => 100.0 + i % 10).ToArray();
        var bb = new BollingerBands(prices, 10);
        bb.Alpha = 0.3;
        var axes = new Axes();
        bb.Apply(axes);
        var fill = axes.Series.OfType<AreaSeries>().First();
        Assert.Equal(0.3, fill.Alpha);
    }
}

// ═══════════════════════════════════════════════════════════════
// Phase F: All indicators use PlotSignal/MakeX through the pipe
// ═══════════════════════════════════════════════════════════════

public class PlotSignalPipeTests
{
    [Fact]
    public void Sma_Apply_UsesPlotSignal_ProducesLineSeries()
    {
        var axes = new Axes();
        new Sma([10, 20, 30, 40, 50], 3).Apply(axes);
        var line = axes.Series.OfType<LineSeries>().First();
        Assert.Equal("SMA(3)", line.Label);
    }

    [Fact]
    public void Ema_Apply_ProducesLineSeries()
    {
        var axes = new Axes();
        new Ema([10, 20, 30, 40, 50], 3).Apply(axes);
        Assert.Contains(axes.Series, s => s is LineSeries);
    }

    [Fact]
    public void Rsi_Apply_SetsYLimits()
    {
        double[] prices = Enumerable.Range(0, 20).Select(i => 100.0 + i).ToArray();
        var axes = new Axes();
        new Rsi(prices, 14).Apply(axes);
        Assert.Equal(0, axes.YAxis.Min);
        Assert.Equal(100, axes.YAxis.Max);
    }

    [Fact]
    public void Vwap_Apply_WithOffset_AppliesCorrectly()
    {
        double[] prices = [100, 101, 102, 103, 104];
        double[] volumes = [1000, 2000, 3000, 4000, 5000];
        var indicator = new Vwap(prices, volumes);
        indicator.Offset = 0.5;
        var axes = new Axes();
        indicator.Apply(axes);
        var line = axes.Series.OfType<LineSeries>().First();
        Assert.Equal(0.5, line.XData[0], precision: 10);
    }

    [Fact]
    public void DrawDown_Apply_UsesMakeX()
    {
        double[] equity = [100, 105, 103, 108, 106, 110];
        var dd = new DrawDown(equity);
        dd.Offset = 0.5;
        var axes = new Axes();
        dd.Apply(axes);
        var fill = axes.Series.OfType<AreaSeries>().First();
        Assert.Equal(0.5, fill.XData[0], precision: 10);
    }
}

/// <summary>Minimal IAxesContext for tests.</summary>
file sealed class AxesContext : IAxesContext
{
    public double? XAxisMin => null;
    public double? XAxisMax => null;
    public double? YAxisMin => null;
    public double? YAxisMax => null;
    public BarMode BarMode => BarMode.Grouped;
    public IReadOnlyList<ISeries> AllSeries => [];
}
