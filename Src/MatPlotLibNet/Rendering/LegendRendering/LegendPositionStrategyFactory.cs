// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Rendering.LegendRendering;

/// <summary>
/// Single static dispatch from <see cref="LegendPosition"/> enum to the matching
/// <see cref="LegendPositionStrategy"/> subtype. This is the only place in the
/// library where the legend-position switch occurs (replaces the duplicated
/// 14-arm switch that previously appeared in both <c>AxesRenderer.RenderLegend</c>
/// and <c>AxesRenderer.ComputeLegendBounds</c> — a textbook DRY violation).
/// </summary>
public static class LegendPositionStrategyFactory
{
    /// <summary>Returns the strategy instance for the given <see cref="LegendPosition"/>.</summary>
    public static LegendPositionStrategy Create(LegendPosition position) => position switch
    {
        LegendPosition.UpperLeft     => new UpperLeftStrategy(),
        LegendPosition.UpperRight    => new UpperRightStrategy(),
        LegendPosition.LowerRight    => new LowerRightStrategy(),
        LegendPosition.LowerLeft     => new LowerLeftStrategy(),
        LegendPosition.Right         => new RightStrategy(),
        LegendPosition.CenterLeft    => new CenterLeftStrategy(),
        LegendPosition.CenterRight   => new CenterRightStrategy(),
        LegendPosition.LowerCenter   => new LowerCenterStrategy(),
        LegendPosition.UpperCenter   => new UpperCenterStrategy(),
        LegendPosition.Center        => new CenterStrategy(),
        LegendPosition.OutsideRight  => new OutsideRightStrategy(),
        LegendPosition.OutsideLeft   => new OutsideLeftStrategy(),
        LegendPosition.OutsideTop    => new OutsideTopStrategy(),
        LegendPosition.OutsideBottom => new OutsideBottomStrategy(),
        _                            => new BestStrategy(),
    };
}
