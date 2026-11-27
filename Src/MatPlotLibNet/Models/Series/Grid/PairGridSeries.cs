// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Multi-panel scatter matrix — the seaborn <c>pairplot</c> idiom. Given
/// <c>N</c> variables, the renderer paints an <c>N×N</c> grid where the diagonal
/// shows a univariate distribution of variable <c>i</c> and each off-diagonal cell
/// shows a bivariate scatter of <c>(i, j)</c>. Optional hue groups colour the
/// off-diagonal scatters by category.</summary>
/// <remarks>The series owns the data and configuration; the actual rendering of
/// each cell is delegated to existing renderers (Histogram / KDE / Scatter)
/// instantiated per cell with a sub-panel <see cref="RenderArea"/>. This matches
/// the composite-renderer pattern from <see cref="ClustermapSeries"/>.</remarks>
public sealed class PairGridSeries : ChartSeries, IKeepsDataCopy, IDotSeries, ISeriesKind<PairGridSeries>
{
    /// <inheritdoc />
    internal override bool DrawsWithoutAxes => true;

    private const double RestingCellSpacing = 0.02;
    private double _cellSpacing = RestingCellSpacing;

    /// <summary>The N input variables. Each <c>double[]</c> is one variable's samples.
    /// All sub-arrays must have equal length; constructor throws otherwise.</summary>
    /// <remarks>The outer array is get-only; the inner per-variable <c>double[]</c> arrays
    /// are exposed by reference for zero-copy rendering. Callers must treat each inner
    /// array as immutable after constructor return — the renderer reads them on every
    /// frame without defensive copies.</remarks>
    public double[][] Variables { get; }

    /// <summary>Optional names, one per variable (length = <c>Variables.Length</c>), drawn as seaborn's <c>pairplot</c> draws them:
    /// under the cell of the variable's column in the bottom row, and turned a quarter beside the cell of its row in the left column.
    /// <see langword="null"/> names no variable, and a variable past the end of a short list goes unnamed.</summary>
    public string[]? Labels { get; set => Change(ref field, value); }

    /// <summary>Optional grouping vector parallel to each variable's samples
    /// (length = <c>Variables[0].Length</c>). When set, off-diagonal scatters
    /// are split into one series per distinct group, coloured from <see cref="HuePalette"/>.</summary>
    /// <remarks>
    /// <para>If <c>HueGroups.Length</c> does not equal <c>Variables[0].Length</c> the
    /// renderer silently falls back to single-colour rendering (defensive identity-fallback,
    /// matching the <c>ClustermapSeries.ResolveLeafOrder</c> convention). Negative group
    /// IDs are accepted and folded modulo the palette length.</para>
    /// <para><b>Hue is ignored when <see cref="OffDiagonalKind"/> is <see cref="PairGridOffDiagonalKind.Hexbin"/></b>:
    /// hexbin density rendering encodes point counts via colormap, so applying a per-group
    /// palette would be ambiguous. A single aggregate density is rendered instead. Use
    /// <see cref="PairGridOffDiagonalKind.Scatter"/> if per-group separation matters.</para>
    /// </remarks>
    public int[]? HueGroups { get; set => Change(ref field, value); }

    /// <summary>Optional human-readable labels for distinct hue groups, indexed by
    /// group ID. <c>HueLabels[g]</c> is the legend label for rows where
    /// <c>HueGroups[i] == g</c>. When null the legend renders the integer group IDs.</summary>
    public string[]? HueLabels { get; set => Change(ref field, value); }

    private const PairGridDiagonalKind RestingDiagonalKind = PairGridDiagonalKind.Histogram;
    /// <summary>Univariate-distribution kind for the diagonal cells. Default
    /// <see cref="RestingDiagonalKind"/>.</summary>
    public PairGridDiagonalKind DiagonalKind { get; set => Change(ref field, value); } = RestingDiagonalKind;

    private const PairGridOffDiagonalKind RestingOffDiagonalKind = PairGridOffDiagonalKind.Scatter;
    /// <summary>Bivariate-distribution kind for the off-diagonal cells. Default
    /// <see cref="RestingOffDiagonalKind"/>.</summary>
    public PairGridOffDiagonalKind OffDiagonalKind { get; set => Change(ref field, value); } = RestingOffDiagonalKind;

