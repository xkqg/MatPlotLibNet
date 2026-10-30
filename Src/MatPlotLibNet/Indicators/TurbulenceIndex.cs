// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Kritzman &amp; Li's Turbulence Index — rolling Mahalanobis distance of the current
/// feature vector from the historical multivariate mean. High values indicate anomalous
/// ("crisis-like") conditions. Reference: Kritzman &amp; Li (2010),
/// <i>Financial Analysts Journal</i> 66(5).</summary>
/// <remarks>Uses regularized sample covariance <c>Σ + λI</c> (default λ = 1e-6) to tolerate
/// near-collinear features. Feature-count cap is 10 — for wider feature sets, use dedicated
/// shrinkage estimators (Ledoit-Wolf, OAS) rather than pure LU inversion.</remarks>
public sealed class TurbulenceIndex : MultivariateIndicator<SignalResult>
{
    private const int MaxFeatures = 10;

    private readonly int _window;
    private readonly double _regularization;

    /// <summary>Creates a new Turbulence Index.</summary>
    /// <param name="features">Feature matrix — rows are time points, columns are features.</param>
    /// <param name="window">Rolling history window (default 252 trading days). Must be ≥ 2.</param>
    /// <param name="regularization">Ridge parameter <c>λ</c> added to the covariance diagonal. Default 1e-6.</param>
    public TurbulenceIndex(double[][] features, int window = 252, double regularization = 1e-6)
        : base(features)
    {
        if (window < 2)
            throw new ArgumentException($"window must be >= 2 (got {window}).", nameof(window));
        if (regularization < 0)
            throw new ArgumentException(
                $"regularization must be >= 0 (got {regularization}).", nameof(regularization));
        if (FeatureCount > MaxFeatures)
            throw new ArgumentException(
                $"TurbulenceIndex supports at most {MaxFeatures} features (got {FeatureCount}); " +
                "use a shrinkage estimator for wider feature sets.", nameof(features));
        _window = window;
        _regularization = regularization;
        Label = $"Turb({window})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        if (BarCount == 0 || BarCount <= _window)
            return Array.Empty<double>();

        int outLen = BarCount - _window;
        var result = new double[outLen];
        for (int w = 0; w < outLen; w++)
            result[w] = Score(Features, w, _window, _regularization);
        return result;
    }

    /// <summary>The turbulence of the row at <c>start + window</c> against the <c>window</c> rows before it: the squared Mahalanobis
    /// distance of the row from their mean, through the inverse of their sample covariance (regularised on the diagonal). The one
    /// place that arithmetic is written, read by <see cref="Compute"/> and by <see cref="Streaming.StreamingTurbulenceIndex"/>.</summary>
    /// <param name="features">The rows; at least <c>start + window + 1</c> of them, all of the same length.</param>
    /// <param name="start">The index of the oldest row of the window.</param>
    /// <param name="window">How many rows the mean and the covariance are taken over.</param>
    /// <param name="regularization">Added to the diagonal of the covariance before it is inverted.</param>
    internal static double Score(double[][] features, int start, int window, double regularization)
    {
        int d = features[0].Length;
        int end = start + window; // exclusive — the current bar is `end`.
        var mu = new double[d];
        var cov = new double[d, d];
        var diff = new double[d];

        // Feature-wise mean over [start, end).
        for (int t = start; t < end; t++)
            for (int k = 0; k < d; k++) mu[k] += features[t][k];
        for (int k = 0; k < d; k++) mu[k] /= window;

        // Sample covariance (divide by N-1) over [start, end).
        for (int t = start; t < end; t++)
        {
            var row = features[t];
            for (int i = 0; i < d; i++)
            {
                double di = row[i] - mu[i];
                for (int j = 0; j < d; j++)
                    cov[i, j] += di * (row[j] - mu[j]);
            }
        }

        double denom = window - 1;
        for (int i = 0; i < d; i++)
            for (int j = 0; j < d; j++) cov[i, j] /= denom;

        // Inverse of (cov + λI) via LU.
        var inv = InvertRegularized(cov, regularization);

        // diff = currentBar - μ
        var current = features[end];
        for (int k = 0; k < d; k++) diff[k] = current[k] - mu[k];

        // TI = diffᵀ · inv · diff
        double ti = 0;
        for (int i = 0; i < d; i++)
        {
            double tmp = 0;
            for (int j = 0; j < d; j++) tmp += inv[i, j] * diff[j];
            ti += diff[i] * tmp;
        }

        return ti;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), warmup: _window);

    /// <summary>Inverts <paramref name="m"/> + <paramref name="regularization"/>·I via
    /// LU with partial pivoting (delegated to <see cref="Linalg.Inv"/>).</summary>
    /// <remarks>Exposed <c>internal</c> for unit testing via <c>InternalsVisibleTo</c>.</remarks>
    internal static double[,] InvertRegularized(double[,] m, double regularization)
    {
        int n = m.GetLength(0);
        var regularized = new double[n, n];
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++) regularized[i, j] = m[i, j];
            regularized[i, i] += regularization;
        }
        return Linalg.Inv(new Mat(regularized)).Data;
    }
}
