// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>A series that keeps whether its author gave it an opacity, because a theme may decide it when they did not: a fill (an area, a
/// stack, a polar bar), a 3-D surface, and the dots of a strip or swarm plot. It states what is its own, as values; the one rule that
/// joins them with the theme's is <see cref="GivenAlphaExtensions.OpacityUnder"/>, which no kind repeats.</summary>
/// <remarks>Implemented by the series' own members.</remarks>
internal interface IGivenAlpha
{
    /// <summary>The opacity its author gave, <see langword="null"/> while none was.</summary>
    double? GivenAlpha { get; }

    /// <summary>The opacity it is drawn at where nobody names one: under the library's own themes the theme names none either.</summary>
    double RestingAlpha { get; }

    /// <summary>The opacity a theme names for a series of this kind, <see langword="null"/> where it names none. A fill takes the
    /// collections' opacity of a theme that draws patches as matplotlib draws collections (<see cref="PatchLook.LikeCollections"/>,
    /// matplotlib's <c>fill_between</c>, <c>stackplot</c> and <c>bar</c> are opaque by default); a kind that is not a fill says its own
    /// number.</summary>
    /// <param name="looks">The looks of the theme the series is drawn under.</param>
    /// <returns>The opacity, 0 to 1, or <see langword="null"/>.</returns>
    double? ThemeAlphaOf(ThemeLooks looks) => looks.Patch.LikeCollections ? looks.Patch.Alpha ?? PatchLook.RestingAlpha : null;
}

/// <summary>The one rule that decides how opaque a series is drawn: what its author gave, else the number its theme names for a kind like
/// it, else the kind's own.</summary>
internal static class GivenAlphaExtensions
{
    extension(IGivenAlpha series)
    {
        /// <summary>The opacity <paramref name="series"/> is drawn at under <paramref name="theme"/>: the one given, else the theme's
        /// (<see cref="IGivenAlpha.ThemeAlphaOf"/>), else the kind's resting one.</summary>
        /// <param name="theme">The theme the series is drawn under.</param>
        /// <returns>The opacity, 0 to 1.</returns>
        internal double OpacityUnder(Theme theme) => series.GivenAlpha ?? series.ThemeAlphaOf(theme.Looks) ?? series.RestingAlpha;
    }
}
