// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a histogram series that bins continuous data into discrete intervals.</summary>
public sealed class HistogramSeries : ChartSeries, IHasColor, IHasAlpha, IHasEdgeColor
{
    public double[] Data { get; }

    public int Bins { get; set; } = 10;

    public Color? Color { get; set; }

    public Color? EdgeColor { get; set; }

    public double Alpha { get; set; } = 1.0;

    public bool Density { get; set; } = false;

    public bool Cumulative { get; set; } = false;

    public HistType HistType { get; set; } = HistType.Bar;

    public double[]? Weights { get; set; }

    public double RWidth { get; set; } = 1.0;

    public HatchPattern Hatch { get; set; } = HatchPattern.None;

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
        double xMin = Data.Length > 0 ? Data.Min() : 0;
        double xMax = Data.Length > 0 ? Data.Max() : 1;
        return new(xMin, xMax, yMin, yMax,
            StickyXMin: xMin, StickyXMax: xMax, StickyYMin: 0);
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
    /// <remarks>Delegates to the shared <see cref="MatPlotLibNet.Numerics.HistogramBinning.Compute"/>
    /// — single source-of-truth for equal-width binning across every series that needs it.</remarks>
    public HistogramBins ComputeBins() => Numerics.HistogramBinning.Compute(Data, Bins);
}
