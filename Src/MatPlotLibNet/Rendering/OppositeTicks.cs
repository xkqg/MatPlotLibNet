// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>What the edge opposite a primary axis draws of that axis' ticks (<see cref="CartesianEdges"/>): its major ticks with their
/// labels where the axis is mirrored (<see cref="Mirrored"/>, matplotlib's <c>tick_params</c>, which reaches the major ticks only by
/// default), the major and minor marks without labels where the theme marks every side (<see cref="Marked"/>, matplotlib's
/// <c>xtick.top</c> and <c>ytick.right</c>, which reach the minor ticks too), nothing where an axis of its own stands there or neither
/// is asked (<see cref="None"/>).</summary>
/// <param name="Majors">What of the major ticks is drawn there.</param>
/// <param name="Minors">What of the minor ticks is drawn there: their marks or nothing.</param>
internal readonly record struct OppositeTicks(TickInk Majors, TickInk Minors)
{
    /// <summary>Nothing on the opposite edge.</summary>
    internal static OppositeTicks None { get; } = new(TickInk.None, TickInk.None);

    /// <summary>The major ticks mirrored, marks and labels, and no minor tick.</summary>
    internal static OppositeTicks Mirrored { get; } = new(TickInk.Both, TickInk.None);

    /// <summary>The major and the minor marks, no label.</summary>
    internal static OppositeTicks Marked { get; } = new(TickInk.Marks, TickInk.Marks);

    /// <summary>What the opposite edge of an axis draws: nothing where an axis of its own stands there
    /// (<paramref name="ownedByAnAxis"/>), the mirrored ticks where the axis asked for them, the marks where the theme marks every
    /// side, else nothing.</summary>
    /// <param name="mirrored">Whether the axis asked for its ticks mirrored (<see cref="Models.TickConfig.Mirror"/>).</param>
    /// <param name="onAllSides">Whether the theme marks every side (<see cref="Styling.AxisTickLook.OnAllSides"/>).</param>
    /// <param name="ownedByAnAxis">Whether an axis of its own is drawn on the opposite edge (the top X axis, a right Y axis).</param>
    /// <returns>The ticks the opposite edge draws.</returns>
    internal static OppositeTicks Of(bool mirrored, bool onAllSides, bool ownedByAnAxis) =>
        ownedByAnAxis ? None : mirrored ? Mirrored : onAllSides ? Marked : None;
}
