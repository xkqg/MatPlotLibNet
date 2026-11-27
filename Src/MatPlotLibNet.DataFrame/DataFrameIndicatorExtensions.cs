// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MsDataFrame = Microsoft.Data.Analysis.DataFrame;

namespace MatPlotLibNet;

/// <summary>
/// The bars a technical indicator reads, from named columns of a <see cref="MsDataFrame"/>: one call for each shape of bars an
/// indicator takes (<see cref="OhlcvBars"/>), and <see cref="DataFrameColumnExtensions"/>' <c>Values</c> for an indicator over one
/// column. The indicator is then made from its own class, which states its parameters, their defaults and what it computes, so
/// every indicator of the library is reached from a data frame, by the one call of the shape it reads.
/// </summary>
/// <example>
/// A moving average and Bollinger bands over the close column, an ATR over the highs, lows and closes:
/// <code>
/// // df has columns: "open", "high", "low", "close", "volume" (double)
/// double[]    sma20 = new Sma(df.Values("close"), 20).Compute();
/// BandsResult bb    = new BollingerBands(df.Values("close"), period: 20).Compute();
/// double[]    atr   = new Atr(df.Hlc("high", "low", "close")).Compute();
///
/// string svg = Plt.Create()
///     .AddSubPlot(1, 1, 1, ax => ax
///         .Plot(Enumerable.Range(0, sma20.Length).Select(i => (double)i).ToArray(), sma20))
///     .ToSvg();
/// </code>
/// A Klinger volume oscillator over the four price columns and the volume:
/// <code>
/// KlingerResult kvo = new KlingerVolumeOscillator(df.Hlcv("high", "low", "close", "volume")).Compute();
/// </code>
/// </example>
public static class DataFrameIndicatorExtensions
{
    /// <param name="df">The data frame the columns are read from.</param>
    extension(MsDataFrame df)
    {
        /// <summary>The bars an indicator over highs, lows and closes reads (<see cref="OhlcvBars.Hlc"/>), from the named columns.</summary>
        /// <param name="highCol">The name of the column of highs.</param>
        /// <param name="lowCol">The name of the column of lows.</param>
        /// <param name="closeCol">The name of the column of closes.</param>
        /// <returns>The bars.</returns>
        /// <exception cref="ArgumentException">The frame has no column of one of the names, or the columns differ in length.</exception>
        public OhlcvBars Hlc(string highCol, string lowCol, string closeCol) =>
            OhlcvBars.Hlc(df.Values(highCol), df.Values(lowCol), df.Values(closeCol));

        /// <summary>The bars an indicator over highs, lows, closes and volumes reads (<see cref="OhlcvBars.Hlcv"/>), from the named
        /// columns.</summary>
        /// <param name="highCol">The name of the column of highs.</param>
        /// <param name="lowCol">The name of the column of lows.</param>
        /// <param name="closeCol">The name of the column of closes.</param>
        /// <param name="volumeCol">The name of the column of volumes.</param>
        /// <returns>The bars.</returns>
        /// <exception cref="ArgumentException">The frame has no column of one of the names, or the columns differ in length.</exception>
        public OhlcvBars Hlcv(string highCol, string lowCol, string closeCol, string volumeCol) =>
            OhlcvBars.Hlcv(df.Values(highCol), df.Values(lowCol), df.Values(closeCol), df.Values(volumeCol));

        /// <summary>The bars an indicator over opens, highs, lows and closes reads (<see cref="OhlcvBars.Ohlc"/>), from the named
        /// columns.</summary>
        /// <param name="openCol">The name of the column of opens.</param>
        /// <param name="highCol">The name of the column of highs.</param>
        /// <param name="lowCol">The name of the column of lows.</param>
        /// <param name="closeCol">The name of the column of closes.</param>
        /// <returns>The bars.</returns>
        /// <exception cref="ArgumentException">The frame has no column of one of the names, or the columns differ in length.</exception>
        public OhlcvBars Ohlc(string openCol, string highCol, string lowCol, string closeCol) =>
            OhlcvBars.Ohlc(df.Values(openCol), df.Values(highCol), df.Values(lowCol), df.Values(closeCol));

        /// <summary>The bars an indicator over closes and volumes reads (<see cref="OhlcvBars.Cv"/>), from the named columns.</summary>
        /// <param name="closeCol">The name of the column of closes.</param>
        /// <param name="volumeCol">The name of the column of volumes.</param>
        /// <returns>The bars.</returns>
        /// <exception cref="ArgumentException">The frame has no column of one of the names, or the columns differ in length.</exception>
        public OhlcvBars Cv(string closeCol, string volumeCol) =>
            OhlcvBars.Cv(df.Values(closeCol), df.Values(volumeCol));

        /// <summary>The bars an indicator over highs and lows reads (<see cref="OhlcvBars.Hl"/>), from the named columns.</summary>
        /// <param name="highCol">The name of the column of highs.</param>
        /// <param name="lowCol">The name of the column of lows.</param>
        /// <returns>The bars.</returns>
        /// <exception cref="ArgumentException">The frame has no column of one of the names, or the columns differ in length.</exception>
        public OhlcvBars Hl(string highCol, string lowCol) =>
            OhlcvBars.Hl(df.Values(highCol), df.Values(lowCol));

        /// <summary>The bars an indicator over highs, lows and volumes reads (<see cref="OhlcvBars.Hlv"/>), from the named columns.</summary>
        /// <param name="highCol">The name of the column of highs.</param>
        /// <param name="lowCol">The name of the column of lows.</param>
        /// <param name="volumeCol">The name of the column of volumes.</param>
        /// <returns>The bars.</returns>
        /// <exception cref="ArgumentException">The frame has no column of one of the names, or the columns differ in length.</exception>
        public OhlcvBars Hlv(string highCol, string lowCol, string volumeCol) =>
            OhlcvBars.Hlv(df.Values(highCol), df.Values(lowCol), df.Values(volumeCol));
    }
}
