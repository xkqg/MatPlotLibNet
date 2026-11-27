// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Regime-uncertainty meta-indicator: per-bar population standard deviation across
/// a set of regime signals. Low values = agreement; high = disagreement. Useful as a
/// confidence gate in regime-aware strategies.</summary>
public sealed class DispersionIndex : MultivariateIndicator<SignalResult>
{
    /// <summary>Creates a new Dispersion Index.</summary>
    /// <param name="signals">Signal matrix — rows are time points, columns are signals.</param>
    /// <exception cref="ArgumentException">Thrown when fewer than 2 signals per row.</exception>
    public DispersionIndex(double[][] signals) : base(signals, Spec(FeatureCountOf(signals)))
    {
        if (BarCount > 0) ThrowIfTooFewSignals(FeatureCount, nameof(signals));
    }

    /// <summary>The signals of a row, refused below two (one has no spread): the one check of the batch and the streaming form.</summary>
    internal static void ThrowIfTooFewSignals(int signals, string name) => signals.ThrowIfFewerThan(2, name, new("A row", "signal", "a dispersion index"));

    /// <summary>The label names how many signals it is given once that is known (a streaming form learns it from its first row); the
    /// first value is at the first row.</summary>
    /// <param name="signalCount">How many signals a row holds; 0 while that is not known.</param>
    internal static IndicatorSpec Spec(int signalCount) =>
        IndicatorSpec.Of(0, signalCount > 0 ? IndicatorSpec.Labelled("Dispersion", signalCount) : "Dispersion");

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        var result = new double[ValuedBars];
        for (int t = 0; t < result.Length; t++)
            result[t] = Of(Features[t]);
        return result;
    }

    /// <summary>The dispersion of one row: the population standard deviation of its signals. The one place that arithmetic is
    /// written, read by <see cref="Compute"/> and by <see cref="Streaming.StreamingDispersionIndex"/>.</summary>
    /// <param name="row">One value of each signal at one bar.</param>
    internal static double Of(double[] row) => row.AsSpan().StdDev();

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotLine(axes, Compute());
}
