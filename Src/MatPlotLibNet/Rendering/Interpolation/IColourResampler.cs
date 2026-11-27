// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Interpolation;

/// <summary>What the library adds to a public <see cref="IInterpolationEngine"/>: an engine that resamples an image's colours, all four
/// channels in one pass, by the same arithmetic it resamples a value with. The built-in smoothing engines say it; an engine of your own is
/// asked channel by channel through <see cref="IInterpolationEngine.Resample"/>, which gives the same colours.</summary>
/// <remarks>Internal and beside the public interface, so an application that implements the engine is not asked for more.</remarks>
internal interface IColourResampler
{
    /// <summary>Resamples <paramref name="source"/>, <paramref name="rows"/> by <paramref name="columns"/> cells row by row, into
    /// <paramref name="target"/>, <paramref name="targetRows"/> by <paramref name="targetColumns"/> cells row by row.</summary>
    /// <param name="source">The cells to read.</param>
    /// <param name="rows">The number of rows of <paramref name="source"/>.</param>
    /// <param name="columns">The number of columns of <paramref name="source"/>.</param>
    /// <param name="target">The cells to write.</param>
    /// <param name="targetRows">The number of rows of <paramref name="target"/>.</param>
    /// <param name="targetColumns">The number of columns of <paramref name="target"/>.</param>
    void ResampleColours(ReadOnlySpan<ColourSample> source, int rows, int columns, Span<ColourSample> target, int targetRows, int targetColumns);
}
