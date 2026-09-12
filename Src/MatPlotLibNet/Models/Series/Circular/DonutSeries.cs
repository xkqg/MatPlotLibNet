// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a donut chart — a pie chart with a hollow center for displaying a summary value.</summary>
public sealed class DonutSeries : ChartSeries
{
    public double[] Sizes { get; }
    public string[]? Labels { get; set; }
    public Color[]? Colors { get; set; }
    public double InnerRadius { get; set; } = 0.4;
    public string? CenterText { get; set; }
    public double StartAngle { get; set; } = 90;

    /// <summary>Initializes a new <see cref="DonutSeries"/> with the given slice sizes.</summary>
    /// <param name="sizes">Fractional or absolute size of each slice; they will be normalized automatically.</param>
    public DonutSeries(double[] sizes) => Sizes = sizes;

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        new(null, null, null, null);

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "donut",
        Sizes = Sizes, PieLabels = Labels,
        InnerRadius = InnerRadius, CenterText = CenterText,
        StartAngle = StartAngle
    };

    /// <summary>Reconstructs a <see cref="DonutSeries"/> from its serialization DTO, including inner radius and center text, and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static DonutSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Donut(dto.Sizes ?? [], dto.PieLabels);
        if (dto.InnerRadius.HasValue)
        {
            s.InnerRadius = dto.InnerRadius.Value;
        }
        s.CenterText = dto.CenterText;
        if (dto.StartAngle.HasValue)
        {
            s.StartAngle = dto.StartAngle.Value;
        }
        return s;
    }


    /// <inheritdoc />
    /// <remarks>One row per slice: its label - or "slice N" when the series carries none - and its size.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        var rows = new IReadOnlyList<DataCell>[Sizes.Length];
        for (int i = 0; i < Sizes.Length; i++)
        {
            string label = Labels is { } labels && i < labels.Length
                ? labels[i]
                : "slice " + (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
            rows[i] = [DataCell.FromText(label), DataCell.FromNumber(Sizes[i])];
        }

        return new ChartDataTable(null, [new("label", DataColumnKind.Text), new(Label ?? "size")], rows);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
