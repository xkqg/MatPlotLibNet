// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a polynomial regression series that fits and draws a trend line with optional confidence bands.</summary>
public sealed class RegressionSeries : ChartSeries, IHasColor
{
    public double[] XData { get; }

    public double[] YData { get; }

    public int Degree { get; set; } = 1;

    public bool ShowConfidence { get; set; } = false;

    public double ConfidenceLevel { get; set; } = 0.95;

    public double LineWidth { get; set; } = 2.0;

    public Color? Color { get; set; }

    public Color? BandColor { get; set; }

    public double BandAlpha { get; set; } = 0.2;

    public LineStyle LineStyle { get; set; } = LineStyle.Solid;

    /// <summary>Initializes a new instance of <see cref="RegressionSeries"/> with the specified X and Y data.</summary>
    public RegressionSeries(double[] xData, double[] yData)
    {
        XData = xData;
        YData = yData;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (XData.Length == 0) return new(0, 1, 0, 1);
        return new(XData.Min(), XData.Max(), YData.Min(), YData.Max());
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "regression",
        XData = XData, YData = YData,
        Degree = Degree == 1 ? null : Degree,
        ShowConfidence = ShowConfidence ? true : null,
        ConfidenceLevel = ConfidenceLevel == 0.95 ? null : ConfidenceLevel,
        LineWidth = LineWidth,
        Color = Color,
        FillColor = BandColor,
        Alpha = BandAlpha == 0.2 ? null : BandAlpha,
        LineStyle = LineStyle == LineStyle.Solid ? null : LineStyle.ToString().ToLowerInvariant()
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
