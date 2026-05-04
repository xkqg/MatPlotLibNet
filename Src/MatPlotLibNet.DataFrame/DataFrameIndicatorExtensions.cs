// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.DataFrame;
using MatPlotLibNet.Indicators;
using MsDataFrame = Microsoft.Data.Analysis.DataFrame;

namespace MatPlotLibNet;

/// <summary>
/// Extension methods that apply financial indicators directly to a <see cref="MsDataFrame"/>,
/// resolving named columns to <see langword="double"/> arrays and delegating to the core indicator types.
/// </summary>
/// <example>
/// SMA and Bollinger Bands overlaid on a candlestick chart:
/// <code>
/// // df has columns: "open", "high", "low", "close" (double)
/// double[]    sma20 = df.Sma("close", 20);
/// BandsResult bb    = df.BollingerBands("close", period: 20, stdDev: 2.0);
///
/// string svg = Plt.Create()
///     .AddSubPlot(1, 1, 1, ax =>
///     {
///         ax.UseBarSlotX()
///           .Candlestick(open, high, low, close)
///           .Signal(sma20, label: "SMA 20")
///           .FillBetween(evalX, bb.Upper, bb.Lower);
///     })
///     .WithTitle("Price + Bollinger Bands")
///     .ToSvg();
/// </code>
/// MACD dashboard with ADX confirmation:
/// <code>
/// MacdResult macd = df.Macd("close");
/// double[]   adx  = df.Adx("high", "low", "close");
/// AdxResult  full = df.AdxFull("high", "low", "close");
///
/// string svg = Plt.Create()
///     .AddSubPlot(2, 1, 1, ax => ax.UseBarSlotX()
///         .Signal(macd.MacdLine,   label: "MACD")
///         .Signal(macd.SignalLine, label: "Signal")
///         .Bar(indices, macd.Histogram))
///     .AddSubPlot(2, 1, 2, ax => ax
///         .Signal(adx,          label: "ADX")
///         .Signal(full.PlusDi,  label: "+DI")
///         .Signal(full.MinusDi, label: "-DI"))
///     .ToSvg();
/// </code>
/// </example>
public static class DataFrameIndicatorExtensions
{
    // ── Price indicators (single price column → result) ──────────────────────

    /// <summary>Computes a Simple Moving Average from the named price column.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="priceCol">Name of the numeric price column.</param>
    /// <param name="period">Lookback period.</param>
    /// <returns>SMA values — same length as the column; leading values are <see cref="double.NaN"/> during warm-up.</returns>
    public static double[] Sma(this MsDataFrame df, string priceCol, int period) =>
        new Indicators.Sma(df.DoubleCol(priceCol), period).Compute();

    /// <summary>Computes an Exponential Moving Average from the named price column.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="priceCol">Name of the numeric price column.</param>
    /// <param name="period">Lookback period.</param>
    /// <returns>EMA values — same length as the column.</returns>
    public static double[] Ema(this MsDataFrame df, string priceCol, int period) =>
        new Indicators.Ema(df.DoubleCol(priceCol), period).Compute();

    /// <summary>Computes the Relative Strength Index from the named price column.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="priceCol">Name of the numeric price column.</param>
    /// <param name="period">RSI period (default 14).</param>
    /// <returns>RSI values in the range [0, 100] — same length as the column.</returns>
    public static double[] Rsi(this MsDataFrame df, string priceCol, int period = 14) =>
        new Indicators.Rsi(df.DoubleCol(priceCol), period).Compute();

    /// <summary>Computes Bollinger Bands from the named price column.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="priceCol">Name of the numeric price column.</param>
    /// <param name="period">Lookback period (default 20).</param>
    /// <param name="stdDev">Standard deviation multiplier (default 2.0).</param>
    /// <returns>A <see cref="BandsResult"/> with <c>Middle</c>, <c>Upper</c>, and <c>Lower</c> arrays.</returns>
    public static BandsResult BollingerBands(
        this MsDataFrame df, string priceCol, int period = 20, double stdDev = 2.0) =>
        new Indicators.BollingerBands(df.DoubleCol(priceCol), period, stdDev).Compute();

    /// <summary>Computes On-Balance Volume from the named close and volume columns.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="closeCol">Name of the numeric closing-price column.</param>
    /// <param name="volumeCol">Name of the numeric volume column.</param>
    /// <returns>Cumulative OBV values — same length as the column.</returns>
    public static double[] Obv(this MsDataFrame df, string closeCol, string volumeCol) =>
        new Indicators.Obv(df.DoubleCol(closeCol), df.DoubleCol(volumeCol)).Compute();

