// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Bandt &amp; Pompe's permutation entropy — Shannon entropy of the ordinal-pattern
/// distribution over a rolling window. Low entropy = predictable / trending;
/// high entropy = noise. Output is normalized to [0, 1]. Reference: Bandt &amp; Pompe (2002),
/// <i>Physical Review Letters</i> 88(17).</summary>
public sealed class PermutationEntropy : PriceIndicator<SignalResult>
{
    private const int MaxOrder = 7; // 7! = 5040 slots; beyond this, factorial growth is prohibitive.
    private readonly int _order;
    private readonly int _window;
    private readonly int[] _factorials;

    /// <summary>Creates a new Permutation Entropy indicator.</summary>
    /// <param name="prices">Price series.</param>
    /// <param name="order">Length of ordinal patterns (<c>d</c>). Must be in [2, 7].</param>
    /// <param name="window">Rolling window of patterns. Must be &gt; <paramref name="order"/>.</param>
    public PermutationEntropy(double[] prices, int order = 4, int window = 100) : base(prices)
    {
        if (order < 2 || order > MaxOrder)
            throw new ArgumentException($"order must be in [2, {MaxOrder}] (got {order}).", nameof(order));
        if (window <= order)
            throw new ArgumentException(
                $"window must be > order (got window={window}, order={order}).", nameof(window));
        _order = order;
        _window = window;
        _factorials = new int[order];
        _factorials[0] = 1;
        for (int i = 1; i < order; i++) _factorials[i] = _factorials[i - 1] * i;
        Label = $"PE(d={order},W={window})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = Prices.Length;
        if (n < _window) return Array.Empty<double>();

        int d = _order;
        int dFact = _factorials[d - 1] * d; // d!
        double logDFact = Math.Log(dFact);
        int outLen = n - _window + 1;
        var result = new double[outLen];

        var counts = new int[dFact];
        var indices = new int[d];

        for (int t = 0; t < outLen; t++)
        {
            Array.Clear(counts, 0, dFact);

            int subCount = _window - d + 1;
            for (int s = 0; s < subCount; s++)
            {
                int start = t + s;
                int idx = PermutationIndex(Prices, start, d, indices);
                counts[idx]++;
            }

            double h = 0;
            for (int k = 0; k < dFact; k++)
            {
                if (counts[k] > 0)
                {
                    double p = (double)counts[k] / subCount;
                    h -= p * Math.Log(p);
                }
            }
            result[t] = h / logDFact;
        }
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        PlotSignal(axes, Compute(), warmup: _window - 1);
        axes.YAxis.Min = 0;
        axes.YAxis.Max = 1;
    }

    /// <summary>Computes the Lehmer-code index of the ordinal permutation of <c>prices[start..start+d)</c>.
    /// Ties are broken by original index (stable sort convention) so constant inputs yield index 0.</summary>
    private int PermutationIndex(double[] prices, int start, int d, int[] indicesScratch)
    {
        for (int i = 0; i < d; i++) indicesScratch[i] = i;
        // Insertion sort (stable) by prices[start + index]. O(d²) but d ≤ 7.
        for (int i = 1; i < d; i++)
        {
            int curr = indicesScratch[i];
            double currVal = prices[start + curr];
            int j = i - 1;
            while (j >= 0 && prices[start + indicesScratch[j]] > currVal)
            {
                indicesScratch[j + 1] = indicesScratch[j];
                j--;
            }
            indicesScratch[j + 1] = curr;
        }

        // Lehmer encode: index = Σ code[i] · (d-1-i)!, where code[i] = #{ j > i : perm[j] < perm[i] }.
        int index = 0;
        for (int i = 0; i < d; i++)
        {
            int smaller = 0;
            int pi = indicesScratch[i];
            for (int j = i + 1; j < d; j++)
                if (indicesScratch[j] < pi) smaller++;
            index += smaller * _factorials[d - 1 - i];
        }
        return index;
    }
}
