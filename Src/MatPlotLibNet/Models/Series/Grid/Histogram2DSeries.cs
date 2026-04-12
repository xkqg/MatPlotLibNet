// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a 2D density histogram that bins X,Y scatter data into a grid and renders density as colors.</summary>
/// <remarks>Similar to matplotlib's <c>hist2d</c>. Takes raw scatter data and computes bin counts at render time.</remarks>
public sealed class Histogram2DSeries : ChartSeries, IColorBarDataProvider, IColormappable, INormalizable
{
    public double[] X { get; }

    public double[] Y { get; }

    public int BinsX { get; set; }

    public int BinsY { get; set; }

    public IColorMap? ColorMap { get; set; }

    public INormalizer? Normalizer { get; set; }

    /// <summary>Initializes a new instance of <see cref="Histogram2DSeries"/> with the specified scatter data.</summary>
    /// <param name="x">The X-axis data values.</param>
    /// <param name="y">The Y-axis data values.</param>
    /// <param name="binsX">The number of bins along the X axis.</param>
    /// <param name="binsY">The number of bins along the Y axis.</param>
    public Histogram2DSeries(double[] x, double[] y, int binsX = 20, int binsY = 20)
    {
        X = x;
        Y = y;
        BinsX = binsX;
        BinsY = binsY;
    }

    /// <inheritdoc />
    public (double Min, double Max) GetColorBarRange()
    {
        if (X.Length == 0 || Y.Length == 0) return (0, 1);
        var counts = ComputeBinCounts();
        double min = double.MaxValue, max = double.MinValue;
        for (int r = 0; r < BinsY; r++)
        for (int c = 0; c < BinsX; c++)
        {
            if (counts[r, c] < min) min = counts[r, c];
            if (counts[r, c] > max) max = counts[r, c];
        }
        return min < max ? (min, max) : (0, 1);
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (X.Length == 0 || Y.Length == 0) return new(null, null, null, null);
        return new(X.Min(), X.Max(), Y.Min(), Y.Max());
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "histogram2d",
        XData = X,
        YData = Y,
        Bins = BinsX,
        BinsY = BinsY,
        ColorMapName = ColorMap?.Name
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);

    /// <summary>Computes the 2D histogram bin counts from the raw scatter data.</summary>
    internal int[,] ComputeBinCounts()
    {
        var counts = new int[BinsY, BinsX];
        if (X.Length == 0) return counts;

        double xMin = X.Min(), xMax = X.Max();
        double yMin = Y.Min(), yMax = Y.Max();

        // Handle degenerate case where all values are the same
        if (xMax == xMin) xMax = xMin + 1;
        if (yMax == yMin) yMax = yMin + 1;

        int n = Math.Min(X.Length, Y.Length);
        for (int i = 0; i < n; i++)
        {
            int bx = (int)((X[i] - xMin) / (xMax - xMin) * BinsX);
            int by = (int)((Y[i] - yMin) / (yMax - yMin) * BinsY);
            // Clamp to valid range (values exactly at max map to BinsX/BinsY)
            if (bx >= BinsX) bx = BinsX - 1;
            if (by >= BinsY) by = BinsY - 1;
            if (bx < 0) bx = 0;
            if (by < 0) by = 0;
            counts[by, bx]++;
        }

        return counts;
    }
}