    /// <summary>Computes the MACD indicator from the named price column.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="priceCol">Name of the numeric price column.</param>
    /// <param name="fast">Fast EMA period (default 12).</param>
    /// <param name="slow">Slow EMA period (default 26).</param>
    /// <param name="signal">Signal EMA period (default 9).</param>
    /// <returns>A <see cref="MacdResult"/> with <c>MacdLine</c>, <c>SignalLine</c>, and <c>Histogram</c> arrays.</returns>
    public static MacdResult Macd(
        this MsDataFrame df, string priceCol, int fast = 12, int slow = 26, int signal = 9) =>
        new Indicators.Macd(df.DoubleCol(priceCol), fast, slow, signal).Compute();

    /// <summary>Computes the drawdown (peak-to-trough decline) from the named equity column.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="priceCol">Name of the numeric equity or close-price column.</param>
    /// <returns>Drawdown percentage values — same length as the column.</returns>
    public static double[] DrawDown(this MsDataFrame df, string priceCol) =>
        new Indicators.DrawDown(df.DoubleCol(priceCol)).Compute();

    // ── Candle indicators (high / low / close columns → result) ──────────────

    /// <summary>Computes the Average Directional Index (scalar ADX line) from high, low, and close columns.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="highCol">Name of the numeric high-price column.</param>
    /// <param name="lowCol">Name of the numeric low-price column.</param>
    /// <param name="closeCol">Name of the numeric close-price column.</param>
    /// <param name="period">ADX period (default 14).</param>
    /// <returns>ADX values — same length as the column. Use <see cref="AdxFull"/> to also obtain +DI and −DI.</returns>
    public static double[] Adx(
        this MsDataFrame df, string highCol, string lowCol, string closeCol, int period = 14) =>
        new Indicators.Adx(df.DoubleCol(highCol), df.DoubleCol(lowCol), df.DoubleCol(closeCol), period).Compute();

    /// <summary>Computes all three ADX signals (+DI, −DI, ADX) from high, low, and close columns.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="highCol">Name of the numeric high-price column.</param>
    /// <param name="lowCol">Name of the numeric low-price column.</param>
    /// <param name="closeCol">Name of the numeric close-price column.</param>
    /// <param name="period">ADX period (default 14).</param>
    /// <returns>An <see cref="AdxResult"/> with <c>Adx</c>, <c>PlusDi</c>, and <c>MinusDi</c> arrays.</returns>
    public static AdxResult AdxFull(
        this MsDataFrame df, string highCol, string lowCol, string closeCol, int period = 14) =>
        new Indicators.Adx(df.DoubleCol(highCol), df.DoubleCol(lowCol), df.DoubleCol(closeCol), period).ComputeFull();

    /// <summary>Computes the Average True Range from high, low, and close columns.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="highCol">Name of the numeric high-price column.</param>
    /// <param name="lowCol">Name of the numeric low-price column.</param>
    /// <param name="closeCol">Name of the numeric close-price column.</param>
    /// <param name="period">ATR period (default 14).</param>
    /// <returns>ATR values — same length as the column.</returns>
    public static double[] Atr(
        this MsDataFrame df, string highCol, string lowCol, string closeCol, int period = 14) =>
        new Indicators.Atr(df.DoubleCol(highCol), df.DoubleCol(lowCol), df.DoubleCol(closeCol), period).Compute();

    /// <summary>Computes the Commodity Channel Index from high, low, and close columns.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="highCol">Name of the numeric high-price column.</param>
    /// <param name="lowCol">Name of the numeric low-price column.</param>
    /// <param name="closeCol">Name of the numeric close-price column.</param>
    /// <param name="period">CCI period (default 20).</param>
    /// <returns>CCI values — same length as the column.</returns>
    public static double[] Cci(
        this MsDataFrame df, string highCol, string lowCol, string closeCol, int period = 20) =>
        new Indicators.Cci(df.DoubleCol(highCol), df.DoubleCol(lowCol), df.DoubleCol(closeCol), period).Compute();

    /// <summary>Computes Williams %R from high, low, and close columns.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="highCol">Name of the numeric high-price column.</param>
    /// <param name="lowCol">Name of the numeric low-price column.</param>
    /// <param name="closeCol">Name of the numeric close-price column.</param>
    /// <param name="period">Lookback period (default 14).</param>
    /// <returns>Williams %R values in the range [−100, 0] — same length as the column.</returns>
    public static double[] WilliamsR(
        this MsDataFrame df, string highCol, string lowCol, string closeCol, int period = 14) =>
        new Indicators.WilliamsR(df.DoubleCol(highCol), df.DoubleCol(lowCol), df.DoubleCol(closeCol), period).Compute();