    private const PairGridTriangle RestingTriangular = PairGridTriangle.Both;
    /// <summary>Which triangle of the N×N grid to render. <see cref="PairGridTriangle.Both"/> = full grid; default <see cref="RestingTriangular"/>.</summary>
    public PairGridTriangle Triangular { get; set => Change(ref field, value); } = RestingTriangular;

    internal const int RestingDiagonalBins = 20;
    /// <summary>Bin count for diagonal histograms. Default <see cref="RestingDiagonalBins"/>.</summary>
    public int DiagonalBins { get; set => Change(ref field, value); } = RestingDiagonalBins;

    /// <summary>Marker radius in pixels for off-diagonal scatter dots.</summary>
    /// <remarks>This is the dot's pixel <em>radius</em>, not the area in points squared that
    /// <c>ScatterSeries.MarkerSize</c> takes (a marker √area points across). PairGrid renders
    /// cells in their own pixel-space sub-panels with no parent-axis coordinate transform, so a
    /// pixel radius is the directly meaningful unit. To match a scatter marker of 25 points
    /// squared, which is 5 points or 6.94 pixels across, pass <c>MarkerSize = 3.47</c>. Given none,
    /// the dots are the size <c>Axes.scatter</c> draws where it is given none (<c>pairplot</c> gives
    /// <c>scatterplot</c> no size) under the matplotlib themes, 6 points across under the default style
    /// and 4.47 under classic, and half the diameter of <see cref="Rests"/> under every other theme; a radius that is given is drawn as pixels.</remarks>
    public double MarkerSize { get => Dot.Diameter / 2 ?? Rests.Diameter / 2; set => Dot = new GivenDot(value * 2); }

    /// <summary>What was named of the dots: the diameter, which is twice the radius <see cref="MarkerSize"/> names; <see langword="null"/>
    /// while none was.</summary>
    internal GivenDot Dot { get; set => Change(ref field, value); }

    /// <summary>What a pair grid rests at: <see cref="MarkerLook.RestingSize"/> pixels across.</summary>
    private static readonly RestingDot Rests = new(MarkerLook.RestingSize);

    GivenDot IDotSeries.Given => Dot;

    RestingDot IDotSeries.Resting => Rests;

    /// <inheritdoc />
    /// <remarks><c>pairplot</c> gives <c>scatterplot</c> no size, so <c>Axes.scatter</c>'s own is drawn.</remarks>
    double IDotSeries.SeabornDiameter(SeriesSlot slot, DotLook look) => slot.ScatterDiameter;

    /// <summary>Fraction of plot bounds reserved as gutter between cells. Clamped on assignment to <c>[0.0, 0.2]</c>.</summary>
    public double CellSpacing
    {
        get => _cellSpacing;
        set => Change(ref _cellSpacing, Math.Clamp(value, 0.0, 0.2));
    }

    /// <summary>Optional palette for hue groups. When null the renderer uses the active theme's PropCycler.</summary>
    public Color[]? HuePalette { get; set => Change(ref field, value); }

    private const int RestingHexbinGridSize = 15;
    /// <summary>Resolution of the hexagonal density grid when
    /// <see cref="OffDiagonalKind"/> is <see cref="PairGridOffDiagonalKind.Hexbin"/>.
    /// Higher values produce finer (smaller) hexagons. Default <see cref="RestingHexbinGridSize"/>; ignored for
    /// <see cref="PairGridOffDiagonalKind.Scatter"/> and <see cref="PairGridOffDiagonalKind.None"/>.</summary>
    public int HexbinGridSize { get; set => Change(ref field, value); } = RestingHexbinGridSize;

    /// <summary>Optional colour map for off-diagonal density rendering when
    /// <see cref="OffDiagonalKind"/> is <see cref="PairGridOffDiagonalKind.Hexbin"/>.
    /// Encodes the per-hex point count. Defaults to <c>Viridis</c> when null.
    /// Property name is general-purpose (<c>OffDiagonal*</c>, not <c>Hexbin*</c>) so future
    /// off-diagonal kinds with density semantics can reuse it.</summary>
    public IColorMap? OffDiagonalColorMap { get; set => Change(ref field, value); }

