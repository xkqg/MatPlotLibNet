// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Base class for series plotted on Cartesian X/Y coordinates.</summary>
public abstract class XYSeries : ChartSeries, IPriceSeries
{
    /// <summary>Gets the X-axis data values.</summary>
    public double[] XData { get; }

    /// <summary>Gets the Y-axis data values.</summary>
    public double[] YData { get; }

    /// <inheritdoc />
    public double[] PriceData => YData;

    /// <summary>
    /// Gets or sets the maximum number of data points to display after downsampling.
    /// When <c>null</c> (default), all points are rendered.
    /// When set, LTTB downsampling is applied after viewport culling to keep at most this many points.
    /// </summary>
    public int? MaxDisplayPoints { get; set; }

    /// <summary>Initializes with X and Y data arrays.</summary>
    protected XYSeries(double[] xData, double[] yData) { XData = xData; YData = yData; }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        new(XData.Min(), XData.Max(), YData.Min(), YData.Max());
}
