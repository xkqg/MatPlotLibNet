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

        int d = FeatureCount;
        int outLen = BarCount - _window;
        var result = new double[outLen];
        var mu = new double[d];
        var cov = new double[d, d];
        var diff = new double[d];

        for (int w = 0; w < outLen; w++)
        {
            int start = w;
            int end = w + _window; // exclusive — current bar is `end`.

            // Feature-wise mean over [start, end).
            Array.Clear(mu, 0, d);
            for (int t = start; t < end; t++)
                for (int k = 0; k < d; k++) mu[k] += Features[t][k];
            for (int k = 0; k < d; k++) mu[k] /= _window;

            // Sample covariance (divide by N-1) over [start, end).
            for (int i = 0; i < d; i++)
                for (int j = 0; j < d; j++)
                    cov[i, j] = 0;
            for (int t = start; t < end; t++)
            {
                var row = Features[t];
                for (int i = 0; i < d; i++)
                {
                    double di = row[i] - mu[i];
                    for (int j = 0; j < d; j++)
                        cov[i, j] += di * (row[j] - mu[j]);
                }
            }
            double denom = _window - 1;
            for (int i = 0; i < d; i++)
                for (int j = 0; j < d; j++) cov[i, j] /= denom;

            // Inverse of (cov + λI) via LU.
            var inv = InvertRegularized(cov, _regularization);

            // diff = currentBar - μ
            var current = Features[end];
            for (int k = 0; k < d; k++) diff[k] = current[k] - mu[k];

            // TI = diffᵀ · inv · diff
            double ti = 0;
            for (int i = 0; i < d; i++)
            {
                double tmp = 0;
                for (int j = 0; j < d; j++) tmp += inv[i, j] * diff[j];
                ti += diff[i] * tmp;
            }
            result[w] = ti;
        }
        return result;
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
