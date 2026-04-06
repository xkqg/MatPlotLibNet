// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a traditional OHLC bar chart with vertical high-low lines and open/close tick marks.</summary>
public sealed class OhlcBarSeries : ChartSeries, IHasDataRange
{
    public double[] Open { get; }
    public double[] High { get; }
    public double[] Low { get; }
    public double[] Close { get; }
    public string[]? DateLabels { get; set; }
    public Color UpColor { get; set; } = Color.Green;
    public Color DownColor { get; set; } = Color.Red;
    public double TickWidth { get; set; } = 0.3;

    public OhlcBarSeries(double[] open, double[] high, double[] low, double[] close)
    {
        Open = open; High = high; Low = low; Close = close;
    }

    /// <inheritdoc />
    public DataRangeContribution ComputeDataRange(IAxesContext context) =>
        new(context.XAxisMin ?? -0.5, context.XAxisMax ?? (Open.Length - 0.5), Low.Min(), High.Max());

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
