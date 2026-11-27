// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators;

/// <summary>Ehlers' Sinewave indicator — extracts the dominant-cycle phase via the shared
/// Hilbert transform and emits <c>sin(phase)</c> + a 45°-lead companion
/// plus a per-bar cycle/trend flag. Reference: Ehlers (2002),
/// <i>Mesa and Trading Market Cycles</i>, Ch. 9.</summary>
public sealed class EhlersSineWave : PriceIndicator<SineWaveResult>
{

    /// <summary>Creates a new Ehlers Sinewave indicator.</summary>
    public EhlersSineWave(double[] prices) : base(prices, Spec())
    {
    }

    internal static IndicatorSpec Spec()
    {
        string label = "SineWave";
        return IndicatorSpec.Of(6, label, label, "LeadSine", "Cyclic");
    }

    /// <inheritdoc />
    public override SineWaveResult Compute() =>
        FoldPrices<EhlersSineWaveStep, SineWavePoint, Lines>(new EhlersSineWaveStep(), new(ValueCount)).Result;

    /// <summary>The sine, the lead sine and whether the market cycles, one entry per bar from the first value on.</summary>
    private readonly struct Lines(int count) : IOutputLines<SineWavePoint>
    {
        private readonly double[] _sineWave = new double[count], _leadSine = new double[count];
        private readonly bool[] _isCyclic = new bool[count];

        internal SineWaveResult Result => new(_sineWave, _leadSine, _isCyclic);

        public void Set(int index, SineWavePoint output)
        {
            _sineWave[index] = output.SineWave;
            _leadSine[index] = output.LeadSine;
            _isCyclic[index] = output.IsCyclic;
        }
    }

    /// <summary>The range the values of the indicator live in: its pane, and the pane of <see cref="Streaming.StreamingEhlersSineWave"/>, are pinned to it.</summary>
    internal static PaneRange Pane { get; } = new(-1.2, 1.2);

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        var result = Compute();
        PlotLine(axes, result.SineWave);
        PlotLine(axes, result.LeadSine, LineLabel(1), null, IndicatorLook.Library.Lines.Second);
        Pane.Pin(axes);
    }
}
