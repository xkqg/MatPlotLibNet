// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a stacked area (stackplot) series where multiple Y datasets are stacked vertically with filled areas.</summary>
public sealed class StackedAreaSeries : ChartSeries
{
    /// <summary>Gets the shared X values.</summary>
    public double[] X { get; }

    /// <summary>Gets the array of Y arrays, one per stacked layer.</summary>
    public double[][] YSets { get; }

    /// <summary>Gets or sets optional labels for each layer.</summary>
    public string[]? Labels { get; set; }

    /// <summary>Gets or sets the fill opacity (0.0 to 1.0).</summary>
    public double Alpha { get; set; } = 0.7;

    /// <summary>Creates a new stacked area series from shared X values and multiple Y datasets.</summary>
    /// <param name="x">The shared X-axis data values.</param>
    /// <param name="ySets">An array of Y arrays, one per stacked layer.</param>
    public StackedAreaSeries(double[] x, double[][] ySets)
    {
        X = x;
        YSets = ySets;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (X.Length == 0 || YSets.Length == 0)
            return new(0, 1, 0, 1);

        double xMin = X.Min();
        double xMax = X.Max();

        // Compute max cumulative sum at any X position
        double yMax = 0;
        for (int i = 0; i < X.Length; i++)
        {
            double cumulative = 0;
            for (int layer = 0; layer < YSets.Length; layer++)
            {
                if (i < YSets[layer].Length)
                    cumulative += YSets[layer][i];
            }
            if (cumulative > yMax) yMax = cumulative;
        }

        return new(xMin, xMax, 0, yMax);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "stackedarea",
        XData = X,
        Datasets = YSets,
        PieLabels = Labels,
        Alpha = Alpha
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
