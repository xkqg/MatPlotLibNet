// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a triangulated surface from unstructured (x, y, z) point data.</summary>
public sealed class Trisurf3DSeries : XYZSeries, IColormappable, INormalizable, IColorScaled, IHasAlpha, IHasEdgeColor, IHasColor, ISurfaceAlpha, ISeriesKind<Trisurf3DSeries>
{
    /// <inheritdoc />
    /// <remarks><c>plot_trisurf</c> asks the lines cycle and makes a <c>Poly3DCollection</c>; one drawn through a colour map asks neither.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.LineFaces3D.NamingItsColours(ColorMap is not null);

    /// <summary>Colormap applied to the Z values of each triangle face. Overrides <see cref="Color"/> when set.</summary>
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


    /// <summary>Whether the triangles are outlined. Set to true, they are outlined under every theme, in
    /// <see cref="EdgeColor"/> or the theme's edge colour; set to false, never. Left alone it reads <see cref="RestingShowWireframe"/> and the surface follows
    /// its theme: outlined when an <see cref="EdgeColor"/> is set, and otherwise not under the matplotlib v2 theme, as
    /// matplotlib's <c>plot_trisurf</c> draws, in black 1 pt wide under the classic one and in a thin translucent black under
    /// every other theme.</summary>
    public bool ShowWireframe { get => GivenWireframe ?? RestingShowWireframe; set => GivenWireframe = value; }

    private const bool RestingShowWireframe = true;
    /// <summary>Whether the author turned the wireframe on or off, <see langword="null"/> when neither and the surface follows
    /// its theme.</summary>
    internal bool? GivenWireframe { get; private set => Change(ref field, value); }

    /// <summary>The colour of the triangles' outlines. When <see langword="null"/>, the theme's outline is drawn
    /// (see <see cref="ShowWireframe"/>).</summary>
    public Color? EdgeColor { get; set => Change(ref field, value); }

    /// <summary>Normalizer for mapping Z values to the colormap range.</summary>
    public INormalizer? Normalizer { get; set => Change(ref field, value); }

    /// <inheritdoc />
    /// <remarks>The heights of the points; a triangle is coloured by the mean of its three.</remarks>
    MinMaxRange IColorScaled.ColourRangeOver(ColorMapping mapping) => mapping.RangeOf(Z.Data);

    /// <summary>Fallback solid color when no colormap is assigned.</summary>
    public Color? Color { get; set => Change(ref field, value); }

    /// <summary>Initializes a new triangulated surface series with the specified data.</summary>
    public Trisurf3DSeries(Vec x, Vec y, Vec z) : base(x, y, z) { }

    static string ISeriesKind<Trisurf3DSeries>.Kind => "trisurf";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        XData = X,
        YData = Y,
        ZData = Z,
        Color = Color,
        ShowWireframe = GivenWireframe,
        Alpha = GivenAlpha,
        ColorScale = this.ColorScaleOnTheWire(),
        EdgeColor = EdgeColor,
        Label = Label,
    };

    /// <summary>The reader of a <see cref="Trisurf3DSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static Trisurf3DSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Trisurf(dto.XData ?? [0.0], dto.YData ?? [0.0], dto.ZData ?? [0.0]);
        if (dto.Color is { } color) s.Color = color;
        if (dto.ShowWireframe is { } showWireframe) s.ShowWireframe = showWireframe;
        if (dto.Alpha is { } alpha) s.Alpha = alpha;
        s.RestoreColorScale(dto);
        s.EdgeColor = dto.EdgeColor;
        return s;
    }


    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
