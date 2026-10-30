// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Schreiber's transfer entropy (2000) — information-theoretic measure of directional
/// influence from one time series to another. Unlike correlation (symmetric, linear),
/// transfer entropy captures asymmetric, nonlinear information flow. Uses equal-width
/// either the Kraskov nearest-neighbour estimate (the default of <see cref="TransferEntropyOptions"/>) or
/// equal-width histogram binning to estimate joint / marginal probabilities; output is a scalar in nats
/// (natural-log units). Reference: Schreiber, T. (2000), <i>Measuring Information Transfer</i>,
/// Physical Review Letters 85(2), 461. Applied to finance: Marschinski &amp; Kantz (2002),
/// Kwon &amp; Yang (2008); feature-importance framing: Lopez de Prado (2018), Ch. 8.</summary>
public sealed class TransferEntropy : Indicator<SignalResult>
{
    private readonly double[] _source;
    private readonly double[] _target;
    private readonly TransferEntropyOptions _options;

    /// <summary>Creates a new transfer-entropy indicator measuring <c>TE(source → target)</c> with the binned estimate. For the
    /// Kraskov estimate, a condition or a different neighbour count use the overload that takes <see cref="TransferEntropyOptions"/>.</summary>
    /// <param name="source">Source series X — the potentially-influencing series.</param>
    /// <param name="target">Target series Y — the potentially-influenced series. Must match
    /// <paramref name="source"/> in length.</param>
    /// <param name="bins">Histogram bin count per series. Default 8. Must be ≥ 2.</param>
    /// <param name="lag">Source lag τ: the triple uses <c>x_{t-τ+1}</c>. Default 1
    /// (<c>x_t</c> predicts <c>y_{t+1}</c>). Must be ≥ 1. Length must be ≥ <c>lag + 2</c>.</param>
    public TransferEntropy(double[] source, double[] target, int bins = 8, int lag = 1)
    {
        if (source is null) throw new ArgumentException("source is required.", nameof(source));
        if (target is null) throw new ArgumentException("target is required.", nameof(target));
        if (source.Length != target.Length)
            throw new ArgumentException(
                $"source ({source.Length}) and target ({target.Length}) must have equal length.");
        if (bins < 2)
            throw new ArgumentException($"bins must be >= 2 (got {bins}).", nameof(bins));
        if (lag < 1)
            throw new ArgumentException($"lag must be >= 1 (got {lag}).", nameof(lag));
        if (source.Length < lag + 2)
            throw new ArgumentException(
                $"length ({source.Length}) must be >= lag + 2 ({lag + 2}) to form triples.",
                nameof(source));
        _source = source;
        _target = target;
        _options = new TransferEntropyOptions(TransferEntropyEstimator.Binned, Bins: bins, Lag: lag);
        Label = $"TE(bins={bins},lag={lag})";
    }

    /// <summary>Creates a new transfer-entropy indicator measuring <c>TE(source → target)</c> as the options say.</summary>
    /// <param name="source">Source series X — the potentially-influencing series.</param>
    /// <param name="target">Target series Y — the potentially-influenced series. Must match <paramref name="source"/> in length.</param>
    /// <param name="options">The estimator and its settings.</param>
    /// <exception cref="ArgumentException">A series is missing or they differ in length; <see cref="TransferEntropyOptions.Neighbours"/>,
    /// <see cref="TransferEntropyOptions.Lag"/> or (binned) <see cref="TransferEntropyOptions.Bins"/> is below its minimum; the
    /// <see cref="TransferEntropyOptions.Condition"/> differs in length or goes with the binned estimate; or the series are too short
    /// to form the samples the estimate needs.</exception>
    public TransferEntropy(double[] source, double[] target, TransferEntropyOptions options)
    {
        if (source is null) throw new ArgumentException("source is required.", nameof(source));
        if (target is null) throw new ArgumentException("target is required.", nameof(target));
        if (source.Length != target.Length)
            throw new ArgumentException(
                $"source ({source.Length}) and target ({target.Length}) must have equal length.");
        if (options.Lag < 1)
            throw new ArgumentException($"Lag must be >= 1 (got {options.Lag}).", nameof(options.Lag));
        if (options.Stride < 1)
            throw new ArgumentException($"Stride must be >= 1 (got {options.Stride}).", nameof(options.Stride));
        if (options.Stride > 1 && options.Window is null)
            throw new ArgumentException("A Stride goes with a Window: it says which windows are worked out.", nameof(options.Stride));

        bool kraskov = options.Estimator == TransferEntropyEstimator.Kraskov;
        if (options.Window is not null && !kraskov)
            throw new ArgumentException(
                "A Window goes with the Kraskov estimate only: the binned estimate is biased upward on a window.", nameof(options.Window));
        if (kraskov)
        {
            if (options.Neighbours < 1)
                throw new ArgumentException($"Neighbours must be >= 1 (got {options.Neighbours}).", nameof(options.Neighbours));
            if (options.Condition is not null && options.Condition.Length != source.Length)
                throw new ArgumentException(
                    $"Condition ({options.Condition.Length}) and source ({source.Length}) must have equal length.", nameof(options.Condition));
            int needed = options.Lag + options.Neighbours + 1;
            if (options.Window is not null)
            {
                if (options.Window < needed)
                    throw new ArgumentException(
                        $"Window ({options.Window}) must be >= lag + neighbours + 1 ({needed}) to have a neighbour for every sample.",
                        nameof(options.Window));
            }
            else if (source.Length < needed)
            {
                throw new ArgumentException(
                    $"length ({source.Length}) must be >= lag + neighbours + 1 ({needed}) to have a neighbour for every sample.",
                    nameof(source));
            }
        }
        else
        {
            if (options.Bins < 2)
                throw new ArgumentException($"Bins must be >= 2 (got {options.Bins}).", nameof(options.Bins));
            if (options.Condition is not null)
                throw new ArgumentException("A Condition goes with the Kraskov estimate only.", nameof(options.Condition));
            if (source.Length < options.Lag + 2)
                throw new ArgumentException(
                    $"length ({source.Length}) must be >= lag + 2 ({options.Lag + 2}) to form triples.",
                    nameof(source));
        }

        _source = source;
        _target = target;
        _options = options;
        Label = LabelOf(options);
    }

