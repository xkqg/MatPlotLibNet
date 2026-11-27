// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators;

/// <summary>Base class for indicators that operate on a multivariate feature matrix — one
/// feature per column, one time point per row. Validates rectangular shape and exposes
/// dimensions to subclasses.</summary>
/// <typeparam name="TResult">The typed computation result.</typeparam>
public abstract class MultivariateIndicator<TResult> : Indicator<TResult>
    where TResult : IIndicatorResult
{
    /// <summary>Feature matrix, indexed as <c>[time][feature]</c>.</summary>
    protected double[][] Features { get; }

    /// <summary>Number of time points (rows).</summary>
    protected int BarCount { get; }

    /// <inheritdoc />
    private protected override int BarsIn => BarCount;

    /// <summary>Number of features per bar (columns).</summary>
    protected int FeatureCount { get; }

    /// <summary>Creates a multivariate indicator. Accepts null or empty arrays as
    /// empty state (no throw) so downstream <c>Compute()</c> can return empty output
    /// without constructor gymnastics.</summary>
    /// <exception cref="ArgumentException">Thrown when rows have differing lengths.</exception>
    protected MultivariateIndicator(double[][] features) : this(features, IndicatorSpec.None) { }

    /// <summary>An indicator of the library over a feature matrix, named and placed by its spec.</summary>
    /// <param name="features">The feature matrix.</param>
    /// <param name="spec">What the batch class's <c>Spec</c> factory said of the parameters.</param>
    /// <exception cref="ArgumentException">Thrown when rows have differing lengths.</exception>
    private protected MultivariateIndicator(double[][] features, IndicatorSpec spec) : base(spec)
    {
        FeatureCount = FeatureCountOf(features);
        if (features is null || features.Length == 0)
        {
            Features = [];
            BarCount = 0;
            return;
        }

        Features = features;
        BarCount = features.Length;
        features.Select(row => row.Length).ThrowIfNotAllEqual(nameof(features));
    }

    /// <summary>How many features a bar of the matrix has: the length of its first row, 0 for no matrix or an empty one.</summary>
    /// <param name="features">The feature matrix.</param>
    /// <returns>The count.</returns>
    private protected static int FeatureCountOf(double[][]? features) => features is { Length: > 0 } ? features[0].Length : 0;
}
