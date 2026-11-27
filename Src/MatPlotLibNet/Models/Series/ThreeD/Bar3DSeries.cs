// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a 3D bar chart series — rectangular prisms rising from the XY-plane.</summary>
public sealed class Bar3DSeries : XYZSeries, IHasColor, IEdgedPatch, ISeriesKind<Bar3DSeries>
{
    /// <summary>Under the library's own themes a face is outlined by a black hairline; under the matplotlib themes the theme decides, as
    /// it does for any patch (matplotlib's <c>bar3d</c> is a <c>Poly3DCollection</c> with no edge colour of its own: black at 1 pt under
    /// the classic style's <c>patch.force_edgecolor</c>, none under the default style).</summary>
    PatchEdge IEdgedPatch.LibraryEdge(Color? fill, Color? themeEdge) => PatchEdge.HalfPixel(Colors.Black);

    /// <inheritdoc />
    /// <remarks><c>Axes3D.bar3d</c>, which asks the patch cycle and makes a <c>Poly3DCollection</c>.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.PatchFaces3D;

    /// <summary>The side length of each bar's square base, in data units. Each bar starts at its X and Y and
    /// extends this far along both axes. Default 0.5.</summary>
    public double BarWidth { get; set => Change(ref field, value); } = 0.5;

    /// <inheritdoc />
    public Color? Color { get; set => Change(ref field, value); }

    /// <summary>Initializes a new instance of <see cref="Bar3DSeries"/>.</summary>
    public Bar3DSeries(Vec x, Vec y, Vec z) : base(x, y, z) { }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (X.Length == 0) return default;
        // matplotlib bar3d convention: X[i] / Y[i] are the LEFT-FRONT corner of the bar,
        // not the centre. Bar spans [X, X+BarWidth] × [Y, Y+BarWidth] × [0, Z]. This aligns
        // X/Y tick labels with bar edges instead of centres. Z takes the floor 0 in, with no sticky edge: matplotlib's 3-D
        // axes pads below it as below any value. Over the drawable coordinates only: a missing one (NaN) and an infinity
        // are left out.
        var x = X.Data.DrawableRange();
        var y = Y.Data.DrawableRange();
        var z = Z.Data.DrawableRange();
        return new(x?.Lo, x?.Hi + BarWidth, y?.Lo, y?.Hi + BarWidth,
            ZMin: z is { } zFloor ? Math.Min(0, zFloor.Lo) : null, ZMax: z is { } zTop ? Math.Max(0, zTop.Hi) : null);
    }

    static string ISeriesKind<Bar3DSeries>.Kind => "bar3d";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        XData = X,
        YData = Y,
        ZData = Z,
        BarWidth = BarWidth,
        Color = Color
    };

    /// <summary>The reader of a <see cref="Bar3DSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static Bar3DSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Bar3D(dto.XData ?? [], dto.YData ?? [], dto.ZData ?? []);
        if (dto.BarWidth is { } barWidth) s.BarWidth = barWidth;
        if (dto.Color is { } color) s.Color = color;
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
