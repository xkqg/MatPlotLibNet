// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming turbulence index: how unusual the row just given is against the rows before it, the squared Mahalanobis
/// distance from the mean of the last <c>window</c> rows through the inverse of their regularised sample covariance. A row holds one
/// value of each feature (up to 10), and every row is as long as the first; the form is given a row, not a bar
/// (<see cref="StreamingIndicator{TInput}.Append"/> takes a <c>double[]</c>), so it is not attached to a candle series. It keeps the
/// last <c>window + 1</c> rows and scores the newest against the rest with the arithmetic of the batch
/// <see cref="TurbulenceIndex"/> (<c>TurbulenceIndex.Score</c>), so it says what the batch says over everything so far. The first value
/// comes with row <c>window + 1</c>. O(window × features²) per row.</summary>
public sealed class StreamingTurbulenceIndex : StreamingIndicator<double[]>
{
    private readonly int _window;
    private readonly double _regularization;
    private InputWindow<double[]> _rows;
    private readonly double[][] _ordered;
    private int _featureCount;

    /// <inheritdoc />
    public override int WarmupPeriod => Spec.Warmup;

    /// <summary>Creates a streaming turbulence index.</summary>
    /// <param name="window">How many rows before the newest the mean and the covariance are taken over; at least 2.</param>
    /// <param name="regularization">What is added to the diagonal of the covariance before it is inverted; not below 0.</param>
    /// <param name="capacity">How many points the line keeps.</param>
    public StreamingTurbulenceIndex(int window = TurbulenceIndex.DefaultWindow, double regularization = TurbulenceIndex.DefaultRegularization, int capacity = StreamingSeries.RestingCapacity) : base(TurbulenceIndex.Spec(window, regularization), capacity)
    {

        _window = window;
        _regularization = regularization;
        _rows = new InputWindow<double[]>(window + 1);
        _ordered = new double[window + 1][];
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">The row has more than 10 features, or a different number than the rows before it.</exception>
    protected override void Step(double[] input, int index, Span<double> outputs)
    {
        TurbulenceIndex.ThrowIfTooManyFeatures(input.Length, nameof(input));
        if (_featureCount != 0) input.Length.ThrowIfMisaligned(_featureCount, nameof(input));

        _featureCount = input.Length;
        _rows.Push((double[])input.Clone());
        if (!_rows.IsFull) return;

        _rows.CopyTo(_ordered);
        outputs[0] = TurbulenceIndex.Score(_ordered, 0, _window, _regularization);
    }
}
