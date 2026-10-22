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
    /// <remarks>Two columns: the x values, and the y values under the series' own label.</remarks>
    public override ChartDataTable? ToDataTable() =>
        ChartDataTable.FromNumberColumns(
            [new("x", Axis: DataAxis.X), new(Label ?? "y", Axis: DataAxis.Y)],
            [XData, YData]);

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        // Empty series contributes no data range.
        if (XData.Length == 0 || YData.Length == 0)
            return new DataRangeContribution(null, null, null, null);
        // Over the values the axis can place: a missing value (NaN) and an infinity are left out, so one gap cannot
        // collapse the range; on a LOG axis a 0 or a negative is masked out too, so one window that priced 0 cannot
        // drive the floor negative and blank the whole panel. A null context (a series ranged outside any axes) reads
        // as linear.
        var x = XData.DrawableRange(context?.XScale ?? AxisScale.Linear);
        var y = YData.DrawableRange(context?.YScale ?? AxisScale.Linear);
        return new(x?.Lo, x?.Hi, y?.Lo, y?.Hi);
    }
}
