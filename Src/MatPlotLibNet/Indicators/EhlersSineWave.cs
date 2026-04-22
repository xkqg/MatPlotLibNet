// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Ehlers;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators;

/// <summary>Ehlers' Sinewave indicator — extracts the dominant-cycle phase via the shared
/// <see cref="HilbertDiscriminator"/> and emits <c>sin(phase)</c> + a 45°-lead companion
/// plus a per-bar cycle/trend flag. Reference: Ehlers (2002),
/// <i>Mesa and Trading Market Cycles</i>, Ch. 9.</summary>
public sealed class EhlersSineWave : PriceIndicator<SineWaveResult>
{
    // Phase-rate tolerance (degrees) — Ehlers-standard heuristic for flagging the
    // cyclic vs trend regime: if |Δphase − 360/period| exceeds the tolerance, treat
    // the bar as trend-mode.
    private const double CyclicToleranceDegrees = 50.0;

    /// <summary>Creates a new Ehlers Sinewave indicator.</summary>
    public EhlersSineWave(double[] prices) : base(prices)
    {
        Label = "SineWave";
    }

    /// <inheritdoc />
    public override SineWaveResult Compute()
    {
        int n = Prices.Length;
        if (n < 7)
            return new SineWaveResult(
                Array.Empty<double>(), Array.Empty<double>(), Array.Empty<bool>());

        var hilbert = ((ReadOnlySpan<double>)Prices).HilbertDiscriminate();

        int outLen = n - 6;
        var sineWave = new double[outLen];
        var leadSine = new double[outLen];
        var isCyclic = new bool[outLen];

        double fortyFiveRad = 45.0 * Math.PI / 180.0;

        for (int w = 0; w < outLen; w++)
        {
            int i = 6 + w;
            double phaseRad = hilbert.Phase[i] * Math.PI / 180.0;
            sineWave[w] = Math.Sin(phaseRad);
            leadSine[w] = Math.Sin(phaseRad + fortyFiveRad);

            // Phase change per bar vs expected 360°/period → in-cycle if close.
            // HilbertDiscriminator's [6, 50] absolute clamp + EMA guarantees Period[i] ≥ 1.2
            // for all i ≥ 6, so no divide-by-zero guard is needed here.
            double expectedRate = 360.0 / hilbert.Period[i];
            double actualRate = Math.Abs(hilbert.Phase[i] - hilbert.Phase[i - 1]);
            isCyclic[w] = Math.Abs(actualRate - expectedRate) < CyclicToleranceDegrees;
        }

        return new SineWaveResult(sineWave, leadSine, isCyclic);
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        var result = Compute();
        PlotSignal(axes, result.SineWave, warmup: 6, label: "SineWave");
        PlotSignal(axes, result.LeadSine, warmup: 6, label: "LeadSine", color: Colors.Tab10Orange);
        axes.YAxis.Min = -1.2;
        axes.YAxis.Max = 1.2;
    }
}
