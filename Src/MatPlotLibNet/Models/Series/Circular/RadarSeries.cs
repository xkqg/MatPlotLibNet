// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a radar (spider) chart series displaying multi-axis categorical data.</summary>
/// <remarks>The web is a polygon with the first spoke at the top and the categories running clockwise. Under the library's own themes
/// its five rings stand at fifths of <see cref="MaxValue"/>, unlabelled. Under the matplotlib themes they stand at the ticks matplotlib's
/// default locator places over the span from 0 to <see cref="MaxValue"/>, each labelled, in the theme's grid, and the outer polygon is
/// drawn once more over the data as a frame in the style of a spine. Several radar series on one axes share one web under the matplotlib
/// themes, as matplotlib's polar axes has one radial axis: one scale, the largest outer value among them, with the web drawn under the
/// first and the frame over the last. Under the library's own themes each draws its own web on its own <see cref="MaxValue"/>.</remarks>
public sealed class RadarSeries : ChartSeries, IHasColor, IHasAlpha, ISeriesKind<RadarSeries>
{
    /// <inheritdoc />
    internal override bool DrawsWithoutAxes => true;

    /// <summary>The name of each spoke of the radar, printed at its tip. The chart needs at least three to be
    /// drawn.</summary>
    public string[] Categories { get; }

    /// <summary>The value on each spoke, in the order of <see cref="Categories"/>. A value that cannot be drawn (NaN or
    /// infinite) is skipped, and one above <see cref="MaxValue"/> is held at the outer ring.</summary>
    public double[] Values { get; }

    /// <inheritdoc />
    public Color? Color { get; set => Change(ref field, value); }

    /// <summary>The fill of the polygon that joins the values; <see langword="null"/> (the default) fills it with the
    /// series colour at <see cref="Alpha"/>, and a colour set here is used as given.</summary>
    public Color? FillColor { get; set => Change(ref field, value); }

    /// <inheritdoc />
    public double Alpha { get; set => Change(ref field, value.ThrowIfNotOpacity(nameof(Alpha))); } = 0.25;

    private const double RestingLineWidth = 2.0;
    /// <summary>Width of the outline of the polygon, in pixels. Default <see cref="RestingLineWidth"/>.</summary>
    public double LineWidth { get; set => Change(ref field, value); } = RestingLineWidth;

    /// <summary>The value that reaches the outer ring; <see langword="null"/> (the default) uses the largest value of
    /// the series. Under the matplotlib themes the radars of one axes share one outer ring, the largest of theirs.</summary>
    public double? MaxValue { get; set => Change(ref field, value); }

    /// <summary>The value this series' outer ring stands for: <see cref="MaxValue"/>, else the largest drawable value; 1 when there is
    /// none, or when it is 0 or below.</summary>
    internal double OuterValue
    {
        get
        {
            double outer = MaxValue ?? Values.DrawableRange()?.Hi ?? 1;
            return outer <= 0 ? 1 : outer;
        }
    }

    /// <summary>Creates a new radar series from the given categories and values.</summary>
    public RadarSeries(string[] categories, double[] values)
    {
        Categories = categories;
        Values = values;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) => default;

    static string ISeriesKind<RadarSeries>.Kind => "radar";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        Categories = Categories, Values = Values,
        Color = Color, FillColor = FillColor,
        Alpha = Alpha, LineWidth = LineWidth, MaxValue = MaxValue
    };

    /// <summary>The reader of a <see cref="RadarSeries"/> that <see cref="SeriesRegistry"/> takes; it also carries fill colour and max value.</summary>
    internal static RadarSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Radar(dto.Categories ?? [], dto.Values ?? []);
        s.Color = dto.Color;
        s.FillColor = dto.FillColor;
        if (dto.Alpha is { } alpha) s.Alpha = alpha;
        if (dto.LineWidth is { } lineWidth) s.LineWidth = lineWidth;
        s.MaxValue = dto.MaxValue;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>The categories as text, then the values under the series' own label.</remarks>
    public override ChartDataTable? ToDataTable() => ChartDataTable.ByCategory(Categories, new NumberColumn(Label ?? "value", Values));

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