    /// <summary>The label of an indicator with the given options (shared with the streaming form).</summary>
    internal static string LabelOf(TransferEntropyOptions options) =>
        options.Estimator == TransferEntropyEstimator.Kraskov
            ? options.Window is null
                ? $"TE(k={options.Neighbours},lag={options.Lag})"
                : $"TE(k={options.Neighbours},lag={options.Lag},W={options.Window})"
            : $"TE(bins={options.Bins},lag={options.Lag})";

    /// <inheritdoc />
    /// <remarks>One value for the whole series; with a <see cref="TransferEntropyOptions.Window"/>, one for each bar: the entropy of the
    /// last <c>Window</c> bars (stamped at the bar the window ends at, so it never depends on a later bar), NaN until the first window
    /// is full and for the bars between the windows a <see cref="TransferEntropyOptions.Stride"/> leaves out.</remarks>
    public override SignalResult Compute()
    {
        if (_options.Window is not int window)
            return new double[] { ComputeScalar() };

        int n = _source.Length;
        var result = new double[n];
        Array.Fill(result, double.NaN);
        for (int end = window - 1; end < n; end += _options.Stride)
        {
            int start = end - window + 1;
            result[end] = EstimateWindow(_options, _source, _target, _options.Condition, start, window, absoluteStart: start);
        }

        return result;
    }

    /// <summary>Computes the scalar transfer-entropy value in nats.</summary>
    internal double ComputeScalar() =>
        _options.Estimator == TransferEntropyEstimator.Kraskov ? ComputeKraskov() : ComputeBinned();

    /// <summary>The Kraskov estimate: the conditional mutual information of the lagged source and the future of the target given the
    /// present of the target (and the condition), over the samples <c>t ∈ [lag-1, n-2]</c>. Every variable is standardised over the
    /// samples used, and a jitter of 1e-10 keyed by the index of the bar (the same bar gets the same jitter wherever it is read)
    /// breaks the ties of quantised data; a variable that never moves stays centred, with no jitter.</summary>
    private double ComputeKraskov() =>
        EstimateWindow(_options, _source, _target, _options.Condition, start: 0, _source.Length, absoluteStart: 0);

    /// <summary>The Kraskov estimate over <paramref name="length"/> bars of the series from <paramref name="start"/>. The bar at
    /// <c>start + i</c> is the bar at position <c>absoluteStart + i</c> of the whole series, which is where its jitter comes from: the
    /// same bar reads the same in every window it is in, and in the streaming form, which keeps the window alone.</summary>
    /// <param name="options">The settings.</param>
    /// <param name="source">The source series.</param>
    /// <param name="target">The target series.</param>
    /// <param name="condition">The series conditioned on, or null.</param>
    /// <param name="start">The first bar of the window in these arrays.</param>
    /// <param name="length">How many bars the window holds.</param>
    /// <param name="absoluteStart">The position of that first bar in the whole series.</param>
    internal static double EstimateWindow(
        TransferEntropyOptions options, double[] source, double[] target, double[]? condition, int start, int length, int absoluteStart)
    {
        int lag = options.Lag;
        int count = length - lag;
        var future = Standardised(target, start + lag, count, TargetSalt, absoluteStart + lag);
        var present = Standardised(target, start + lag - 1, count, TargetSalt, absoluteStart + lag - 1);
        var lagged = Standardised(source, start, count, SourceSalt, absoluteStart);

        var given = new List<double[]> { present };
        if (condition is not null)
            given.Add(Standardised(condition, start, count, ConditionSalt, absoluteStart));

        return KraskovEstimator.ConditionalMutualInformation([lagged], [future], given, options.Neighbours);
    }

    private const int SourceSalt = 1;
    private const int TargetSalt = 2;
    private const int ConditionSalt = 3;
    private const double JitterScale = 1e-10;

