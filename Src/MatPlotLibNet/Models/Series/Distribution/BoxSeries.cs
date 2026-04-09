// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a box plot series displaying statistical distribution summaries.</summary>
public sealed class BoxSeries : ChartSeries
{
    /// <summary>Gets the array of datasets, each containing the values for one box.</summary>
    public double[][] Datasets { get; }

    /// <summary>Gets or sets the fill color of the boxes.</summary>
    public Color? Color { get; set; }

    /// <summary>Gets or sets the color of the median line within each box.</summary>
    public Color? MedianColor { get; set; }

    /// <summary>Gets or sets whether outlier points are displayed.</summary>
    public bool ShowOutliers { get; set; } = true;


    /// <summary>Initializes a new instance of <see cref="BoxSeries"/> with the specified datasets.</summary>
    /// <param name="datasets">An array of datasets, each containing the values for one box.</param>
    public BoxSeries(double[][] datasets)
    {
        Datasets = datasets;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        double xMin = context.XAxisMin ?? -0.5;
        double xMax = context.XAxisMax ?? (Datasets.Length - 0.5);
        double yMin = double.MaxValue, yMax = double.MinValue;
        foreach (var ds in Datasets)
        {
            double dsMin = ds.Min(), dsMax = ds.Max();
            if (dsMin < yMin) yMin = dsMin;
            if (dsMax > yMax) yMax = dsMax;
        }
        return new(xMin, xMax, yMin, yMax);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new() { Type = "box", Datasets = Datasets };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
