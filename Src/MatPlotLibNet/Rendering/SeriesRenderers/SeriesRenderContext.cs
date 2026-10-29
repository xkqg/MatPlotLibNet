// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Lighting;
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

    /// <summary>What the theme hands out at this series' slot: its colour and the cycler's set. A renderer asks it how
    /// its series is stroked (<see cref="SeriesSlot.LineStyleOf"/>, <see cref="SeriesSlot.LineWidthOf"/>,
    /// <see cref="SeriesSlot.LineMarkerOf"/>, <see cref="SeriesSlot.ScatterMarkerOf"/>), the same owner the legend asks.</summary>
    internal SeriesSlot Slot => Theme.ThemedSlot(SeriesColor, CycledProps);

    public Projection3D? Projection3D { get; init; }

    public ILightSource? LightSource { get; init; }

    public bool Emit3DData { get; init; }

    /// <summary>
    /// Optional shared depth sink. When set, 3-D series renderers must push their
    /// drawable primitives into this queue instead of calling <c>Ctx.Draw*</c> directly,
    /// so the axes renderer can sort across all 3-D series before painting.
    /// </summary>
    public DepthQueue3D? DepthQueue { get; init; }

    /// <summary>The active visual theme, used by renderers for theme-specific defaults (e.g. matplotlib patch edge color).</summary>
    public Styling.Theme Theme { get; init; } = Styling.Theme.Default;

    /// <summary>The radial scale of the polar axes the series is drawn on, worked out once per paint by the axes; none on any
    /// other axes, and none when a renderer is invoked directly (a polar series then scales itself, alone).</summary>
    internal RadialScale? RadialScale { get; init; }

    /// <summary>The first colour slot the series holds (<see cref="SeriesSlotExtensions.ColorSlots"/>): a series that colours
    /// its own parts takes the slots from here on, one for each part, so the parts of a series drawn after another wear the
    /// colours past the ones before it. 0 for a renderer invoked on its own.</summary>
    internal int FirstColorSlot { get; init; }
}
