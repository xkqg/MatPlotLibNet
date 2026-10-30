// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a point plot series that shows the mean ± confidence interval for each category.</summary>
public sealed class PointplotSeries : DatasetSeries, IHasColor
{
    /// <summary>The name of each category, in the order of the datasets; it names the rows of the series' data table.
    /// <see langword="null"/> numbers the categories from 1.</summary>
    public string[]? Categories { get; set; }

    /// <inheritdoc />
    public Color? Color { get; set; }

    /// <summary>Diameter of the dot that marks the mean of each category, in pixels. Default 8.</summary>
    public double MarkerSize { get; set; } = 8;

    /// <summary>Width of the caps at the two ends of each interval, in category units (neighbouring categories are 1
    /// apart). Default 0.2.</summary>
    public double CapSize { get; set; } = 0.2;

    /// <summary>The share of the observations of each category that its interval covers, between 0 and 1: the default
    /// 0.95 runs from the 2.5th to the 97.5th percentile. A category with fewer than two observations gets an interval
    /// of zero length.</summary>
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
