// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Styling;

/// <summary>The dots of a strip plot, a swarm plot, a point plot, a residual plot and a pair grid where their author names
/// nothing: seaborn's numbers as values, never as literals in the series or the renderers. A number a look names is the one a dot is
/// drawn with where the author named none; a number it leaves <see langword="null"/> is the kind's own (<see cref="RestingDot"/>).
/// The one rule that joins them with what the author named is <see cref="DotSeriesExtensions"/>.</summary>
internal sealed record DotLook : IThemeKeys<DotLook>
{
    /// <summary>The library's own dots: every number the kind's own, at the opacity, jitter and size their series have always had.</summary>
    internal static DotLook Library { get; } = new();

    /// <inheritdoc />
    /// <remarks>The dots are named as a whole: seaborn's (<see cref="Seaborn"/>) or the library's.</remarks>
    public void WriteKeys(ThemeDto keys) => keys.DotsLikeSeaborn = LikeSeaborn.OrNullAtRest(false);

    /// <inheritdoc />
    public static DotLook ReadKeys(ThemeDto keys) => keys.DotsLikeSeaborn == true ? Seaborn : Library;

    /// <summary>The dots as seaborn 0.13.2 draws them over matplotlib's own defaults. Opaque: a strip's and a swarm's <c>ax.scatter</c> is
    /// given no alpha. Jittered by 0.1 of a category (the <c>jlim</c> of 0.1 in <c>stripplot(jitter=True)</c>). 5 points across (the
    /// <c>size=5</c> of <c>stripplot</c> and <c>swarmplot</c>, <c>ax.scatter</c> reads <c>get_sizes() == [25]</c>, at 100 dpi). A point plot's
    /// mean is <c>lines.linewidth * 1.8 * sqrt(2 * pi)</c> points across (<c>categorical.py</c>: <c>markersize = linewidth * sqrt(2 * pi)</c>),
    /// its lines <c>lines.linewidth * 1.8</c> wide (<c>categorical.py:1180</c>: <c>plot_kws.setdefault("linewidth",
    /// mpl.rcParams["lines.linewidth"] * 1.8)</c>), its caps none (<c>capsize=0</c>) and its means joined (<c>join=True</c>). A residual
    /// plot's dots are 0.8 opaque and outlined in their own colour (<c>regression.py</c>'s <c>scatterplot</c>).</summary>
    internal static DotLook Seaborn { get; } = new()
    {
        LikeSeaborn = true,
        Alpha = 1.0,
        Jitter = 0.1,
        Diameter = 5.0.PointsInPixels,
        MeanPerLine = Math.Sqrt(2 * Math.PI),
        CapSize = 0.0,
        JoinsPoints = true,
        PointplotLine = LineWidthRule.SeabornPointplotLines,
        RegressionAlpha = 0.8,
        RegressionEdged = true,
    };

    /// <summary>Whether the dots are drawn as seaborn draws them over matplotlib's own defaults, in the two things a number cannot say: a
    /// point plot's estimate (the confidence interval of the mean, a line through the means, dots outlined as a line's markers) and a
    /// swarm's layout (placed in pixels as seaborn's <c>Beeswarm</c> places them).</summary>
    public bool LikeSeaborn { get; init; }

    /// <summary>The opacity of a strip plot's or a swarm plot's dots, 0 to 1, where their author names none; <see langword="null"/>
    /// leaves it to the series' own.</summary>
    internal double? Alpha { get; init; }

    /// <summary>How far a strip plot's dots may stray sideways from their category, in category units, where their author names no jitter;
    /// <see langword="null"/> leaves it to the series' own.</summary>
    internal double? Jitter { get; init; }

    /// <summary>The diameter, in pixels, of the dots of a kind sized by the look itself (a strip plot, a swarm plot) where their author
    /// names none; <see langword="null"/> leaves it to the series' own. A kind sized by its line or by scatter says so itself
    /// (<see cref="IDotSeries.SeabornDiameter"/>).</summary>
    internal double? Diameter { get; init; }

    /// <summary>How many times a point plot's line width its mean is across, where its author names no size; <see langword="null"/>
    /// when the look names no such ratio.</summary>
    internal double? MeanPerLine { get; init; }

    /// <summary>The width of a point plot's caps, in category units, where its author names none; <see langword="null"/> leaves it to the
    /// series' own.</summary>
    internal double? CapSize { get; init; }

    /// <summary>Whether a point plot joins the means of neighbouring categories where its author names no choice.</summary>
    internal bool JoinsPoints { get; init; }

    /// <summary>How wide a point plot's lines (the line through its means and its intervals) are drawn where their author names no
    /// width: under the library's look the series' own 1.5 pixels whatever width the theme names; under seaborn's
    /// (<see cref="Seaborn"/>) the theme's width times 1.8.</summary>
    internal LineWidthRule PointplotLine { get; init; } = LineWidthRule.Own;

    /// <summary>The opacity of a residual plot's dots where their author names none: seaborn's regression scatter
    /// (<c>scatter_kws.setdefault("alpha", .8)</c>); <see langword="null"/> leaves it to the series' own (opaque).</summary>
    internal double? RegressionAlpha { get; init; }

    /// <summary>Whether a residual plot's dots are outlined in their own colour at the theme's marker edge width, as seaborn's regression
    /// scatter is (<c>ax.scatter(color=c, linewidths=lines.markeredgewidth)</c>: the colour names the face and the edge); not under the
    /// library's own look.</summary>
    internal bool RegressionEdged { get; init; }

    /// <summary>The reach of a point plot's caps: the one given, else this look's, else <paramref name="resting"/>.</summary>
    /// <param name="given">The value its author gave, <see langword="null"/> when none.</param>
    /// <param name="resting">The value its owner rests at where the theme names none.</param>
    /// <returns>The value drawn.</returns>
    internal double CapSizeOver(double? given, double resting) => given ?? CapSize ?? resting;

    /// <summary>Whether a point plot's points are joined: the choice given, else this look's.</summary>
    /// <param name="given">The value its author gave, <see langword="null"/> when none.</param>
    /// <returns>The value drawn.</returns>
    internal bool JoinsPointsOver(bool? given) => given ?? JoinsPoints;

    /// <summary>How far a dot is jittered: the amount given, else this look's, else <paramref name="resting"/>.</summary>
    /// <param name="given">The value its author gave, <see langword="null"/> when none.</param>
    /// <param name="resting">The value its owner rests at where the theme names none.</param>
    /// <returns>The value drawn.</returns>
    internal double JitterOver(double? given, double resting) => given ?? Jitter ?? resting;
}
