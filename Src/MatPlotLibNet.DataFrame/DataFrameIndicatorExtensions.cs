// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

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
        new Indicators.Sma(Col(df, priceCol), period).Compute();

    /// <summary>Computes an Exponential Moving Average from the named price column.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="priceCol">Name of the numeric price column.</param>
    /// <param name="period">Lookback period.</param>
    /// <returns>EMA values — same length as the column.</returns>
    public static double[] Ema(this MsDataFrame df, string priceCol, int period) =>
        new Indicators.Ema(Col(df, priceCol), period).Compute();

    /// <summary>Computes the Relative Strength Index from the named price column.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="priceCol">Name of the numeric price column.</param>
    /// <param name="period">RSI period (default 14).</param>
    /// <returns>RSI values in the range [0, 100] — same length as the column.</returns>
    public static double[] Rsi(this MsDataFrame df, string priceCol, int period = 14) =>
        new Indicators.Rsi(Col(df, priceCol), period).Compute();

    /// <summary>Computes Bollinger Bands from the named price column.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="priceCol">Name of the numeric price column.</param>
    /// <param name="period">Lookback period (default 20).</param>
    /// <param name="stdDev">Standard deviation multiplier (default 2.0).</param>
    /// <returns>A <see cref="BandsResult"/> with <c>Middle</c>, <c>Upper</c>, and <c>Lower</c> arrays.</returns>
    public static BandsResult BollingerBands(
        this MsDataFrame df, string priceCol, int period = 20, double stdDev = 2.0) =>
        new Indicators.BollingerBands(Col(df, priceCol), period, stdDev).Compute();

    /// <summary>Computes On-Balance Volume from the named close and volume columns.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="closeCol">Name of the numeric closing-price column.</param>
    /// <param name="volumeCol">Name of the numeric volume column.</param>
    /// <returns>Cumulative OBV values — same length as the column.</returns>
    public static double[] Obv(this MsDataFrame df, string closeCol, string volumeCol) =>
        new Indicators.Obv(Col(df, closeCol), Col(df, volumeCol)).Compute();

    /// <summary>Computes the MACD indicator from the named price column.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="priceCol">Name of the numeric price column.</param>
    /// <param name="fast">Fast EMA period (default 12).</param>
    /// <param name="slow">Slow EMA period (default 26).</param>
    /// <param name="signal">Signal EMA period (default 9).</param>
    /// <returns>A <see cref="MacdResult"/> with <c>MacdLine</c>, <c>SignalLine</c>, and <c>Histogram</c> arrays.</returns>
    public static MacdResult Macd(
        this MsDataFrame df, string priceCol, int fast = 12, int slow = 26, int signal = 9) =>
        new Indicators.Macd(Col(df, priceCol), fast, slow, signal).Compute();

    /// <summary>Computes the drawdown (peak-to-trough decline) from the named equity column.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="priceCol">Name of the numeric equity or close-price column.</param>
    /// <returns>Drawdown percentage values — same length as the column.</returns>
    public static double[] DrawDown(this MsDataFrame df, string priceCol) =>
        new Indicators.DrawDown(Col(df, priceCol)).Compute();

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
        new Indicators.Adx(Col(df, highCol), Col(df, lowCol), Col(df, closeCol), period).Compute();

    /// <summary>Computes all three ADX signals (+DI, −DI, ADX) from high, low, and close columns.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="highCol">Name of the numeric high-price column.</param>
    /// <param name="lowCol">Name of the numeric low-price column.</param>
    /// <param name="closeCol">Name of the numeric close-price column.</param>
    /// <param name="period">ADX period (default 14).</param>
    /// <returns>An <see cref="AdxResult"/> with <c>Adx</c>, <c>PlusDi</c>, and <c>MinusDi</c> arrays.</returns>
    public static AdxResult AdxFull(
        this MsDataFrame df, string highCol, string lowCol, string closeCol, int period = 14) =>
        new Indicators.Adx(Col(df, highCol), Col(df, lowCol), Col(df, closeCol), period).ComputeFull();

    /// <summary>Computes the Average True Range from high, low, and close columns.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="highCol">Name of the numeric high-price column.</param>
    /// <param name="lowCol">Name of the numeric low-price column.</param>
    /// <param name="closeCol">Name of the numeric close-price column.</param>
    /// <param name="period">ATR period (default 14).</param>
    /// <returns>ATR values — same length as the column.</returns>
    public static double[] Atr(
        this MsDataFrame df, string highCol, string lowCol, string closeCol, int period = 14) =>
        new Indicators.Atr(Col(df, highCol), Col(df, lowCol), Col(df, closeCol), period).Compute();

    /// <summary>Computes the Commodity Channel Index from high, low, and close columns.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="highCol">Name of the numeric high-price column.</param>
    /// <param name="lowCol">Name of the numeric low-price column.</param>
    /// <param name="closeCol">Name of the numeric close-price column.</param>
    /// <param name="period">CCI period (default 20).</param>
    /// <returns>CCI values — same length as the column.</returns>
    public static double[] Cci(
        this MsDataFrame df, string highCol, string lowCol, string closeCol, int period = 20) =>
        new Indicators.Cci(Col(df, highCol), Col(df, lowCol), Col(df, closeCol), period).Compute();

    /// <summary>Computes Williams %R from high, low, and close columns.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="highCol">Name of the numeric high-price column.</param>
    /// <param name="lowCol">Name of the numeric low-price column.</param>
    /// <param name="closeCol">Name of the numeric close-price column.</param>
    /// <param name="period">Lookback period (default 14).</param>
    /// <returns>Williams %R values in the range [−100, 0] — same length as the column.</returns>
    public static double[] WilliamsR(
        this MsDataFrame df, string highCol, string lowCol, string closeCol, int period = 14) =>
        new Indicators.WilliamsR(Col(df, highCol), Col(df, lowCol), Col(df, closeCol), period).Compute();

    /// <summary>Computes the Stochastic Oscillator from high, low, and close columns.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="highCol">Name of the numeric high-price column.</param>
    /// <param name="lowCol">Name of the numeric low-price column.</param>
    /// <param name="closeCol">Name of the numeric close-price column.</param>
    /// <param name="period">%K period (default 14).</param>
    /// <returns>A <see cref="StochasticResult"/> with <c>K</c> and <c>D</c> arrays.</returns>
    public static StochasticResult Stochastic(
        this MsDataFrame df, string highCol, string lowCol, string closeCol, int period = 14) =>
        new Indicators.Stochastic(Col(df, highCol), Col(df, lowCol), Col(df, closeCol), period).Compute();

    /// <summary>Computes the Parabolic SAR stop-and-reverse levels from high and low columns.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="highCol">Name of the numeric high-price column.</param>
    /// <param name="lowCol">Name of the numeric low-price column.</param>
    /// <param name="step">Acceleration factor increment (default 0.02).</param>
    /// <param name="max">Maximum acceleration factor (default 0.2).</param>
    /// <returns>SAR stop levels — same length as the column.</returns>
    public static double[] ParabolicSar(
        this MsDataFrame df, string highCol, string lowCol, double step = 0.02, double max = 0.2) =>
        new Indicators.ParabolicSar(Col(df, highCol), Col(df, lowCol), step, max).Compute().Sar;

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
            Col(df, highCol), Col(df, lowCol), Col(df, closeCol), period, atrMultiplier).Compute();

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
        var high   = Col(df, highCol);
        var low    = Col(df, lowCol);
        var close  = Col(df, closeCol);
        var volume = Col(df, volumeCol);

        var typical = new double[high.Length];
        for (int i = 0; i < high.Length; i++)
            typical[i] = (high[i] + low[i] + close[i]) / 3.0;

        return new Indicators.Vwap(typical, volume).Compute();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static double[] Col(MsDataFrame df, string name)
    {
        if (!df.Columns.Any(c => c.Name == name))
            throw new ArgumentException($"DataFrame has no column '{name}'.", nameof(name));
        return DataFrameColumnReader.ToDoubleArray(df[name]);
    }
}
