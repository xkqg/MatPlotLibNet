// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Algorithms;
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

    /// <summary>How many levels bound the bands when <see cref="LevelValues"/> names none: this many levels, evenly spaced
    /// from the lowest value of the grid to the highest, make <c>Levels - 1</c> bands. A value below 2 counts as 2.</summary>
    public int Levels { get; set; } = 10;

    /// <summary>The levels that bound the bands, when you want them at values of your own choosing. Left <see langword="null"/>
    /// or empty, <see cref="Levels"/> decides. Once it holds values, those are the levels and <see cref="Levels"/> is
    /// ignored: they are taken in ascending order, each once, and a value that is not a number or is infinite is left out.
    /// Each pair of neighbouring levels bounds one band, and the iso-lines (<see cref="ShowLines"/>) stand on the levels; a
    /// level outside the range of the grid crosses no cell, so it adds no line. The colours follow the levels too: the colour
    /// map runs from the first level to the last, and so does the colour bar, whatever range the grid itself spans. The
    /// lowest band is painted under the whole plot, so a value below the first level is not left empty; it shows that band's
    /// colour. Fewer than two levels that can be drawn leave no band, and the series draws nothing.</summary>
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
    /// <remarks>The values of the grid, or, when <see cref="LevelValues"/> names at least two levels that can be drawn, from
    /// the first of them to the last: the span the colour map runs over.</remarks>
    public MinMaxRange GetColorBarRange()
    {
        if (LevelValues.ExplicitLevels() is { Length: >= 2 } named)
        {
            return new(named[0], named[^1]);
        }

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
        // Over the drawable grid coordinates only: a missing one (NaN) and an infinity are left out.
        var x = XData.DrawableRange();
        var y = YData.DrawableRange();
        double? xMin = x?.Lo, xMax = x?.Hi, yMin = y?.Lo, yMax = y?.Hi;
        // Filled contour grids fill their [xMin,xMax] Ã— [yMin,yMax] rectangle â€” sticky edges
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
        HeatmapData = ZData.To2DList(),
        Levels = Levels.OrNullAtRest(10),
        LevelValues = LevelValues,
        Alpha = Alpha.OrNullAtRest(1.0),
        ShowLines = ShowLines ? null : false,
        LineWidth = LineWidth.OrNullAtRest(0.5),
        ColorMapName = ColorMap.NameForJson(),
        ColorMap = ColorMap.DescriptionForJson(),
        Hatches = Hatches is null ? null : [.. Hatches],
        Normalizer = this.DescribeNormalizer(),
    };

    /// <summary>Reconstructs a <see cref="ContourfSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static ContourfSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Contourf(dto.XData ?? [], dto.YData ?? [], dto.HeatmapData.From2DList());
        if (dto.Levels.HasValue)
        {
            s.Levels = dto.Levels.Value;
        }
        s.LevelValues = dto.LevelValues;
        if (dto.Alpha.HasValue)
        {
            s.Alpha = dto.Alpha.Value;
        }
        if (dto.ShowLines.HasValue)
        {
            s.ShowLines = dto.ShowLines.Value;
        }
        if (dto.LineWidth.HasValue)
        {
            s.LineWidth = dto.LineWidth.Value;
        }
        s.RestoreColorMap(dto);
        s.Hatches = dto.Hatches?.ToArray();
        s.RestoreNormalizer(dto);
        return s;
    }


    /// <inheritdoc />
    /// <remarks>Long form over the grid: the x and y the cell sits at, and its z.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        int rows = ZData.GetLength(0), cols = ZData.GetLength(1);
        var out_ = new List<IReadOnlyList<DataCell>>(rows * cols);
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                out_.Add(
                [
                    DataCell.FromNumber(c < XData.Length ? XData[c] : c),
                    DataCell.FromNumber(r < YData.Length ? YData[r] : r),
                    DataCell.FromNumber(ZData[r, c]),
                ]);
            }
        }

        return new ChartDataTable(null, [new("x", Axis: DataAxis.X), new("y", Axis: DataAxis.Y), new(Label ?? "z")], out_);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
