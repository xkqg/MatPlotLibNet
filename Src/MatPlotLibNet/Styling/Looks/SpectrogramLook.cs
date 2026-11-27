// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Styling;

/// <summary>What the cells of a spectrogram hold and where they stand. The matplotlib themes draw it as matplotlib's <c>specgram</c>
/// does (<see cref="Specgram"/>); the library's own themes as the library always has (<see cref="Library"/>): the magnitude of each bin,
/// on a mesh of cells over 0 to the signal's duration, each row centred on its bin.</summary>
internal sealed record SpectrogramLook : IThemeKeys<SpectrogramLook>
{
    /// <inheritdoc />
    /// <remarks>The look is named as a whole: matplotlib's (<see cref="Specgram"/>) or the library's.</remarks>
    public void WriteKeys(ThemeDto keys) => keys.SpectrogramsLikeSpecgram = LikeSpecgram.OrNullAtRest(false);

    /// <inheritdoc />
    public static SpectrogramLook ReadKeys(ThemeDto keys) => keys.SpectrogramsLikeSpecgram == true ? Specgram : Library;

    /// <summary>The library's own spectrogram.</summary>
    internal static SpectrogramLook Library { get; } = new();

    /// <summary>matplotlib's <c>specgram</c> (3.11.2): the decibels of the one-sided power density of each window (<c>mode='psd'</c>,
    /// <c>scale='dB'</c>), drawn with
    /// <c>imshow</c> on the extent from the centre of the first window less half a step to the centre of the last plus half a step, and
    /// from the first frequency to the last.</summary>
    internal static SpectrogramLook Specgram { get; } = new() { LikeSpecgram = true };

    /// <summary>Whether a spectrogram is drawn as matplotlib's <c>specgram</c> draws it: its cells hold the decibels of the one-sided power
    /// density (<see cref="StftResultExtensions.OneSidedDensity"/>) and stand as an image on <see cref="ExtentOf"/>, which the theme's image look
    /// scales (<see cref="ImageLook.InterpolationOver"/>); otherwise they hold the magnitudes, on the library's mesh.</summary>
    public bool LikeSpecgram { get; init; }

    /// <summary>What the cells hold under this look: the decibels of the one-sided power density of each window (<c>specgram</c>'s
    /// <c>Z = 10. * np.log10(spec)</c>, which its image interpolates and colours linearly), or the magnitude of each bin.</summary>
    /// <param name="stft">The short-time transform of the signal.</param>
    /// <param name="windowSize">The length of each window.</param>
    /// <param name="sampleRate">The sample rate.</param>
    /// <returns>The values, [frequency bin, window].</returns>
    internal double[,] CellsOf(StftResult stft, int windowSize, int sampleRate) =>
        LikeSpecgram ? stft.OneSidedDensity(windowSize, sampleRate).PowerToDecibelsInPlace() : stft.Magnitudes;

    /// <summary>The extent of <c>specgram</c>'s image: across, the centre of the first window less half a step to the centre of the last
    /// plus half a step (<c>pad_xextent = (NFFT - noverlap) / Fs / 2</c>); up, the first frequency to the last.</summary>
    /// <param name="frames">How the transform cuts the signal into windows.</param>
    /// <param name="sampleRate">The sample rate.</param>
    /// <returns>The extent along X (time) and along Y (frequency).</returns>
    internal static SpecgramExtent ExtentOf(StftFrames frames, int sampleRate)
    {
        double pad = frames.Step / (double)sampleRate / 2;
        return new(new Range1D(frames.CentreOf(0, sampleRate) - pad, frames.CentreOf(frames.Count - 1, sampleRate) + pad),
            new Range1D(frames.FrequencyOf(0, sampleRate), frames.FrequencyOf(frames.Bins - 1, sampleRate)));
    }
}

/// <summary>Where the image of a spectrogram stands under matplotlib's <c>specgram</c>.</summary>
/// <param name="Time">From the left edge of the image to its right, in seconds.</param>
/// <param name="Frequency">From its bottom edge to its top, in hertz.</param>
internal readonly record struct SpecgramExtent(Range1D Time, Range1D Frequency);
