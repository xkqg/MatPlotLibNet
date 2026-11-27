// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Ehlers' Inverse Fisher Transform (2004) — squashes any input series through
/// <c>tanh(scale × x)</c>, turning sluggish oscillator curves into sharp transitions between
/// <c>+1</c> and <c>-1</c> states. Apply as a meta-indicator over RSI, stochastic, CCI, or any
/// pre-normalised oscillator output to produce cleaner crossover signals. Reference:
/// Ehlers, J. F. (2004), <i>The Inverse Fisher Transform</i>, Stocks &amp; Commodities 22(5).</summary>
public sealed class InverseFisherTransform : Indicator<SignalResult>
{
    /// <summary>The <c>scale</c> the indicator is made with when the caller gives none.</summary>
    public const double DefaultScale = 1.0;

    private readonly double[] _input;
    private readonly double _scale;

    /// <inheritdoc />
    private protected override int BarsIn => _input.Length;

    /// <summary>Creates a new Inverse Fisher Transform.</summary>
    /// <param name="input">Input series to transform. Typically a pre-normalised oscillator in
    /// roughly <c>[-5, +5]</c>; values outside that range saturate to ±1.</param>
    /// <param name="scale">Multiplier applied before the tanh squash. Default <see cref="DefaultScale"/>. Higher values
    /// steepen the transition. Must be &gt; 0.</param>
    public InverseFisherTransform(double[] input, double scale = DefaultScale) : base(Spec(scale))
    {
        _input = input ?? [];
        _input.ThrowIfEmpty("input is required and must be non-empty.", nameof(input));
        _scale = scale;
    }

    internal static IndicatorSpec Spec(double scale)
    {
        scale.ThrowIfNotAbove(0, nameof(scale));
        return IndicatorSpec.Of(0, $"IFT(scale={scale.InvariantText("0.##")})");
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = _input.Length;
        var result = new double[n];
        for (int i = 0; i < n; i++)
            result[i] = Math.Tanh(_scale * _input[i]);
        return result;
    }

    /// <summary>The range the values of the indicator live in: its pane, and the pane of <see cref="Streaming.StreamingInverseFisherTransform"/>, are pinned to it.</summary>
    internal static PaneRange Pane { get; } = PaneRange.Signed;

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        PlotLine(axes, Compute());
        Pane.Pin(axes);
    }
}
