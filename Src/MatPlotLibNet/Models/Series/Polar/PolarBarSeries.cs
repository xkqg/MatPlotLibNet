// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a bar chart in polar coordinates with wedge-shaped bars.</summary>
public sealed class PolarBarSeries : PolarSeries, IHasColor, IHasAlpha, IEdgedPatch, IGivenAlpha, ISeriesKind<PolarBarSeries>
{
    /// <summary>A polar bar gives no edge of its own; the opacity is the one given (<see cref="IGivenAlpha.GivenAlpha"/>), which the edge a theme
    /// decides takes too.</summary>
    GivenPatchEdge IEdgedPatch.GivenEdge => new(null, null, GivenAlpha);

    /// <summary>Under the library's own themes a bar is outlined half a pixel wide in its own colour.</summary>
    PatchEdge IEdgedPatch.LibraryEdge(Color? fill, Color? themeEdge) => PatchEdge.HalfPixel(fill);

    /// <inheritdoc />
    /// <remarks>matplotlib's <c>bar</c> on a polar axes.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Patch;

    /// <summary>The angular width of each wedge, in radians, centred on its angle. Reads the library's own width until one is
    /// given: a bar given none is drawn as wide as its theme says (0.8 under the matplotlib themes, as matplotlib's <c>bar</c> on a
    /// polar axes, and the library's own width under the library's own themes), a width given as it is under every theme. A larger value gives wider wedges, and
    /// bars spaced evenly round a full circle touch when the width is 2π divided by their number.</summary>
    public double BarWidth { get => GivenBarWidth ?? Styling.PolarLook.Library.BarWidth; set => GivenBarWidth = value; }

    /// <summary>The width that was given, <see langword="null"/> when none was and the bar follows its theme
    /// (<see cref="Styling.PolarLook.BarWidth"/>).</summary>
    internal double? GivenBarWidth { get; private set => Change(ref field, value); }

    /// <inheritdoc />
    public Color? Color { get; set => Change(ref field, value); }

    /// <inheritdoc />
    internal override LegendSwatch LegendSwatch => LegendSwatch.EdgedPatch;

    /// <inheritdoc />
    /// <remarks>A polar bar given no alpha reads <see cref="RestingAlpha"/>. It is drawn at that value under the library's own themes and opaque under the matplotlib
    /// themes, as matplotlib's polar <c>bar</c> gives its bars no alpha of their own; an alpha that was given, the default included, is drawn as
    /// it is under every theme.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 0, above 1 or not a number.</exception>
    public double Alpha { get => GivenAlpha ?? RestingAlpha; set => GivenAlpha = value.ThrowIfNotOpacity(nameof(Alpha)); }

    /// <summary>The value of <see cref="Alpha"/> its author gave, <see langword="null"/> while none was.</summary>
    internal double? GivenAlpha { get; private set => Change(ref field, value); }

    /// <summary>The alpha a polar bar reads when none was given.</summary>
    internal const double RestingAlpha = 0.8;

    /// <summary>The alpha that was given, <see langword="null"/> when none was and the bar follows its theme.</summary>
    double? IGivenAlpha.GivenAlpha => GivenAlpha;

    double IGivenAlpha.RestingAlpha => RestingAlpha;

    /// <summary>Initializes a new polar bar series.</summary>
    public PolarBarSeries(double[] r, double[] theta) : base(r, theta) { }

    /// <inheritdoc />
    /// <remarks>A bar stands on the centre, so its radii reach 0 as well as its own end, as matplotlib's <c>bar</c> puts its
    /// bottoms (0) into the data limits.</remarks>
    internal override RadialExtent? RadialExtent =>
        base.RadialExtent is { } reach ? reach with { Min = Math.Min(reach.Min, 0), Max = Math.Max(reach.Max, 0) } : null;

    static string ISeriesKind<PolarBarSeries>.Kind => "polarbar";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        RData = R,
        ThetaData = Theta,
        Color = Color,
        Alpha = GivenAlpha,
        BarWidth = GivenBarWidth,
    };

    /// <summary>The reader of a <see cref="PolarBarSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static PolarBarSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.PolarBar(dto.RData ?? [], dto.ThetaData ?? []);
        s.Color = dto.Color;
        if (dto.Alpha is { } alpha) s.Alpha = alpha;
        if (dto.BarWidth is { } barWidth) s.BarWidth = barWidth;
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
