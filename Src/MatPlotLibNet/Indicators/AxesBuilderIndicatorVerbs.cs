// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models;

namespace MatPlotLibNet;

/// <summary>The indicator verbs of the axes builder that draw over the prices (moving averages, bands, trailing stops,
/// volume-weighted prices, adaptive trend lines), the entry point for any other indicator, and the one tail every indicator
/// verb ends with.</summary>
public static class AxesBuilderIndicatorVerbs
{
    extension(AxesBuilder axes)
    {
        /// <summary>Marks this panel as using bar-slot X coordinates (slot [i, i+1], centre at i+0.5).
        /// All subsequently added indicators will automatically apply a +0.5 offset so their points
        /// align with bar centres rather than the left edge of each bar slot.</summary>
        /// <remarks>Must be called <em>before</em> adding any indicator series; the bar-slot X mapping
        /// is fixed at subplot construction time and cannot be applied retroactively.</remarks>
        public AxesBuilder UseBarSlotX() { axes.Axes.BarSlotX = true; return axes; }

        /// <summary>Applies a technical indicator to this axes, adding computed series or decorations.</summary>
        /// <remarks>Overlay indicators (SMA, EMA, Bollinger Bands) add series to the same axes.
        /// Panel indicators (RSI, MACD) should be placed in a separate subplot by the caller. In a bar-slot context
        /// (a categorical series already on the axes), nudges the indicator's X positions onto the category slot's
        /// centre -- the same nudge every dedicated indicator verb (Sma, Rsi, ...) applies via
        /// the one placement of every indicator, so the same indicator lands in the same place through either entry
        /// point.</remarks>
        public AxesBuilder AddIndicator(IIndicator indicator)
        {
            if (indicator is Indicators.Indicator concrete) return axes.ApplyIndicator(concrete, null);
            indicator.Apply(axes.Axes);
            return axes;
        }

        /// <summary>Adds a Simple Moving Average overlay. Auto-extracts price data from the last series on the axes.</summary>
        public AxesBuilder Sma(int period, Action<Indicators.Sma>? configure = null)
            => axes.ApplyIndicator(new Indicators.Sma(axes.Axes.PriceData, period), configure);

        /// <summary>Adds an Exponential Moving Average overlay.</summary>
        public AxesBuilder Ema(int period, Action<Indicators.Ema>? configure = null)
            => axes.ApplyIndicator(new Indicators.Ema(axes.Axes.PriceData, period), configure);

        /// <summary>Adds Bollinger Bands overlay.</summary>
        public AxesBuilder BollingerBands(int period = Indicators.BollingerBands.DefaultPeriod, double stdDev = Indicators.BollingerBands.DefaultStdDevMultiplier, Action<Indicators.BollingerBands>? configure = null)
            => axes.ApplyIndicator(new Indicators.BollingerBands(axes.Axes.PriceData, period, stdDev), configure);

        /// <summary>Adds a Parabolic SAR overlay to the price axes.</summary>
        public AxesBuilder ParabolicSar(double[] high, double[] low, double step = Indicators.ParabolicSar.DefaultStep, double max = Indicators.ParabolicSar.DefaultMax, Action<Indicators.ParabolicSar>? configure = null)
            => axes.ApplyIndicator(new Indicators.ParabolicSar(high, low, step, max), configure);

        /// <summary>Adds a MAMA/FAMA adaptive-moving-average overlay (Ehlers 2001). Two lines.</summary>
        public AxesBuilder MamaFama(double[] prices, double fastLimit = Indicators.MamaFama.DefaultFastLimit, double slowLimit = Indicators.MamaFama.DefaultSlowLimit,
            Action<Indicators.MamaFama>? configure = null)
            => axes.ApplyIndicator(new Indicators.MamaFama(prices, fastLimit, slowLimit), configure);

        /// <summary>Adds a volume-weighted average price overlay, from the first bar or from an anchor bar: the average of the
        /// typical price, (high + low + close) / 3, weighted by volume, summed from the anchor on. The line starts at the anchor bar;
        /// before it the average has no value and nothing is drawn.</summary>
        /// <param name="bars">High, Low, Close and Volume; built via <see cref="Indicators.OhlcvBars.Hlcv"/>.</param>
        /// <param name="anchor">The index of the bar the average starts at, from 0 (the first bar, the default) to the last bar.</param>
        /// <param name="configure">Optional styling of the indicator, run once before it is drawn.</param>
        /// <returns>The same builder, for chaining.</returns>
        /// <exception cref="ArgumentException">The arrays of <paramref name="bars"/> differ in number.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="anchor"/> is below 0 or not before the end of the bars.</exception>
        public AxesBuilder Vwap(Indicators.OhlcvBars bars, int anchor = Indicators.Vwap.DefaultAnchor,
            Action<Indicators.Vwap>? configure = null)
            => axes.ApplyIndicator(new Indicators.Vwap(bars, anchor), configure);