    /// <summary>A column of <paramref name="count"/> values of a series from <paramref name="first"/>, centred and divided by their
    /// standard deviation (only centred when they are all alike), plus the jitter of the position of each in the whole series
    /// (<paramref name="absoluteFirst"/> is the position of the first).</summary>
    private static double[] Standardised(double[] series, int first, int count, int salt, int absoluteFirst)
    {
        double mean = 0;
        for (int i = 0; i < count; i++) mean += series[first + i];
        mean /= count;
        double sumSquares = 0;
        for (int i = 0; i < count; i++)
        {
            double d = series[first + i] - mean;
            sumSquares += d * d;
        }

        double deviation = Math.Sqrt(sumSquares / count);
        var column = new double[count];
        for (int i = 0; i < count; i++)
        {
            double centred = series[first + i] - mean;
            column[i] = deviation > 0 ? centred / deviation + JitterScale * Jitter(absoluteFirst + i, salt) : centred;
        }

        return column;
    }

    /// <summary>A number in [-0.5, 0.5) fixed by the index of a bar and the series it belongs to (a 64-bit integer mix, no random
    /// state and no libm), so the same bar reads the same wherever it is used.</summary>
    private static double Jitter(int index, int salt)
    {
        ulong z = unchecked((ulong)index * 0x9E3779B97F4A7C15UL + (ulong)salt * 0xBF58476D1CE4E5B9UL + 0x94D049BB133111EBUL);
        z = unchecked((z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL);
        z = unchecked((z ^ (z >> 27)) * 0x94D049BB133111EBUL);
        z ^= z >> 31;
        return (z >> 11) / (double)(1UL << 53) - 0.5;
    }

    /// <summary>The binned estimate, as it always was.</summary>
    private double ComputeBinned()
    {
        int n = _source.Length;
        int bins = _options.Bins;
        int lag = _options.Lag;
        var sBins = Discretize(_source, bins);
        var tBins = Discretize(_target, bins);

        // Triple (y_future, y_present, xlagged) at time t, t ∈ [lag-1, n-2]
        // y_future = Y[t+1], y_present = Y[t], xlagged = X[t - lag + 1].
        var joint = new int[bins, bins, bins];
        var yfp = new int[bins, bins];    // (y_future, y_present)
        var yxp = new int[bins, bins];    // (y_present, xlagged)
        var yp = new int[bins];            // y_present
        int total = 0;
        for (int t = lag - 1; t <= n - 2; t++)
        {
            int yf = tBins[t + 1];
            int ypCur = tBins[t];
            int xl = sBins[t - lag + 1];
            joint[yf, ypCur, xl]++;
            yfp[yf, ypCur]++;
            yxp[ypCur, xl]++;
            yp[ypCur]++;
            total++;
        }

        if (total == 0) return 0.0;
        double invTotal = 1.0 / total;
        double te = 0;
        for (int a = 0; a < bins; a++)
        for (int b = 0; b < bins; b++)
        for (int c = 0; c < bins; c++)
        {
            int cnt = joint[a, b, c];
            if (cnt == 0) continue;
            double pj = cnt * invTotal;
            double pf = yfp[a, b] * invTotal;
            double pc = yxp[b, c] * invTotal;
            double pb = yp[b] * invTotal;
            if (pf <= 0 || pc <= 0 || pb <= 0) continue;
            te += pj * Math.Log(pj * pb / (pf * pc));
        }
        return te;
    }

    /// <summary>Equal-width binning. If the series has zero range (all values equal) every
    /// sample maps to bin 0 — no information content, TE collapses to 0.</summary>
    private static int[] Discretize(double[] values, int bins)
    {
        int n = values.Length;
        var result = new int[n];
        double min = values[0], max = values[0];
        for (int i = 1; i < n; i++)
        {
            if (values[i] < min) min = values[i];
            if (values[i] > max) max = values[i];
        }
        double range = max - min;
        if (range == 0) return result; // all zeros — all samples in bin 0
        double invWidth = bins / range;
        for (int i = 0; i < n; i++)
        {
            int b = (int)((values[i] - min) * invWidth);
            if (b < 0) b = 0;
            if (b >= bins) b = bins - 1;
            result[i] = b;
        }
        return result;
    }

    /// <inheritdoc />
    /// <remarks>A rolling entropy (with a <see cref="TransferEntropyOptions.Window"/>) is drawn as a line through its values.
    /// The scalar is a number, not a per-bar series — callers typically take <see cref="ComputeScalar"/> and display it as a number or
    /// annotation — so <c>Apply</c> draws nothing for it, to keep the <see cref="IIndicator"/> contract without a misleading line.</remarks>
    public override void Apply(Axes axes)
    {
        if (_options.Window is null) return;

        // Through the windows that were worked out, at the bar each ends at: with a stride the bars between are not values, and a
        // line broken at every NaN would draw nothing at all.
        double[] all = Compute();
        double[] ends = [.. Enumerable.Range(0, all.Length).Where(i => !double.IsNaN(all[i])).Select(i => (double)i)];
        double[] values = [.. ends.Select(i => all[(int)i])];
        PlotSignalAt(axes, ApplyOffset(ends), values);
    }
}
