// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents an error bar series showing uncertainty at each data point.</summary>
public sealed class ErrorBarSeries : XYSeries
{
    /// <summary>Gets the lower Y error magnitudes.</summary>
    public double[] YErrorLow { get; }

    /// <summary>Gets the upper Y error magnitudes.</summary>
    public double[] YErrorHigh { get; }

    /// <summary>Gets or sets the optional lower X error magnitudes.</summary>
    public double[]? XErrorLow { get; set; }

    /// <summary>Gets or sets the optional upper X error magnitudes.</summary>
    public double[]? XErrorHigh { get; set; }

    /// <summary>Gets or sets the line and marker color.</summary>
    public Color? Color { get; set; }

    /// <summary>Gets or sets the error bar line width.</summary>
    public double LineWidth { get; set; } = 1.5;

    /// <summary>Gets or sets the cap size in pixels at the ends of error bars.</summary>
    public double CapSize { get; set; } = 5.0;

    /// <summary>Creates a new error bar series from the given data and error magnitudes.</summary>
    public ErrorBarSeries(double[] xData, double[] yData, double[] yErrorLow, double[] yErrorHigh)
        : base(xData, yData)
    {
        YErrorLow = yErrorLow;
        YErrorHigh = yErrorHigh;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        double xMin = XData.Min(), xMax = XData.Max();
        double yMin = double.MaxValue, yMax = double.MinValue;
        for (int i = 0; i < YData.Length; i++)
        {
            double lo = YData[i] - YErrorLow[i];
            double hi = YData[i] + YErrorHigh[i];
            if (lo < yMin) yMin = lo;
            if (hi > yMax) yMax = hi;
        }
        if (XErrorLow is not null && XErrorHigh is not null)
        {
            for (int i = 0; i < XData.Length; i++)
            {
                double lo = XData[i] - XErrorLow[i];
                double hi = XData[i] + XErrorHigh[i];
                if (lo < xMin) xMin = lo;
                if (hi > xMax) xMax = hi;
            }
        }
        return new(xMin, xMax, yMin, yMax);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
