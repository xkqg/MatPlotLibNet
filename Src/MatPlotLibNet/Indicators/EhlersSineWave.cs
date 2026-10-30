// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Ehlers;
using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators;

/// <summary>Ehlers' Sinewave indicator — extracts the dominant-cycle phase via the shared
/// <see cref="HilbertDiscriminator"/> and emits <c>sin(phase)</c> + a 45°-lead companion
/// plus a per-bar cycle/trend flag. Reference: Ehlers (2002),
/// <i>Mesa and Trading Market Cycles</i>, Ch. 9.</summary>
public sealed class EhlersSineWave : PriceIndicator<SineWaveResult>
{
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

        int outLen = n - 6;
        var sineWave = new double[outLen];
        var leadSine = new double[outLen];
        var isCyclic = new bool[outLen];
        var step = new EhlersSineWaveStep();
        for (int i = 0; i < n; i++)
        {
            var point = step.Next(Prices[i]);
            if (i < 6) continue;
            sineWave[i - 6] = point.SineWave;
            leadSine[i - 6] = point.LeadSine;
            isCyclic[i - 6] = point.IsCyclic;
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
