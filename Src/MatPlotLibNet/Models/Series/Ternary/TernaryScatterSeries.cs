// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>A scatter plot in a ternary diagram: a marker on each composition of three parts.</summary>
public sealed class TernaryScatterSeries : TernarySeries, IHasColor, ISeriesKind<TernaryScatterSeries>
{
    /// <inheritdoc />
    /// <remarks>mpltern's ternary <c>scatter</c>, which is matplotlib's.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Scatter;

    /// <summary>The colour of the markers; the theme's colour for this series when <see langword="null"/>.</summary>
    public Color? Color { get; set => Change(ref field, value); }

    /// <summary>The diameter of a marker, in points; <see cref="MarkerLook.RestingSize"/> unless said otherwise. Given no size, the marker is the size its theme says
    /// (20 points squared under the classic theme); a size that is given, the default included, is drawn as given.</summary>
    public double MarkerSize { get => GivenMarkerSize ?? MarkerLook.RestingSize; set => GivenMarkerSize = value; }

    /// <summary>The marker size that was given, <see langword="null"/> when none was and the marker is the size its theme says.</summary>
    internal double? GivenMarkerSize { get; private set => Change(ref field, value); }

    /// <inheritdoc />
    internal override LegendSwatch LegendSwatch => new(LegendSwatchKind.Scatter, MarkerSize: GivenMarkerSize);

    /// <summary>Initializes a new ternary scatter series. Built through <c>Axes.TernaryScatter</c>, which refuses a sample that
    /// cannot be placed.</summary>
    /// <param name="top">The part at the apex, for each sample.</param>
    /// <param name="left">The part at the bottom left corner, for each sample.</param>
    /// <param name="right">The part at the bottom right corner, for each sample.</param>
    public TernaryScatterSeries(double[] top, double[] left, double[] right) : base(top, left, right) { }

    static string ISeriesKind<TernaryScatterSeries>.Kind => "ternaryscatter";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        TopData = Top,
        LeftData = Left,
        RightData = Right,
        Color = Color,
        MarkerSize = GivenMarkerSize,
    };

    /// <summary>The reader of a <see cref="TernaryScatterSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static TernaryScatterSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.TernaryScatter(dto.TopData ?? [], dto.LeftData ?? [], dto.RightData ?? []);
        s.Color = dto.Color;
        if (dto.MarkerSize is { } markerSize) s.MarkerSize = markerSize;

        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
