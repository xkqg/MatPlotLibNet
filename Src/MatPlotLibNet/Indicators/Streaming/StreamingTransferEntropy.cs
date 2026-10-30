// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming transfer entropy: the Kraskov transfer entropy of the last <see cref="TransferEntropyOptions.Window"/> samples,
/// one value after each sample, stamped at the sample it is known at. It is the rolling <see cref="TransferEntropy"/> taken one
/// sample at a time (the same estimate on the same window, with the jitter of each sample keyed by its place in the whole series),
/// so after each sample it says what the batch says over everything so far; it is NaN until the first window is full and, with a
/// <see cref="TransferEntropyOptions.Stride"/>, between the windows. It is given a <see cref="TransferEntropySample"/> (two series, or
/// three when it conditions), not a bar, so it is not attached to a candle series. O(Window²) per window worked out.</summary>
public sealed class StreamingTransferEntropy : WindowedStreamingIndicator<TransferEntropySample>
{
    private readonly TransferEntropyOptions _options;
    private readonly bool _conditioned;
    private readonly double[] _source;
    private readonly double[] _target;
    private readonly double[]? _condition;

    /// <summary>Creates a streaming transfer entropy.</summary>
    /// <param name="options">The settings; it must have a <see cref="TransferEntropyOptions.Window"/> (the number of samples kept) and
    /// the Kraskov estimate, and no <see cref="TransferEntropyOptions.Condition"/> array (a condition is given in each sample).</param>
    /// <param name="conditioned">Whether to condition the flow on <see cref="TransferEntropySample.Condition"/>.</param>
    /// <param name="capacity">How many points the line keeps.</param>
    /// <exception cref="ArgumentException">The options have no window, are not Kraskov, carry a condition array, or are otherwise refused
    /// by <see cref="TransferEntropy"/> (the window too short for the neighbours, a stride below 1).</exception>
    public StreamingTransferEntropy(TransferEntropyOptions options, bool conditioned = false, int capacity = 10_000)
        : base(WidthOf(options), capacity)
    {
        _options = options;
        _conditioned = conditioned;
        _source = new double[Width];
        _target = new double[Width];
        _condition = conditioned ? new double[Width] : null;
        Label = TransferEntropy.LabelOf(options);
    }

    /// <summary>The window of the options, after the same checks the batch indicator makes.</summary>
    private static int WidthOf(TransferEntropyOptions options)
    {
        if (options.Window is not int window)
            throw new ArgumentException("A streaming transfer entropy keeps a window of samples: give the options a Window.", nameof(options.Window));
        if (options.Condition is not null)
            throw new ArgumentException("Give the condition in each sample, not as an array in the options.", nameof(options.Condition));

        _ = new TransferEntropy(new double[window], new double[window], options);
        return window;
    }

    /// <inheritdoc />
    /// <remarks>The index of the newest sample is <see cref="StreamingIndicator{TInput}.ProcessedCount"/>: the count is raised after
    /// the outputs are written.</remarks>
    protected override void Recompute(ReadOnlySpan<TransferEntropySample> window, Span<double> outputs)
    {
        int newest = ProcessedCount;
        if (window.Length < Width) return;
        if ((newest - (Width - 1)) % _options.Stride != 0) return;

        for (int i = 0; i < Width; i++)
        {
            _source[i] = window[i].Source;
            _target[i] = window[i].Target;
            if (_condition is not null) _condition[i] = window[i].Condition;
        }

        outputs[0] = TransferEntropy.EstimateWindow(_options, _source, _target, _condition, start: 0, Width, absoluteStart: newest - Width + 1);
    }
}
