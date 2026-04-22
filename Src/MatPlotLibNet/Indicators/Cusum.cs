// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

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
    private readonly double _threshold;
    private readonly double _drift;

    /// <summary>Creates a new CUSUM filter.</summary>
    /// <param name="prices">Close prices (all strictly positive).</param>
    /// <param name="threshold">Break-detection threshold <c>h</c> (must be &gt; 0).</param>
    /// <param name="drift">Drift-control offset <c>θ</c> (default 0).</param>
    /// <exception cref="ArgumentException">Thrown on non-positive prices or non-positive threshold.</exception>
    public Cusum(double[] prices, double threshold, double drift = 0.0) : base(prices)
    {
        if (threshold <= 0)
            throw new ArgumentException($"CUSUM threshold must be > 0 (got {threshold}).", nameof(threshold));
        for (int i = 0; i < prices.Length; i++)
        {
            if (prices[i] <= 0)
                throw new ArgumentException(
                    $"CUSUM requires strictly positive prices; bar {i} has {prices[i]}.", nameof(prices));
        }
        _threshold = threshold;
        _drift = drift;
        Label = $"CUSUM(h={threshold:0.##})";
    }

    /// <inheritdoc />
    public override CusumResult Compute()
    {
        int n = Prices.Length;
        if (n < 2)
            return new CusumResult([], [], []);

        int outLen = n - 1;
        var signal = new double[outLen];
        var sPos = new double[outLen];
        var sNeg = new double[outLen];

        double accPos = 0, accNeg = 0;
        for (int i = 0; i < outLen; i++)
        {
            double y = Math.Log(Prices[i + 1] / Prices[i]);
            accPos = Math.Max(0, accPos + y - _drift);
            accNeg = Math.Min(0, accNeg + y + _drift);

            if (accPos > _threshold)
            {
                signal[i] = 1;
                accPos = 0;
            }
            else if (accNeg < -_threshold)
            {
                signal[i] = -1;
                accNeg = 0;
            }

            sPos[i] = accPos;
            sNeg[i] = accNeg;
        }

        return new CusumResult(signal, sPos, sNeg);
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        var result = Compute();
        PlotSignal(axes, result.Signal, warmup: 1);
    }
}