    /// <summary>Computes the Stochastic Oscillator from high, low, and close columns.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="highCol">Name of the numeric high-price column.</param>
    /// <param name="lowCol">Name of the numeric low-price column.</param>
    /// <param name="closeCol">Name of the numeric close-price column.</param>
    /// <param name="period">%K period (default 14).</param>
    /// <returns>A <see cref="StochasticResult"/> with <c>K</c> and <c>D</c> arrays.</returns>
    public static StochasticResult Stochastic(
        this MsDataFrame df, string highCol, string lowCol, string closeCol, int period = 14) =>
        new Indicators.Stochastic(df.DoubleCol(highCol), df.DoubleCol(lowCol), df.DoubleCol(closeCol), period).Compute();

    /// <summary>Computes the Parabolic SAR stop-and-reverse levels from high and low columns.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="highCol">Name of the numeric high-price column.</param>
    /// <param name="lowCol">Name of the numeric low-price column.</param>
    /// <param name="step">Acceleration factor increment (default 0.02).</param>
    /// <param name="max">Maximum acceleration factor (default 0.2).</param>
    /// <returns>SAR stop levels — same length as the column.</returns>
    public static double[] ParabolicSar(
        this MsDataFrame df, string highCol, string lowCol, double step = 0.02, double max = 0.2) =>
        new Indicators.ParabolicSar(df.DoubleCol(highCol), df.DoubleCol(lowCol), step, max).Compute().Sar;

    /// <summary>Computes Keltner Channels from high, low, and close columns.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="highCol">Name of the numeric high-price column.</param>
    /// <param name="lowCol">Name of the numeric low-price column.</param>
    /// <param name="closeCol">Name of the numeric close-price column.</param>
    /// <param name="period">EMA and ATR period (default 20).</param>
    /// <param name="atrMultiplier">ATR band-width multiplier (default 1.5).</param>
    /// <returns>A <see cref="BandsResult"/> with <c>Middle</c>, <c>Upper</c>, and <c>Lower</c> arrays.</returns>
    public static BandsResult KeltnerChannels(
        this MsDataFrame df, string highCol, string lowCol, string closeCol,
        int period = 20, double atrMultiplier = 1.5) =>
        new Indicators.KeltnerChannels(
            df.DoubleCol(highCol), df.DoubleCol(lowCol), df.DoubleCol(closeCol), period, atrMultiplier).Compute();

    /// <summary>Computes the Volume-Weighted Average Price from high, low, close, and volume columns.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="highCol">Name of the numeric high-price column.</param>
    /// <param name="lowCol">Name of the numeric low-price column.</param>
    /// <param name="closeCol">Name of the numeric close-price column.</param>
    /// <param name="volumeCol">Name of the numeric volume column.</param>
    /// <returns>VWAP values — same length as the column.</returns>
    public static double[] Vwap(
        this MsDataFrame df,
        string highCol, string lowCol, string closeCol, string volumeCol)
    {
        // VWAP uses the typical price ((H+L+C)/3) as its price input and volume separately.
        // The Vwap indicator takes prices (typical) and volumes.
        var high   = df.DoubleCol(highCol);
        var low    = df.DoubleCol(lowCol);
        var close  = df.DoubleCol(closeCol);
        var volume = df.DoubleCol(volumeCol);

        var typical = new double[high.Length];
        for (int i = 0; i < high.Length; i++)
            typical[i] = (high[i] + low[i] + close[i]) / 3.0;

        return new Indicators.Vwap(typical, volume).Compute();
    }

    // ── Price-column indicators ───────────────────────────────────────────────

    /// <summary>Computes the Center of Gravity oscillator from the named price column.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="priceCol">Name of the numeric price column.</param>
    /// <param name="period">Lookback period (default 10).</param>
    /// <returns>CG oscillator values — same length as the column.</returns>
    public static double[] CgOscillator(this MsDataFrame df, string priceCol, int period = 10) =>
        new Indicators.CgOscillator(df.DoubleCol(priceCol), period).Compute();

    /// <summary>Computes the Bayesian Online Change-Point Detection signal from the named price column.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="priceCol">Name of the numeric price column.</param>
    /// <param name="hazard">Hazard rate — probability of a new change point per bar (default 0.01).</param>
    /// <param name="priorVariance">Prior variance of the normal-gamma model (default 1.0).</param>
    /// <param name="maxRunLength">Maximum run length considered (default 500).</param>
    /// <returns>BOCPD signal values — same length as the column.</returns>
    public static double[] Bocpd(
        this MsDataFrame df, string priceCol,
        double hazard = 0.01, double priorVariance = 1.0, int maxRunLength = 500) =>
        new Indicators.Bocpd(df.DoubleCol(priceCol), hazard, priorVariance, maxRunLength).Compute();