        /// <summary>Adds a volume profile ("volume by price") overlay: horizontal bars on the secondary X axis, one for each equal
        /// price bin of the closes, as long as the volume that traded in it, growing from the right edge of the plot. The longest bar
        /// covers a quarter of the plot. It takes the secondary X axis (the top edge) for itself.</summary>
        /// <param name="bars">Close and Volume; built via <see cref="Indicators.OhlcvBars.Cv"/>.</param>
        /// <param name="bins">The number of equal price bins, 1 or more. Default <see cref="Indicators.VolumeProfile.DefaultBins"/>.</param>
        /// <param name="configure">Optional styling of the indicator, run once before it is drawn.</param>
        /// <returns>The same builder, for chaining.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bins"/> is below 1.</exception>
        public AxesBuilder VolumeProfile(Indicators.OhlcvBars bars, int bins = Indicators.VolumeProfile.DefaultBins,
            Action<Indicators.VolumeProfile>? configure = null)
            => axes.ApplyIndicator(new Indicators.VolumeProfile(bars, bins), configure);

        /// <summary>Adds a Supertrend overlay (Seban 2008) — ATR-based trailing stop with direction
        /// flips. Renders as a single line below price in uptrends, above price in downtrends.</summary>
        public AxesBuilder Supertrend(Indicators.OhlcvBars bars,
            int period = Indicators.Supertrend.DefaultPeriod, double multiplier = Indicators.Supertrend.DefaultMultiplier,
            Action<Indicators.Supertrend>? configure = null)
            => axes.ApplyIndicator(new Indicators.Supertrend(bars, period, multiplier), configure);

        /// <summary>Adds an Ehlers Decycler overlay (Ehlers 2015) — price minus the one-pole
        /// high-pass filter output. Removes the dominant cycle band, leaving the trend.</summary>
        public AxesBuilder Decycler(double[] prices, int hpPeriod = Indicators.Decycler.DefaultHpPeriod,
            Action<Indicators.Decycler>? configure = null)
            => axes.ApplyIndicator(new Indicators.Decycler(prices, hpPeriod), configure);

        /// <summary>Adds an Ehlers Instantaneous Trendline overlay (Ehlers 2001) — adaptive
        /// linearly-weighted MA whose window length equals the Hilbert-derived dominant cycle.</summary>
        public AxesBuilder EhlersITrend(double[] prices,
            Action<Indicators.EhlersITrend>? configure = null)
            => axes.ApplyIndicator(new Indicators.EhlersITrend(prices), configure);

        /// <summary>Applies any <see cref="Indicators.IIndicator"/> to the current axes.</summary>
        /// <remarks>Generic entry point for indicators that don't have a dedicated shortcut
        /// (e.g. <c>Macd</c>, <c>Stochastic</c>, <c>Atr</c>, <c>Ichimoku</c>). The indicator instance
        /// is constructed by the caller. Same method as <see cref="AddIndicator"/>, under the name this overload
        /// shipped with first; kept so neither call site breaks.</remarks>
        public AxesBuilder Indicator(Indicators.IIndicator indicator) => axes.AddIndicator(indicator);

        /// <summary>The shared tail every indicator verb (Sma, Rsi, KlingerVolumeOscillator, ...) ends with: the axes places the
        /// indicator (<see cref="AxesBuilderIndicatorVerbs"/>), and the builder is returned for chaining.</summary>
        internal AxesBuilder ApplyIndicator<T>(T indicator, Action<T>? configure) where T : Indicators.Indicator
        {
            axes.Axes.Place(indicator, configure);
            return axes;
        }
    }

    extension(Axes axes)
    {
        /// <summary>Whether the indicators of this axes stand at the centres of bar slots: it holds a series of categories (a
        /// candlestick, a bar chart), or it was told so (<see cref="Axes.BarSlotX"/>) for a panel aligned to such a chart.</summary>
        private bool SlotsBars => axes.BarSlotX || axes.Series.Any(s => s is ICategoryLabeled);

        /// <summary>The prices an indicator shortcut computes over: those of the price series added last, searched over the whole
        /// list, so indicators chain (<c>.Plot(x, close).Sma(20).Sma(5)</c> computes the second over the first's output) and a
        /// candlestick or OHLC bar chart gives its closes.</summary>
        /// <exception cref="InvalidOperationException">The axes holds no price series.</exception>
        private double[] PriceData => axes.LastSeriesOf<IPriceSeries>() is { } price
            ? price.PriceData
            : throw new InvalidOperationException("No price data found. Add a series with Y data before calling indicator shortcuts.");

        /// <summary>Places <paramref name="indicator"/> on this axes: at the centres of the bar slots where the axes slots bars
        /// (its <c>SlotsBars</c>), configured by the caller, then applied.</summary>
        /// <typeparam name="T">The indicator's type, so the caller configures it as what it is.</typeparam>
        /// <param name="indicator">The indicator.</param>
        /// <param name="configure">The caller's configuration, run after the placement so an offset it sets wins.</param>
        private void Place<T>(T indicator, Action<T>? configure) where T : Indicator
        {
            if (axes.SlotsBars) indicator.Offset = CategorySlot.CentreOffset;
            configure?.Invoke(indicator);
            indicator.Apply(axes);
        }
    }
}
