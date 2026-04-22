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

    /// <summary>Number of features per bar (columns).</summary>
    protected int FeatureCount { get; }

    /// <summary>Creates a multivariate indicator. Accepts null or empty arrays as
    /// empty state (no throw) so downstream <c>Compute()</c> can return empty output
    /// without constructor gymnastics.</summary>
    /// <exception cref="ArgumentException">Thrown when rows have differing lengths.</exception>
    protected MultivariateIndicator(double[][] features)
    {
        if (features is null || features.Length == 0)
        {
            Features = [];
            BarCount = 0;
            FeatureCount = 0;
            return;
        }

        Features = features;
        BarCount = features.Length;
        FeatureCount = features[0].Length;
        for (int t = 1; t < BarCount; t++)
        {
            if (features[t].Length != FeatureCount)
                throw new ArgumentException(
                    $"All feature rows must have the same length; row 0 has {FeatureCount}, " +
                    $"row {t} has {features[t].Length}.", nameof(features));
        }
    }
}
