// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a waterfall chart showing how an initial value is affected by sequential positive and negative changes.</summary>
public sealed class WaterfallSeries : ChartSeries
{
    public string[] Categories { get; }
    public double[] Values { get; }
    public Color IncreaseColor { get; set; } = Colors.Green;
    public Color DecreaseColor { get; set; } = Colors.Red;
    public Color TotalColor { get; set; } = Colors.Tab10Blue;
    public double BarWidth { get; set; } = 0.6;

    public WaterfallSeries(string[] categories, double[] values)
    {
        Categories = categories; Values = values;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        double xMin = context.XAxisMin ?? -0.5;
        double xMax = context.XAxisMax ?? (Categories.Length - 0.5);
        double yMin = 0, yMax = 0;
        double cum = 0;
        foreach (var v in Values)
        {
            cum += v;
            if (cum < yMin) yMin = cum;
            if (cum > yMax) yMax = cum;
        }
        if (0 < yMin) yMin = 0;
        return new(xMin, xMax, yMin, yMax);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "waterfall",
        Categories = Categories, Values = Values,
        IncreaseColor = IncreaseColor, DecreaseColor = DecreaseColor,
        TotalColor = TotalColor, BarWidth = BarWidth
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
