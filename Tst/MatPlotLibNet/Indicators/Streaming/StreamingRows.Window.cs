// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Indicators.Streaming;

namespace MatPlotLibNet.Tests.Indicators.Streaming;

/// <summary>The rows of the equality theory for the windowed streaming forms: the batch indicator over the last bars the form
/// keeps.</summary>
public static partial class StreamingRows
{
    private static IEnumerable<StreamingRow> Windowed()
    {
        yield return new("CgOscillator", () => new StreamingCgOscillator(5),
            b => [StreamingRow.Last(new CgOscillator(b.Close, 5).Compute())]);

        yield return new("FractionalDifferentiation", () => new StreamingFractionalDifferentiation(0.4, 1e-3),
            b => [StreamingRow.Last(new FractionalDifferentiation(b.Close, 0.4, 1e-3).Compute())]);

        yield return new("InverseFisherTransform", () => new StreamingInverseFisherTransform(0.05),
            b => [StreamingRow.Last(new InverseFisherTransform(b.Close, 0.05).Compute())]);

        yield return new("KaufmanEfficiencyRatio", () => new StreamingKaufmanEfficiencyRatio(5),
            b => [StreamingRow.Last(new KaufmanEfficiencyRatio(b.Close, 5).Compute())]);

        yield return new("PermutationEntropy", () => new StreamingPermutationEntropy(3, 8),
            b => [StreamingRow.Last(new PermutationEntropy(b.Close, 3, 8).Compute())]);

        yield return new("ProfitLoss", () => new StreamingProfitLoss(),
            b => [StreamingRow.Last(new ProfitLoss(b.Close).Compute())]);

        yield return new("Roc", () => new StreamingRoc(5),
            b => [StreamingRow.Last(new Roc(b.Close, 5).Compute())]);

        yield return new("RollSpread", () => new StreamingRollSpread(5),
            b => [StreamingRow.Last(new RollSpread(b.Close, 5).Compute())]);

        yield return new("WaveletEnergyRatio", () => new StreamingWaveletEnergyRatio(8, 0),
            b => [StreamingRow.Last(new WaveletEnergyRatio(b.Close, 8, 0).Compute())]);

        yield return new("WaveletEntropy", () => new StreamingWaveletEntropy(8),
            b => [StreamingRow.Last(new WaveletEntropy(b.Close, 8).Compute())]);

        yield return new("VolumeIndicator", () => new StreamingVolumeIndicator(),
            b => [StreamingRow.Last(new VolumeIndicator(b.Volume).Compute())]);

        yield return new("AmihudIlliquidity", () => new StreamingAmihudIlliquidity(5),
            b => [StreamingRow.Last(new AmihudIlliquidity(OhlcvBars.Cv(b.Close, b.Volume), 5).Compute())]);

        yield return new("AroonOscillator", () => new StreamingAroonOscillator(5),
            b => [StreamingRow.Last(new AroonOscillator(OhlcvBars.Hl(b.High, b.Low), 5).Compute())]);

        yield return new("CorwinSchultz", () => new StreamingCorwinSchultz(5),
            b => [StreamingRow.Last(new CorwinSchultz(OhlcvBars.Hl(b.High, b.Low), 5).Compute())]);

        yield return new("EaseOfMovement", () => new StreamingEaseOfMovement(5),
            b => [StreamingRow.Last(new EaseOfMovement(OhlcvBars.Hlv(b.High, b.Low, b.Volume), 5).Compute())]);

        yield return new("GarmanKlass", () => new StreamingGarmanKlass(5),
            b => [StreamingRow.Last(new GarmanKlass(OhlcvBars.Ohlc(b.Open, b.High, b.Low, b.Close), 5).Compute())]);

        yield return new("VwapZScore", () => new StreamingVwapZScore(5),
            b => [StreamingRow.Last(new VwapZScore(OhlcvBars.Cv(b.Close, b.Volume), 5).Compute())]);

        yield return new("Vpin", () => new StreamingVpin(4, 4),
            b => [StreamingRow.Last(new Vpin(OhlcvBars.Cv(b.Close, b.Volume), 4, 4).Compute())]);

        yield return new("YangZhang", () => new StreamingYangZhang(5),
            b => [StreamingRow.Last(new YangZhang(OhlcvBars.Ohlc(b.Open, b.High, b.Low, b.Close), 5).Compute())]);

        yield return new("YangZhangVolRatio", () => new StreamingYangZhangVolRatio(3, 6),
            b => [StreamingRow.Last(new YangZhangVolRatio(OhlcvBars.Ohlc(b.Open, b.High, b.Low, b.Close), 3, 6).Compute())]);

        yield return new("RelativeVigorIndex", () => new StreamingRelativeVigorIndex(4),
            b =>
            {
                var r = new RelativeVigorIndex(OhlcvBars.Ohlc(b.Open, b.High, b.Low, b.Close), 4).Compute();
                return [StreamingRow.Last(r.Rvi), StreamingRow.Last(r.Signal)];
            });

        yield return new("SqueezeMomentum", () => new StreamingSqueezeMomentum(5, 2.0, 1.5),
            b =>
            {
                var r = new SqueezeMomentum(OhlcvBars.Hlc(b.High, b.Low, b.Close), 5, 2.0, 1.5).Compute();
                return [StreamingRow.Last(r.Momentum), r.SqueezeOn.Length > 0 ? (r.SqueezeOn[^1] ? 1 : 0) : double.NaN];
            });

        yield return new("Ichimoku", () => new StreamingIchimoku(3, 6, 12),
            b =>
            {
                var r = new Ichimoku(OhlcvBars.Hlc(b.High, b.Low, b.Close), 3, 6, 12, 6).Compute();
                return [StreamingRow.Last(r.TenkanSen), StreamingRow.Last(r.KijunSen), StreamingRow.Last(r.SenkouSpanA), StreamingRow.Last(r.SenkouSpanB), StreamingRow.Last(r.ChikouSpan)];
            });
    }
}
