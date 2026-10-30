// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Indicators.Streaming;

namespace MatPlotLibNet.Tests.Indicators.Streaming;

/// <summary>The rows of the equality theory for the streaming forms of the recursive filters and smoothers: the batch indicator over every prefix.</summary>
public static partial class StreamingRows
{
    private static IEnumerable<StreamingRow> Filters()
    {
        yield return new("AdaptiveStochastic", () => new StreamingAdaptiveStochastic(3),
            b => [StreamingRow.Last(new AdaptiveStochastic(OhlcvBars.Hlc(b.High, b.Low, b.Close), 3).Compute())]);

        yield return new("Adx", () => new StreamingAdx(4),
            b =>
            {
                var r = new Adx(OhlcvBars.Hlc(b.High, b.Low, b.Close), 4).ComputeFull();
                return [StreamingRow.Last(r.Adx), StreamingRow.Last(r.PlusDi), StreamingRow.Last(r.MinusDi)];
            });

        yield return new("CyberCycle", () => new StreamingCyberCycle(0.07),
            b => [StreamingRow.Last(new CyberCycle(b.Close, 0.07).Compute())]);

        yield return new("Decycler", () => new StreamingDecycler(10),
            b => [StreamingRow.Last(new Decycler(b.Close, 10).Compute())]);

        yield return new("EhlersITrend", () => new StreamingEhlersITrend(),
            b => [StreamingRow.Last(new EhlersITrend(b.Close).Compute())]);

        yield return new("EhlersSineWave", () => new StreamingEhlersSineWave(),
            b =>
            {
                var r = new EhlersSineWave(b.Close).Compute();
                return [StreamingRow.Last(r.SineWave), StreamingRow.Last(r.LeadSine), r.IsCyclic.Length > 0 ? (r.IsCyclic[^1] ? 1 : 0) : double.NaN];
            });

        yield return new("EhlersSuperSmoother", () => new StreamingEhlersSuperSmoother(5),
            b => [StreamingRow.Last(new EhlersSuperSmoother(b.Close, 5).Compute())]);

        yield return new("ForceIndex", () => new StreamingForceIndex(5),
            b => [StreamingRow.Last(new ForceIndex(OhlcvBars.Cv(b.Close, b.Volume), 5).Compute())]);

        yield return new("KeltnerChannels", () => new StreamingKeltnerChannels(5, 1.5),
            b =>
            {
                var r = new KeltnerChannels(OhlcvBars.Hlc(b.High, b.Low, b.Close), 5, 1.5).Compute();
                return [StreamingRow.Last(r.Middle), StreamingRow.Last(r.Upper), StreamingRow.Last(r.Lower)];
            });

        yield return new("LaguerreRsi", () => new StreamingLaguerreRsi(0.2),
            b => [StreamingRow.Last(new LaguerreRsi(b.Close, 0.2).Compute())]);

        yield return new("MamaFama", () => new StreamingMamaFama(0.5, 0.05),
            b =>
            {
                var r = new MamaFama(b.Close, 0.5, 0.05).Compute();
                return [StreamingRow.Last(r.Mama), StreamingRow.Last(r.Fama)];
            });

        yield return new("RoofingFilter", () => new StreamingRoofingFilter(12, 5),
            b => [StreamingRow.Last(new RoofingFilter(b.Close, 12, 5).Compute())]);

        yield return new("TwiggsMoneyFlow", () => new StreamingTwiggsMoneyFlow(5),
            b => [StreamingRow.Last(new TwiggsMoneyFlow(OhlcvBars.Hlcv(b.High, b.Low, b.Close, b.Volume), 5).Compute())]);
    }
}
