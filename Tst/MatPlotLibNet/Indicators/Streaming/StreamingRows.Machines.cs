// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Indicators.Streaming;

namespace MatPlotLibNet.Tests.Indicators.Streaming;

/// <summary>The rows of the equality theory for the streaming forms of the state machines, the running totals and the indicators over a row of several series: the batch indicator over every prefix.</summary>
public static partial class StreamingRows
{
    private static IEnumerable<StreamingRow> Machines()
    {
        yield return new("Bocpd", () => new StreamingBocpd(0.05, 4.0, 30),
            b => [StreamingRow.Last(new Bocpd(b.Close, 0.05, 4.0, 30).Compute())]);

        yield return new("Cusum", () => new StreamingCusum(0.02),
            b =>
            {
                var r = new Cusum(b.Close, 0.02).Compute();
                return [StreamingRow.Last(r.Signal), StreamingRow.Last(r.SPos), StreamingRow.Last(r.SNeg)];
            });

        yield return new("DrawDown", () => new StreamingDrawDown(),
            b => [StreamingRow.Last(new DrawDown(b.Close).Compute())]);

        yield return new("EquityCurve", () => new StreamingEquityCurve(10000),
            b => [StreamingRow.Last(new EquityCurve(b.Close, 10000).Compute())]);

        yield return new("KlingerVolumeOscillator", () => new StreamingKlingerVolumeOscillator(3, 6, 4),
            b =>
            {
                var r = new KlingerVolumeOscillator(OhlcvBars.Hlcv(b.High, b.Low, b.Close, b.Volume), 3, 6, 4).Compute();
                return [StreamingRow.Last(r.Kvo), StreamingRow.Last(r.Signal)];
            });

        yield return new("ParabolicSar", () => new StreamingParabolicSar(0.02, 0.2),
            b =>
            {
                var r = new ParabolicSar(b.High, b.Low, 0.02, 0.2).Compute();
                return [StreamingRow.Last(r.Sar), r.IsLong.Length > 0 ? (r.IsLong[^1] ? 1 : 0) : double.NaN];
            });

        yield return new("Supertrend", () => new StreamingSupertrend(5, 3.0),
            b =>
            {
                var r = new Supertrend(OhlcvBars.Hlc(b.High, b.Low, b.Close), 5, 3.0).Compute();
                return [StreamingRow.Last(r.Line), r.Direction.Length > 0 ? r.Direction[^1] : double.NaN, r.Flipped.Length > 0 ? (r.Flipped[^1] ? 1 : 0) : double.NaN];
            });

        yield return new("DispersionIndex", () => new StreamingDispersionIndex(),
            b => [StreamingRow.Last(new DispersionIndex(StreamingRows.FeatureRows(b)).Compute())],
            Feed: (form, bars, i) => ((StreamingIndicator<double[]>)form).Append(StreamingRows.FeatureRow(bars, i)));

        yield return new("TurbulenceIndex", () => new StreamingTurbulenceIndex(10, 1e-6),
            b => [StreamingRow.Last(new TurbulenceIndex(StreamingRows.FeatureRows(b), 10, 1e-6).Compute())],
            Feed: (form, bars, i) => ((StreamingIndicator<double[]>)form).Append(StreamingRows.FeatureRow(bars, i)));
    }
}
