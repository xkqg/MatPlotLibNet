// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Bayesian Online Changepoint Detection (Adams &amp; MacKay 2007). Maintains a
/// posterior over run lengths; the changepoint probability is <c>P(r_t = 0)</c> — the
/// posterior probability that the current bar starts a new run.</summary>
/// <remarks>Output is per-bar changepoint probability in [0, 1]. Output length is
/// <c>prices.Length − 1</c> (first bar has no prior). Reference:
/// Adams &amp; MacKay (2007), arXiv:0710.3742.</remarks>
public sealed class Bocpd : PriceIndicator<SignalResult>
{
    /// <summary>The <c>hazard</c> the indicator is made with when the caller gives none.</summary>
    public const double DefaultHazard = 0.01;

    /// <summary>The <c>priorVariance</c> the indicator is made with when the caller gives none.</summary>
    public const double DefaultPriorVariance = 1.0;

    /// <summary>The <c>maxRunLength</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultMaxRunLength = 500;

    private readonly double _hazard;
    private readonly double _priorVariance;
    private readonly int _maxRunLength;

    /// <summary>Creates a new BOCPD indicator.</summary>
    /// <param name="prices">Price series.</param>
    /// <param name="hazard">Per-bar prior probability of a changepoint, in (0, 1). Default <see cref="DefaultHazard"/>.</param>
    /// <param name="priorVariance">Broad prior variance <c>κ²</c> added to the predictive. Must be &gt; 0.</param>
    /// <param name="maxRunLength">Maximum run length tracked (memory bound). Must be ≥ 1.</param>
    public Bocpd(double[] prices, double hazard = DefaultHazard, double priorVariance = DefaultPriorVariance,
                 int maxRunLength = DefaultMaxRunLength)
        : base(prices, Spec(hazard, priorVariance, maxRunLength))
    {
        _hazard = hazard;
        _priorVariance = priorVariance;
        _maxRunLength = maxRunLength;
    }

    internal static IndicatorSpec Spec(double hazard, double priorVariance, int maxRunLength)
    {
        hazard.ThrowIfNotBetween(0, 1, nameof(hazard));
        priorVariance.ThrowIfNotAbove(0, nameof(priorVariance));
        maxRunLength.ThrowIfBelow(1, nameof(maxRunLength));
        return IndicatorSpec.Of(1, $"BOCPD(h={hazard.InvariantText("0.###")})");
    }

    /// <inheritdoc />
    public override SignalResult Compute() => FoldPrices(new BocpdStep(_hazard, _priorVariance, _maxRunLength));

    /// <summary>The range the values of the indicator live in: its pane, and the pane of <see cref="Streaming.StreamingBocpd"/>, are pinned to it.</summary>
    internal static PaneRange Pane { get; } = PaneRange.Unit;

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        PlotLine(axes, Compute());
        Pane.Pin(axes);
    }
}
