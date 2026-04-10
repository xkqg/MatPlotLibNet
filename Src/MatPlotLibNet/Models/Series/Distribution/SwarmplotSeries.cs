// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a swarm plot series that draws non-overlapping dots per category using beeswarm layout.</summary>
public sealed class SwarmplotSeries : ChartSeries
{
    /// <summary>Gets the array of datasets, one per category.</summary>
    public double[][] Datasets { get; }

    /// <summary>Gets or sets the radius of each dot in pixels.</summary>
    public double MarkerSize { get; set; } = 5;

    /// <summary>Gets or sets the color of the dots. If <see langword="null"/>, the current cycle color is used.</summary>
    public Color? Color { get; set; }

    /// <summary>Gets or sets the opacity of the dots (0.0 to 1.0).</summary>
    public double Alpha { get; set; } = 0.8;

    /// <summary>Initializes a new instance of <see cref="SwarmplotSeries"/> with the specified datasets.</summary>
    /// <param name="datasets">An array of datasets, each containing values for one category.</param>
    public SwarmplotSeries(double[][] datasets)
    {
        Datasets = datasets;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (Datasets.Length == 0) return new(0, 1, 0, 1);
        double yMin = Datasets.SelectMany(d => d).DefaultIfEmpty(0).Min();
        double yMax = Datasets.SelectMany(d => d).DefaultIfEmpty(1).Max();
        return new(-0.5, Datasets.Length - 0.5, yMin, yMax);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "swarmplot",
        Datasets = Datasets,
        MarkerSize = MarkerSize,
        Color = Color,
        Alpha = Alpha
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
