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
/// returns every value bit for bit, so no coordinate is told apart by a flag.</remarks>
internal sealed class LinkedTickPlacement : ITickPlacement
{
    /// <summary>The coordinate that is not linked: <c>(value - 0) / 1</c>, the value itself, a missing value included.</summary>
    private static readonly AxisTransform Unlinked = AxisTransform.Linear(1);

    private readonly DataTransform _primary;
    private readonly AxisTransform _x;
    private readonly AxisTransform _y;

    private LinkedTickPlacement(DataTransform primary, AxisTransform x, AxisTransform y)
    {
        _primary = primary;
        _x = x;
        _y = y;
    }

    /// <summary>The placement of a linked X axis (the top one): a tick of value v stands at the primary's X of
    /// <paramref name="link"/>'s inverse of v.</summary>
    /// <param name="primary">The primary axes' transform.</param>
    /// <param name="link">How the linked axis writes the primary's X values.</param>
    /// <returns>The placement.</returns>
    internal static LinkedTickPlacement AlongX(DataTransform primary, AxisTransform link) => new(primary, link, Unlinked);

    /// <summary>The placement of a linked Y axis (a slot past the primary): a tick of value v stands at the primary's Y of
    /// <paramref name="link"/>'s inverse of v.</summary>
    /// <param name="primary">The primary axes' transform.</param>
    /// <param name="link">How the linked axis writes the primary's Y values.</param>
    /// <returns>The placement.</returns>
    internal static LinkedTickPlacement AlongY(DataTransform primary, AxisTransform link) => new(primary, Unlinked, link);

    /// <inheritdoc/>
    public Point DataToPixel(double x, double y) => _primary.DataToPixel(_x.Inverse(x), _y.Inverse(y));

    /// <inheritdoc/>
    public Rect PlotBounds => _primary.PlotBounds;
}
