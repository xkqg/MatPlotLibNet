// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a histogram series that bins continuous data into discrete intervals.</summary>
public sealed class HistogramSeries : ChartSeries, IHasDataRange
{
    /// <summary>Gets the raw data values to be binned.</summary>
    public double[] Data { get; }

    /// <summary>Gets or sets the number of histogram bins.</summary>
    public int Bins { get; set; } = 10;

    /// <summary>Gets or sets the fill color of the histogram bars.</summary>
    public Color? Color { get; set; }

    /// <summary>Gets or sets the edge color of the histogram bars.</summary>
    public Color? EdgeColor { get; set; }

    /// <summary>Gets or sets the opacity of the histogram bars (0.0 to 1.0).</summary>
    public double Alpha { get; set; } = 0.7;


    /// <summary>Initializes a new instance of <see cref="HistogramSeries"/> with the specified data.</summary>
    /// <param name="data">The raw data values to be binned.</param>
    public HistogramSeries(double[] data)
    {
        Data = data;
    }

    /// <inheritdoc />
    public DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        double yMin = 0, yMax = 0;
        if (Data.Length > 0)
        {
            var bins = ComputeBins();
            if (bins.Counts.Length > 0)
                yMax = bins.Counts.Max();
        }
        return new(Data.Length > 0 ? Data.Min() : 0, Data.Length > 0 ? Data.Max() : 1, yMin, yMax);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);

    /// <summary>Computes histogram bin counts. Used by both range computation and rendering.</summary>
    public HistogramBins ComputeBins()
    {
        if (Data.Length == 0) return new HistogramBins(0, 1, []);

        double min = Data[0], max = Data[0];
        foreach (var v in Data) { if (v < min) min = v; if (v > max) max = v; }

        double binWidth = (max - min) / Bins;
        if (binWidth == 0) binWidth = 1;

        var counts = new int[Bins];
        foreach (var val in Data)
            counts[Math.Min((int)((val - min) / binWidth), Bins - 1)]++;

        return new HistogramBins(min, binWidth, counts);
    }
}

/// <summary>Computed histogram bin data.</summary>
/// <param name="Min">Minimum data value.</param>
/// <param name="BinWidth">Width of each bin.</param>
/// <param name="Counts">Count of values in each bin.</param>
public readonly record struct HistogramBins(double Min, double BinWidth, int[] Counts);
