// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>CUSUM filter — classic statistical-process-control applied to financial log-returns.
/// Detects structural breaks when the cumulative drift exceeds a threshold. O(1) per bar,
/// zero lookahead.</summary>
/// <remarks>For each bar, given <c>y_t = ln(p_t / p_{t-1})</c>:
/// <c>S⁺_t = max(0, S⁺_{t-1} + y_t − θ)</c> and
/// <c>S⁻_t = min(0, S⁻_{t-1} + y_t + θ)</c>.
/// Emits +1 when <c>S⁺ &gt; h</c> (and resets), −1 when <c>S⁻ &lt; −h</c> (and resets), else 0.
/// Reference: Page (1954) <i>Continuous Inspection Schemes</i>; Lopez de Prado (2018)
/// <i>Advances in Financial Machine Learning</i> §2.</remarks>
public sealed class Cusum : PriceIndicator<CusumResult>
{
    /// <summary>The <c>drift</c> the indicator is made with when the caller gives none.</summary>
    public const double DefaultDrift = 0.0;

    /// <summary>The name the checks of its prices give it, in the batch and the streaming form.</summary>
    internal const string CheckName = "CUSUM";

    private readonly double _threshold;
    private readonly double _drift;

    /// <summary>Creates a new CUSUM filter.</summary>
    /// <param name="prices">Close prices (all strictly positive).</param>
    /// <param name="threshold">Break-detection threshold <c>h</c> (must be &gt; 0).</param>
    /// <param name="drift">Drift-control offset <c>θ</c> (default 0).</param>
    /// <exception cref="ArgumentException">Thrown on non-positive prices or non-positive threshold.</exception>
    public Cusum(double[] prices, double threshold, double drift = DefaultDrift) : base(prices, Spec(threshold))
    {
        prices.ThrowIfNotPositivePrices(CheckName, "prices", nameof(prices));
        _threshold = threshold;
        _drift = drift;
    }

    internal static IndicatorSpec Spec(double threshold)
    {
        threshold.ThrowIfNotAbove(0, nameof(threshold));
        string label = $"CUSUM(h={threshold.InvariantText("0.##")})";
        return IndicatorSpec.Of(1, label, label, $"{label} S+", $"{label} S-");
    }

    /// <inheritdoc />
    public override CusumResult Compute() => FoldPrices<CusumStep, CusumPoint, Lines>(new(_threshold, _drift), new(ValueCount)).Result;

    /// <summary>The signal and the two sums, one entry per bar from the first value on.</summary>
    private readonly struct Lines(int count) : IOutputLines<CusumPoint>
    {
        private readonly double[] _signal = new double[count], _positive = new double[count], _negative = new double[count];

        internal CusumResult Result => new(_signal, _positive, _negative);

        public void Set(int index, CusumPoint output)
        {
            _signal[index] = output.Signal;
            _positive[index] = output.SPos;
            _negative[index] = output.SNeg;
        }
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        var result = Compute();
        PlotLine(axes, result.Signal);
    }
}
