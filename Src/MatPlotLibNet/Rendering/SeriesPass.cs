// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Lighting;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>One pass over series drawn into one plot area: what every series in it is drawn through, and the one loop that draws them
/// (<see cref="DrawSteps"/>). A pass is made by its factories, which say what kind of pass it is: <see cref="OverPlot"/> over a Cartesian
/// plot (the primary series, a Y axis stacked past the primary, the top X axis), <see cref="OnUnitSquare"/> for a pass whose series place
/// their marks themselves (polar, ternary, 3-D) ordered by their own z-order, and <see cref="Layered"/> for a polar or ternary pass whose
/// grid and patches stand among its series where the theme places them. The group, the clip and the renderer a series gets, the slot it
/// is numbered by and what is drawn inside its group are decided here, once.</summary>
internal readonly record struct SeriesPass
{
    private SeriesPass(Axes axes, RenderPass render, DataTransform transform)
    {
        Axes = axes;
        Render = render;
        Transform = transform;
        OrderedBy = render.Theme;
    }

    /// <summary>A pass over a Cartesian plot through <paramref name="transform"/>: each series' group carries the transform as the
    /// mapping a data cursor converts a click with, its marks are cut off at the plot area of <paramref name="render"/> (inside
    /// <paramref name="outline"/> when it is told one) when the axes clips its marks, and its series stand where the theme places them.
    /// The primary series of <paramref name="axes"/> unless it is handed another seat's (<see cref="Series"/>, <see cref="Pair"/>,
    /// <see cref="FirstIndexInPair"/>).</summary>
    /// <param name="axes">The axes that owns the series.</param>
    /// <param name="render">The plot area, the context and the theme.</param>
    /// <param name="transform">The transform from data to pixels.</param>
    /// <param name="outline">The outline of a map the primary series stand in, inside the plot; <see langword="null"/> (the default)
    /// cuts the marks off at the plot area alone.</param>
    /// <returns>The pass.</returns>
    internal static SeriesPass OverPlot(Axes axes, RenderPass render, DataTransform transform, IReadOnlyList<PathSegment>? outline = null) =>
        new SeriesPass(axes, render, transform) { ClickMapping = transform, Clip = new ClipRegion(render.PlotArea, outline) };

    /// <summary>A pass whose series place their marks themselves (polar, ternary, 3-D) through the unit square over the plot area, in
    /// the order of their own z-order (<see cref="Theme.Default"/>'s placing), <paramref name="patches"/> among them, a patch given no
    /// z-order under every series; its groups carry no mapping and nothing is clipped unless it is told a clip.</summary>
    /// <param name="axes">The axes that owns the series.</param>
    /// <param name="render">The plot area, the context and the theme.</param>
    /// <param name="patches">The patches drawn among the series; <see langword="null"/> for none.</param>
    /// <returns>The pass.</returns>
    internal static SeriesPass OnUnitSquare(Axes axes, RenderPass render, IReadOnlyList<Patch>? patches) =>
        new SeriesPass(axes, render, UnitSquare(render.PlotArea)) { Patches = patches, OrderedBy = Theme.Default };

    /// <summary>The mapping a pass over the unit square draws through (<see cref="OnUnitSquare"/>): 0 to 1 on both axes across
    /// <paramref name="plotArea"/>, the one a polar or 3-D axes also collects the marks of its series through for a legend at Best.</summary>
    internal static DataTransform UnitSquare(Rect plotArea) => new DataTransform(0, 1, 0, 1, plotArea);

    /// <summary>The pass of a polar or ternary axes (<see cref="OnUnitSquare"/>) whose grid, patches and series stand where the theme
    /// places them, the grid first among its equals, as a Cartesian axes orders its data layer (<see cref="ZOrderExtensions"/>).</summary>
    /// <param name="axes">The axes that owns the series.</param>
    /// <param name="render">The plot area, the context and the theme.</param>
    /// <param name="patches">The patches drawn among the series; <see langword="null"/> for none.</param>
    /// <returns>The pass.</returns>
    internal static SeriesPass Layered(Axes axes, RenderPass render, IReadOnlyList<Patch>? patches) =>
        OnUnitSquare(axes, render, patches) with { GridZ = render.Theme.GridZ, OrderedBy = render.Theme };

    /// <summary>The axes that owns the series.</summary>
    internal Axes Axes { get; }

    /// <summary>The plot area, the context and the theme the series are drawn into.</summary>
    internal RenderPass Render { get; }

    /// <summary>The transform from data to pixels the series are drawn through.</summary>
    internal DataTransform Transform { get; }

    /// <summary>The mapping a series' group carries for a data cursor to convert a click with: <see cref="Transform"/> over a Cartesian
    /// plot; <see langword="null"/> for a pass whose series place their marks themselves.</summary>
    internal DataTransform? ClickMapping { get; private init; }

    /// <summary>Where the marks of each series are cut off when the axes clips its marks (<see cref="SeriesGroupExtensions"/>, the one
    /// clip rule; the series' text never is): the plot area of a Cartesian pass, or the outline of a map under the matplotlib themes, the
    /// disc or ring of a polar one; <see langword="null"/> for a pass whose marks cannot reach past its plot (ternary, 3-D).</summary>
    internal ClipRegion? Clip { get; init; }

    /// <summary>The radial scale of a polar pass; <see langword="null"/> for any other.</summary>
    internal RadialScale? Radial { get; init; }

    /// <summary>The projection of a 3-D pass; <see langword="null"/> for any other.</summary>
    internal Projection3D? Projection { get; init; }

    /// <summary>The light a 3-D pass shades its faces with; <see langword="null"/> for none.</summary>
    internal ILightSource? Light { get; init; }

    /// <summary>Whether a 3-D pass writes the vertex data a browser turns the scene with.</summary>
    internal bool Emit3D { get; init; }

    /// <summary>The queue a 3-D pass sorts its faces in across series; <see langword="null"/> for none.</summary>
    internal DepthQueue3D? DepthQueue { get; init; }

    /// <summary>The series this pass draws, in the order they were added, which a series that stands for another one drawn before it
    /// looks in (a quiver key, <see cref="SeriesRenderContext.AxesSeries"/>): the primary series of <see cref="Axes"/> unless the pass is
    /// handed another seat's (a Y axis slot's own, the top X axis' own), so a key stands for a quiver on its own axis.</summary>
    internal IReadOnlyList<ISeries> Series { get => field ?? Axes.Series; init; }

    /// <summary>The series list that numbers the series of this pass (<see cref="SeriesSlotExtensions.SlotIndex"/>): the primary's unless
    /// the pass draws another seat.</summary>
    internal SeriesAxisPair Pair { get; init; }

    /// <summary>The index the first series of this pass has in <see cref="Pair"/>'s list: the series of every slot nearer the plot on
    /// the same list stand before it.</summary>
    internal int FirstIndexInPair { get; init; }

    /// <summary>The patches drawn among the series (<see cref="Order"/>); <see langword="null"/> for none, as on a Cartesian axes, which
    /// draws its patches in its own data layer.</summary>
    internal IReadOnlyList<Patch>? Patches { get; private init; }

    /// <summary>Where the grid drawn among the series stands in the z-order; <see langword="null"/> for a pass whose grid is drawn on its
    /// own.</summary>
    internal double? GridZ { get; private init; }

    /// <summary>The theme whose z-order rule orders the steps (<see cref="ZOrderExtensions"/>): the pass' own theme where the theme places
    /// the series, <see cref="Theme.Default"/> where they stand by their own <see cref="ISeries.ZOrder"/>.</summary>
    internal Theme OrderedBy { get; init; }

    /// <summary>The steps of this pass in the order they are drawn in (<see cref="LayerOrder"/>): its grid (<see cref="GridZ"/>), its
    /// patches and its series, each at the z-order <see cref="OrderedBy"/> gives it, in the order they were added for a tie. The caller
    /// hands the order back (<see cref="LayerOrder.Return"/>).</summary>
    /// <returns>The order.</returns>
    internal LayerOrder Order()
    {
        var patches = Patches;
        var series = Series;
        int patchCount = patches?.Count ?? 0;
        var order = new LayerOrder((GridZ is null ? 0 : 1) + patchCount + series.Count);
        if (GridZ is { } gridZ) order.Add(gridZ, DataLayer.Grid, 0);
        for (int i = 0; i < patchCount; i++) order.Add(OrderedBy.ZOf(patches![i]), DataLayer.Patch, i);
        for (int i = 0; i < series.Count; i++) order.Add(OrderedBy.ZOf(series[i]), DataLayer.Series, i);
        order.Sort();
        return order;
    }

    /// <summary>Draws every step of this pass in its order (<see cref="Order"/>) through <see cref="DrawSteps{THost}"/>.</summary>
    /// <typeparam name="THost">The kind of host: a renderer, or a value on the stack of a render.</typeparam>
    /// <param name="host">The renderer the pass draws for.</param>
    internal void Draw<THost>(ref THost host) where THost : ILayerHost
    {
        var order = Order();
        try
        {
            DrawSteps(order.Steps, ref host);
        }
        finally
        {
            order.Return();
        }
    }

    /// <summary>The one loop every pass draws its steps by: a series step draws that series of <see cref="Series"/> unless it is hidden,
    /// in its group (<see cref="SeriesGroupExtensions"/>) keyed by its identity slot (<see cref="Pair"/>, <see cref="FirstIndexInPair"/>),
    /// clipped to <see cref="Clip"/>, in the colour its slot holds, then what belongs inside its group (the host's
    /// <see cref="ILayerHost.DrawInsideSeriesGroup"/>: a series' end label; a series of the top X axis holds none), then the group closed;
    /// any other step is the host's (<see cref="ILayerHost.DrawLayerAt"/>).</summary>
    /// <typeparam name="THost">The kind of host: a renderer, or a value on the stack of a render (the Cartesian data layer), handed by
    /// reference so it is never copied or boxed.</typeparam>
    /// <param name="steps">The steps, in the order they are drawn in.</param>
    /// <param name="host">The renderer the pass draws for.</param>
    internal void DrawSteps<THost>(ReadOnlySpan<LayerStep> steps, ref THost host) where THost : ILayerHost
    {
        for (int k = 0; k < steps.Length; k++)
        {
            if (steps[k].Layer == DataLayer.Series) DrawSeriesOf(steps[k].Index, ref host);
            else k = host.DrawLayerAt(this, steps, k);
        }
    }

    /// <summary>Draws series <paramref name="index"/> of <see cref="Series"/> in its group, unless it is hidden.</summary>
    private void DrawSeriesOf<THost>(int index, ref THost host) where THost : ILayerHost
    {
        var series = Series[index];
        if (!series.Visible) return;
        int slot = Axes.SlotIndex(Pair, FirstIndexInPair + index);
        bool openedGroup = DrawInItsGroup(series, slot, host.ColorSlots[slot]);
        host.DrawInsideSeriesGroup(slot);
        if (openedGroup) Render.Context.EndGroup();
    }

    /// <summary>Draws <paramref name="series"/> inside its group: opens the group (<see cref="SeriesGroupExtensions"/>), clips its marks
    /// to <see cref="Clip"/>, draws them in the colour at <paramref name="colorSlot"/>, and ends the clip. The group stays open. A series
    /// drawn in the 3-D scene of the pass (<see cref="DepthQueue"/>; its kind is drawn in 3-D, <c>ChartSeries.DrawnIn</c>) hands its
    /// marks to the scene's queue, which opens the group around each run of them (<see cref="SeriesGroup"/>), so no group is opened here;
    /// one that hands it no mark has its group held by the queue, which writes it where the series stands among the others. A flat series
    /// on a 3-D axes is drawn at its turn in its group, as on any axes.</summary>
    /// <param name="series">The series, visible.</param>
    /// <param name="slotIndex">The series' identity slot, which its group carries.</param>
    /// <param name="colorSlot">The first colour slot the series holds.</param>
    /// <returns><see langword="true"/> when a group is open.</returns>
    private bool DrawInItsGroup(ISeries series, int slotIndex, int colorSlot)
    {
        var ctx = Render.Context;
        var plotArea = Render.PlotArea;
        var scene = series is ChartSeries { DrawnIn: CoordinateSystem.ThreeD } ? DepthQueue : null;
        SeriesGroup? sceneGroup = scene is null ? null : new SeriesGroup(Axes, series, slotIndex);
        int queued = scene?.MarkCount ?? 0;
        bool openedGroup = sceneGroup is null && ctx.BeginSeriesGroup(Axes, series, slotIndex, ClickMapping);
        using var clipScope = Clip is { } clip ? ctx.ClippedForSeries(Axes, series, clip, Render.Theme) : default;
        var slot = Render.Theme.SeriesSlot(colorSlot);
        var renderer = new SvgSeriesRenderer(new SeriesRenderContext(Transform, ctx, slot.UnnamedColorOf(series), new RenderArea(plotArea, ctx))
        {
            TooltipsEnabled = Axes.EnableTooltips,
            CycledProps = slot.CycledProperties,
            Projection3D = Projection,
            LightSource = Light,
            Emit3DData = Emit3D,
            Theme = Render.Theme,
            DepthQueue = scene,
            SceneGroup = sceneGroup,
            RadialScale = Radial,
            AxesSeries = Series,
            FirstColorSlot = colorSlot,
            Stretch = Render.Stretch,
        });
        series.Accept(renderer, new RenderArea(plotArea, ctx));
        if (sceneGroup is { } group && scene!.MarkCount == queued) scene.HoldGroup(group);
        return openedGroup;
    }
}

