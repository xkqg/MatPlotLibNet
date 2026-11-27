// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a hexagonal binning series that aggregates scatter data into a hex grid and maps counts to colors.</summary>
/// <remarks>Analogous to matplotlib's <c>hexbin</c>, with its grid: hexagons with a vertex up and down and a vertical edge on each
/// side, laid over the extent of the data, and drawn in a colour of their own, clipped to the plot. Like matplotlib's, it asks its
/// axes for a tight range: the axes end where the data and the margin end, under every theme.</remarks>
public sealed class HexbinSeries : ChartSeries, IColormappable, INormalizable, IColorBarDataProvider, IColorScaled, IAsksOfAxes, ISeriesKind<HexbinSeries>
{
    /// <inheritdoc />
    /// <remarks>matplotlib's <c>hexbin</c> asks neither cycle.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Mesh;

    /// <inheritdoc />
    /// <remarks>The hexagons at the edge of the extent reach half a step past it.</remarks>
    internal override bool MarksReachPastItsRange(Theme theme) => true;

    /// <summary>A tight range under every theme: matplotlib's <c>Axes.hexbin</c> calls <c>self._request_autoscale_view(tight=True)</c>.</summary>
    AxesAsk IAsksOfAxes.AskUnder(Styling.Theme theme) => new(Tight: true, RowsDown: false);

    /// <summary>The X coordinate of each observation, paired by index with <see cref="Y"/>.</summary>
    public double[] X { get; }

    /// <summary>The Y coordinate of each observation, paired by index with <see cref="X"/>.</summary>
    public double[] Y { get; }

    private const int RestingGridSize = 20;
    /// <summary>The number of hexagons across the X range of the data, as matplotlib's <c>gridsize</c>; more hexagons make each one
    /// smaller. There are <c>int(GridSize / sqrt(3))</c> rows. Between 1 and 2,048. Default <see cref="RestingGridSize"/>.</summary>
    public int GridSize { get; set => Change(ref field, value); } = RestingGridSize;

    private const int RestingMinCount = 1;
    /// <summary>The fewest observations a hexagon must hold to be drawn; hexagons with fewer are left blank. Default <see cref="RestingMinCount"/>, which
    /// leaves the empty hexagons blank. With 0 every hexagon of the grid over the plot is drawn, the empty ones in the colour of a
    /// count of 0, the lowest of the scale and of the colour bar, as matplotlib's <c>hexbin</c> draws its grid when it is given no
    /// <c>mincnt</c>.</summary>
    public int MinCount { get; set => Change(ref field, value); } = RestingMinCount;

    /// <inheritdoc />
    public IColorMap? ColorMap { get; set => Change(ref field, value); }

    /// <inheritdoc />
    public INormalizer? Normalizer { get; set => Change(ref field, value); }

    /// <summary>Initializes a new instance of <see cref="HexbinSeries"/> with the specified scatter data.</summary>
    public HexbinSeries(double[] x, double[] y)
    {
        x.ThrowIfMisaligned(y);
        X = x;
        Y = y;
    }

    /// <inheritdoc />
    /// <remarks>The range its colour bar covers under the library's own themes (<c>ColourRangeUnder</c>).</remarks>
    public MinMaxRange GetColorBarRange() => this.ColourRangeUnder(Theme.Default);

    /// <inheritdoc />
    /// <remarks>The counts of the hexagons over the extent of the points that can be placed (a missing coordinate and an infinity are
    /// not binned; with none, 0 to 1), by <see cref="ColourRangeOver(int[], ColorMapping)"/>: the range the hexagons are coloured over
    /// and the bar names, so the two cannot read a count differently.</remarks>
    MinMaxRange IColorScaled.ColourRangeOver(ColorMapping mapping) => HexExtent.Of(X, Y) is { } extent
        ? ColourRangeOver(HexLattice.Over(extent, GridSize).Count(X, Y), mapping)
        : mapping.RangeOf((Range1D?)null);

