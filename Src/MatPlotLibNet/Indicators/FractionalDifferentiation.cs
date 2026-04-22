// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Lopez de Prado's fixed-width fractional differentiation — produces a stationary
/// series from non-stationary prices while preserving long-memory information (unlike integer
/// differencing, which destroys it). Foundational for ML on financial time series.</summary>
/// <remarks>Weight recurrence: <c>w_0 = 1</c>, <c>w_k = w_{k-1} · (k − 1 − d) / k</c> for k ≥ 1.
/// Truncated at the first <c>|w_k| &lt; tolerance</c>. Output length is
/// <c>prices.Length − weights.Length + 1</c>. Reference: Lopez de Prado (2018),
/// <i>Advances in Financial Machine Learning</i>, §5.5.</remarks>
public sealed class FractionalDifferentiation : PriceIndicator<SignalResult>
{
    // Hard cap on weight-series length to prevent unbounded loops for pathological
    // (d, tolerance) combinations. 100_000 weights covers every practical configuration;
    // beyond that, switch to FFT-based convolution (TODO for future optimisation).
    internal const int MaxWeights = 100_000;

    private readonly double _d;
    private readonly double _tolerance;
    private readonly double[] _weights;

    /// <summary>Creates a new fractional-differentiation indicator.</summary>
    /// <param name="prices">Input price series.</param>
    /// <param name="d">Fractional order, must be strictly between 0 and 1.</param>
    /// <param name="tolerance">Truncation tolerance (stop when <c>|w_k| &lt; tolerance</c>).</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="d"/> ∉ (0,1) or
    /// <paramref name="tolerance"/> ≤ 0.</exception>
    public FractionalDifferentiation(double[] prices, double d = 0.4, double tolerance = 1e-3)
        : base(prices)
    {
        _d = d;
        _tolerance = tolerance;
        _weights = ComputeWeights(d, tolerance);
        Label = $"FFD(d={d:0.00})";
    }

    /// <summary>Computes the truncated FFD weight series for order <paramref name="d"/>.</summary>
    /// <remarks>Exposed <c>internal</c> for unit testing via <c>InternalsVisibleTo</c>.</remarks>
    internal static double[] ComputeWeights(double d, double tolerance)
    {
        if (d <= 0 || d >= 1)
            throw new ArgumentException($"d must be in (0, 1); got {d}.", nameof(d));
        if (tolerance <= 0)
            throw new ArgumentException($"tolerance must be > 0; got {tolerance}.", nameof(tolerance));

        var buf = new List<double>(capacity: 64) { 1.0 };
        double w = 1.0;
        for (int k = 1; k < MaxWeights; k++)
        {
            // Recurrence: w_k = w_{k-1} * (k - 1 - d) / k
            w = w * (k - 1 - d) / k;
            if (Math.Abs(w) < tolerance) return buf.ToArray();
            buf.Add(w);
        }
        throw new InvalidOperationException(
            $"FFD weight series did not converge within {MaxWeights} weights (d={d}, tolerance={tolerance}).");
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = Prices.Length;
        int m = _weights.Length;
        if (n < m) return Array.Empty<double>();

        int outLen = n - m + 1;
        var result = new double[outLen];
        for (int t = 0; t < outLen; t++)
        {
            double sum = 0;
            // X_t = Σ w_k · p_{t+m-1-k} for k = 0..m-1
            for (int k = 0; k < m; k++)
                sum += _weights[k] * Prices[t + m - 1 - k];
            result[t] = sum;
        }
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), _weights.Length - 1);
}
