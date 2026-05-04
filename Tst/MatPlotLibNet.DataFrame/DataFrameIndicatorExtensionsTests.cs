// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Data.Analysis;
using MatPlotLibNet.Indicators;

namespace MatPlotLibNet.Tests.DataFrame;

/// <summary>Verifies <see cref="DataFrameIndicatorExtensions"/> extension methods.</summary>
public class DataFrameIndicatorExtensionsTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private const int N = 60; // Use enough data for all indicators with their default periods

    private static Microsoft.Data.Analysis.DataFrame MakePriceDf(int count = N, double start = 100.0, double step = 1.0)
    {
        var prices = Enumerable.Range(0, count).Select(i => start + i * step).ToArray();
        return new Microsoft.Data.Analysis.DataFrame(
            new PrimitiveDataFrameColumn<double>("close", prices));
    }

    private static Microsoft.Data.Analysis.DataFrame MakeCandleDf(int count = N)
    {
        var high   = Enumerable.Range(0, count).Select(i => 102.0 + i).ToArray();
        var low    = Enumerable.Range(0, count).Select(i => 98.0  + i).ToArray();
        var close  = Enumerable.Range(0, count).Select(i => 100.0 + i).ToArray();
        var volume = Enumerable.Range(0, count).Select(i => 1000.0 + i * 10).ToArray();
        return new Microsoft.Data.Analysis.DataFrame(
            new PrimitiveDataFrameColumn<double>("high",   high),
            new PrimitiveDataFrameColumn<double>("low",    low),
            new PrimitiveDataFrameColumn<double>("close",  close),
            new PrimitiveDataFrameColumn<double>("volume", volume));
    }

    private static Microsoft.Data.Analysis.DataFrame MakeOhlcvDf(int count = N)
    {
        var open   = Enumerable.Range(0, count).Select(i => 99.0  + i).ToArray();
        var high   = Enumerable.Range(0, count).Select(i => 102.0 + i).ToArray();
        var low    = Enumerable.Range(0, count).Select(i => 98.0  + i).ToArray();
        var close  = Enumerable.Range(0, count).Select(i => 100.0 + i).ToArray();
        var volume = Enumerable.Range(0, count).Select(i => 1000.0 + i * 10).ToArray();
        return new Microsoft.Data.Analysis.DataFrame(
            new PrimitiveDataFrameColumn<double>("open",   open),
            new PrimitiveDataFrameColumn<double>("high",   high),
            new PrimitiveDataFrameColumn<double>("low",    low),
            new PrimitiveDataFrameColumn<double>("close",  close),
            new PrimitiveDataFrameColumn<double>("volume", volume));
    }

    // Indicators return trimmed arrays (not NaN-padded).
    private static void AssertNonEmptyResult(double[] result) => Assert.NotEmpty(result);

    // ── SMA ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Sma_ReturnsNonEmptyArray()
    {
        double[] result = MakePriceDf().Sma("close", period: 5);
        AssertNonEmptyResult(result);
    }

    [Fact]
    public void Sma_OutputShorterThanInput_ByWarmUpPeriod()
    {
        // SMA(period=5) on N rows → N - 5 + 1 valid output rows
        double[] result = MakePriceDf(30).Sma("close", period: 5);
        Assert.Equal(30 - 5 + 1, result.Length);
    }

    // ── EMA ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Ema_ReturnsNonEmptyDoubleArray()
    {
        double[] result = MakePriceDf().Ema("close", period: 5);
        AssertNonEmptyResult(result);
    }

    // ── RSI ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Rsi_ReturnsNonEmptyArray()
    {
        double[] result = MakePriceDf().Rsi("close");
        AssertNonEmptyResult(result);
    }

    [Fact]
    public void Rsi_OutputHasExpectedWarmUp()
    {
        // RSI(period=14) on 30 rows → 30 - 14 = 16 valid output rows
        double[] result = MakePriceDf(30).Rsi("close", period: 14);
        Assert.Equal(30 - 14, result.Length);
    }

    // ── Bollinger Bands ───────────────────────────────────────────────────────

    [Fact]
    public void BollingerBands_AllThreeArraysSameLength()
    {
        BandsResult bands = MakePriceDf().BollingerBands("close");
        Assert.Equal(bands.Middle.Length, bands.Upper.Length);
        Assert.Equal(bands.Middle.Length, bands.Lower.Length);
    }

    [Fact]
    public void BollingerBands_NonEmptyResult()
    {
        BandsResult bands = MakePriceDf().BollingerBands("close");
        Assert.NotEmpty(bands.Middle);
    }

    [Fact]
    public void BollingerBands_UpperAlwaysAtLeastMiddle()
    {
        BandsResult bands = MakePriceDf().BollingerBands("close");
        for (int i = 0; i < bands.Middle.Length; i++)
            Assert.True(bands.Upper[i] >= bands.Middle[i],
                $"Upper[{i}]={bands.Upper[i]} < Middle[{i}]={bands.Middle[i]}");
    }

    // ── OBV ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Obv_TwoColumns_ReturnsNonEmptyDoubleArray()
    {
        double[] result = MakeCandleDf().Obv("close", "volume");
        AssertNonEmptyResult(result);
    }

    // ── MACD ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Macd_ReturnsMacdResult_WithNonEmptyArrays()
    {
        MacdResult result = MakePriceDf().Macd("close");
        Assert.NotNull(result);
        Assert.NotEmpty(result.MacdLine);
        Assert.NotEmpty(result.SignalLine);
        Assert.NotEmpty(result.Histogram);
        // Signal line has extra warm-up vs MACD line — both are non-empty but may differ in length
        Assert.True(result.SignalLine.Length <= result.MacdLine.Length);
    }

    // ── DrawDown ──────────────────────────────────────────────────────────────

    [Fact]
    public void DrawDown_ReturnsNonEmptyArray()
    {
        double[] result = MakePriceDf().DrawDown("close");
        AssertNonEmptyResult(result);
    }

    // ── ADX ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Adx_CandleIndicator_ReturnsNonEmptyDoubleArray()
    {
        double[] result = MakeCandleDf().Adx("high", "low", "close");
        AssertNonEmptyResult(result);
    }

    [Fact]
    public void AdxFull_ReturnsAllThreeSignals_SameLength()
    {
        AdxResult result = MakeCandleDf().AdxFull("high", "low", "close");
        Assert.Equal(result.Adx.Length, result.PlusDi.Length);
        Assert.Equal(result.Adx.Length, result.MinusDi.Length);
        Assert.NotEmpty(result.Adx);
    }

    // ── ATR ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Atr_ReturnsNonEmptyArray()
    {
        double[] result = MakeCandleDf().Atr("high", "low", "close");
        AssertNonEmptyResult(result);
    }

    // ── CCI ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Cci_ReturnsNonEmptyArray()
    {
        double[] result = MakeCandleDf().Cci("high", "low", "close");
        AssertNonEmptyResult(result);
    }

    // ── WilliamsR ─────────────────────────────────────────────────────────────

    [Fact]
    public void WilliamsR_ReturnsNonEmptyArray()
    {
        double[] result = MakeCandleDf().WilliamsR("high", "low", "close");
        AssertNonEmptyResult(result);
    }

    // ── Stochastic ────────────────────────────────────────────────────────────

    [Fact]
    public void Stochastic_ReturnsBothArraysNonEmpty()
    {
        StochasticResult result = MakeCandleDf().Stochastic("high", "low", "close");
        Assert.NotEmpty(result.K);
        Assert.NotEmpty(result.D);
        // D-line has additional smoothing warm-up, so it may be shorter than K
        Assert.True(result.D.Length <= result.K.Length);
    }

    // ── ParabolicSar ──────────────────────────────────────────────────────────

    [Fact]
    public void ParabolicSar_ReturnsNonEmptyDoubleArray()
    {
        double[] result = MakeCandleDf().ParabolicSar("high", "low");
        AssertNonEmptyResult(result);
    }

    // ── KeltnerChannels ───────────────────────────────────────────────────────

    [Fact]
    public void KeltnerChannels_AllThreeArraysSameLength()
    {
        BandsResult result = MakeCandleDf().KeltnerChannels("high", "low", "close");
        Assert.Equal(result.Middle.Length, result.Upper.Length);
        Assert.Equal(result.Middle.Length, result.Lower.Length);
        Assert.NotEmpty(result.Middle);
    }

    // ── VWAP ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Vwap_ReturnsNonEmptyArray()
    {
        double[] result = MakeCandleDf().Vwap("high", "low", "close", "volume");
        AssertNonEmptyResult(result);
    }

    // ── Error handling ────────────────────────────────────────────────────────

    [Fact]
    public void UnknownColumn_Throws_ArgumentException_WithColumnNameInMessage()
    {
        var ex = Assert.Throws<ArgumentException>(() => MakePriceDf().Sma("typo_column", 5));
        Assert.Contains("typo_column", ex.Message);
    }

    [Fact]
    public void UnknownVolumeColumn_Throws_ArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => MakeCandleDf().Obv("close", "missing_vol"));
        Assert.Contains("missing_vol", ex.Message);
    }

    // ── Price-column indicator wrappers ───────────────────────────────────────

    [Fact] public void CgOscillator_ReturnsNonEmptyArray()
        => Assert.NotEmpty(MakePriceDf().CgOscillator("close"));

    [Fact] public void Bocpd_ReturnsNonEmptyArray()
        => Assert.NotEmpty(MakePriceDf().Bocpd("close"));

    [Fact] public void CyberCycle_ReturnsNonEmptyArray()
        => Assert.NotEmpty(MakePriceDf().CyberCycle("close"));

    [Fact] public void Decycler_ReturnsNonEmptyArray()
        => Assert.NotEmpty(MakePriceDf().Decycler("close"));

    [Fact] public void EhlersITrend_ReturnsNonEmptyArray()
        => Assert.NotEmpty(MakePriceDf().EhlersITrend("close"));

    [Fact] public void EhlersSineWave_ReturnsNonEmptySineArray()
    {
        SineWaveResult r = MakePriceDf().EhlersSineWave("close");
        Assert.NotEmpty(r.SineWave);
    }

    [Fact] public void EhlersSuperSmoother_ReturnsNonEmptyArray()
        => Assert.NotEmpty(MakePriceDf().EhlersSuperSmoother("close"));

    [Fact] public void FractionalDifferentiation_ReturnsNonEmptyArray()
        => Assert.NotEmpty(MakePriceDf().FractionalDifferentiation("close"));

    [Fact] public void KaufmanEfficiencyRatio_ReturnsNonEmptyArray()
        => Assert.NotEmpty(MakePriceDf().KaufmanEfficiencyRatio("close"));

    [Fact] public void LaguerreRsi_ReturnsNonEmptyArray()
        => Assert.NotEmpty(MakePriceDf().LaguerreRsi("close"));

    [Fact] public void MamaFama_ReturnsMamaArray()
    {
        MamaFamaResult r = MakePriceDf().MamaFama("close");
        Assert.NotEmpty(r.Mama);
    }

    [Fact] public void PermutationEntropy_ReturnsNonEmptyArray()
        => Assert.NotEmpty(MakePriceDf().PermutationEntropy("close", order: 4, window: 20));

    [Fact] public void RollSpread_ReturnsNonEmptyArray()
        => Assert.NotEmpty(MakePriceDf().RollSpread("close"));

    [Fact] public void RoofingFilter_ReturnsNonEmptyArray()
        => Assert.NotEmpty(MakePriceDf().RoofingFilter("close"));

    [Fact] public void WaveletEnergyRatio_ReturnsNonEmptyArray()
        => Assert.NotEmpty(MakePriceDf().WaveletEnergyRatio("close", window: 32));

    [Fact] public void WaveletEntropy_ReturnsNonEmptyArray()
        => Assert.NotEmpty(MakePriceDf().WaveletEntropy("close", window: 32));

    [Fact] public void Cusum_ReturnsNonEmptySignal()
    {
        CusumResult r = MakePriceDf().Cusum("close", threshold: 2.0);
        Assert.NotEmpty(r.Signal);
    }

    // ── Close + Volume indicators ─────────────────────────────────────────────

    [Fact] public void AmihudIlliquidity_ReturnsNonEmptyArray()
        => Assert.NotEmpty(MakeCandleDf().AmihudIlliquidity("close", "volume"));

    [Fact] public void ForceIndex_ReturnsNonEmptyArray()
        => Assert.NotEmpty(MakeCandleDf().ForceIndex("close", "volume"));

    [Fact] public void Vpin_ReturnsNonEmptyArray()
        => Assert.NotEmpty(MakeCandleDf().Vpin("close", "volume", bucketPeriod: 10, sigmaPeriod: 10));

    [Fact] public void VwapZScore_ReturnsNonEmptyArray()
        => Assert.NotEmpty(MakeCandleDf().VwapZScore("close", "volume"));

    // ── High + Low indicators ─────────────────────────────────────────────────

    [Fact] public void AroonOscillator_ReturnsNonEmptyArray()
        => Assert.NotEmpty(MakeCandleDf().AroonOscillator("high", "low"));

    [Fact] public void CorwinSchultz_ReturnsNonEmptyArray()
        => Assert.NotEmpty(MakeCandleDf().CorwinSchultz("high", "low"));

    // ── High + Low + Close indicators ────────────────────────────────────────

    [Fact] public void AdaptiveStochastic_ReturnsNonEmptyArray()
        => Assert.NotEmpty(MakeCandleDf().AdaptiveStochastic("high", "low", "close"));

    [Fact] public void Ichimoku_ReturnsTenkanSen()
    {
        IchimokuResult r = MakeCandleDf(120).Ichimoku("high", "low", "close");
        Assert.NotEmpty(r.TenkanSen);
    }

    [Fact] public void Supertrend_ReturnsLineArray()
    {
        SupertrendResult r = MakeCandleDf().Supertrend("high", "low", "close");
        Assert.NotEmpty(r.Line);
    }

    [Fact] public void SqueezeMomentum_ReturnsMomentum()
    {
        SqueezeResult r = MakeCandleDf().SqueezeMomentum("high", "low", "close");
        Assert.NotEmpty(r.Momentum);
    }

    // ── High + Low + Volume indicator ────────────────────────────────────────

    [Fact] public void EaseOfMovement_ReturnsNonEmptyArray()
        => Assert.NotEmpty(MakeCandleDf().EaseOfMovement("high", "low", "volume"));

    // ── HLCV indicators ───────────────────────────────────────────────────────

    [Fact] public void KlingerVolumeOscillator_ReturnsKvo()
    {
        KlingerResult r = MakeCandleDf().KlingerVolumeOscillator("high", "low", "close", "volume");
        Assert.NotEmpty(r.Kvo);
    }

    [Fact] public void TwiggsMoneyFlow_ReturnsNonEmptyArray()
        => Assert.NotEmpty(MakeCandleDf().TwiggsMoneyFlow("high", "low", "close", "volume"));

    // ── OHLC indicators ───────────────────────────────────────────────────────

    [Fact] public void GarmanKlass_ReturnsNonEmptyArray()
        => Assert.NotEmpty(MakeOhlcvDf().GarmanKlass("open", "high", "low", "close"));

    [Fact] public void RelativeVigorIndex_ReturnsRviArray()
    {
        RviResult r = MakeOhlcvDf().RelativeVigorIndex("open", "high", "low", "close");
        Assert.NotEmpty(r.Rvi);
    }

    [Fact] public void YangZhang_ReturnsNonEmptyArray()
        => Assert.NotEmpty(MakeOhlcvDf().YangZhang("open", "high", "low", "close"));

    [Fact] public void YangZhangVolRatio_ReturnsNonEmptyArray()
        => Assert.NotEmpty(MakeOhlcvDf(80).YangZhangVolRatio("open", "high", "low", "close"));

    // ── Two-column indicator ──────────────────────────────────────────────────

    [Fact] public void TransferEntropy_ReturnsNonEmptyArray()
        => Assert.NotEmpty(MakeCandleDf().TransferEntropy("close", "high"));
}
