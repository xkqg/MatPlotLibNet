// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a donut chart — a pie chart with a hollow center for displaying a summary value.</summary>
public sealed class DonutSeries : WedgeSeries, ISeriesKind<DonutSeries>, IGivenTextSize
{
    /// <summary>The hole a new donut leaves, as a fraction of the outer radius.</summary>
    private const double RestingInnerRadius = 0.4;
    /// <summary>Radius of the hole as a fraction of the outer radius: 0 is a full pie, values towards 1 give a thinner
    /// ring. Default <see cref="RestingInnerRadius"/>.</summary>
    public double InnerRadius { get; set => Change(ref field, value); } = RestingInnerRadius;
    /// <summary>Text drawn in bold in the middle of the hole, such as a total; <see langword="null"/> (the default)
    /// draws none.</summary>
    public string? CenterText { get; set => Change(ref field, value); }

    /// <summary>Initializes a new <see cref="DonutSeries"/> with the given slice sizes.</summary>
    /// <param name="sizes">Fractional or absolute size of each slice; they will be normalized automatically.</param>
    public DonutSeries(double[] sizes) : base(sizes)
    {
    }

    static string ISeriesKind<DonutSeries>.Kind => "donut";

    /// <summary>The size of the text in the hole (<see cref="CenterText"/>), in pixels (the unit of <see cref="Font.Size"/>). Default <see cref="RestingFontSize"/>. Given no size, the text in the hole (<see cref="CenterText"/>)
    /// is the size of the theme's text under the matplotlib themes, as matplotlib draws the text it writes beside its data
    /// (<c>font.size</c>), and <see cref="RestingFontSize"/> under every other theme; a size that is given, the default included, is drawn as given.</summary>
    public double FontSize { get => GivenFontSize ?? RestingFontSize; set => GivenFontSize = value; }

    /// <summary>The size the text in the hole takes under a theme that names none.</summary>
    private const double RestingFontSize = 14;

    /// <summary>The size that was given, <see langword="null"/> when none was.</summary>
    internal double? GivenFontSize { get; private set => Change(ref field, value); }

    double? IGivenTextSize.GivenTextSize => GivenFontSize;

    double IGivenTextSize.RestingTextSize => RestingFontSize;

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => WedgesOnTheWire() with
    {
        Type = this.WireKind(),
        FontSize = GivenFontSize,
        InnerRadius = InnerRadius.OrNullAtRest(RestingInnerRadius),
        CenterText = CenterText,
    };

    /// <summary>The reader of a <see cref="DonutSeries"/> that <see cref="SeriesRegistry"/> takes; it also carries inner radius and center text.</summary>
    internal static DonutSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Donut(dto.Sizes ?? [], dto.PieLabels);
        s.RestoreWedges(dto);
        if (dto.InnerRadius is { } innerRadius) s.InnerRadius = innerRadius;
        s.CenterText = dto.CenterText;
        if (dto.FontSize is { } fontSize) s.FontSize = fontSize;
        return s;
    }


    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
