// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a radar (spider) chart series displaying multi-axis categorical data.</summary>
public sealed class RadarSeries : ChartSeries, IHasColor, IHasAlpha
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
    public Color? Color { get; set; }

    /// <summary>The fill of the polygon that joins the values; <see langword="null"/> (the default) fills it with the
    /// series colour at <see cref="Alpha"/>, and a colour set here is used as given.</summary>
    public Color? FillColor { get; set; }

    /// <inheritdoc />
    public double Alpha { get; set; } = 0.25;

    /// <summary>Width of the outline of the polygon, in pixels. Default 2.</summary>
    public double LineWidth { get; set; } = 2.0;

    /// <summary>The value that reaches the outer ring; <see langword="null"/> (the default) uses the largest value of
    /// the series.</summary>
    public double? MaxValue { get; set; }

    /// <summary>Creates a new radar series from the given categories and values.</summary>
    public RadarSeries(string[] categories, double[] values)
    {
        Categories = categories;
        Values = values;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        new(null, null, null, null);

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "radar",
        Categories = Categories, Values = Values,
        Color = Color, FillColor = FillColor,
        Alpha = Alpha, LineWidth = LineWidth, MaxValue = MaxValue
    };

    /// <summary>Reconstructs a <see cref="RadarSeries"/> from its serialization DTO, including fill colour and max value, and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static RadarSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Radar(dto.Categories ?? [], dto.Values ?? []);
        s.Color = dto.Color;
        s.FillColor = dto.FillColor;
        if (dto.Alpha.HasValue)
        {
            s.Alpha = dto.Alpha.Value;
        }
        s.LineWidth = dto.LineWidth ?? 2.0;
        s.MaxValue = dto.MaxValue;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>The categories as text, then the values under the series' own label.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        int count = Math.Min(Categories.Length, Values.Length);
        var rows = new IReadOnlyList<DataCell>[count];
        for (int i = 0; i < count; i++)
        {
            rows[i] = [DataCell.FromText(Categories[i]), DataCell.FromNumber(Values[i])];
        }

        return new ChartDataTable(null, [new("category", DataColumnKind.Text), new(Label ?? "value")], rows);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
