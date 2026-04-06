// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Specifies where the step transition occurs relative to adjacent data points.</summary>
public enum StepPosition
{
    /// <summary>Step occurs before the data point (step at the beginning of the interval).</summary>
    Pre,

    /// <summary>Step occurs at the midpoint between data points.</summary>
    Mid,

    /// <summary>Step occurs after the data point (step at the end of the interval).</summary>
    Post
}

/// <summary>Represents a step-function line series.</summary>
public sealed class StepSeries : ChartSeries, IHasDataRange
{
    /// <summary>Gets the X-axis data values.</summary>
    public double[] XData { get; }

    /// <summary>Gets the Y-axis data values.</summary>
    public double[] YData { get; }

    /// <summary>Gets or sets where the step transition occurs.</summary>
    public StepPosition StepPosition { get; set; } = StepPosition.Post;

    /// <summary>Gets or sets the line color.</summary>
    public Color? Color { get; set; }

    /// <summary>Gets or sets the line style.</summary>
    public LineStyle LineStyle { get; set; } = LineStyle.Solid;

    /// <summary>Gets or sets the line width.</summary>
    public double LineWidth { get; set; } = 1.5;

    /// <summary>Gets or sets the optional marker style at each data point.</summary>
    public MarkerStyle? Marker { get; set; }

    /// <summary>Creates a new step series from the given X and Y data.</summary>
    public StepSeries(double[] xData, double[] yData)
    {
        XData = xData;
        YData = yData;
    }

    /// <inheritdoc />
    public DataRangeContribution ComputeDataRange(IAxesContext context) =>
        new(XData.Min(), XData.Max(), YData.Min(), YData.Max());

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
