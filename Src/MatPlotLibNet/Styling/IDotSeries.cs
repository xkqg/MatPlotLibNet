// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Styling;

/// <summary>Something drawn as dots whose size, opacity and jitter a theme can decide: a strip plot, a swarm plot, a point plot, a residual
/// plot and a pair grid. It states what is its own, as values; the one rule that joins them with the theme's is
/// <see cref="DotSeriesExtensions"/>, which no kind repeats.</summary>
internal interface IDotSeries : IGivenAlpha
{
    /// <summary>What its author named.</summary>
    GivenDot Given { get; }

    /// <summary>What it rests at where nobody names a number.</summary>
    RestingDot Resting { get; }

    double? IGivenAlpha.GivenAlpha => Given.Alpha;

    double IGivenAlpha.RestingAlpha => Resting.Alpha;

    /// <summary>The opacity of the dots the theme's dot look names (seaborn's, under the matplotlib themes).</summary>
    double? IGivenAlpha.ThemeAlphaOf(ThemeLooks looks) => looks.Dots.Alpha;

    /// <summary>The diameter, in pixels, seaborn draws it at where the theme draws dots as seaborn does and its author names none: the
    /// look's (<see cref="DotLook.Diameter"/>, the <c>size=5</c> of the categorical plots), else its own resting one. A kind that seaborn
    /// sizes another way (a point plot by its line, a residual plot and a pair grid by scatter's size) says so.</summary>
    /// <param name="slot">The slot of the series, which holds the width of the line and the size of scatter the theme names.</param>
    /// <param name="look">The theme's dot look.</param>
    /// <returns>The diameter, in pixels.</returns>
    double SeabornDiameter(SeriesSlot slot, DotLook look) => look.Diameter ?? Resting.Diameter;
}

/// <summary>The one rule that decides the size and jitter of a dot: what its author named, else the number the theme's dot look names, else
/// the kind's own. Its opacity is the one rule of every series' opacity (<see cref="GivenAlphaExtensions.OpacityUnder"/>).</summary>
internal static class DotSeriesExtensions
{
    extension(IDotSeries dots)
    {
        /// <summary>The diameter of the dots, in pixels, under <paramref name="theme"/>: the one named, else seaborn's where the theme draws
        /// dots as seaborn does (<see cref="IDotSeries.SeabornDiameter"/>), else the kind's own.</summary>
        /// <param name="theme">The theme the series is drawn under.</param>
        /// <param name="slot">The slot of the series.</param>
        /// <returns>The diameter, in pixels.</returns>
        internal double DiameterUnder(Theme theme, SeriesSlot slot) =>
            dots.Given.Diameter
            ?? (theme.Looks.Dots is { LikeSeaborn: true } look ? dots.SeabornDiameter(slot, look) : dots.Resting.Diameter);

        /// <summary>How far a dot may stray sideways from its category under <paramref name="theme"/>, in category units: the jitter named,
        /// else the look's, else the kind's own.</summary>
        /// <param name="theme">The theme the series is drawn under.</param>
        /// <returns>The reach of a dot, in category units.</returns>
        internal double JitterUnder(Theme theme) => theme.Looks.Dots.JitterOver(dots.Given.Jitter, dots.Resting.Jitter);
    }
}
