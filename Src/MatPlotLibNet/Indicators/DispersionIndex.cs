// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Regime-uncertainty meta-indicator: per-bar population standard deviation across
/// a set of regime signals. Low values = agreement; high = disagreement. Useful as a
/// confidence gate in regime-aware strategies.</summary>
public sealed class DispersionIndex : MultivariateIndicator<SignalResult>
{
    /// <summary>Creates a new Dispersion Index.</summary>
    /// <param name="signals">Signal matrix — rows are time points, columns are signals.</param>
    /// <exception cref="ArgumentException">Thrown when fewer than 2 signals per row.</exception>
    public DispersionIndex(double[][] signals) : base(signals)
    {
        if (BarCount > 0 && FeatureCount < 2)
            throw new ArgumentException(
                $"DispersionIndex requires at least 2 signals per row (got {FeatureCount}).",
                nameof(signals));
        Label = $"Dispersion({FeatureCount})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        if (BarCount == 0) return Array.Empty<double>();

        var result = new double[BarCount];
        for (int t = 0; t < BarCount; t++)
        {
            var row = Features[t];
            double sum = 0;
            for (int k = 0; k < FeatureCount; k++) sum += row[k];
            double mean = sum / FeatureCount;

            double m2 = 0;
            for (int k = 0; k < FeatureCount; k++)
            {
                double d = row[k] - mean;
                m2 += d * d;
            }
            result[t] = Math.Sqrt(m2 / FeatureCount);
        }
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), warmup: 0);
}
