// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.CartesianParts;

/// <summary>What every part of one Cartesian axes render draws with: the axes, its plot area, the context it draws into, the active
/// theme, the colour slot of each of its series and the transform its primary series are drawn with. The renderer makes it once a render
/// and builds every part from it, so no part is handed them one by one.</summary>
/// <param name="Axes">The axes being drawn.</param>
/// <param name="Pass">The plot area, the context the parts draw into and measure with, and the active theme: the renderer's own pass
/// (<see cref="AxesRenderer.Pass"/>), which a part draws its series through (<see cref="SeriesPass.OverPlot"/>).</param>
/// <param name="ColorSlots">The colour slot of each series of the axes in identity order (<see cref="SeriesSlotExtensions.ColorSlots"/>), made
/// once by the renderer (<see cref="AxesRenderer.ColorSlots"/>), so no part makes a table of its own.</param>
/// <param name="Transform">The transform the primary series are drawn with; a part of an axis past the primary draws through its own
/// (<see cref="PlacedAxis.Transform"/>).</param>
internal readonly record struct CartesianPartContext(Axes Axes, RenderPass Pass, int[] ColorSlots, DataTransform Transform)
{
    /// <summary>The plot area, in pixels.</summary>
    internal Rect PlotArea => Pass.PlotArea;

    /// <summary>The context the parts draw into and measure with.</summary>
    internal IRenderContext Ctx => Pass.Context;

    /// <summary>The active theme.</summary>
    internal Theme Theme => Pass.Theme;
}
