// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The parts a Kagi chart is made of, handed to the callback of <see cref="PriceAxesExtensions.Kagi"/> so each can be
/// styled on its own: the thick line and the thin line. They are the very series on the axes, so a change made through them is
/// the chart's, and both exist even when the prices drew no line of one width.
/// <para>A value of this type can only be made by the library: it has no public constructor, and its properties have no
/// setters.</para></summary>
public readonly record struct KagiParts
{
    /// <summary>Makes the parts of one Kagi chart.</summary>
    /// <param name="thick">The line drawn thick.</param>
    /// <param name="thin">The line drawn thin.</param>
    internal KagiParts(LineSeries thick, LineSeries thin)
    {
        Thick = thick;
        Thin = thin;
    }

    /// <summary>The parts of the path the chart draws thick: the legs (or the stretches of a leg) that went above the last
    /// shoulder and have not yet gone below the last waist, and the connectors between them. Separate stretches are separated by
    /// a missing value, so they are lines of their own.</summary>
    public LineSeries Thick { get; }

    /// <summary>The parts of the path drawn thin, in the same form.</summary>
    public LineSeries Thin { get; }
}
