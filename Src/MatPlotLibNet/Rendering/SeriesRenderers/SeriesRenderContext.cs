// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Immutable record bundling the four required rendering parameters plus optional settings.</summary>
/// <remarks>Use <c>with</c> expressions to customize: <c>context with { TooltipsEnabled = true }</c>.</remarks>
/// <param name="Transform">Coordinate transform from data space to pixel space.</param>
/// <param name="Ctx">The drawing target (SVG, Skia, MAUI).</param>
/// <param name="SeriesColor">The theme cycle color assigned to this series.</param>
/// <param name="Area">The pixel bounds of the plot area.</param>
public record SeriesRenderContext(
    DataTransform Transform,
    IRenderContext Ctx,
    Color SeriesColor,
    RenderArea Area)
{
    public bool TooltipsEnabled { get; init; }

    public CycledProperties? CycledProps { get; init; }
}
