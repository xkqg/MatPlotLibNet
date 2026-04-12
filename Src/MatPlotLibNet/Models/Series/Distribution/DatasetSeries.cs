// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Abstract base for series that operate on multiple datasets (one per category).
/// Provides the shared <see cref="Datasets"/> property and a default <see cref="ComputeDataRange"/>.</summary>
public abstract class DatasetSeries : ChartSeries
{
    public double[][] Datasets { get; }

    /// <summary>Creates a new dataset series.</summary>
    protected DatasetSeries(double[][] datasets)
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
}
