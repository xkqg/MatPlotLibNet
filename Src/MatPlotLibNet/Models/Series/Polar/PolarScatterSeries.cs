// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a scatter plot in polar coordinates (r, theta).</summary>
public sealed class PolarScatterSeries : PolarSeries, IHasColor, ISeriesKind<PolarScatterSeries>
{
    /// <inheritdoc />
    /// <remarks>matplotlib's <c>scatter</c> on a polar axes.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Scatter;

    /// <inheritdoc />
    public Color? Color { get; set => Change(ref field, value); }

    /// <summary>The size of each point marker, in points. Default <see cref="MarkerLook.RestingSize"/>. Given no size, the marker is the size its theme says (20 points
    /// squared under the classic theme); a size that is given, the default included, is drawn as given.</summary>
    public double MarkerSize { get => GivenMarkerSize ?? MarkerLook.RestingSize; set => GivenMarkerSize = value; }

    /// <summary>The marker size that was given, <see langword="null"/> when none was and the marker is the size its theme says.</summary>
    internal double? GivenMarkerSize { get; private set => Change(ref field, value); }

    /// <inheritdoc />
    internal override LegendSwatch LegendSwatch => new(LegendSwatchKind.Scatter, MarkerSize: GivenMarkerSize);

    /// <summary>Initializes a new polar scatter series.</summary>
    public PolarScatterSeries(double[] r, double[] theta) : base(r, theta) { }

    static string ISeriesKind<PolarScatterSeries>.Kind => "polarscatter";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        RData = R,
        ThetaData = Theta,
        Color = Color,
        MarkerSize = GivenMarkerSize,
    };

    /// <summary>The reader of a <see cref="PolarScatterSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static PolarScatterSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.PolarScatter(dto.RData ?? [], dto.ThetaData ?? []);
        s.Color = dto.Color;
        if (dto.MarkerSize is { } markerSize) s.MarkerSize = markerSize;
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
