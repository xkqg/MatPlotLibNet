// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>Every Y axis of an <see cref="Axes"/>: one <see cref="YAxisStack"/> per side. The primary Y axis is slot 0
/// of <see cref="Left"/>; each further axis stacks outward past the one before it on either side, as many as
/// wanted.</summary>
/// <example><code>var axes = new Axes();
/// axes.YAxes.Right.Add().Axis.Label = "Rate";      // right slot 0
/// axes.YAxes.Right.Add().Axis.Label = "Latency";   // right slot 1, past it
/// axes.YAxes.Left.Add().Axis.Label = "Queue";      // left slot 1, past the primary</code></example>
public sealed class YAxisContainer
{
    internal YAxisContainer() { }

    /// <summary>The left side; slot 0 is the primary Y axis (<see cref="Axes.YAxis"/>).</summary>
    public YAxisStack Left { get; } = new(AxisSide.Left, primarySlots: 1);

    /// <summary>The right side; empty until a right axis is added.</summary>
    public YAxisStack Right { get; } = new(AxisSide.Right, primarySlots: 0);

    /// <summary>The blank gap, in pixels, between the ink of a Y axis title and the ink of the widest tick label beside it,
    /// the same for every slot on both sides, the primary included; also the gap between the far side of a slot and the
    /// tick marks of the slot stacked past it. 4 by default, matplotlib's <c>axes.labelpad</c>. A title's own
    /// <see cref="Styling.TextStyle.Pad"/> moves that title further out on top of it.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value set is negative, NaN or infinite.</exception>
    public double TitleGap
    {
        get => _titleGap;
        set => _titleGap = ValidTitleGap(value, nameof(value));
    }
    private double _titleGap = 4;

    /// <summary>The one check of a title gap: a finite number of pixels, 0 or more.</summary>
    /// <param name="pixels">The gap to check.</param>
    /// <param name="paramName">The caller's parameter name, for the exception.</param>
    /// <returns><paramref name="pixels"/>, when it is a gap.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="pixels"/> is negative, NaN or infinite.</exception>
    internal static double ValidTitleGap(double pixels, string paramName) =>
        pixels >= 0 && double.IsFinite(pixels)
            ? pixels
            : throw new ArgumentOutOfRangeException(paramName, pixels, "A Y axis title gap is a finite number of pixels, 0 or more.");

    /// <summary>Every slot in identity order: the primary, then every right slot nearest the plot first, then every
    /// left slot past the primary. This order numbers the series of the subplot (<see cref="Axes.AllSeries"/>), and
    /// with it the SVG's <c>data-series-index</c>, a legend toggle's target and the default colours, so it never
    /// follows the order the slots were added in.</summary>
    public IEnumerable<YAxisSlot> IdentityOrder =>
        Left.Take(Left.OutwardOffset).Concat(Right).Concat(Left.Skip(Left.OutwardOffset));

    /// <summary>The stack of <paramref name="side"/>.</summary>
    /// <param name="side">The side.</param>
    internal YAxisStack StackOn(AxisSide side) => side == AxisSide.Left ? Left : Right;
}