    /// <summary>Initializes a new instance of <see cref="PairGridSeries"/>.</summary>
    /// <param name="variables">The N input variables (each <c>double[]</c> = one variable's samples).
    /// Must contain ≥ 1 variable, and all sub-arrays must have equal length.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="variables"/> is empty or
    /// contains sub-arrays of unequal length.</exception>
    public PairGridSeries(double[][] variables)
    {
        variables.ThrowIfEmpty("PairGridSeries requires at least one variable.");
        variables.Select(variable => variable.Length).ThrowIfNotAllEqual(nameof(variables), "variable");
        Variables = variables;
    }

    /// <summary>The densities the last estimate made, with what they were made from.</summary>
    [RenderCache]
    private KeptEstimate<PairGridDensity[][]>? _diagonalDensities;

    [RenderCache]
    private DataSnapshot? _dataCopy;

    ref DataSnapshot? IKeepsDataCopy.DataCopy => ref _dataCopy;

    /// <summary>The density curves of a <see cref="PairGridDiagonalKind.Kde"/> diagonal under <paramref name="looks"/>, one array per
    /// variable: one curve per hue group, smallest group first, when the grid is split by hue (<c>HasValidHue</c>), else one curve of
    /// the whole variable. Each is the density curve of its sample by the theme's rule (<c>DensityCurve</c> under
    /// <see cref="Styling.ThemeLooks.DensityCurveRule"/>, the rule a <see cref="KdeSeries"/> is estimated by: seaborn's <c>pairplot</c>
    /// draws its diagonal with <c>kdeplot</c>). They are estimated once and kept until a setting or a value of <see cref="Variables"/>
    /// or <see cref="HueGroups"/> changes or they are asked under another rule (<see cref="KeptEstimate{T}"/>), so a figure drawn again
    /// with the grid unchanged estimates no density.</summary>
    /// <param name="looks">The looks of the theme the grid is drawn under.</param>
    /// <returns>The densities, by variable then group; the caller reads them and never writes into them.</returns>
    internal PairGridDensity[][] DiagonalDensitiesUnder(Styling.ThemeLooks looks) =>
        KeptEstimate<PairGridDensity[][]>.Of(ref _diagonalDensities, this, looks.DensityCurveRule,
            static (series, rule) => series.EstimateDiagonalDensities(rule));

    /// <summary>Estimates the density of every variable, or of every hue group of it, under <paramref name="rule"/>.</summary>
    private PairGridDensity[][] EstimateDiagonalDensities(Numerics.DensityRule rule)
    {
        bool byHue = this.HasValidHue;
        var densities = new PairGridDensity[Variables.Length][];
        for (int v = 0; v < densities.Length; v++)
        {
            densities[v] = byHue
                ? [.. this.SamplesByHue(v).Select(slice => new PairGridDensity(slice.Group, slice.Data.DensityCurve(null, rule)))]
                : [new PairGridDensity(0, Variables[v].DensityCurve(null, rule))];
        }

        return densities;
    }

