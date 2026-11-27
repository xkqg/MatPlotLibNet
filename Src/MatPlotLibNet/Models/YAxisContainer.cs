// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Models;

/// <summary>Every stacked axis of an <see cref="Axes"/>: one <see cref="YAxisStack"/> per side, the Y axes on the left and the
/// right and the linked X axes below the plot. The primary Y axis is slot 0 of <see cref="Left"/>; each further axis stacks
/// outward past the one before it on its side, as many as wanted.</summary>
/// <example><code>var axes = new Axes();
/// axes.YAxes.Right.Add().Axis.Label = "Rate";      // right slot 0
/// axes.YAxes.Right.Add().Axis.Label = "Latency";   // right slot 1, past it
/// axes.YAxes.Left.Add().Axis.Label = "Queue";      // left slot 1, past the primary</code></example>
public sealed class YAxisContainer : IChangeTracked
{
    private long _stamp;
    private readonly Axes _owner;

    /// <summary>Makes the container of <paramref name="owner"/>: its primary Y axis and no other.</summary>
    /// <param name="owner">The axes the container belongs to; a series added at any of its slots puts it in the coordinate system the
    /// series is drawn in.</param>
    internal YAxisContainer(Axes owner)
    {
        _owner = owner;
        Left = new(AxisSide.Left, primarySlots: 1, owner);
        Right = new(AxisSide.Right, primarySlots: 0, owner);
    }

    /// <summary>Stores <paramref name="value"/> and marks the container changed, unless it already holds an equal value.</summary>
    private void Change<T>(ref T slot, T value) => ChangeClock.Change(ref slot, value, ref _stamp);

    /// <summary>The mark of the newest change to the container, to either side or to any slot on it.</summary>
    internal long LatestChange =>
        ChangeClock.Latest(_stamp, ChangeClock.Latest(Left.LatestChange, ChangeClock.Latest(Right.LatestChange, _bottom?.LatestChange ?? 0)));

    long IChangeTracked.LatestChange => LatestChange;

    /// <summary>The left side; slot 0 is the primary Y axis (<see cref="Axes.YAxis"/>).</summary>
    public YAxisStack Left { get; }

    /// <summary>The right side; empty until a right axis is added.</summary>
    public YAxisStack Right { get; }

    /// <summary>The linked X axes below the plot (matplotlib's <c>secondary_xaxis('bottom')</c>); empty until one is added. Each shows
    /// the primary X axis' range in its own unit and holds no series of its own, so none of them is in <see cref="IdentityOrder"/>.</summary>
    public YAxisStack Bottom => _bottom ??= new(AxisSide.Bottom, primarySlots: 0, _owner);
    private YAxisStack? _bottom;

    /// <summary>How many linked X axes stand below the plot, asked without making the <see cref="Bottom"/> stack of an axes that has
    /// none (almost every one).</summary>
    internal int BottomCount => _bottom?.Count ?? 0;

    /// <summary>The blank gap, in pixels, between the ink of a Y axis title and the ink of the widest tick label beside it,
    /// the same for every slot on both sides, the primary included; also the gap between the far side of a slot and the
    /// tick marks of the slot stacked past it. <see cref="RestingTitleGap"/> by default. Given no gap, it is matplotlib's <c>axes.labelpad</c> under the
    /// matplotlib themes (<c>TextLook.LabelPad</c>) and <see cref="RestingTitleGap"/> under every other theme; a gap that is given, the default included,
    /// is drawn as given. A title's own <see cref="Styling.TextStyle.Pad"/> moves that title further out on top of it.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value set is negative, NaN or infinite.</exception>
    public double TitleGap
    {
        get => GivenTitleGap ?? RestingTitleGap;
        set => GivenTitleGap = ValidTitleGap(value, nameof(value));
    }
    /// <summary>The gap a new container reads from <see cref="TitleGap"/>.</summary>
    private const double RestingTitleGap = 4;

    /// <summary>The gap that was given, <see langword="null"/> when none was.</summary>
    internal double? GivenTitleGap { get; private set => Change(ref field, value); }

    /// <summary>The gap under <paramref name="theme"/>, in pixels: the one that was given, else the theme's
    /// <see cref="Styling.TextLook.LabelPad"/>, else <see cref="RestingTitleGap"/>. The gap the titles and the stacked slots of every Y axis are placed by.</summary>
    /// <param name="theme">The theme the axes is drawn under.</param>
    /// <returns>The gap.</returns>
    internal double TitleGapUnder(Styling.Theme theme) => theme.Looks.Text.LabelPadOver(GivenTitleGap, RestingTitleGap);

    /// <summary>The one check of a title gap: a finite number of pixels, 0 or more.</summary>
    /// <param name="pixels">The gap to check.</param>
    /// <param name="paramName">The caller's parameter name, for the exception.</param>
    /// <returns><paramref name="pixels"/>, when it is a gap.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="pixels"/> is negative, NaN or infinite.</exception>
    internal static double ValidTitleGap(double pixels, string paramName) =>
        pixels.ThrowIfNotFinite(paramName).ThrowIfBelow(0, paramName);

    /// <summary>Every Y slot in identity order: the primary, then every right slot nearest the plot first, then every
    /// left slot past the primary; the linked X axes below the plot, which hold no series, are not among them. This order numbers the series of the subplot (<see cref="Axes.AllSeries"/>), and
    /// with it the SVG's <c>data-series-index</c>, a legend toggle's target and the default colours, so it never
    /// follows the order the slots were added in.</summary>
    public IEnumerable<YAxisSlot> IdentityOrder =>
        Left.Take(Left.OutwardOffset).Concat(Right).Concat(Left.Skip(Left.OutwardOffset));

    /// <summary>The stack of <paramref name="side"/>.</summary>
    /// <param name="side">The side.</param>
    /// <returns>The stack of axes on that side.</returns>
    internal YAxisStack StackOn(AxisSide side) => side switch
    {
        AxisSide.Left  => Left,
        AxisSide.Right => Right,
        _              => Bottom,
    };
}
