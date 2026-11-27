// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>The one drawing of the patches of an axes, whatever its coordinate system: a hidden patch (<see cref="Patch.Visible"/>)
/// is not drawn, the style is the patch's own (<see cref="Patch.DrawnStyleUnder"/>), and in a picture with interactive attributes a
/// patch with a legend row is drawn in the group that row toggles (<see cref="RenderContextMarkupExtensions"/>'
/// <c>OpenPatchGroup</c>: the slot after every series of the axes, <see cref="SeriesSlotExtensions"/>' <c>FirstPatchSlot</c>, plus
/// its place in <see cref="Axes.Patches"/>). Where the outline stands is the axes' own say (<see cref="IDataPlacement"/>). Made for one
/// render of one axes.</summary>
internal readonly struct PatchDrawing
{
    private readonly Axes _axes;
    private readonly IRenderContext _ctx;
    private readonly Theme _theme;
    private readonly SvgEmit _emit;
    private readonly int _firstSlot;

    /// <summary>The drawing of the patches of <paramref name="axes"/> on <paramref name="ctx"/> under <paramref name="theme"/>.</summary>
    /// <param name="axes">The axes whose patches are drawn.</param>
    /// <param name="ctx">The surface drawn on.</param>
    /// <param name="theme">The theme the figure is drawn under.</param>
    internal PatchDrawing(Axes axes, IRenderContext ctx, Theme theme)
    {
        _axes = axes;
        _ctx = ctx;
        _theme = theme;
        _emit = SvgEmit.Of(axes, ctx);
        _firstSlot = _emit.InteractiveAttributes ? axes.FirstPatchSlot : 0;
    }

    /// <summary>The outline of the patch at <paramref name="index"/> in pixels, placed by <paramref name="placement"/>
    /// (<see cref="PathSegmentExtensions"/>' <c>ToPixels</c>); empty for a hidden patch and one that cannot be drawn.</summary>
    /// <param name="index">The patch's place in <see cref="Axes.Patches"/>.</param>
    /// <param name="placement">Where the axes places a data point.</param>
    /// <returns>The outline in pixels.</returns>
    /// <typeparam name="TPlace">The kind of placement.</typeparam>
    internal IReadOnlyList<PathSegment> Placed<TPlace>(int index, TPlace placement) where TPlace : IDataPlacement
    {
        var patch = _axes.Patches[index];
        return patch.Visible ? patch.Outline().ToPixels(placement) : [];
    }

    /// <summary>Draws <paramref name="path"/>, the outline of the patch at <paramref name="index"/> in pixels, in the patch's style
    /// and its group.</summary>
    /// <param name="index">The patch's place in <see cref="Axes.Patches"/>.</param>
    /// <param name="path">Its outline in pixels (<see cref="Placed{TPlace}"/>), not empty.</param>
    internal void Draw(int index, IReadOnlyList<PathSegment> path)
    {
        bool grouped = OpenGroup(index);
        _ctx.DrawPath(path, _axes.Patches[index].DrawnStyleUnder(_theme));
        if (grouped) _ctx.EndGroup();
    }

    /// <summary>Places the patch at <paramref name="index"/> by <paramref name="placement"/> and draws it, cut off at
    /// <paramref name="clip"/> when one is given, unless it is hidden or cannot be drawn.</summary>
    /// <param name="index">The patch's place in <see cref="Axes.Patches"/>.</param>
    /// <param name="placement">Where the axes places a data point.</param>
    /// <param name="clip">Where the patch is cut off, when it can reach past the plot; <see langword="null"/> for no clip.</param>
    /// <typeparam name="TPlace">The kind of placement.</typeparam>
    internal void Draw<TPlace>(int index, TPlace placement, ClipRegion? clip = null) where TPlace : IDataPlacement
    {
        var path = Placed(index, placement);
        if (path.Count == 0) return;

        using var scope = clip is { } region ? _ctx.ClippedTo(region) : default;
        Draw(index, path);
    }

    /// <summary>Opens the group the patch at <paramref name="index"/> is drawn in, when the picture is interactive and the patch has
    /// a legend row.</summary>
    /// <param name="index">The patch's place in <see cref="Axes.Patches"/>.</param>
    /// <returns><see langword="true"/> when a group was opened; the caller closes it with <see cref="IRenderContext.EndGroup"/>.</returns>
    internal bool OpenGroup(int index)
    {
        var patch = _axes.Patches[index];
        return patch.HasLegendRow && _ctx.OpenPatchGroup(_emit, _firstSlot + index, patch.Label!);
    }
}