    /// <summary>Computes John Ehlers' Cyber Cycle from the named price column.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="priceCol">Name of the numeric price column.</param>
    /// <param name="alpha">Smoothing coefficient (default 0.07).</param>
    /// <returns>Cyber Cycle values — same length as the column.</returns>
    public static double[] CyberCycle(this MsDataFrame df, string priceCol, double alpha = 0.07) =>
        new Indicators.CyberCycle(df.DoubleCol(priceCol), alpha).Compute();

    /// <summary>Computes John Ehlers' Decycler from the named price column.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="priceCol">Name of the numeric price column.</param>
    /// <param name="hpPeriod">High-pass filter period (default 60).</param>
    /// <returns>Decycler values — same length as the column.</returns>
    public static double[] Decycler(this MsDataFrame df, string priceCol, int hpPeriod = 60) =>
        new Indicators.Decycler(df.DoubleCol(priceCol), hpPeriod).Compute();

    /// <summary>Computes John Ehlers' Instantaneous Trend from the named price column.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="priceCol">Name of the numeric price column.</param>
    /// <returns>ITrend values — same length as the column.</returns>
    public static double[] EhlersITrend(this MsDataFrame df, string priceCol) =>
        new Indicators.EhlersITrend(df.DoubleCol(priceCol)).Compute();

    /// <summary>Computes John Ehlers' Sine Wave indicator from the named price column.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="priceCol">Name of the numeric price column.</param>
    /// <returns>A <see cref="SineWaveResult"/> with <c>SineWave</c>, <c>LeadSine</c>, and <c>IsCyclic</c> arrays.</returns>
    public static SineWaveResult EhlersSineWave(this MsDataFrame df, string priceCol) =>
        new Indicators.EhlersSineWave(df.DoubleCol(priceCol)).Compute();

    /// <summary>Computes John Ehlers' Super Smoother filter from the named price column.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="priceCol">Name of the numeric price column.</param>
    /// <param name="period">Filter period (default 10).</param>
    /// <returns>Super Smoother values — same length as the column.</returns>
    public static double[] EhlersSuperSmoother(this MsDataFrame df, string priceCol, int period = 10) =>
        new Indicators.EhlersSuperSmoother(df.DoubleCol(priceCol), period).Compute();

    /// <summary>Computes the Fractional Differentiation transform from the named price column.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="priceCol">Name of the numeric price column.</param>
    /// <param name="d">Fractional differencing order (default 0.4).</param>
    /// <param name="tolerance">Weight truncation tolerance (default 1e-3).</param>
    /// <returns>Fractionally differenced values — same length as the column.</returns>
    public static double[] FractionalDifferentiation(
        this MsDataFrame df, string priceCol, double d = 0.4, double tolerance = 1e-3) =>
        new Indicators.FractionalDifferentiation(df.DoubleCol(priceCol), d, tolerance).Compute();

    /// <summary>Computes the Kaufman Efficiency Ratio from the named price column.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="priceCol">Name of the numeric price column.</param>
    /// <param name="period">Lookback period (default 10).</param>
    /// <returns>Efficiency Ratio values in [0, 1] — same length as the column.</returns>
    public static double[] KaufmanEfficiencyRatio(this MsDataFrame df, string priceCol, int period = 10) =>
        new Indicators.KaufmanEfficiencyRatio(df.DoubleCol(priceCol), period).Compute();

    /// <summary>Computes the Laguerre RSI from the named price column.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="priceCol">Name of the numeric price column.</param>
    /// <param name="alpha">Damping factor (default 0.2).</param>
    /// <returns>Laguerre RSI values in [0, 1] — same length as the column.</returns>
    public static double[] LaguerreRsi(this MsDataFrame df, string priceCol, double alpha = 0.2) =>
        new Indicators.LaguerreRsi(df.DoubleCol(priceCol), alpha).Compute();

    /// <summary>Computes MESA Adaptive Moving Average and Following Adaptive Moving Average from the named price column.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="priceCol">Name of the numeric price column.</param>
    /// <param name="fastLimit">Fast alpha limit (default 0.5).</param>
    /// <param name="slowLimit">Slow alpha limit (default 0.05).</param>
    /// <returns>A <see cref="MamaFamaResult"/> with <c>Mama</c> and <c>Fama</c> arrays.</returns>
    public static MamaFamaResult MamaFama(
        this MsDataFrame df, string priceCol, double fastLimit = 0.5, double slowLimit = 0.05) =>
        new Indicators.MamaFama(df.DoubleCol(priceCol), fastLimit, slowLimit).Compute();

