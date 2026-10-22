// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>A stretch of a category axis, and the one rule for where a category sits on it: category <c>k</c> owns the
/// slot from <c>k</c> to <c>k + 1</c> and is centred <see cref="CentreOffset"/> past its index. A bar stands inside its
/// category's slot (<see cref="Bar"/>), or around its x on a numeric axis (<see cref="Around"/>).
/// <para>Everything that places something on a category reads this type, so no two of them can disagree: the bar renderer
/// draws a bar where it says (<see cref="BarSeries.SlotOf"/>), a bar series' data range makes room for those bars, the
/// category labels and a candlestick's body sit on its centres, the indicator shortcuts shift their points onto them, and
/// a verb that marks a bar (a Pareto chart's running share) puts its point on the bar's <see cref="Centre"/>.</para></summary>
/// <param name="Left">Where the stretch begins along the axis.</param>
/// <param name="Centre">Its middle: where a mark that belongs to it is placed.</param>
/// <param name="Right">Where it ends.</param>
internal readonly record struct CategorySlot(double Left, double Centre, double Right)
{
    /// <summary>How far past its index a category's centre lies: half a slot.</summary>
    internal const double CentreOffset = 0.5;

    /// <summary>The whole slot of category <paramref name="category"/>: from its index to the next index.</summary>
    /// <param name="category">The category's index along the axis, from 0.</param>
    /// <returns>The slot, centred <see cref="CentreOffset"/> past the index.</returns>
    internal static CategorySlot Of(int category) => new(category, category + CentreOffset, category + 1);

    /// <summary>A bar <paramref name="width"/> wide around <paramref name="centre"/>: how a bar stands on a numeric axis,
    /// and how a centred bar stands in its slot.</summary>
    /// <param name="centre">Where the bar's middle lies along the axis.</param>
    /// <param name="width">The bar's width, in axis units.</param>
    /// <returns>The stretch the bar covers.</returns>
    internal static CategorySlot Around(double centre, double width) => new(centre - width / 2, centre, centre + width / 2);

    /// <summary>The bar that stands in this slot: <paramref name="width"/> wide and moved along the axis by
    /// <paramref name="offset"/> (the bars of a group step aside for each other). A centred bar stands around the slot's
    /// centre; an edge-aligned one starts at the slot's left edge.</summary>
    /// <param name="width">The bar's width, in axis units.</param>
    /// <param name="offset">How far the bar is moved from where it would stand alone.</param>
    /// <param name="align">Whether the bar stands on the slot's centre or starts at its left edge.</param>
    /// <returns>The stretch the bar covers.</returns>
    internal CategorySlot Bar(double width, double offset, BarAlignment align) =>
        align == BarAlignment.Edge
            ? new(Left + offset, Left + offset + width / 2, Left + offset + width)
            : Around(Centre + offset, width);
}
