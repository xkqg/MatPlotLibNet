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

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
