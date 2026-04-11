// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a filled contour plot that fills the regions between iso-levels using a painter's algorithm.</summary>
public sealed class ContourfSeries : ChartSeries, IColormappable, INormalizable, IColorBarDataProvider
{
    /// <summary>Gets the X-axis grid coordinates.</summary>
    public double[] XData { get; }

    /// <summary>Gets the Y-axis grid coordinates.</summary>
    public double[] YData { get; }

    /// <summary>Gets the 2D matrix of Z values at each grid point.</summary>
    public double[,] ZData { get; }

    /// <summary>Gets or sets the number of iso-levels (produces Levels-1 filled bands). Default 10.</summary>
    public int Levels { get; set; } = 10;

    /// <summary>Gets or sets explicit iso-level thresholds. When set, overrides <see cref="Levels"/> count.</summary>
    public double[]? LevelValues { get; set; }

    /// <summary>Gets or sets the fill opacity in [0, 1]. Default 1.0.</summary>
    public double Alpha { get; set; } = 1.0;

    /// <summary>Gets or sets whether iso-lines are drawn on top of the filled bands. Default true.</summary>
    public bool ShowLines { get; set; } = true;

    /// <summary>Gets or sets the width of the overlay iso-lines. Default 0.5.</summary>
    public double LineWidth { get; set; } = 0.5;

    /// <inheritdoc />
    public IColorMap? ColorMap { get; set; }

    /// <inheritdoc />
    public INormalizer? Normalizer { get; set; }

    /// <summary>Gets or sets per-level hatch patterns. Null means no hatching. The array is indexed modulo the number of bands.</summary>
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
    public (double Min, double Max) GetColorBarRange()
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
        return (min, max);
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        new(null, null, null, null);

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
