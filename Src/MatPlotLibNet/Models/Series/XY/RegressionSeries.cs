// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a polynomial regression series that fits and draws a trend line with optional confidence bands.</summary>
public sealed class RegressionSeries : ChartSeries
{
    /// <summary>Gets the X data values used for fitting.</summary>
    public double[] XData { get; }

    /// <summary>Gets the Y data values used for fitting.</summary>
    public double[] YData { get; }

    /// <summary>Gets or sets the polynomial degree (1 = linear, 2 = quadratic, …). Capped at 10.</summary>
    public int Degree { get; set; } = 1;

    /// <summary>Gets or sets whether to draw the confidence band around the regression line.</summary>
    public bool ShowConfidence { get; set; } = false;

    /// <summary>Gets or sets the confidence level for the band (default 0.95 = 95%).</summary>
    public double ConfidenceLevel { get; set; } = 0.95;

    /// <summary>Gets or sets the width of the regression line in pixels.</summary>
    public double LineWidth { get; set; } = 2.0;

    /// <summary>Gets or sets the line color. If <see langword="null"/>, the current cycle color is used.</summary>
    public Color? Color { get; set; }

    /// <summary>Gets or sets the confidence band fill color. If <see langword="null"/>, the line color with reduced alpha is used.</summary>
    public Color? BandColor { get; set; }

    /// <summary>Gets or sets the opacity of the confidence band fill (0.0 to 1.0).</summary>
    public double BandAlpha { get; set; } = 0.2;

    /// <summary>Gets or sets the line style of the regression line.</summary>
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
