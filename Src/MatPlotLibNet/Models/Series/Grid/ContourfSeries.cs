// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a filled contour plot that fills the regions between iso-levels using a painter's algorithm.</summary>
public sealed class ContourfSeries : ChartSeries, IColormappable, INormalizable, IColorBarDataProvider, IHasAlpha
{
    public double[] XData { get; }

    public double[] YData { get; }

    public double[,] ZData { get; }

    public int Levels { get; set; } = 10;

    public double[]? LevelValues { get; set; }

    public double Alpha { get; set; } = 1.0;

    public bool ShowLines { get; set; } = true;

    public double LineWidth { get; set; } = 0.5;

    public IColorMap? ColorMap { get; set; }

    public INormalizer? Normalizer { get; set; }

    public HatchPattern[]? Hatches { get; set; }

    /// <summary>Initializes a new instance of <see cref="ContourfSeries"/> with grid data.</summary>
    /// <param name="xData">X-axis grid coordinates.</param>
    /// <param name="yData">Y-axis grid coordinates.</param>
    /// <param name="zData">2D Z values indexed as [row, col].</param>
    public ContourfSeries(double[] xData, double[] yData, double[,] zData)
    {
        XData = xData;
        YData = yData;
        ZData = zData;
    }

    /// <inheritdoc />
    public MinMaxRange GetColorBarRange()
    {
        double min = double.MaxValue;
        double max = double.MinValue;
        int rows = ZData.GetLength(0);
        int cols = ZData.GetLength(1);
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                double v = ZData[r, c];
                if (v < min) min = v;
                if (v > max) max = v;
            }
        return new(min, max);
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (XData.Length == 0 || YData.Length == 0)
            return new(null, null, null, null);
        double xMin = XData.Min(), xMax = XData.Max();
        double yMin = YData.Min(), yMax = YData.Max();
        // Filled contour grids fill their [xMin,xMax] × [yMin,yMax] rectangle — sticky edges
        // on all four sides keep the 5 % margin from pushing whitespace between the fill and
        // the spines, matching ContourSeries.
        return new(xMin, xMax, yMin, yMax,
            StickyXMin: xMin, StickyXMax: xMax, StickyYMin: yMin, StickyYMax: yMax);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "contourf",
        XData = XData,
        YData = YData,
        HeatmapData = ChartSerializer.To2DList(ZData)
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
