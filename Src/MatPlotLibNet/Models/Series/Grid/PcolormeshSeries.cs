// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a pseudocolor mesh series that renders a non-uniform rectangular grid as colored cells.</summary>
/// <remarks>X has M+1 edge coordinates, Y has N+1 edge coordinates, and C is an N×M data matrix.</remarks>
public sealed class PcolormeshSeries : ChartSeries, IColorBarDataProvider, IColormappable, INormalizable
{
    public Vec X { get; }

    public Vec Y { get; }

    public double[,] C { get; }

    public IColorMap? ColorMap { get; set; }

    public INormalizer? Normalizer { get; set; }

    /// <inheritdoc />
    public MinMaxRange GetColorBarRange()
    {
        double min = double.MaxValue, max = double.MinValue;
        for (int r = 0; r < C.GetLength(0); r++)
            for (int c = 0; c < C.GetLength(1); c++)
            {
                if (C[r, c] < min) min = C[r, c];
                if (C[r, c] > max) max = C[r, c];
            }
        return min < max ? new(min, max) : new(0, 1);
    }

    /// <summary>Initializes a new instance of <see cref="PcolormeshSeries"/>.</summary>
    /// <param name="x">The M+1 X-axis edge coordinates.</param>
    /// <param name="y">The N+1 Y-axis edge coordinates.</param>
    /// <param name="c">The N×M data matrix.</param>
    public PcolormeshSeries(Vec x, Vec y, double[,] c)
    {
        X = x;
        Y = y;
        C = c;
    }

    /// <inheritdoc />
    /// <remarks>A mesh fills its plot rectangle exactly, the way a heatmap does: the outermost edges the caller
    /// gave are the ends of the axes, so the 5 % margin never leaves a gutter between the cells and the spines.
    /// The edges are the caller's own coordinates, which is the whole difference from a heatmap — a date on x and
    /// latency buckets on y stay real values.</remarks>
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (X.Length == 0 || Y.Length == 0) return new(0, 1, 0, 1);
        double xMin = X.Min(), xMax = X.Max(), yMin = Y.Min(), yMax = Y.Max();
        return new(xMin, xMax, yMin, yMax,
            StickyXMin: xMin, StickyXMax: xMax, StickyYMin: yMin, StickyYMax: yMax);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "pcolormesh",
        XData = X,
        YData = Y,
        HeatmapData = ChartSerializer.To2DList(C),
        ColorMapName = ColorMap?.Name
    };

    /// <summary>Reconstructs a <see cref="PcolormeshSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static PcolormeshSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Pcolormesh(dto.XData ?? [], dto.YData ?? [], ChartSerializer.From2DList(dto.HeatmapData));
        if (dto.ColorMapName is not null)
        {
            s.ColorMap = Styling.ColorMaps.ColorMapRegistry.Get(dto.ColorMapName);
        }
        return s;
    }


    /// <inheritdoc />
    /// <remarks>Long form: one row per cell. A wide grid would make the column headers indices, which say
    /// nothing a reader can use; row and column as numbers are readable at any size.</remarks>
    /// <inheritdoc />
    /// <remarks>One row per cell, and the cell is named by the coordinates the caller gave rather than by its
    /// place in the matrix: the lower edge of its column and the lower edge of its row, which is how a bucket is
    /// named everywhere else. This is the whole difference from a heatmap — a mesh has real coordinates, so a
    /// clock stays a clock and a latency bucket stays a number of milliseconds. Marking the two columns as
    /// positions on the axes is what lets a date axis spell them as times.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        int rows = C.GetLength(0), cols = C.GetLength(1);
        if (X.Length == 0 || Y.Length == 0)
        {
            return null;
        }

        var out_ = new List<IReadOnlyList<DataCell>>(rows * cols);
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                out_.Add([
                    DataCell.FromNumber(X[Math.Min(c, X.Length - 1)]),
                    DataCell.FromNumber(Y[Math.Min(r, Y.Length - 1)]),
                    DataCell.FromNumber(C[r, c])]);
            }
        }

        return new ChartDataTable(null,
            [new("x", DataColumnKind.Number, DataAxis.X),
             new("y", DataColumnKind.Number, DataAxis.Y),
             new(Label ?? "value")],
            out_);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
