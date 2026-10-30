// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a contour plot series displaying iso-lines or filled regions for 3D surface data.</summary>
public sealed class ContourSeries : ChartSeries, IColormappable, ILabelable
{
    /// <summary>The X coordinate of each grid column, one per column of <see cref="ZData"/>.</summary>
    public double[] XData { get; }

    /// <summary>The Y coordinate of each grid row, one per row of <see cref="ZData"/>.</summary>
    public double[] YData { get; }

    /// <summary>The height of the surface at each grid point, indexed [row, column] with rows along Y; the iso-lines
    /// trace levels of these values.</summary>
    public double[,] ZData { get; }

    /// <summary>How many iso-lines are drawn when <see cref="LevelValues"/> names none: this many levels, evenly spaced
    /// strictly between the lowest value of the grid and the highest. A value below 2 counts as 2.</summary>
    public int Levels { get; set; } = 10;

    /// <summary>The levels of the iso-lines, when you want them at values of your own choosing. Left <see langword="null"/>
    /// or empty, <see cref="Levels"/> decides. Once it holds values, those are the levels drawn, one line each, and
    /// <see cref="Levels"/> is ignored: they are taken in ascending order, each once, and a value that is not a number or is
    /// infinite is left out. A level outside the range of the grid crosses no cell, so it draws no line. When none of the
    /// values can be drawn, nothing is drawn. The colour of a line and the label of a line (<see cref="ShowLabels"/>) follow
    /// its level. A filled contour (<see cref="Filled"/>) fills the bands between these levels, from the lowest value to the highest.</summary>
    public double[]? LevelValues { get; set; }

    /// <summary>When <see langword="true"/> the bands between neighbouring levels are filled with the colour map as
    /// well; the iso-lines are drawn either way. Default <see langword="false"/>.</summary>
    public bool Filled { get; set; }

    /// <inheritdoc />
    public bool ShowLabels { get; set; }

    /// <inheritdoc />
    public string? LabelFormat { get; set; }

    /// <summary>Font size of the level labels, in points. Default 10.</summary>
    public double LabelFontSize { get; set; } = 10;

    /// <inheritdoc />
    public IColorMap? ColorMap { get; set; }


    /// <summary>Initializes a new instance of <see cref="ContourSeries"/> with the specified grid data.</summary>
    /// <param name="xData">The X-axis grid coordinates.</param>
    /// <param name="yData">The Y-axis grid coordinates.</param>
    /// <param name="zData">The 2D matrix of Z values at each grid point.</param>
    public ContourSeries(double[] xData, double[] yData, double[,] zData)
    {
        XData = xData;
        YData = yData;
        ZData = zData;
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
        // Contour grids occupy the full [xMin,xMax] × [yMin,yMax] rectangle — margin
        // expansion would push whitespace between the grid edge and the spines.
        return new(xMin, xMax, yMin, yMax,
            StickyXMin: xMin, StickyXMax: xMax, StickyYMin: yMin, StickyYMax: yMax);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "contour",
        XData = XData, YData = YData, HeatmapData = ZData.To2DList(),
        Levels = Levels.OrNullAtRest(10),
        LevelValues = LevelValues,
        Filled = Filled ? true : null,
        ShowLabels = ShowLabels ? true : null,
        LabelFormat = LabelFormat,
        FontSize = LabelFontSize.OrNullAtRest(10.0),
        ColorMapName = ColorMap.NameForJson(),
        ColorMap = ColorMap.DescriptionForJson(),
    };

    /// <summary>Reconstructs a <see cref="ContourSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static ContourSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Contour(dto.XData ?? [], dto.YData ?? [], dto.HeatmapData.From2DList());
        if (dto.Levels.HasValue)
        {
            s.Levels = dto.Levels.Value;
        }
        s.LevelValues = dto.LevelValues;
        s.Filled = dto.Filled ?? false;
        s.ShowLabels = dto.ShowLabels ?? false;
        s.LabelFormat = dto.LabelFormat;
        if (dto.FontSize.HasValue)
        {
            s.LabelFontSize = dto.FontSize.Value;
        }
        s.RestoreColorMap(dto);
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
