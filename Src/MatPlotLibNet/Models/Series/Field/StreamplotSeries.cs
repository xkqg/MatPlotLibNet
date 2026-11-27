// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a streamline plot through a 2D vector field (U, V components on a grid).</summary>
/// <remarks>The streamlines are laid out as matplotlib's <c>streamplot</c> lays them out: a grid of 30 x 30 cells times the density, of
/// which each streamline may cross a cell once, seeded from the edge of the grid inward and followed both ways along the field, with one
/// arrow halfway along each. The axes span the grid with no margin. Under the matplotlib themes the lines are as wide as the theme's lines
/// and each arrow is matplotlib's filled head. The grid is read as evenly spaced from its first step.</remarks>
public sealed class StreamplotSeries : ChartSeries, IHasColor, ISeriesKind<StreamplotSeries>, IWidthGiven
{
    /// <inheritdoc />
    /// <remarks>2: <c>streamplot</c> draws its lines at <c>Line2D.zorder</c>.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Line;

    /// <inheritdoc />
    /// <remarks>The arrow at the middle of a streamline reaches past the grid the lines run on.</remarks>
    internal override bool MarksReachPastItsRange(Theme theme) => true;

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

    private const double RestingDensity = 1.0;
    /// <summary>How closely the streamlines are spaced: the plot is divided into 30 x 30 cells times this value, and a streamline may
    /// cross each cell once, as matplotlib's <c>density</c> does. Default <see cref="RestingDensity"/>; larger values draw more, closer lines; a value under 2 / 30
    /// draws none.</summary>
    public double Density { get; set => Change(ref field, value); } = RestingDensity;

    /// <inheritdoc />
    public Color? Color { get; set => Change(ref field, value); }

    /// <summary>Width of the streamlines, in pixels, read and drawn as a line's (<see cref="LineSeries.LineWidth"/>) but resting at
    /// the library's one-pixel width of a collection.</summary>
    public double LineWidth { get => this.ReportedLineWidth; set => GivenLineWidth = value; }

    /// <summary>How wide the streamlines are drawn: as a collection of lines (<c>streamplot</c> takes <c>lines.linewidth</c> and never the
    /// cycler's), with a resting width of its own.</summary>
    LineWidthRule IWidthGiven.WidthRule => LineWidthRule.OnePixelCollection;

    /// <summary>The width the series was given; <see langword="null"/> when it was given none, and its rule decides.</summary>
    internal double? GivenLineWidth { get; private set => Change(ref field, value); }

    double? IWidthGiven.GivenLineWidth => GivenLineWidth;

    private const double RestingArrowSize = 1.0;
    /// <summary>Scale of the arrowheads along the streamlines: the default, <see cref="RestingArrowSize"/>, draws heads 6 pixels long, or, under the matplotlib themes,
    /// matplotlib's heads at <c>arrowsize=1</c> (4 pt long and 4 pt across).</summary>
    public double ArrowSize { get; set => Change(ref field, value); } = RestingArrowSize;

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
        // Over the drawable grid coordinates only: a missing one (NaN) and an infinity are left out. The grid's edges are sticky, as
        // matplotlib's streamplot makes them (lc.sticky_edges), so the axes span the grid with no margin.
        return DataRangeContribution.PinnedOver(X, Y);
    }

    static string ISeriesKind<StreamplotSeries>.Kind => "streamplot";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        XData = X,
        YData = Y,
        HeatmapData = U.To2DList(),
        VFieldData = V.To2DList(),
        LineWidth = GivenLineWidth,
        Color = Color,
        ArrowSize = ArrowSize.OrNullAtRest(RestingArrowSize),
        StreamDensity = Density.OrNullAtRest(RestingDensity),
    };

    /// <summary>The reader of a <see cref="StreamplotSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static StreamplotSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Streamplot(dto.XData ?? [], dto.YData ?? [], dto.HeatmapData.From2DList(), dto.VFieldData.From2DList());
        if (dto.Color is { } color) s.Color = color;
        if (dto.LineWidth is { } lineWidth) s.LineWidth = lineWidth;
        if (dto.ArrowSize is { } arrowSize) s.ArrowSize = arrowSize;
        if (dto.StreamDensity is { } streamDensity) s.Density = streamDensity;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>Long form over the vector grid: the point, and the vector at it. The streamlines are integrated
    /// from this field by the renderer — the field is the data.</remarks>
    public override ChartDataTable? ToDataTable() => ChartDataTable.OverGrid(X, Y, new GridValues("u", U), new GridValues("v", V));

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
