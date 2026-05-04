// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
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
public sealed class PairGridSeries : ChartSeries
{
    private double _cellSpacing = 0.02;

    /// <summary>The N input variables. Each <c>double[]</c> is one variable's samples.
    /// All sub-arrays must have equal length; constructor throws otherwise.</summary>
    /// <remarks>The outer array is get-only; the inner per-variable <c>double[]</c> arrays
    /// are exposed by reference for zero-copy rendering. Callers must treat each inner
    /// array as immutable after constructor return — the renderer reads them on every
    /// frame without defensive copies.</remarks>
    public double[][] Variables { get; }

    /// <summary>Optional axis labels, one per variable (length = <c>Variables.Length</c>).
    /// Defaults to <c>"v0", "v1", …</c> at render time when null or short.</summary>
    public string[]? Labels { get; set; }

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
    public int[]? HueGroups { get; set; }

    /// <summary>Optional human-readable labels for distinct hue groups, indexed by
    /// group ID. <c>HueLabels[g]</c> is the legend label for rows where
    /// <c>HueGroups[i] == g</c>. When null the legend renders the integer group IDs.</summary>
    public string[]? HueLabels { get; set; }

    /// <summary>Univariate-distribution kind for the diagonal cells. Default
    /// <see cref="PairGridDiagonalKind.Histogram"/>.</summary>
    public PairGridDiagonalKind DiagonalKind { get; set; } = PairGridDiagonalKind.Histogram;

    /// <summary>Bivariate-distribution kind for the off-diagonal cells. Default
    /// <see cref="PairGridOffDiagonalKind.Scatter"/>.</summary>
    public PairGridOffDiagonalKind OffDiagonalKind { get; set; } = PairGridOffDiagonalKind.Scatter;

    /// <summary>Which triangle of the N×N grid to render. <see cref="PairGridTriangle.Both"/> = full grid (default).</summary>
    public PairGridTriangle Triangular { get; set; } = PairGridTriangle.Both;

    /// <summary>Bin count for diagonal histograms. Default <c>20</c>.</summary>
    public int DiagonalBins { get; set; } = 20;

    /// <summary>Marker radius in pixels for off-diagonal scatter dots. Default <c>3.0</c>.</summary>
    /// <remarks>This is the dot's pixel <em>radius</em>, not matplotlib's pt² area
    /// convention used by <c>ScatterSeries.MarkerSize</c> (which converts pt² →
    /// radius via <c>√(s_pt² / π) × DPI/72</c>). PairGrid renders cells in their own
    /// pixel-space sub-panels with no parent-axis coordinate transform, so a px
    /// radius is the directly meaningful unit. To get a 5 pt² Scatter equivalent,
    /// pass <c>MarkerSize = √(5/π) ≈ 1.26</c>.</remarks>
    public double MarkerSize { get; set; } = 3.0;

    /// <summary>Fraction of plot bounds reserved as gutter between cells. Clamped on assignment to <c>[0.0, 0.2]</c>.</summary>
    public double CellSpacing
    {
        get => _cellSpacing;
        set => _cellSpacing = Math.Clamp(value, 0.0, 0.2);
    }

    /// <summary>Optional palette for hue groups. When null the renderer uses the active theme's PropCycler.</summary>
    public Color[]? HuePalette { get; set; }

    /// <summary>Resolution of the hexagonal density grid when
    /// <see cref="OffDiagonalKind"/> is <see cref="PairGridOffDiagonalKind.Hexbin"/>.
    /// Higher values produce finer (smaller) hexagons. Default <c>15</c>; ignored for
    /// <see cref="PairGridOffDiagonalKind.Scatter"/> and <see cref="PairGridOffDiagonalKind.None"/>.</summary>
    public int HexbinGridSize { get; set; } = 15;

    /// <summary>Optional colour map for off-diagonal density rendering when
    /// <see cref="OffDiagonalKind"/> is <see cref="PairGridOffDiagonalKind.Hexbin"/>.
    /// Encodes the per-hex point count. Defaults to <c>Viridis</c> when null.
    /// Property name is general-purpose (<c>OffDiagonal*</c>, not <c>Hexbin*</c>) so future
    /// off-diagonal kinds with density semantics can reuse it.</summary>
    public IColorMap? OffDiagonalColorMap { get; set; }

    /// <summary>Initializes a new instance of <see cref="PairGridSeries"/>.</summary>
    /// <param name="variables">The N input variables (each <c>double[]</c> = one variable's samples).
    /// Must contain ≥ 1 variable, and all sub-arrays must have equal length.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="variables"/> is empty or
    /// contains sub-arrays of unequal length.</exception>
    public PairGridSeries(double[][] variables)
    {
        if (variables.Length == 0)
            throw new ArgumentException("PairGridSeries requires at least one variable.", nameof(variables));
        int n = variables[0].Length;
        for (int i = 1; i < variables.Length; i++)
        {
            if (variables[i].Length != n)
                throw new ArgumentException(
                    $"All variables must have equal length; variable[{i}] has length {variables[i].Length}, expected {n}.",
                    nameof(variables));
        }
        Variables = variables;
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
        return new(0, n, 0, n,
            StickyXMin: 0, StickyXMax: n, StickyYMin: 0, StickyYMax: n);
    }

    /// <inheritdoc />
    /// <remarks>The hue palette and inter-cell sub-renderer state are not serialised —
    /// same project-wide convention as <see cref="HeatmapSeries.Normalizer"/> and the
    /// <see cref="ClustermapSeries"/> trees.</remarks>
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "pairgrid",
        Variables = ChartSerializer.JaggedTo2DList(Variables),
        PairGridLabels       = Labels,
        PairGridHueGroups    = HueGroups,
        PairGridHueLabels    = HueLabels,
        PairGridDiagonal     = DiagonalKind    != PairGridDiagonalKind.Histogram ? DiagonalKind.ToString()    : null,
        PairGridOffDiagonal  = OffDiagonalKind != PairGridOffDiagonalKind.Scatter ? OffDiagonalKind.ToString() : null,
        PairGridTriangular   = Triangular      != PairGridTriangle.Both           ? Triangular.ToString()      : null,
        PairGridDiagonalBins = DiagonalBins != 20    ? DiagonalBins : null,
        PairGridMarkerSize   = MarkerSize  != 3.0    ? MarkerSize   : null,
        PairGridCellSpacing  = _cellSpacing != 0.02  ? _cellSpacing : null,
        PairGridHexbinGridSize       = HexbinGridSize != 15 ? HexbinGridSize : null,
        PairGridOffDiagonalColorMap  = OffDiagonalColorMap?.Name,
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
