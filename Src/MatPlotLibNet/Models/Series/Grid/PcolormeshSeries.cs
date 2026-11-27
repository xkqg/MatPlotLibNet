// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a pseudocolor mesh series that renders a non-uniform rectangular grid as colored cells.</summary>
/// <remarks>X has M+1 edge coordinates, Y has N+1 edge coordinates, and C is an N×M data matrix.</remarks>
public sealed class PcolormeshSeries : ChartSeries, IColorBarDataProvider, IColorScaled, IColormappable, INormalizable, ISeriesKind<PcolormeshSeries>
{
    /// <inheritdoc />
    /// <remarks>matplotlib's <c>pcolormesh</c> asks neither cycle.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Mesh;

    /// <summary>The M + 1 cell edges along X: column <c>c</c> of <see cref="C"/> spans from <c>X[c]</c> to <c>X[c +
    /// 1]</c>, and the spacing may be uneven.</summary>
    public Vec X { get; }

    /// <summary>The N + 1 cell edges along Y: row <c>r</c> of <see cref="C"/> spans from <c>Y[r]</c> to <c>Y[r +
    /// 1]</c>, and the spacing may be uneven.</summary>
    public Vec Y { get; }

    /// <summary>The cell values, an N-by-M matrix indexed [row, column]: each fills the cell between its edges on
    /// <see cref="X"/> and <see cref="Y"/>.</summary>
    public double[,] C { get; }

    /// <inheritdoc />
    public IColorMap? ColorMap { get; set => Change(ref field, value); }

    /// <inheritdoc />
    public INormalizer? Normalizer { get; set => Change(ref field, value); }

    /// <inheritdoc />
    public MinMaxRange GetColorBarRange() => this.ColourRangeUnder(Theme.Default);

    /// <inheritdoc />
    MinMaxRange IColorScaled.ColourRangeOver(ColorMapping mapping) => mapping.RangeOf(C);

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
    internal override bool HoldsNoData => X.Length == 0 || Y.Length == 0;

    /// <inheritdoc />
    /// <remarks>A mesh fills its plot rectangle exactly, the way a heatmap does: the outermost edges the caller
    /// gave are the ends of the axes, so the 5 % margin never leaves a gutter between the cells and the spines.
    /// The edges are the caller's own coordinates, which is the whole difference from a heatmap — a date on x and
    /// latency buckets on y stay real values.</remarks>
    public override DataRangeContribution ComputeDataRange(IAxesContext context) => PinnedOverItsPoints(X.Data, Y.Data);

    static string ISeriesKind<PcolormeshSeries>.Kind => "pcolormesh";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        XData = X,
        YData = Y,
        HeatmapData = C.To2DList(),
        ColorScale = this.ColorScaleOnTheWire(),
    };

    /// <summary>The reader of a <see cref="PcolormeshSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static PcolormeshSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Pcolormesh(dto.XData ?? [], dto.YData ?? [], dto.HeatmapData.From2DList());
        s.RestoreColorScale(dto);
        return s;
    }

    /// <inheritdoc />
    /// <remarks>One row per cell, and the cell is named by the coordinates the caller gave rather than by its
    /// place in the matrix: the lower edge of its column and the lower edge of its row, which is how a bucket is
    /// named everywhere else. This is the whole difference from a heatmap — a mesh has real coordinates, so a
    /// clock stays a clock and a latency bucket stays a number of milliseconds. Marking the two columns as
    /// positions on the axes is what lets a date axis spell them as times.</remarks>
    public override ChartDataTable? ToDataTable() =>
        X.Length == 0 || Y.Length == 0 ? null : ChartDataTable.PerCell(C.GetLength(0), C.GetLength(1),
            [ChartDataColumn.Position(DataAxis.X), ChartDataColumn.Position(DataAxis.Y), new(Label ?? "value")],
            (r, c) => [
                DataCell.FromNumber(X[Math.Min(c, X.Length - 1)]),
                DataCell.FromNumber(Y[Math.Min(r, Y.Length - 1)]),
                DataCell.FromNumber(C[r, c])]);

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
