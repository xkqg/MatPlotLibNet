// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

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
}
