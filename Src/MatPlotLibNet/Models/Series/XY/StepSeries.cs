// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
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
public sealed class StepSeries : XYSeries, IHasColor
{
    public StepPosition StepPosition { get; set; } = StepPosition.Post;

    public Color? Color { get; set; }

    public LineStyle LineStyle { get; set; } = LineStyle.Solid;

    public double LineWidth { get; set; } = 1.5;

    public MarkerStyle? Marker { get; set; }

    /// <summary>Creates a new step series from the given X and Y data.</summary>
    public StepSeries(double[] xData, double[] yData) : base(xData, yData) { }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "step",
        XData = XData, YData = YData, Color = Color,
        LineStyle = LineStyle.ToString().ToLowerInvariant(),
        LineWidth = LineWidth,
        StepPosition = StepPosition.ToString().ToLowerInvariant()
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
