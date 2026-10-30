// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
    private const int MaxFeatures = 10;

    private readonly int _window;
    private readonly double _regularization;
    private readonly double[][] _ring;
    private readonly double[][] _ordered;
    private int _featureCount;

    /// <inheritdoc />
    public override int WarmupPeriod => _window + 1;

    /// <summary>Creates a streaming turbulence index.</summary>
    /// <param name="window">How many rows before the newest the mean and the covariance are taken over; at least 2.</param>
    /// <param name="regularization">What is added to the diagonal of the covariance before it is inverted; not below 0.</param>
    /// <param name="capacity">How many points the line keeps.</param>
    public StreamingTurbulenceIndex(int window = 252, double regularization = 1e-6, int capacity = 10_000) : base(capacity)
    {
        if (window < 2)
            throw new ArgumentException($"window must be >= 2 (got {window}).", nameof(window));
        if (regularization < 0)
            throw new ArgumentException($"regularization must be >= 0 (got {regularization}).", nameof(regularization));

        _window = window;
        _regularization = regularization;
        _ring = new double[window + 1][];
        _ordered = new double[window + 1][];
        Label = $"Turb({window})";
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">The row has more than 10 features, or a different number than the rows before it.</exception>
    protected override void Step(double[] input, int index, Span<double> outputs)
    {
        if (input.Length > MaxFeatures)
            throw new ArgumentException(
                $"TurbulenceIndex supports at most {MaxFeatures} features (got {input.Length}); use a shrinkage estimator for wider feature sets.",
                nameof(input));
        if (_featureCount != 0 && input.Length != _featureCount)
            throw new ArgumentException(
                $"All feature rows must have the same length; row 0 has {_featureCount}, row {index} has {input.Length}.", nameof(input));

        _featureCount = input.Length;
        _ring[index % _ring.Length] = (double[])input.Clone();
        if (index < _window) return;

        // The rows in the order they came: the oldest of the window + 1 is where the next row will go.
        int oldest = (index - _window) % _ring.Length;
        for (int k = 0; k < _ordered.Length; k++) _ordered[k] = _ring[(oldest + k) % _ring.Length];

        outputs[0] = TurbulenceIndex.Score(_ordered, 0, _window, _regularization);
    }
}
