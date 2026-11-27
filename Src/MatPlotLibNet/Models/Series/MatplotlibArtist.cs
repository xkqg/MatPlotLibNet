// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Where a theme takes the colour of a kind of series from when its author names none and it holds no place in a colour
/// group: a look of the theme that is made for that kind, or none (the slot's colour).</summary>
internal enum ThemeColorRole : byte
{
    /// <summary>None: the series takes the colour of its slot.</summary>
    None,

    /// <summary>The body colour of a violin (<see cref="Styling.Theme.ViolinBodyColor"/>), whether or not the violin is in a colour group.</summary>
    ViolinBody,

    /// <summary>The colour of a quiver, its key and barbs (<see cref="Styling.VectorFieldLook.Color"/>).</summary>
    VectorField,

    /// <summary>The face of a scatter marker (<see cref="Styling.ScatterLook.Face"/>).</summary>
    ScatterFace,

    /// <summary>The outline of a box (<see cref="Styling.BoxLook.Outline"/>).</summary>
    BoxOutline,
}

/// <summary>What a kind of series is to matplotlib: the call its colour is asked with (which of the axes' two cycles it takes it from,
/// <see cref="CycleCall"/>), the zorder of the artist its verb draws, where the theme takes its colour from where its author names
/// none, and whether the artist is cut off at the axes. A fact of the kind, so a kind declares it once (<see cref="ChartSeries.Artist"/>)
/// and the colour slots, the z-order, the slot's colour and the clip all read that one value; a new kind states its artist and edits
/// nothing else. Values were read from matplotlib 3.11.2: a <c>Line2D</c> is at <c>zorder = 2</c>, a <c>Patch</c>, a <c>Collection</c>
/// and a <c>QuadMesh</c> at 1, an <c>AxesImage</c> at 0, a <c>LineCollection</c> at 2; every artist an axes adds is <c>clip_on</c> True
/// but the wedges of a pie and a table.</summary>
/// <param name="Call">How the artist's verb asks the axes for a colour.</param>
/// <param name="ZOrder">The zorder of the artist, which the matplotlib themes draw a series given no z-order at.</param>
/// <param name="ThemeColor">Where the theme takes the colour of the series from where its author names none.</param>
/// <param name="ClipOn">Whether the marks of the artist are cut off at the axes (matplotlib's <c>clip_on</c>), which the matplotlib
/// themes clip a series by (<see cref="Styling.AxesFrameLook.ClipsEveryArtist"/>): True for every artist an axes adds, a kind of the
/// library's own too (a treemap, a gauge, a Sankey diagram, a relative rotation graph), whose names and values are text and stand whole
/// past the plot, as every text does (<see cref="Rendering.MarkClips{TClip}"/>).</param>
internal readonly record struct MatplotlibArtist(CycleCall Call, double ZOrder, ThemeColorRole ThemeColor, bool ClipOn = true)
{
    /// <summary>Whether matplotlib draws the artist as a <c>Collection</c> (printed with 3.11.2): a fill between two curves, a stackplot's
    /// layers and a violin's bodies (<c>FillBetweenPolyCollection</c>), a mesh, a hexbin, a broken bar and a contour set, a scatter's
    /// <c>PathCollection</c>, a <c>LineCollection</c> and a <c>Quiver</c>; not a <c>Line2D</c>, a <c>Patch</c>, an <c>AxesImage</c> or a
    /// <c>QuiverKey</c>. The outline of a collection takes the join of its theme, a patch keeps the mitre
    /// (<see cref="Styling.ResolvedPatchEdge.StyleOf(Styling.Color, Styling.PatchEdge, Styling.HatchFill)"/>).</summary>
    internal bool Collection { get; init; }

    /// <summary>A <c>Line2D</c> (<c>plot</c>, <c>step</c>, <c>errorbar</c>, <c>axhline</c>): the lines cycle, at 2.</summary>
    internal static MatplotlibArtist Line { get; } = new(CycleCall.Line, 2, ThemeColorRole.None);

    /// <summary>A kind with no artist of matplotlib's of its own (a table, a treemap, a gauge, a waffle, a Sankey diagram, a stacked
    /// area, whose layers are matplotlib's <c>stackplot</c>, <c>clip_on</c> True as printed with 3.11.2 under both styles): it takes the
    /// next colour of the lines cycle like any series, stands at 1, a patch's, and is cut off at the axes as every artist an axes adds.</summary>
    internal static MatplotlibArtist Own { get; } = new(CycleCall.Line, 1, ThemeColorRole.None);

    /// <summary>A <c>Patch</c> (<c>bar</c>, <c>pie</c>, <c>fill</c>, <c>bar3d</c>, <c>voxels</c>): the patch cycle in every mode, at 1. Also
    /// the zorder of a patch of an axes.</summary>
    internal static MatplotlibArtist Patch { get; } = new(CycleCall.Patch, 1, ThemeColorRole.None);

    /// <summary>The faces of <c>bar3d</c> and <c>voxels</c>: a <c>Poly3DCollection</c> (printed with 3.11.2) whose colour is asked of the
    /// patch cycle, as a patch's, at 1; a collection, so its outline is joined as a collection's (<c>Axes3D.bar</c>'s flat bars are
    /// <c>Patch3D</c>, <see cref="Patch"/>).</summary>
    internal static MatplotlibArtist PatchFaces3D { get; } = Patch with { Collection = true };

    /// <summary>The faces of <c>plot_trisurf</c>: a <c>Poly3DCollection</c> (printed with 3.11.2) whose colour is the next of the lines cycle
    /// (<c>self._get_lines.get_next_color()</c>), at 1, a collection's.</summary>
    internal static MatplotlibArtist LineFaces3D { get; } = new(CycleCall.Line, 1, ThemeColorRole.None) { Collection = true };

    /// <summary>The wedges of a <c>pie</c> (a donut is a pie whose wedges have a width): patches, which <c>Axes.pie</c> draws with
    /// <c>clip_on=False</c> (printed with 3.11.2 under both styles: the wedges and their texts <c>clip_on False</c>), so a label or an
    /// exploded wedge is drawn whole past the axes.</summary>
    internal static MatplotlibArtist PieWedge { get; } = Patch with { ClipOn = false };

    /// <summary>The layers of a <c>stackplot</c>: a kind of its own (<see cref="Own"/>: the next colour of the lines cycle, at 1), each layer a
    /// <c>FillBetweenPolyCollection</c> (printed with 3.11.2), so its outline is joined as a collection's.</summary>
    internal static MatplotlibArtist StackPlot { get; } = Own with { Collection = true };

    /// <summary>A <c>table</c>: a kind of its own (<see cref="Own"/>: the next colour of the lines cycle), which <c>Axes.table</c> draws
    /// with <c>clip_on=False</c> (printed with 3.11.2: the table and its cells <c>clip_on False</c>), so it stands whole beside, below or
    /// over the axes, at 0 (<c>Table</c> keeps an artist's zorder, printed 0 under both styles), under every other artist of the axes.</summary>
    internal static MatplotlibArtist Table { get; } = Own with { ZOrder = 0, ClipOn = false };

    /// <summary>A <c>PolyCollection</c> of <c>fill_between</c>: the patch cycle, none in classic mode, at 1.</summary>
    internal static MatplotlibArtist FillBetween { get; } = new(CycleCall.FillBetween, 1, ThemeColorRole.None) { Collection = true };

    /// <summary>A <c>PathCollection</c> of <c>scatter</c>: the patch cycle, none in classic mode, at 1, with the face the theme's scatter look names.</summary>
    internal static MatplotlibArtist Scatter { get; } = new(CycleCall.Scatter, 1, ThemeColorRole.ScatterFace) { Collection = true };

    /// <summary>The dots seaborn draws with <c>scatter</c> (a strip plot, a swarm plot) and a streaming scatter: asked like a scatter, but
    /// the theme's scatter look does not name their face.</summary>
    internal static MatplotlibArtist Dots { get; } = new(CycleCall.Scatter, 1, ThemeColorRole.None) { Collection = true };

    /// <summary>A collection coloured by a colour map or by data, whose verb asks the axes for no colour (<c>pcolormesh</c>, <c>hexbin</c>,
    /// <c>contourf</c>, <c>plot_surface</c>, <c>broken_barh</c>): no cycle, at 1.</summary>
    internal static MatplotlibArtist Mesh { get; } = new(CycleCall.Neither, 1, ThemeColorRole.None) { Collection = true };

    /// <summary>An <c>AxesImage</c> (<c>imshow</c>, <c>specgram</c>, a heat map): no cycle, at 0, under every other artist.</summary>
    internal static MatplotlibArtist Image { get; } = new(CycleCall.Neither, 0, ThemeColorRole.None);

    /// <summary>A <c>LineCollection</c> (<c>stem</c>, <c>eventplot</c>, <c>contour</c>, <c>tricontour</c>): no cycle, at 2.</summary>
    internal static MatplotlibArtist LineCollection { get; } = new(CycleCall.Neither, 2, ThemeColorRole.None) { Collection = true };

    /// <summary>A <c>Quiver</c> or <c>Barbs</c>: no cycle, at 1, in the colour the theme's vector-field look names.</summary>
    internal static MatplotlibArtist VectorField { get; } = new(CycleCall.Neither, 1, ThemeColorRole.VectorField) { Collection = true };

    /// <summary>A <c>QuiverKey</c>: as a quiver, drawn over it, at 1.1; an artist of its own, no collection.</summary>
    internal static MatplotlibArtist VectorFieldKey { get; } = VectorField with { ZOrder = 1.1, Collection = false };

    /// <summary>A <c>boxplot</c>: no cycle, at 2, in the outline the theme's box look names.</summary>
    internal static MatplotlibArtist Box { get; } = new(CycleCall.Neither, 2, ThemeColorRole.BoxOutline);

    /// <summary>A <c>violinplot</c>: the lines cycle, none in classic mode, at 1, in the body colour the theme names.</summary>
    internal static MatplotlibArtist Violin { get; } = new(CycleCall.Violin, 1, ThemeColorRole.ViolinBody) { Collection = true };

    /// <summary>This artist where its colours are named by the series' own data (a colour array, a colour map): it asks the axes for no
    /// colour, whatever else it is.</summary>
    /// <param name="named">Whether the series names its colours.</param>
    /// <returns>This artist, asking no cycle when <paramref name="named"/>.</returns>
    internal MatplotlibArtist NamingItsColours(bool named) => named ? this with { Call = CycleCall.Neither } : this;
}
