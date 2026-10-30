// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Steps;

/// <summary>One step of Bayesian online change-point detection: the recursion that the batch <see cref="Bocpd"/> folds over its
/// prices and <see cref="Streaming.StreamingBocpd"/> takes one price at a time. For every run length (the number of prices since the
/// last change) it keeps the probability, the count, the mean and the sum of squared deviations of the prices in the run. A new
/// price is scored under each run by a normal predictive, the mass moves one run longer (or to the longest run) or falls back to
/// run length 0 by the hazard, and the probabilities are normalised; the output is the probability of run length 0, that is, of a
/// change at this price. The first price only starts the first run, so the step says NaN for it. The state is as large as the longest run length.</summary>
internal struct BocpdStep
{
    private static readonly double Sqrt2Pi = Math.Sqrt(2 * Math.PI);

    private readonly double _hazard;
    private readonly double _priorVariance;
    private readonly int _maxRun;
    private readonly double[] _probability;
    private readonly int[] _count;
    private readonly double[] _mean;
    private readonly double[] _m2;
    private readonly double[] _next;
    private int _seen;

    /// <summary>A step with the given settings.</summary>
    /// <param name="hazard">The prior probability of a change at any price, in (0, 1).</param>
    /// <param name="priorVariance">The variance added to every run's own, above 0.</param>
    /// <param name="maxRunLength">The longest run length kept; longer runs are merged into it.</param>
    internal BocpdStep(double hazard, double priorVariance, int maxRunLength)
    {
        _hazard = hazard;
        _priorVariance = priorVariance;
        _maxRun = maxRunLength;
        int size = maxRunLength + 1;
        _probability = new double[size];
        _count = new int[size];
        _mean = new double[size];
        _m2 = new double[size];
        _next = new double[size];
    }

    /// <summary>Takes one price and returns the probability that a change happened at it, NaN for the first price.</summary>
    /// <param name="price">The price.</param>
    internal double Next(double price)
    {
        if (_seen++ == 0)
        {
            _probability[0] = 1.0;
            _count[0] = 1;
            _mean[0] = price;
            return double.NaN;
        }

        int size = _maxRun + 1;
        double xt = price;
        Array.Clear(_next, 0, size);

        for (int r = 0; r < size; r++)
        {
            if (_count[r] == 0) continue;
            double variance = (_count[r] > 1 ? _m2[r] / (_count[r] - 1) : 0.0) + _priorVariance;
            double diff = xt - _mean[r];
            double pred = Math.Exp(-0.5 * diff * diff / variance) / (Sqrt2Pi * Math.Sqrt(variance));
            double weight = _probability[r] * pred;

            if (r < _maxRun)
                _next[r + 1] += weight * (1 - _hazard);
            else
                _next[r] += weight * (1 - _hazard);

            _next[0] += weight * _hazard;
        }

        double sum = 0;
        for (int r = 0; r < size; r++) sum += _next[r];
        if (sum > 0)
        {
            for (int r = 0; r < size; r++) _next[r] /= sum;
        }
        else
        {
            Array.Clear(_next, 0, size);
            _next[0] = 1.0;
        }

        for (int r = _maxRun - 1; r >= 0; r--)
        {
            if (_count[r] == 0)
            {
                _count[r + 1] = 0;
                _mean[r + 1] = 0;
                _m2[r + 1] = 0;
                continue;
            }

            int newCount = _count[r] + 1;
            double delta = xt - _mean[r];
            double newMean = _mean[r] + delta / newCount;
            double delta2 = xt - newMean;
            _count[r + 1] = newCount;
            _mean[r + 1] = newMean;
            _m2[r + 1] = _m2[r] + delta * delta2;
        }

        _count[0] = 1;
        _mean[0] = xt;
        _m2[0] = 0;

        Array.Copy(_next, _probability, size);
        return _next[0];
    }
}