    /// <summary>Computes Permutation Entropy from the named price column.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="priceCol">Name of the numeric price column.</param>
    /// <param name="order">Pattern order / embedding dimension (default 4).</param>
    /// <param name="window">Rolling window size (default 100).</param>
    /// <returns>Permutation Entropy values — same length as the column.</returns>
    public static double[] PermutationEntropy(
        this MsDataFrame df, string priceCol, int order = 4, int window = 100) =>
        new Indicators.PermutationEntropy(df.DoubleCol(priceCol), order, window).Compute();

    /// <summary>Computes Roll's implied bid-ask spread from the named price column.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="priceCol">Name of the numeric price column.</param>
    /// <param name="period">Rolling window size (default 20).</param>
    /// <returns>Roll Spread values — same length as the column.</returns>
    public static double[] RollSpread(this MsDataFrame df, string priceCol, int period = 20) =>
        new Indicators.RollSpread(df.DoubleCol(priceCol), period).Compute();

    /// <summary>Computes John Ehlers' Roofing Filter from the named price column.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="priceCol">Name of the numeric price column.</param>
    /// <param name="hpPeriod">High-pass filter period (default 48).</param>
    /// <param name="lpPeriod">Low-pass period (default 10).</param>
    /// <returns>Roofing Filter values — same length as the column.</returns>
    public static double[] RoofingFilter(
        this MsDataFrame df, string priceCol, int hpPeriod = 48, int lpPeriod = 10) =>
        new Indicators.RoofingFilter(df.DoubleCol(priceCol), hpPeriod, lpPeriod).Compute();

    /// <summary>Computes Wavelet Energy Ratio from the named price column.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="priceCol">Name of the numeric price column.</param>
    /// <param name="window">DWT window size — must be a power of 2 (default 64).</param>
    /// <param name="level">Decomposition level to measure (default 0).</param>
    /// <returns>Wavelet Energy Ratio values — same length as the column.</returns>
    public static double[] WaveletEnergyRatio(
        this MsDataFrame df, string priceCol, int window = 64, int level = 0) =>
        new Indicators.WaveletEnergyRatio(df.DoubleCol(priceCol), window, level).Compute();

    /// <summary>Computes Wavelet Entropy from the named price column.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="priceCol">Name of the numeric price column.</param>
    /// <param name="window">DWT window size — must be a power of 2 (default 64).</param>
    /// <returns>Wavelet Entropy values — same length as the column.</returns>
    public static double[] WaveletEntropy(this MsDataFrame df, string priceCol, int window = 64) =>
        new Indicators.WaveletEntropy(df.DoubleCol(priceCol), window).Compute();

    /// <summary>Computes the Cumulative Sum (CUSUM) change-point signal from the named price column.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="priceCol">Name of the numeric price column.</param>
    /// <param name="threshold">Detection threshold (h value). Required.</param>
    /// <param name="drift">Allowable drift before signalling (default 0.0).</param>
    /// <returns>A <see cref="CusumResult"/> with <c>Signal</c>, <c>SPos</c>, and <c>SNeg</c> arrays.</returns>
    public static CusumResult Cusum(
        this MsDataFrame df, string priceCol, double threshold, double drift = 0.0) =>
        new Indicators.Cusum(df.DoubleCol(priceCol), threshold, drift).Compute();

    // ── Close + Volume indicators ─────────────────────────────────────────────

    /// <summary>Computes the Amihud Illiquidity ratio from close and volume columns.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="closeCol">Name of the numeric close-price column.</param>
    /// <param name="volumeCol">Name of the numeric volume column.</param>
    /// <param name="period">Rolling window size (default 20).</param>
    /// <returns>Amihud Illiquidity values — same length as the column.</returns>
    public static double[] AmihudIlliquidity(
        this MsDataFrame df, string closeCol, string volumeCol, int period = 20) =>
        new Indicators.AmihudIlliquidity(df.DoubleCol(closeCol), df.DoubleCol(volumeCol), period).Compute();

    /// <summary>Computes the Force Index from close and volume columns.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="closeCol">Name of the numeric close-price column.</param>
    /// <param name="volumeCol">Name of the numeric volume column.</param>
    /// <param name="period">EMA smoothing period (default 13).</param>
    /// <returns>Force Index values — same length as the column.</returns>
    public static double[] ForceIndex(
        this MsDataFrame df, string closeCol, string volumeCol, int period = 13) =>
        new Indicators.ForceIndex(df.DoubleCol(closeCol), df.DoubleCol(volumeCol), period).Compute();

