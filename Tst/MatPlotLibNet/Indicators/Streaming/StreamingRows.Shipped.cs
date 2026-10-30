// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Indicators.Streaming;

namespace MatPlotLibNet.Tests.Indicators.Streaming;

/// <summary>The rows of the classification map for the streaming forms that shipped before the map existed.</summary>
public static partial class StreamingRows
{
    private static IEnumerable<StreamingRow> Shipped()
    {
        yield return new("Sma", () => new StreamingSma(5),
            b => [StreamingRow.Last(new Sma(b.Close, 5).Compute())]);

        yield return new("Ema", () => new StreamingEma(5),
            b => [StreamingRow.Last(new Ema(b.Close, 5).Compute())]);

        yield return new("Rsi", () => new StreamingRsi(5),
            b => [StreamingRow.Last(new Rsi(b.Close, 5).Compute())]);

        yield return new("BollingerBands", () => new StreamingBollinger(5, 2.0),
            b =>
            {
                var bands = new BollingerBands(b.Close, 5, 2.0).Compute();
                return [StreamingRow.Last(bands.Middle), StreamingRow.Last(bands.Upper), StreamingRow.Last(bands.Lower)];
            });

        yield return new("Macd", () => new StreamingMacd(3, 6, 4),
            b =>
            {
                var macd = new Macd(b.Close, 3, 6, 4).Compute();
                return [StreamingRow.Last(macd.MacdLine), StreamingRow.Last(macd.SignalLine), StreamingRow.Last(macd.Histogram)];
            });

        yield return new("Atr", () => new StreamingAtr(5),
            b => [StreamingRow.Last(new Atr(OhlcvBars.Hlc(b.High, b.Low, b.Close), 5).Compute())]);

        yield return new("Stochastic", () => new StreamingStochastic(5, 3),
            b =>
            {
                var stochastic = new Stochastic(OhlcvBars.Hlc(b.High, b.Low, b.Close), 5, 3).Compute();
                return [StreamingRow.Last(stochastic.K), StreamingRow.Last(stochastic.D)];
            });

        yield return new("WilliamsR", () => new StreamingWilliamsR(5),
            b => [StreamingRow.Last(new WilliamsR(OhlcvBars.Hlc(b.High, b.Low, b.Close), 5).Compute())]);

        yield return new("Cci", () => new StreamingCci(5),
            b => [StreamingRow.Last(new Cci(OhlcvBars.Hlc(b.High, b.Low, b.Close), 5).Compute())]);

        yield return new("Obv", () => new StreamingObv(),
            b => [StreamingRow.Last(new Obv(b.Close, b.Volume).Compute())]);

        yield return new("Vwap", () => new StreamingVwap(),
            b => [StreamingRow.Last(new Vwap(OhlcvBars.Hlcv(b.High, b.Low, b.Close, b.Volume)).Compute())]);
    }
}
