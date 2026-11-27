// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Styling;

/// <summary>How the wedges of a pie or a donut are laid where their author names nothing: the angle the first starts at, the way the
/// others follow it, and how a pie's name beside its wedge is aligned. The matplotlib themes lay them as <c>Axes.pie</c> does
/// (<see cref="Matplotlib"/>); the library's own themes as the library always has (<see cref="Library"/>).</summary>
internal sealed record WedgeLook : IThemeKeys<WedgeLook>
{
    /// <inheritdoc />
    /// <remarks>The look is named as a whole: matplotlib's (<see cref="Matplotlib"/>) or the library's.</remarks>
    public void WriteKeys(ThemeDto keys) => keys.WedgesLikeMatplotlib = (this == Matplotlib).OrNullAtRest(false);

    /// <inheritdoc />
    public static WedgeLook ReadKeys(ThemeDto keys) => keys.WedgesLikeMatplotlib == true ? Matplotlib : Library;

    /// <summary>The library's own wedges: the first from the top, the others clockwise, a name centred where its wedge's middle is
    /// within a tenth of the radius of straight up or straight down.</summary>
    internal static WedgeLook Library { get; } = new();

    /// <summary>The wedges of matplotlib 3.11.2's <c>Axes.pie</c>: <c>startangle=0</c> (three o'clock), <c>counterclock=True</c>, and a name
    /// beside its wedge starting at its anchor right of the centre and ending there left of it (<c>Axes.pie_label</c>:
    /// <c>'left' if xt &gt; 0 else 'right'</c>).</summary>
    internal static WedgeLook Matplotlib { get; } = new() { StartAngle = 0, CounterClockwise = true, NameCentreBand = 0, PercentInk = GroundInk.TextColour };

    /// <summary>How a pie's percentages are inked on their wedges: readable under the library's own themes, <c>text.color</c> under the
    /// matplotlib themes (<c>autopct</c>'s texts take the axes' text colour).</summary>
    public GroundInk PercentInk { get; init; } = GroundInk.Readable;

    /// <summary>The angle in degrees, counter-clockwise from three o'clock, at which the first wedge starts.</summary>
    public double StartAngle { get; init; } = 90;

    /// <summary>Whether the wedges follow each other counter-clockwise.</summary>
    public bool CounterClockwise { get; init; }

    /// <summary>The angle the first wedge starts at: the one its author gave, else this look's.</summary>
    /// <param name="given">The angle the author gave, or <see langword="null"/>.</param>
    /// <returns>The angle in degrees, counter-clockwise from three o'clock.</returns>
    internal double StartAngleOver(double? given) => given ?? StartAngle;

    /// <summary>Whether the wedges follow each other counter-clockwise: as their author gave it, else as this look lays them.</summary>
    /// <param name="given">The direction the author gave, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> for counter-clockwise.</returns>
    internal bool CounterClockwiseOver(bool? given) => given ?? CounterClockwise;

    /// <summary>How near to straight up or straight down a name's anchor stands, as the cosine of its angle, for the name to be centred
    /// on it; past it a name starts at its anchor on the right of the pie and ends there on the left.</summary>
    public double NameCentreBand { get; init; } = 0.1;
}
