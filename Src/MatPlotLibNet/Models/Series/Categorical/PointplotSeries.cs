// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a point plot series that shows the mean ± confidence interval for each category.</summary>
public sealed class PointplotSeries : DatasetSeries, IHasColor
{
    public string[]? Categories { get; set; }

    public Color? Color { get; set; }

    public double MarkerSize { get; set; } = 8;

    public double CapSize { get; set; } = 0.2;

    public double ConfidenceLevel { get; set; } = 0.95;

    /// <summary>Initializes a new instance of <see cref="PointplotSeries"/> with the specified datasets.</summary>
    /// <param name="datasets">An array of datasets, each containing values for one category.</param>
    public PointplotSeries(double[][] datasets) : base(datasets) { }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "pointplot",
        Datasets = Datasets,
        Categories = Categories,
        Color = Color,
        MarkerSize = MarkerSize,
        CapSize = CapSize,
        ConfidenceLevel = ConfidenceLevel
    };

    /// <summary>Reconstructs a <see cref="PointplotSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static PointplotSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Pointplot(dto.Datasets ?? []);
        if (dto.MarkerSize.HasValue)
        {
            s.MarkerSize = dto.MarkerSize.Value;
        }
        if (dto.CapSize.HasValue)
        {
            s.CapSize = dto.CapSize.Value;
        }
        if (dto.ConfidenceLevel.HasValue)
        {
            s.ConfidenceLevel = dto.ConfidenceLevel.Value;
        }
        if (dto.Color.HasValue)
        {
            s.Color = dto.Color.Value;
        }
        if (dto.Categories is not null)
        {
            s.Categories = dto.Categories;
        }
        return s;
    }


    /// <inheritdoc />
    /// <remarks>Long form over the samples, named by the category the point plot draws them under.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        var rows = new List<IReadOnlyList<DataCell>>();
        for (int d = 0; d < Datasets.Length; d++)
        {
            string name = Categories is { } categories && d < categories.Length
                ? categories[d]
                : (d + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
            var label = DataCell.FromText(name);
            foreach (var value in Datasets[d])
            {
                rows.Add([label, DataCell.FromNumber(value)]);
            }
        }

        return new ChartDataTable(null, [new("category", DataColumnKind.Text), new(Label ?? "value")], rows);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
