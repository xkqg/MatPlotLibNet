// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>A kind that writes text of its own at a size its author may give (a table, a tree grid, a 3-D text, the labels of a contour):
/// what it was given and what it rests at. The one rule that joins them with the theme is <see cref="GivenTextSizeExtensions.TextSizeUnder"/>.</summary>
internal interface IGivenTextSize
{
    /// <summary>The size its author gave, in the units of <see cref="Font.Size"/>, <see langword="null"/> while none was.</summary>
    double? GivenTextSize { get; }

    /// <summary>The size the kind draws its text at under a theme that names none.</summary>
    double RestingTextSize { get; }
}

/// <summary>The size a series' own text is drawn at.</summary>
internal static class GivenTextSizeExtensions
{
    extension(IGivenTextSize series)
    {
        /// <summary>The size the text of <paramref name="series"/> is drawn at under <paramref name="theme"/>: the one given, else the
        /// theme's (<see cref="Theme.SeriesTextSizeOver"/>).</summary>
        /// <param name="theme">The theme the series is drawn under.</param>
        /// <returns>The size, in the units of <see cref="Font.Size"/>.</returns>
        internal double TextSizeUnder(Theme theme) => theme.SeriesTextSizeOver(series.GivenTextSize, series.RestingTextSize);
    }
}
