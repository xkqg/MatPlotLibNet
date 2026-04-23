// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Ehlers' Inverse Fisher Transform (2004) — squashes any input series through
/// <c>tanh(scale × x)</c>, turning sluggish oscillator curves into sharp transitions between
/// <c>+1</c> and <c>-1</c> states. Apply as a meta-indicator over RSI, stochastic, CCI, or any
/// pre-normalised oscillator output to produce cleaner crossover signals. Reference:
/// Ehlers, J. F. (2004), <i>The Inverse Fisher Transform</i>, Stocks &amp; Commodities 22(5).</summary>
public sealed class InverseFisherTransform : Indicator<SignalResult>
{
    private readonly double[] _input;
    private readonly double _scale;

    /// <summary>Creates a new Inverse Fisher Transform.</summary>
    /// <param name="input">Input series to transform. Typically a pre-normalised oscillator in
    /// roughly <c>[-5, +5]</c>; values outside that range saturate to ±1.</param>
    /// <param name="scale">Multiplier applied before the tanh squash. Default 1.0. Higher values
    /// steepen the transition. Must be &gt; 0.</param>
    public InverseFisherTransform(double[] input, double scale = 1.0)
    {
        if (input is null || input.Length == 0)
            throw new ArgumentException("input is required and must be non-empty.", nameof(input));
        if (scale <= 0)
            throw new ArgumentException($"scale must be > 0 (got {scale}).", nameof(scale));
        _input = input;
        _scale = scale;
        Label = $"IFT(scale={scale:0.##})";
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

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        PlotSignal(axes, Compute(), warmup: 0);
        axes.YAxis.Min = -1;
        axes.YAxis.Max = 1;
    }
}
