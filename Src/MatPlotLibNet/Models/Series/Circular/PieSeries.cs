// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a pie chart series displaying proportional data as circular slices.</summary>
public sealed class PieSeries : WedgeSeries, IEdgedPatch, ISeriesKind<PieSeries>
{
    /// <summary>Under the library's own themes a wedge is outlined by one white pixel.</summary>
    PatchEdge IEdgedPatch.LibraryEdge(Color? fill, Color? themeEdge) => PatchEdge.WhitePixel;

    /// <summary>How far each wedge is pulled out from the centre, as a fraction of the radius, by position; a wedge
    /// past the end of the array stays in place. <see langword="null"/> pulls none out.</summary>
    public double[]? Explode { get; set => Change(ref field, value); }

    /// <summary>A .NET format string for the percentage printed inside each wedge, such as <c>"{0:F1}%"</c>, where
    /// <c>{0}</c> is the wedge's share of the total as a number from 0 to 100. <see langword="null"/> (the default)
    /// prints no percentages. The printf form of matplotlib's <c>autopct</c> is read as well: <c>"%.1f%%"</c> prints the
    /// same as <c>"{0:F1}%"</c>, <c>%d</c> prints the share rounded to a whole number, and flags and width are ignored. The text is written in the theme's text colour when that can be read against the wedge's
    /// colour, and in black or white, whichever stands out, when it cannot.</summary>
    public string? AutoPct { get; set => Change(ref field, value); }

    /// <summary>When <see langword="true"/> a soft dark copy of each wedge is drawn slightly offset behind it. Default
    /// <see langword="false"/>.</summary>
    public bool Shadow { get; set => Change(ref field, value); }

    /// <summary>The radius of the pie as a fraction of half the shorter side of the plot; <see langword="null"/> (the
    /// default) uses 0.8 of it, which leaves room for the labels.</summary>
    public double? Radius { get; set => Change(ref field, value); }

    /// <summary>The hatch pattern of each wedge, by position; a wedge past the end of the array, or every wedge when
    /// this is <see langword="null"/>, is left plain.</summary>
    public HatchPattern[]? Hatches { get; set => Change(ref field, value); }

    /// <summary>Initializes a new instance of <see cref="PieSeries"/> with the specified slice sizes.</summary>
    /// <param name="sizes">The numeric sizes of each pie slice.</param>
    public PieSeries(double[] sizes) : base(sizes)
    {
    }

    static string ISeriesKind<PieSeries>.Kind => "pie";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => WedgesOnTheWire() with
    {
        Type = this.WireKind(),
        Explode = Explode,
        AutoPct = AutoPct,
        Shadow = Shadow.OrNullAtRest(false),
        Radius = Radius,
        Hatches = Hatches?.ToList(),
    };

    /// <summary>The reader of a <see cref="PieSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static PieSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Pie(dto.Sizes ?? [], dto.PieLabels);
        s.RestoreWedges(dto);
        s.Explode = dto.Explode;
        s.AutoPct = dto.AutoPct;
        if (dto.Shadow is { } shadow) s.Shadow = shadow;
        s.Radius = dto.Radius;
        s.Hatches = dto.Hatches?.ToArray();
        return s;
    }


    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
