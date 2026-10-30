// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The parts a Renko chart is made of, handed to the callback of <see cref="PriceAxesExtensions.Renko"/> so each can be
/// styled on its own: the bricks the price rose to complete and the bricks it fell to complete. They are the very series on the
/// axes, so a change made through them is the chart's, and both exist even when the prices drew no brick of one kind.
/// <para>A value of this type can only be made by the library: it has no public constructor, and its properties have no
/// setters.</para></summary>
public readonly record struct RenkoParts
{
    /// <summary>Makes the parts of one Renko chart.</summary>
    /// <param name="up">The series of the bricks the price rose to complete.</param>
    /// <param name="down">The series of the bricks the price fell to complete.</param>
    internal RenkoParts(BarSeries up, BarSeries down)
    {
        Up = up;
        Down = down;
    }

    /// <summary>The bricks the price rose to complete: floating bars, a brick wide, each standing on the bottom of its brick (so
    /// its table gives the brick's number and where it starts and ends).</summary>
    public BarSeries Up { get; }

    /// <summary>The bricks the price fell to complete, in the same form.</summary>
    public BarSeries Down { get; }
}