    /// <summary>Computes the Volume-synchronised Probability of INformed trading (VPIN) from close and volume columns.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="closeCol">Name of the numeric close-price column.</param>
    /// <param name="volumeCol">Name of the numeric volume column.</param>
    /// <param name="bucketPeriod">Volume bucket size in bars (default 50).</param>
    /// <param name="sigmaPeriod">Rolling sigma window in buckets (default 50).</param>
    /// <returns>VPIN values — same length as the column.</returns>
    public static double[] Vpin(
        this MsDataFrame df, string closeCol, string volumeCol,
        int bucketPeriod = 50, int sigmaPeriod = 50) =>
        new Indicators.Vpin(df.DoubleCol(closeCol), df.DoubleCol(volumeCol), bucketPeriod, sigmaPeriod).Compute();

    /// <summary>Computes the VWAP Z-Score from close and volume columns.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="closeCol">Name of the numeric close-price column.</param>
    /// <param name="volumeCol">Name of the numeric volume column.</param>
    /// <param name="window">Rolling VWAP window size (default 20).</param>
    /// <returns>VWAP Z-Score values — same length as the column.</returns>
    public static double[] VwapZScore(
        this MsDataFrame df, string closeCol, string volumeCol, int window = 20) =>
        new Indicators.VwapZScore(df.DoubleCol(closeCol), df.DoubleCol(volumeCol), window).Compute();

    // ── High + Low indicators ─────────────────────────────────────────────────

    /// <summary>Computes the Aroon Oscillator from high and low columns.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="highCol">Name of the numeric high-price column.</param>
    /// <param name="lowCol">Name of the numeric low-price column.</param>
    /// <param name="period">Lookback period (default 25).</param>
    /// <returns>Aroon Oscillator values in [−100, 100] — same length as the column.</returns>
    public static double[] AroonOscillator(
        this MsDataFrame df, string highCol, string lowCol, int period = 25) =>
        new Indicators.AroonOscillator(df.DoubleCol(highCol), df.DoubleCol(lowCol), period).Compute();

    /// <summary>Computes the Corwin-Schultz high-low spread estimator from high and low columns.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="highCol">Name of the numeric high-price column.</param>
    /// <param name="lowCol">Name of the numeric low-price column.</param>
    /// <param name="period">Rolling window (default 20).</param>
    /// <returns>Corwin-Schultz spread values — same length as the column.</returns>
    public static double[] CorwinSchultz(
        this MsDataFrame df, string highCol, string lowCol, int period = 20) =>
        new Indicators.CorwinSchultz(df.DoubleCol(highCol), df.DoubleCol(lowCol), period).Compute();

    // ── High + Low + Close indicators ─────────────────────────────────────────

    /// <summary>Computes the Adaptive Stochastic Oscillator (smoothed %K) from high, low, and close columns.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="highCol">Name of the numeric high-price column.</param>
    /// <param name="lowCol">Name of the numeric low-price column.</param>
    /// <param name="closeCol">Name of the numeric close-price column.</param>
    /// <param name="smoothingPeriod">Super-smoother period for %K (default 3).</param>
    /// <returns>Smoothed %K values — same length as the column.</returns>
    public static double[] AdaptiveStochastic(
        this MsDataFrame df, string highCol, string lowCol, string closeCol, int smoothingPeriod = 3) =>
        new Indicators.AdaptiveStochastic(
            df.DoubleCol(highCol), df.DoubleCol(lowCol), df.DoubleCol(closeCol), smoothingPeriod).Compute();

    /// <summary>Computes the Ichimoku Cloud from high, low, and close columns.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="highCol">Name of the numeric high-price column.</param>
    /// <param name="lowCol">Name of the numeric low-price column.</param>
    /// <param name="closeCol">Name of the numeric close-price column.</param>
    /// <param name="tenkanPeriod">Tenkan-Sen period (default 9).</param>
    /// <param name="kijunPeriod">Kijun-Sen period (default 26).</param>
    /// <param name="senkouBPeriod">Senkou Span B period (default 52).</param>
    /// <param name="displacement">Cloud displacement in bars (default 26).</param>
    /// <returns>An <see cref="IchimokuResult"/> with all five Ichimoku lines.</returns>
    public static IchimokuResult Ichimoku(
        this MsDataFrame df, string highCol, string lowCol, string closeCol,
        int tenkanPeriod = 9, int kijunPeriod = 26, int senkouBPeriod = 52, int displacement = 26) =>
        new Indicators.Ichimoku(
            df.DoubleCol(highCol), df.DoubleCol(lowCol), df.DoubleCol(closeCol),
            tenkanPeriod, kijunPeriod, senkouBPeriod, displacement).Compute();

