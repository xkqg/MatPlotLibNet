// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a stacked area (stackplot) series where multiple Y datasets are stacked vertically with filled areas.</summary>
public sealed class StackedAreaSeries : ChartSeries, IHasAlpha
{
    public double[] X { get; }

    public double[][] YSets { get; }

    public string[]? Labels { get; set; }

    public double Alpha { get; set; } = 0.7;

    public StackedBaseline Baseline { get; set; } = StackedBaseline.Zero;

    public HatchPattern Hatch { get; set; } = HatchPattern.None;

    public Color? HatchColor { get; set; }

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

        // Use the baseline strategy to find the true yMin and yMax
        var baselines = Baseline.ComputeFor(YSets, X.Length);

        double yMin = double.MaxValue;
        double yMax = double.MinValue;

        for (int layer = 0; layer < YSets.Length; layer++)
        {
            for (int i = 0; i < X.Length; i++)
            {
                double bot = baselines[layer][i];
                double top = bot + (i < YSets[layer].Length ? YSets[layer][i] : 0.0);
                if (bot < yMin) yMin = bot;
                if (top > yMax) yMax = top;
            }
        }

        if (yMin == double.MaxValue) yMin = 0;
        if (yMax == double.MinValue) yMax = 1;

        // Stack baseline (Zero) is a hard floor iff all stacked values are non-negative.
        double? stickyYMin = Baseline == StackedBaseline.Zero && yMin >= 0 ? 0 : null;
        return new(xMin, xMax, yMin, yMax,
            StickyXMin: xMin, StickyXMax: xMax, StickyYMin: stickyYMin);
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
