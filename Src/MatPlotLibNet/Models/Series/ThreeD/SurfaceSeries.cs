// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a 3D surface plot rendered as colored quadrilaterals with optional wireframe.</summary>
public sealed class SurfaceSeries : GridSeries3D, IColormappable, INormalizable, IColorScaled, IHasAlpha, IHasEdgeColor, ISurfaceAlpha, ISeriesKind<SurfaceSeries>
{
    /// <inheritdoc />
    /// <remarks>A surface is drawn through a colour map, as <c>plot_surface(cmap=...)</c>, which asks neither cycle.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Mesh;

    /// <inheritdoc />
    public IColorMap? ColorMap { get; set => Change(ref field, value); }

    /// <summary>The opacity of the surface's faces, 0 to 1. A surface given none reads <see cref="PatchLook.RestingAlpha"/> and is drawn as its theme says: opaque
    /// under the matplotlib themes, as matplotlib's <c>plot_surface</c> and <c>plot_trisurf</c> draw, <see cref="PatchLook.RestingAlpha"/> under every other. An
    /// opacity that was given, the default among them, is drawn as it is under every theme.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 0, above 1 or not a number.</exception>
    public double Alpha { get => GivenAlpha ?? PatchLook.RestingAlpha; set => GivenAlpha = value.ThrowIfNotOpacity(nameof(Alpha)); }

    /// <summary>The value of <see cref="Alpha"/> its author gave, <see langword="null"/> while none was.</summary>
    internal double? GivenAlpha { get; private set => Change(ref field, value); }

    /// <summary>The opacity the author gave, <see langword="null"/> when none was given and the surface follows its theme.</summary>
    double? IGivenAlpha.GivenAlpha => GivenAlpha;


    /// <summary>Whether the surface's cells are outlined. Set to true, they are outlined under every theme, in
    /// <see cref="EdgeColor"/> or the theme's edge colour; set to false, never. Left alone it reads <see cref="RestingShowWireframe"/> and the surface follows
    /// its theme: outlined when an <see cref="EdgeColor"/> is set, and otherwise not under the matplotlib v2 theme, as
    /// matplotlib's <c>plot_surface</c> draws, in black 1 pt wide under the classic one and in a thin translucent black under
    /// every other theme.</summary>
    public bool ShowWireframe { get => GivenWireframe ?? RestingShowWireframe; set => GivenWireframe = value; }

    private const bool RestingShowWireframe = true;
    /// <summary>Whether the author turned the wireframe on or off, <see langword="null"/> when neither and the surface follows
    /// its theme.</summary>
    internal bool? GivenWireframe { get; private set => Change(ref field, value); }

    /// <inheritdoc />
    public INormalizer? Normalizer { get; set => Change(ref field, value); }

    /// <inheritdoc />
    /// <remarks>The mean heights of the patches drawn at the theme's strides (<c>SurfaceFaces.ColourRangeOf</c>), as matplotlib's
    /// <c>plot_surface</c> autoscales its norm over <c>avg_z</c>.</remarks>
    MinMaxRange IColorScaled.ColourRangeOver(ColorMapping mapping) => SurfaceFaces.ColourRangeOf(this, mapping);

    /// <inheritdoc />
    public Color? EdgeColor { get; set => Change(ref field, value); }

    /// <summary>How many grid rows one drawn patch spans: 1 draws every row, a larger value draws a coarser, faster surface,
    /// each patch over the whole block of rows between two drawn ones. 0 (the default, and any value below 1) leaves it to
    /// matplotlib's rule: 10 when <see cref="ColStride"/> is set; otherwise 10 under the classic theme, and elsewhere as
    /// many rows as keep the patches along a side to 50 at most (every row of a grid up to 50 rows).</summary>
    public int RowStride { get; set => Change(ref field, value); }

    /// <summary>How many grid columns one drawn patch spans: 1 draws every column, a larger value draws a coarser, faster
    /// surface. 0 (the default, and any value below 1) leaves it to matplotlib's rule, as <see cref="RowStride"/> says.</summary>
    public int ColStride { get; set => Change(ref field, value); }

    /// <summary>The faces as the last draw made them, kept for the next draw; see <see cref="SurfaceFaces"/>.</summary>
    [RenderCache]
    internal SurfaceFaces? DrawnFaces;

    /// <summary>Initializes a new surface series with the specified grid data.</summary>
    public SurfaceSeries(double[] x, double[] y, double[,] z) : base(x, y, z) { }

    static string ISeriesKind<SurfaceSeries>.Kind => "surface";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        XData = X,
        YData = Y,
        ZGridData = Z.To2DList(),
        ShowWireframe = GivenWireframe,
        RowStride = RowStride > 0 ? RowStride : null,
        ColStride = ColStride > 0 ? ColStride : null,
        Alpha = GivenAlpha,
        ColorScale = this.ColorScaleOnTheWire(),
        EdgeColor = EdgeColor,
        Label = Label,
    };

    /// <summary>The reader of a <see cref="SurfaceSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static SurfaceSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var z = dto.ZGridData.From2DList();
        var s = axes.Surface(dto.XData ?? [0.0, 1.0], dto.YData ?? [0.0, 1.0],
            z.GetLength(0) > 0 ? z : new double[,] { { 0, 0 }, { 0, 0 } });
        if (dto.ShowWireframe is { } showWireframe) s.ShowWireframe = showWireframe;
        if (dto.RowStride is { } rowStride) s.RowStride = rowStride;
        if (dto.ColStride is { } colStride) s.ColStride = colStride;
        if (dto.Alpha is { } alpha) s.Alpha = alpha;
        s.RestoreColorScale(dto);
        s.EdgeColor = dto.EdgeColor;
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
