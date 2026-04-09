// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a vector field (quiver) series with arrows at each grid point.</summary>
public sealed class QuiverSeries : ChartSeries
{
    /// <summary>Gets the X-axis positions of the arrow origins.</summary>
    public double[] XData { get; }

    /// <summary>Gets the Y-axis positions of the arrow origins.</summary>
    public double[] YData { get; }

    /// <summary>Gets the X-component of each arrow vector.</summary>
    public double[] UData { get; }

    /// <summary>Gets the Y-component of each arrow vector.</summary>
    public double[] VData { get; }

    /// <summary>Gets or sets the arrow color.</summary>
    public Color? Color { get; set; }

    /// <summary>Gets or sets the vector scaling factor.</summary>
    public double Scale { get; set; } = 1.0;

    /// <summary>Gets or sets the arrowhead size as a fraction of the arrow length.</summary>
    public double ArrowHeadSize { get; set; } = 0.3;

    /// <summary>Creates a new quiver series from the given position and vector data.</summary>
    public QuiverSeries(double[] xData, double[] yData, double[] uData, double[] vData)
    {
        XData = xData;
        YData = yData;
        UData = uData;
        VData = vData;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        double xMin = double.MaxValue, xMax = double.MinValue;
        double yMin = double.MaxValue, yMax = double.MinValue;
        for (int i = 0; i < XData.Length; i++)
        {
            double x0 = XData[i], x1 = x0 + UData[i] * Scale;
            double y0 = YData[i], y1 = y0 + VData[i] * Scale;
            if (Math.Min(x0, x1) < xMin) xMin = Math.Min(x0, x1);
            if (Math.Max(x0, x1) > xMax) xMax = Math.Max(x0, x1);
            if (Math.Min(y0, y1) < yMin) yMin = Math.Min(y0, y1);
            if (Math.Max(y0, y1) > yMax) yMax = Math.Max(y0, y1);
        }
        return new(xMin, xMax, yMin, yMax);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "quiver",
        XData = XData, YData = YData,
        UData = UData, VData = VData,
        Color = Color, Scale = Scale, ArrowHeadSize = ArrowHeadSize
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
