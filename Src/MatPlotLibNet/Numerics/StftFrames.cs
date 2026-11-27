// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>How a short-time transform cuts a signal into windows: how many, how far apart, and where each window's centre and each
/// bin stand. The one rule <see cref="Fft.Stft"/> lays its windows out by and a spectrogram places its cells by, so the extent of a
/// spectrogram is known without transforming the signal.</summary>
/// <param name="Count">The number of windows: as many whole windows as the signal holds, at least one.</param>
/// <param name="Step">The samples one window is moved by: the window size less the overlap, at least one.</param>
/// <param name="WindowSize">The samples in each window, the length of its transform.</param>
internal readonly record struct StftFrames(int Count, int Step, int WindowSize)
{
    /// <summary>The windows of a signal of <paramref name="length"/> samples.</summary>
    /// <param name="length">The number of samples.</param>
    /// <param name="windowSize">The samples in each window.</param>
    /// <param name="overlap">The samples neighbouring windows share.</param>
    /// <returns>The layout.</returns>
    internal static StftFrames Of(int length, int windowSize, int overlap)
    {
        int step = Math.Max(1, windowSize - overlap);
        return new(Math.Max(1, (length - windowSize) / step + 1), step, windowSize);
    }

    /// <summary>The number of frequency bins each window gives, 0 to half the window size.</summary>
    internal int Bins => WindowSize / 2 + 1;

    /// <summary>The time of the centre of window <paramref name="frame"/>, in seconds.</summary>
    /// <param name="frame">The window, from 0.</param>
    /// <param name="sampleRate">The sample rate.</param>
    /// <returns>The time.</returns>
    internal double CentreOf(int frame, int sampleRate) => (frame * Step + WindowSize / 2.0) / sampleRate;

    /// <summary>The frequency of bin <paramref name="bin"/>, <c>bin · sampleRate / WindowSize</c> hertz.</summary>
    /// <param name="bin">The bin, 0 to <see cref="Bins"/> less one.</param>
    /// <param name="sampleRate">The sample rate.</param>
    /// <returns>The frequency.</returns>
    internal double FrequencyOf(int bin, int sampleRate) => bin * (double)sampleRate / WindowSize;
}