    /// <summary>The range the hexagons of <paramref name="counts"/> are coloured over when they are mapped by <paramref name="mapping"/>,
    /// the one rule the hexagons and the colour bar read (<see cref="ColourRangeOver(int[], int, ColorMapping)"/> with this series'
    /// <see cref="MinCount"/>).</summary>
    /// <param name="counts">The count of each hexagon.</param>
    /// <param name="mapping">The map, the normaliser and the look the series is drawn under.</param>
    /// <returns>The range.</returns>
    internal MinMaxRange ColourRangeOver(int[] counts, ColorMapping mapping) => ColourRangeOver(counts, MinCount, mapping);

    /// <summary>The range the hexagons of <paramref name="counts"/> are coloured over when a hexagon holding fewer than
    /// <paramref name="minCount"/> points is left blank, for this series and for any mesh of counts drawn as a hexbin is (a pair grid's
    /// hexbin panel). With no minimum count every hexagon is drawn and the low end is 0, the smallest count above 0 on the log scale of
    /// matplotlib's log norm (a hexagon of no points is masked). With one, a theme that colours counts from the least drawn
    /// (<see cref="ColorMapLook.CountsFromTheLeastDrawn"/>, matplotlib's norm autoscaled to the drawn cells) starts at the least count
    /// drawn; the library's own themes start half a count under the least count asked for, so it is not the darkest colour of the map. A
    /// range of no width is opened by the one rule of a colour range (<see cref="ColorMapping.Opened"/>).</summary>
    /// <param name="counts">The count of each hexagon.</param>
    /// <param name="minCount">The fewest points a hexagon that is drawn holds; 0 draws every hexagon.</param>
    /// <param name="mapping">The map, the normaliser and the look the counts are drawn under.</param>
    /// <returns>The range.</returns>
    internal static MinMaxRange ColourRangeOver(int[] counts, int minCount, ColorMapping mapping)
    {
        double hi = counts.Max();
        double lo = minCount > 0
            ? mapping.Look.CountsFromTheLeastDrawn ? LeastDrawn(counts, minCount) : minCount - 0.5
            : mapping.RangeScale.HasPlaceFor(0) ? 0 : SmallestPositive(counts);
        return ColorMapping.Opened(lo, hi);
    }
    /// <summary>The least count of a hexagon that is drawn: at least <paramref name="minCount"/>, and above 0 where a log norm masks 0.</summary>
    private static int LeastDrawn(int[] counts, int minCount)
    {
        int least = int.MaxValue;
        foreach (int count in counts)
            if (count >= minCount && count < least) least = count;
        return least == int.MaxValue ? minCount : least;
    }

    /// <summary>The smallest count above 0, or 0 when every hexagon is empty: the low end of the counts a log scale colours.</summary>
    /// <param name="counts">The count of each hexagon.</param>
    /// <returns>The smallest positive count.</returns>
    internal static int SmallestPositive(int[] counts)
    {
        int smallest = 0;
        foreach (int count in counts)
        {
            if (count > 0 && (smallest == 0 || count < smallest)) smallest = count;
        }

        return smallest;
    }

    /// <inheritdoc />
    /// <remarks>The extent of the hexagonal grid, whose corners matplotlib's <c>hexbin</c> makes the data
    /// limits: the points that can be placed, a side of one value widened by a tenth of it. The tight range the hexbin asks of its axes
    /// is the axes' (<c>AxesRangeExtensions.AskUnder</c>).</remarks>
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        HexExtent.Of(X, Y) is { } grid ? new(grid.XMin, grid.XMax, grid.YMin, grid.YMax) : default;

    static string ISeriesKind<HexbinSeries>.Kind => "hexbin";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        XData = X,
        YData = Y,
        GridSize = GridSize.OrNullAtRest(RestingGridSize),
        MinCount = MinCount.OrNullAtRest(RestingMinCount),
        ColorScale = this.ColorScaleOnTheWire(),
    };

    /// <summary>The reader of a <see cref="HexbinSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static HexbinSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Hexbin(dto.XData ?? [], dto.YData ?? []);
        if (dto.GridSize is { } gridSize) s.GridSize = gridSize;
        if (dto.MinCount is { } minCount) s.MinCount = minCount;
        s.RestoreColorScale(dto);
        return s;
    }


    /// <inheritdoc />
    /// <remarks>The observations the hexagons are binned from.</remarks>
    public override ChartDataTable? ToDataTable() =>
        ChartDataTable.XY(null, X, Y);

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
