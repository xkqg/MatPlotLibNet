// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a filled area series, rendering the region between a line and a baseline (or between two Y datasets).</summary>
public sealed class AreaSeries : XYSeries, IHasColor, IHasAlpha, IHasEdgeColor
{
    public double[]? YData2 { get; set; }

    public Color? Color { get; set; }

    public double Alpha { get; set; } = 0.3;

    public LineStyle LineStyle { get; set; } = LineStyle.Solid;

    public double LineWidth { get; set; } = 1.5;

    public Color? FillColor { get; set; }

    public HatchPattern Hatch { get; set; } = HatchPattern.None;

    public Color? HatchColor { get; set; }

    public Color? EdgeColor { get; set; }

    public DrawStyle StepMode { get; set; } = DrawStyle.Default;

    /// <summary>Creates a new area series from the given X and Y data.</summary>
    /// <remarks>ZOrder defaults to -1 so fills render behind all other series (ZOrder 0).</remarks>
    public AreaSeries(double[] xData, double[] yData) : base(xData, yData) { ZOrder = -1; }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        double yMin = YData.Min(), yMax = YData.Max();
        if (YData2 is not null)
        {
            yMin = Math.Min(yMin, YData2.Min());
            yMax = Math.Max(yMax, YData2.Max());
        }
        else if (0 < yMin) yMin = 0;
        return new(XData.Min(), XData.Max(), yMin, yMax);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "area",
        XData = XData, YData = YData, YData2 = YData2,
        Color = Color, Alpha = Alpha,
        LineStyle = LineStyle.ToString().ToLowerInvariant(),
        LineWidth = LineWidth
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
