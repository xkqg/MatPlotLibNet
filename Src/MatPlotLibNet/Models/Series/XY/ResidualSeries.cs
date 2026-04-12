// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a residual plot series that displays regression residuals as a scatter plot with an optional zero line.</summary>
public sealed class ResidualSeries : ChartSeries, IHasColor
{
    public Vec XData { get; }

    public Vec YData { get; }

    public int Degree { get; set; } = 1;

    public double MarkerSize { get; set; } = 6;

    public Color? Color { get; set; }

    public bool ShowZeroLine { get; set; } = true;

    /// <summary>Initializes a new instance of <see cref="ResidualSeries"/> with the specified X and Y data.</summary>
    /// <param name="xData">The X data values.</param>
    /// <param name="yData">The Y data values.</param>
    public ResidualSeries(Vec xData, Vec yData)
    {
        XData = xData;
        YData = yData;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (XData.Length == 0) return new(0, 1, -1, 1);
        double[] coeffs = LeastSquares.PolyFit(XData, YData, Degree);
        double[] predicted = LeastSquares.PolyEval(coeffs, XData);
        Vec residuals = YData - new Vec(predicted);
        double rMin = residuals.Min();
        double rMax = residuals.Max();
        double padding = Math.Max(Math.Abs(rMax - rMin) * 0.1, 0.1);
        return new(XData.Min(), XData.Max(), rMin - padding, rMax + padding);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "residual",
        XData = XData,
        YData = YData,
        Degree = Degree,
        MarkerSize = MarkerSize,
        Color = Color
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