    /// <inheritdoc />
    /// <remarks>The pair-grid renders sub-cells in their own sub-panel coordinate
    /// systems; the outer axes only need a placeholder range <c>[0, n]</c> so spine
    /// rendering and any consumer that reads the parent data range stays sane. The
    /// outer ticks are not aligned with cell centres (cell <c>i</c> spans
    /// <c>[i, i+1]</c>, centre at <c>i + 0.5</c>) — the renderer paints its own
    /// per-cell labels via the diagonal histograms / scatter cells, so outer-axis
    /// tick labels are typically suppressed at the figure level.</remarks>
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        int n = Variables.Length;
        return DataRangeContribution.Pinned(new(0, n), new(0, n));
    }

    static string ISeriesKind<PairGridSeries>.Kind => "pairgrid";

    /// <inheritdoc />
    /// <remarks>The hue palette travels in the colour list every series that colours its parts one by one shares;
    /// the sub-renderer state is rebuilt on every draw and is not written.</remarks>
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        Colors = HuePalette?.ToList(),
        Variables = Variables.JaggedTo2DList(),
        PairGridLabels       = Labels,
        PairGridHueGroups    = HueGroups,
        PairGridHueLabels    = HueLabels,
        PairGridDiagonal     = DiagonalKind.OrNullAtRest(RestingDiagonalKind)?.ToString(),
        PairGridOffDiagonal  = OffDiagonalKind.OrNullAtRest(RestingOffDiagonalKind)?.ToString(),
        PairGridTriangular   = Triangular.OrNullAtRest(RestingTriangular)?.ToString(),
        PairGridDiagonalBins = DiagonalBins.OrNullAtRest(RestingDiagonalBins),
        PairGridMarkerSize   = Dot.Diameter / 2,
        PairGridCellSpacing  = _cellSpacing.OrNullAtRest(RestingCellSpacing),
        PairGridHexbinGridSize       = HexbinGridSize.OrNullAtRest(RestingHexbinGridSize),
        PairGridOffDiagonalMap = OffDiagonalColorMap.ColorMapOnTheWire(),
    };

    /// <summary>The reader of a <see cref="PairGridSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static PairGridSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var variables = dto.Variables.FromJaggedList();
        if (variables.Length == 0)
        {
            throw new InvalidOperationException(
                "Cannot reconstruct PairGridSeries: the JSON DTO has no Variables. " +
                "Pair-grid serialization requires at least one variable, matching the model constructor invariant.");
        }
        var s = axes.PairGrid(variables);
        if (dto.PairGridLabels is { } pairGridLabels) s.Labels = pairGridLabels;
        if (dto.PairGridHueGroups is { } pairGridHueGroups) s.HueGroups = pairGridHueGroups;
        if (dto.PairGridHueLabels is { } pairGridHueLabels) s.HueLabels = pairGridHueLabels;
        if (dto.PairGridDiagonal.WireEnum<PairGridDiagonalKind>() is { } pairGridDiagonal) s.DiagonalKind = pairGridDiagonal;
        if (dto.PairGridOffDiagonal.WireEnum<PairGridOffDiagonalKind>() is { } pairGridOffDiagonal) s.OffDiagonalKind = pairGridOffDiagonal;
        if (dto.PairGridTriangular.WireEnum<PairGridTriangle>() is { } pairGridTriangular) s.Triangular = pairGridTriangular;
        if (dto.PairGridDiagonalBins is { } pairGridDiagonalBins) s.DiagonalBins = pairGridDiagonalBins;
        if (dto.PairGridMarkerSize is { } pairGridMarkerSize) s.MarkerSize = pairGridMarkerSize;
        if (dto.PairGridCellSpacing is { } pairGridCellSpacing) s.CellSpacing = pairGridCellSpacing;
        if (dto.PairGridHexbinGridSize is { } pairGridHexbinGridSize) s.HexbinGridSize = pairGridHexbinGridSize;
        s.OffDiagonalColorMap = dto.PairGridOffDiagonalMap.Read();
        s.HuePalette = dto.Colors?.ToArray();
        return s;
    }


    /// <inheritdoc />
    /// <remarks>Long form over the variables: one row per sample per variable, named by the variable's own
    /// label. The grid draws every PAIR of them; the data is the variables.</remarks>
    public override ChartDataTable? ToDataTable() =>
        ChartDataTable.OfGroups([new("variable", DataColumnKind.Text), new(Label ?? "value")], Labels, "variable ", Variables,
            static value => [DataCell.FromNumber(value)]);

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}

/// <summary>One density curve on a pair grid's diagonal.</summary>
/// <param name="Group">The hue group it is the density of, which picks its colour; 0 when the grid is not split by hue.</param>
/// <param name="Curve">The density curve of the group's samples (<c>DensityCurve</c>); <see langword="null"/> when none of them
/// can be drawn.</param>
internal readonly record struct PairGridDensity(int Group, XYCurve? Curve);