    /// <summary>Computes the Supertrend indicator from high, low, and close columns.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="highCol">Name of the numeric high-price column.</param>
    /// <param name="lowCol">Name of the numeric low-price column.</param>
    /// <param name="closeCol">Name of the numeric close-price column.</param>
    /// <param name="period">ATR period (default 10).</param>
    /// <param name="multiplier">ATR band multiplier (default 3.0).</param>
    /// <returns>A <see cref="SupertrendResult"/> with <c>Line</c>, <c>Direction</c>, and <c>Flipped</c> arrays.</returns>
    public static SupertrendResult Supertrend(
        this MsDataFrame df, string highCol, string lowCol, string closeCol,
        int period = 10, double multiplier = 3.0) =>
        new Indicators.Supertrend(
            df.DoubleCol(highCol), df.DoubleCol(lowCol), df.DoubleCol(closeCol), period, multiplier).Compute();

    /// <summary>Computes the Squeeze Momentum indicator from high, low, and close columns.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="highCol">Name of the numeric high-price column.</param>
    /// <param name="lowCol">Name of the numeric low-price column.</param>
    /// <param name="closeCol">Name of the numeric close-price column.</param>
    /// <param name="period">Bollinger/Keltner period (default 20).</param>
    /// <param name="bbMult">Bollinger Bands multiplier (default 2.0).</param>
    /// <param name="kcMult">Keltner Channels multiplier (default 1.5).</param>
    /// <returns>A <see cref="SqueezeResult"/> with <c>SqueezeOn</c> flags and <c>Momentum</c> values.</returns>
    public static SqueezeResult SqueezeMomentum(
        this MsDataFrame df, string highCol, string lowCol, string closeCol,
        int period = 20, double bbMult = 2.0, double kcMult = 1.5) =>
        new Indicators.SqueezeMomentum(
            df.DoubleCol(highCol), df.DoubleCol(lowCol), df.DoubleCol(closeCol), period, bbMult, kcMult).Compute();

    // ── High + Low + Volume indicator ─────────────────────────────────────────

    /// <summary>Computes the Ease of Movement indicator from high, low, and volume columns.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="highCol">Name of the numeric high-price column.</param>
    /// <param name="lowCol">Name of the numeric low-price column.</param>
    /// <param name="volumeCol">Name of the numeric volume column.</param>
    /// <param name="period">EMA smoothing period (default 14).</param>
    /// <param name="scale">Volume scale divisor (default 1,000,000).</param>
    /// <returns>EOM values — same length as the column.</returns>
    public static double[] EaseOfMovement(
        this MsDataFrame df, string highCol, string lowCol, string volumeCol,
        int period = 14, double scale = 1_000_000) =>
        new Indicators.EaseOfMovement(
            df.DoubleCol(highCol), df.DoubleCol(lowCol), df.DoubleCol(volumeCol), period, scale).Compute();

    // ── HLCV indicators ───────────────────────────────────────────────────────

    /// <summary>Computes the Klinger Volume Oscillator from high, low, close, and volume columns.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="highCol">Name of the numeric high-price column.</param>
    /// <param name="lowCol">Name of the numeric low-price column.</param>
    /// <param name="closeCol">Name of the numeric close-price column.</param>
    /// <param name="volumeCol">Name of the numeric volume column.</param>
    /// <param name="fastPeriod">Fast EMA period (default 34).</param>
    /// <param name="slowPeriod">Slow EMA period (default 55).</param>
    /// <param name="signalPeriod">Signal EMA period (default 13).</param>
    /// <returns>A <see cref="KlingerResult"/> with <c>Kvo</c> and <c>Signal</c> arrays.</returns>
    public static KlingerResult KlingerVolumeOscillator(
        this MsDataFrame df, string highCol, string lowCol, string closeCol, string volumeCol,
        int fastPeriod = 34, int slowPeriod = 55, int signalPeriod = 13) =>
        new Indicators.KlingerVolumeOscillator(
            df.DoubleCol(highCol), df.DoubleCol(lowCol), df.DoubleCol(closeCol), df.DoubleCol(volumeCol),
            fastPeriod, slowPeriod, signalPeriod).Compute();

    /// <summary>Computes Colin Twiggs' Money Flow from high, low, close, and volume columns.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="highCol">Name of the numeric high-price column.</param>
    /// <param name="lowCol">Name of the numeric low-price column.</param>
    /// <param name="closeCol">Name of the numeric close-price column.</param>
    /// <param name="volumeCol">Name of the numeric volume column.</param>
    /// <param name="period">Wilder smoothing period (default 21).</param>
    /// <returns>TMF values in [−1, 1] — same length as the column.</returns>
    public static double[] TwiggsMoneyFlow(
        this MsDataFrame df, string highCol, string lowCol, string closeCol, string volumeCol,
        int period = 21) =>
        new Indicators.TwiggsMoneyFlow(
            df.DoubleCol(highCol), df.DoubleCol(lowCol), df.DoubleCol(closeCol), df.DoubleCol(volumeCol),
            period).Compute();

