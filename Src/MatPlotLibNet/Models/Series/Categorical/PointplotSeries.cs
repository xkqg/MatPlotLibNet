// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a point plot series that shows the mean ± confidence interval for each category.</summary>
public sealed class PointplotSeries : ChartSeries
{
    /// <summary>Gets the array of datasets, one per category.</summary>
    public double[][] Datasets { get; }

    /// <summary>Gets or sets optional category labels.</summary>
    public string[]? Categories { get; set; }

    /// <summary>Gets or sets the color of the point markers and CI lines. If <see langword="null"/>, cycle color is used.</summary>
    public Color? Color { get; set; }

    /// <summary>Gets or sets the radius of each point marker in pixels.</summary>
    public double MarkerSize { get; set; } = 8;

    /// <summary>Gets or sets the width of the CI cap relative to category spacing.</summary>
    public double CapSize { get; set; } = 0.2;

    /// <summary>Gets or sets the confidence level for the interval (e.g. 0.95 for 95% CI).</summary>
    public double ConfidenceLevel { get; set; } = 0.95;

    /// <summary>Initializes a new instance of <see cref="PointplotSeries"/> with the specified datasets.</summary>
    /// <param name="datasets">An array of datasets, each containing values for one category.</param>
    public PointplotSeries(double[][] datasets)
    {
        Datasets = datasets;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (Datasets.Length == 0) return new(0, 1, 0, 1);
        var all = Datasets.SelectMany(d => d).ToArray();
        double yMin = all.Length > 0 ? all.Min() : 0;
        double yMax = all.Length > 0 ? all.Max() : 1;
        return new(-0.5, Datasets.Length - 0.5, yMin, yMax);
    }

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
