// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a histogram series that bins continuous data into discrete intervals.</summary>
public sealed class HistogramSeries : ChartSeries
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

    /// <summary>Gets or sets whether to normalize bin heights to a probability density (area sums to 1).</summary>
    public bool Density { get; set; } = false;

    /// <summary>Gets or sets whether to display a cumulative histogram (prefix sum of bin counts).</summary>
    public bool Cumulative { get; set; } = false;

    /// <summary>Gets or sets the histogram rendering style (Bar, Step, or StepFilled).</summary>
    public HistType HistType { get; set; } = HistType.Bar;

    /// <summary>Gets or sets per-sample weights applied when computing bin counts. Null means each sample has weight 1.</summary>
    public double[]? Weights { get; set; }

    /// <summary>Gets or sets the relative bar width within each bin slot (0.0 to 1.0). A value of 1.0 fills the bin exactly.</summary>
    public double RWidth { get; set; } = 1.0;

    /// <summary>Gets or sets the hatch pattern drawn inside each histogram bar. Default is <see cref="HatchPattern.None"/>.</summary>
    public HatchPattern Hatch { get; set; } = HatchPattern.None;

    /// <summary>Gets or sets the hatch line color. When null, the edge color is used.</summary>
    public Color? HatchColor { get; set; }

    /// <summary>Initializes a new instance of <see cref="HistogramSeries"/> with the specified data.</summary>
    /// <param name="data">The raw data values to be binned.</param>
    public HistogramSeries(double[] data)
    {
        Data = data;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
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
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "histogram",
        Data = Data, Bins = Bins, Color = Color
    };

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
