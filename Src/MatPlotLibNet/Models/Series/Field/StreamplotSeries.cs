// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a streamline plot through a 2D vector field (U, V components on a grid).</summary>
public sealed class StreamplotSeries : ChartSeries, IHasColor
{
    /// <summary>The X coordinate of each grid column, one per column of <see cref="U"/> and <see cref="V"/>.</summary>
    public double[] X { get; }

    /// <summary>The Y coordinate of each grid row, one per row of <see cref="U"/> and <see cref="V"/>.</summary>
    public double[] Y { get; }

    /// <summary>The X component of the field at each grid point, indexed [row, column] with rows along Y and columns
    /// along X.</summary>
    public double[,] U { get; }

    /// <summary>The Y component of the field at each grid point, indexed [row, column] with rows along Y and columns
    /// along X.</summary>
    public double[,] V { get; }

    /// <summary>How closely streamlines are seeded across the grid: about half the number of grid points along each
    /// axis, times this value (at least 2). Default 1; larger values draw more, closer lines.</summary>
    public double Density { get; set; } = 1.0;

    /// <inheritdoc />
    public Color? Color { get; set; }

    /// <summary>Width of the streamlines, in pixels. Default 1.</summary>
    public double LineWidth { get; set; } = 1.0;

    /// <summary>Scale of the arrowheads along the streamlines: 1 (the default) draws heads 6 pixels long.</summary>
    public double ArrowSize { get; set; } = 1.0;

    /// <summary>Creates a new streamplot series from grid coordinates and velocity field components.</summary>
    /// <param name="x">1D array of X grid coordinates.</param>
    /// <param name="y">1D array of Y grid coordinates.</param>
    /// <param name="u">2D array of X-velocity components (rows=Y, cols=X).</param>
    /// <param name="v">2D array of Y-velocity components (rows=Y, cols=X).</param>
    public StreamplotSeries(double[] x, double[] y, double[,] u, double[,] v)
    {
        X = x;
        Y = y;
        U = u;
        V = v;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        // Over the drawable grid coordinates only: a missing one (NaN) and an infinity are left out.
        var x = X.DrawableRange();
        var y = Y.DrawableRange();
        return new(x?.Lo, x?.Hi, y?.Lo, y?.Hi);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "streamplot",
        XData = X,
        YData = Y,
        HeatmapData = U.To2DList(),
        VFieldData = V.To2DList(),
        LineWidth = LineWidth,
        Color = Color,
        ArrowSize = ArrowSize.OrNullAtRest(1.0),
        StreamDensity = Density.OrNullAtRest(1.0),
    };

    /// <summary>Reconstructs a <see cref="StreamplotSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static StreamplotSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Streamplot(dto.XData ?? [], dto.YData ?? [], dto.HeatmapData.From2DList(), dto.VFieldData.From2DList());
        if (dto.Color.HasValue)
        {
            s.Color = dto.Color.Value;
        }
        if (dto.LineWidth.HasValue)
        {
            s.LineWidth = dto.LineWidth.Value;
        }
        if (dto.ArrowSize.HasValue)
        {
            s.ArrowSize = dto.ArrowSize.Value;
        }
        if (dto.StreamDensity.HasValue)
        {
            s.Density = dto.StreamDensity.Value;
        }
        return s;
    }


    /// <inheritdoc />
    /// <remarks>Long form over the vector grid: the point, and the vector at it. The streamlines are integrated
    /// from this field by the renderer — the field is the data.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        int rows = U.GetLength(0), cols = U.GetLength(1);
        var out_ = new List<IReadOnlyList<DataCell>>(rows * cols);
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                out_.Add(
                [
                    DataCell.FromNumber(c < X.Length ? X[c] : c),
                    DataCell.FromNumber(r < Y.Length ? Y[r] : r),
                    DataCell.FromNumber(U[r, c]),
                    DataCell.FromNumber(V[r, c]),
                ]);
            }
        }

        return new ChartDataTable(null, [new("x", Axis: DataAxis.X), new("y", Axis: DataAxis.Y), new("u"), new("v")], out_);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
