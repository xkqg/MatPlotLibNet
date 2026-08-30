// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Base class for series plotted on Cartesian X/Y coordinates.</summary>
public abstract class XYSeries : ChartSeries, IPriceSeries
{
    public virtual double[] XData { get; }

    public double[] YData { get; }

    /// <inheritdoc />
    public double[] PriceData => YData;

    public int? MaxDisplayPoints { get; set; }

    /// <summary>Initializes with X and Y data arrays.</summary>
    protected XYSeries(double[] xData, double[] yData) { XData = xData; YData = yData; }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        // Empty series contributes no data range. Min()/Max() throw on empty.
        if (XData.Length == 0 || YData.Length == 0)
            return new DataRangeContribution(null, null, null, null);
        // On a LOG axis only positive values exist: a 0 or a negative is masked out of the range, so one
        // window that priced 0 cannot drive the floor negative and blank the whole panel (measured 2026-08-30).
        // A null context (a series ranged outside any axes) reads as linear.
        var (xMin, xMax) = context?.XScale == AxisScale.Log ? PositiveRange(XData) : (XData.Min(), XData.Max());
        var (yMin, yMax) = context?.YScale == AxisScale.Log ? PositiveRange(YData) : (YData.Min(), YData.Max());
        return new(xMin, xMax, yMin, yMax);
    }

    /// <summary>The extremes over the POSITIVE values only; (null, null) when there is none.</summary>
    private static (double? Min, double? Max) PositiveRange(double[] data)
    {
        double min = double.MaxValue, max = double.MinValue;
        foreach (var v in data)
        {
            if (v > 0)
            {
                if (v < min) min = v;
                if (v > max) max = v;
            }
        }
        return min == double.MaxValue ? (null, null) : (min, max);
    }
}
