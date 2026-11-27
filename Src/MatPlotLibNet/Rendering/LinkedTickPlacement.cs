// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Rendering;

/// <summary>Where the ticks of a linked axis (<see cref="Axis.Link"/>) stand: the tick of value v stands where the primary axis
/// shows <see cref="AxisTransform.Inverse"/>(v), through the primary's own transform, so its scale, its direction and its breaks
/// place the tick as they place the data. matplotlib's <c>secondary_xaxis</c> and <c>secondary_yaxis</c> stand their ticks by the
/// same rule. The label still reads v, and minor ticks are split between the values, in the linked unit.</summary>
/// <remarks>One coordinate is linked, the one the ticks run along; the other is handed to the primary as it is given. Each
/// coordinate goes through a transform, the one across the edge through <see cref="AxisTransform.Linear"/>(1), whose inverse
/// returns every value bit for bit, so no coordinate is told apart by a flag: the two transforms are one pair, and so are the ranges
/// the linked axis shows, which a direction picks its member of.</remarks>
internal sealed class LinkedTickPlacement : ITickPlacement
{
    /// <summary>The coordinate that is not linked: <c>(value - 0) / 1</c>, the value itself, a missing value included.</summary>
    private static readonly AxisTransform Unlinked = AxisTransform.Linear(1);

    private readonly DataTransform _primary;
    private readonly AxisPair<AxisTransform> _link;
    private readonly AxisPair<LinkedRange?> _shown;

    private LinkedTickPlacement(DataTransform primary, AxisPair<AxisTransform> link)
    {
        _primary = primary;
        _link = link;
        _shown = link.Select(primary, static (transform, alongX, shown) => transform.ImageOf(shown.ShownAlong(alongX)));
    }

    /// <summary>The placement of a linked axis along X (the top one or one below the plot: a tick of value v stands at the primary's X
    /// of <paramref name="link"/>'s inverse of v) or along Y (a slot past the primary, at the primary's Y).</summary>
    /// <param name="alongX">Whether the linked axis runs along X: the top one or one below the plot; a Y slot otherwise.</param>
    /// <param name="primary">The primary axes' transform.</param>
    /// <param name="link">How the linked axis writes the primary's values.</param>
    /// <returns>The placement.</returns>
    internal static LinkedTickPlacement Along(bool alongX, DataTransform primary, AxisTransform link) =>
        new(primary, new AxisPair<AxisTransform>(Unlinked, Unlinked).With(alongX, link));

    /// <inheritdoc/>
    public Point DataToPixel(double x, double y) => _primary.DataToPixel(_link.X.Inverse(x), _link.Y.Inverse(y));

    /// <summary>The pixel along the edge where the tick of <paramref name="value"/> stands, where the primary shows the value it
    /// maps back to, without asking whether the plot shows it (<see cref="OnPlot"/> asks).</summary>
    /// <param name="value">The tick's value, in the linked unit.</param>
    /// <param name="alongX">Whether the value is an X; a Y otherwise.</param>
    /// <returns>The pixel coordinate along that edge; NaN where the primary has no place for the value.</returns>
    internal double PixelAlong(double value, bool alongX) => _primary.PixelAlong(_link.Along(alongX).Inverse(value), alongX);

    /// <summary>Where the tick of <paramref name="value"/> stands along its edge when the plot shows it: it lies on the range
    /// the linked axis shows (the image of the primary's range through the link, <see cref="AxisTransform.ImageOf"/>), in the
    /// linked unit, as matplotlib keeps a secondary axis' tick in view, and it stands where the primary shows the value it maps
    /// back to; nothing when the link cannot place the primary's range.</summary>
    /// <param name="value">The tick's value, in the linked unit.</param>
    /// <param name="alongX">Whether the value is an X (the top axis); a Y otherwise.</param>
    /// <returns>The pixel along the edge, or <see langword="null"/> when the plot does not show the value.</returns>
    public double? OnPlot(double value, bool alongX)
    {
        if (_shown.Along(alongX) is not { } shown || !shown.Interval.HoldsClose(value)) return null;
        double pos = PixelAlong(value, alongX);
        return pos.IsDrawable() ? pos : null;
    }

    /// <inheritdoc/>
    public ScaleRule ScaleAlong(bool alongX) => _primary.RuleAlong(alongX).UnderALink;

    /// <inheritdoc/>
    /// <remarks>The image of the primary's range through the link, low to high.</remarks>
    public Range1D? ViewAlong(bool alongX) => _shown.Along(alongX)?.Interval;
}
