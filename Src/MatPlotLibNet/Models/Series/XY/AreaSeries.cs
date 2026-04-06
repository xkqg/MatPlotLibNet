// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a filled area series, rendering the region between a line and a baseline (or between two Y datasets).</summary>
public sealed class AreaSeries : XYSeries, ISeriesSerializable
{
    /// <summary>Gets or sets the optional secondary Y data for fill-between-two-curves mode. When null, fills to y=0.</summary>
    public double[]? YData2 { get; set; }

    /// <summary>Gets or sets the line and fill color.</summary>
    public Color? Color { get; set; }

    /// <summary>Gets or sets the fill opacity (0.0 = transparent, 1.0 = opaque).</summary>
    public double Alpha { get; set; } = 0.3;

    /// <summary>Gets or sets the top edge line style.</summary>
    public LineStyle LineStyle { get; set; } = LineStyle.Solid;

    /// <summary>Gets or sets the top edge line width.</summary>
    public double LineWidth { get; set; } = 1.5;

    /// <summary>Gets or sets an optional separate fill color. When null, uses <see cref="Color"/> with <see cref="Alpha"/>.</summary>
    public Color? FillColor { get; set; }

    /// <summary>Creates a new area series from the given X and Y data.</summary>
    public AreaSeries(double[] xData, double[] yData) : base(xData, yData) { }

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
    public SeriesDto ToSeriesDto() => new()
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