    // ── OHLC indicators ───────────────────────────────────────────────────────

    /// <summary>Computes the Garman-Klass volatility estimator from open, high, low, and close columns.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="openCol">Name of the numeric open-price column.</param>
    /// <param name="highCol">Name of the numeric high-price column.</param>
    /// <param name="lowCol">Name of the numeric low-price column.</param>
    /// <param name="closeCol">Name of the numeric close-price column.</param>
    /// <param name="period">Rolling window size (default 20).</param>
    /// <returns>Annualised Garman-Klass volatility values — same length as the column.</returns>
    public static double[] GarmanKlass(
        this MsDataFrame df, string openCol, string highCol, string lowCol, string closeCol,
        int period = 20) =>
        new Indicators.GarmanKlass(
            df.DoubleCol(openCol), df.DoubleCol(highCol), df.DoubleCol(lowCol), df.DoubleCol(closeCol),
            period).Compute();

    /// <summary>Computes the Relative Vigor Index from open, high, low, and close columns.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="openCol">Name of the numeric open-price column.</param>
    /// <param name="highCol">Name of the numeric high-price column.</param>
    /// <param name="lowCol">Name of the numeric low-price column.</param>
    /// <param name="closeCol">Name of the numeric close-price column.</param>
    /// <param name="period">Lookback period (default 10).</param>
    /// <returns>An <see cref="RviResult"/> with <c>Rvi</c> and <c>Signal</c> arrays.</returns>
    public static RviResult RelativeVigorIndex(
        this MsDataFrame df, string openCol, string highCol, string lowCol, string closeCol,
        int period = 10) =>
        new Indicators.RelativeVigorIndex(
            df.DoubleCol(openCol), df.DoubleCol(highCol), df.DoubleCol(lowCol), df.DoubleCol(closeCol),
            period).Compute();

    /// <summary>Computes the Yang-Zhang volatility estimator from open, high, low, and close columns.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="openCol">Name of the numeric open-price column.</param>
    /// <param name="highCol">Name of the numeric high-price column.</param>
    /// <param name="lowCol">Name of the numeric low-price column.</param>
    /// <param name="closeCol">Name of the numeric close-price column.</param>
    /// <param name="period">Rolling window size (default 20).</param>
    /// <returns>Annualised Yang-Zhang volatility values — same length as the column.</returns>
    public static double[] YangZhang(
        this MsDataFrame df, string openCol, string highCol, string lowCol, string closeCol,
        int period = 20) =>
        new Indicators.YangZhang(
            df.DoubleCol(openCol), df.DoubleCol(highCol), df.DoubleCol(lowCol), df.DoubleCol(closeCol),
            period).Compute();

    /// <summary>Computes the Yang-Zhang volatility ratio from open, high, low, and close columns.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="openCol">Name of the numeric open-price column.</param>
    /// <param name="highCol">Name of the numeric high-price column.</param>
    /// <param name="lowCol">Name of the numeric low-price column.</param>
    /// <param name="closeCol">Name of the numeric close-price column.</param>
    /// <param name="shortWindow">Short volatility window (default 20).</param>
    /// <param name="longWindow">Long volatility window (default 60).</param>
    /// <returns>Yang-Zhang volatility ratio values — same length as the column.</returns>
    public static double[] YangZhangVolRatio(
        this MsDataFrame df, string openCol, string highCol, string lowCol, string closeCol,
        int shortWindow = 20, int longWindow = 60) =>
        new Indicators.YangZhangVolRatio(
            df.DoubleCol(openCol), df.DoubleCol(highCol), df.DoubleCol(lowCol), df.DoubleCol(closeCol),
            shortWindow, longWindow).Compute();

    // ── Two-column indicator ──────────────────────────────────────────────────

    /// <summary>Computes the Transfer Entropy from two named columns (source → target direction).</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="sourceCol">Name of the numeric source column.</param>
    /// <param name="targetCol">Name of the numeric target column.</param>
    /// <param name="bins">Number of histogram bins for probability estimation (default 8).</param>
    /// <param name="lag">Prediction lag in bars (default 1).</param>
    /// <returns>Transfer Entropy values — same length as the column.</returns>
    public static double[] TransferEntropy(
        this MsDataFrame df, string sourceCol, string targetCol, int bins = 8, int lag = 1) =>
        new Indicators.TransferEntropy(
            df.DoubleCol(sourceCol), df.DoubleCol(targetCol), bins, lag).Compute();
}
