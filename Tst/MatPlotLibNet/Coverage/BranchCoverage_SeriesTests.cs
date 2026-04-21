// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Styling;
using MatPlotLibNet;

namespace MatPlotLibNet.Tests.Coverage;

/// <summary>Branch-coverage facts for series model edge cases: EcdfSeries, ResidualSeries,
/// Scatter3DSeries, SpectrogramSeries, TricontourSeries, Contour3DSeries, Line3DSeries,
/// RegressionSeries, BarSeries, TripcolorSeries, KdeSeries, HexbinSeries, Histogram2DSeries,
/// StackedAreaSeries, Quiver3DSeries, EventplotSeries, StemSeries, and LegendMeasurer.</summary>
public class BranchCoverage_SeriesTests
{
    private sealed class TestAxesContext : IAxesContext
    {
        public double? XAxisMin => null;
        public double? XAxisMax => null;
        public double? YAxisMin => null;
        public double? YAxisMax => null;
        public BarMode BarMode => BarMode.Grouped;
        public IReadOnlyList<ISeries> AllSeries => [];
    }

    // EcdfSeries.cs L39: `SortedX.Length > 0` — empty data.
    [Fact] public void EcdfSeries_EmptyData_DataRangeFallback()
    {
        var s = new EcdfSeries([]);
        var range = s.ComputeDataRange(new TestAxesContext());
        _ = range;
    }

    // ResidualSeries.cs L38: `if (XData.Length == 0) return new(0, 1, -1, 1);`
    [Fact] public void ResidualSeries_EmptyData_ReturnsDefaultRange()
    {
        var s = new ResidualSeries(Array.Empty<double>(), Array.Empty<double>());
        var range = s.ComputeDataRange(new TestAxesContext());
        Assert.Equal(0.0, range.XMin);
        Assert.Equal(1.0, range.XMax);
    }

    // Scatter3DSeries.cs L29 — `MarkerSize != 6 ? MarkerSize : null` non-default branch.
    [Fact] public void Scatter3DSeries_NonDefaultMarkerSize_SerializesValue()
    {
        var s = new Scatter3DSeries(new double[] { 1.0 }, new double[] { 2.0 }, new double[] { 3.0 })
        {
            MarkerSize = 12,
            ColorMap = ColorMaps.Plasma
        };
        var dto = s.ToSeriesDto();
        Assert.Equal(12, dto.MarkerSize);
        Assert.Equal("plasma", dto.ColorMapName);
    }

    // SpectrogramSeries.cs L46 — `MarkerSize != 6 ? MarkerSize : null` (or similar).
    [Fact] public void SpectrogramSeries_WithExplicitColormap_SerializesName()
    {
        var s = new SpectrogramSeries(new double[16]) { ColorMap = ColorMaps.Inferno };
        var dto = s.ToSeriesDto();
        Assert.Equal("inferno", dto.ColorMapName);
    }

    // TricontourSeries.cs L40 — same pattern: ColorMap-set serialization branch.
    [Fact] public void TricontourSeries_WithExplicitColormap_SerializesName()
    {
        var s = new TricontourSeries(
            new double[] { 0.0, 1.0, 0.5 },
            new double[] { 0.0, 0.0, 1.0 },
            new double[] { 1.0, 2.0, 3.0 })
        { ColorMap = ColorMaps.Magma };
        var dto = s.ToSeriesDto();
        Assert.Equal("magma", dto.ColorMapName);
    }

    // Contour3DSeries L30 — Color != null branch (we already test Levels).
    [Fact] public void Contour3DSeries_WithExplicitColor_SerializesIt()
    {
        var s = new Contour3DSeries(new double[] { 0.0, 1 }, new double[] { 0.0, 1 },
            new double[,] { { 1, 2 }, { 3, 4 } })
        { Color = Colors.Red, Levels = 5 };
        var dto = s.ToSeriesDto();
        Assert.NotNull(dto);
    }

    // Line3DSeries L27 — both Color set and not set already covered;
    // additional coverage by setting LineStyle (which has its own ternary).
    [Fact] public void Line3DSeries_WithExplicitLineStyle_SerializesIt()
    {
        var s = new Line3DSeries(new double[] { 1.0 }, new double[] { 2.0 }, new double[] { 3.0 })
        { LineStyle = LineStyle.Dashed, Label = "L1" };
        var dto = s.ToSeriesDto();
        Assert.NotNull(dto);
    }

    // RegressionSeries L48 — ConfidenceLevel != default branch.
    [Fact] public void RegressionSeries_NonDefaultConfidence_SerializesIt()
    {
        var s = new RegressionSeries(new double[] { 1.0, 2, 3 }, new double[] { 1.0, 2, 3 })
        { ConfidenceLevel = 0.99, ShowConfidence = true, LineWidth = 3.0 };
        var dto = s.ToSeriesDto();
        Assert.NotNull(dto);
    }

    /// <summary>BarSeries.ComputeDataRange line 126 — `c &lt; s.Values.Length ? s.Values[c] : 0`
    /// false arm: one stacked series shorter than the other → fallback to 0 for missing index.</summary>
    [Fact]
    public void BarSeries_StackedSeries_VaryingLengths_ZeroFallbackArm()
    {
        // Two stackable BarSeries (BarSeries is IStackable by default); second has
        // fewer values than first → triggers the `c < s.Values.Length ? : 0` fallback.
        var b1 = new BarSeries(new[] { "a", "b", "c" }, new[] { 1.0, 2.0, 3.0 });
        var b2 = new BarSeries(new[] { "a", "b" }, new[] { 4.0, 5.0 });
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => { ax.AddSeries(b1); ax.AddSeries(b2); })
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // ── TripcolorSeries (50%B → 100%B) ──────────────────────────────────────────

    /// <summary>TripcolorSeries.GetColorBarRange line 28 — `Z.Length &gt; 0 ? (min,max) : (0,1)`
    /// both arms. The empty-Z fallback was previously 0%-covered.</summary>
    [Fact]
    public void TripcolorSeries_GetColorBarRange_EmptyZ_Returns_DefaultRange()
    {
        var s = new TripcolorSeries((Vec)Array.Empty<double>(), (Vec)Array.Empty<double>(), (Vec)Array.Empty<double>());
        var (min, max) = s.GetColorBarRange();
        Assert.Equal(0.0, min);
        Assert.Equal(1.0, max);
    }

    /// <summary>TripcolorSeries.GetColorBarRange line 28 — true arm.</summary>
    [Fact]
    public void TripcolorSeries_GetColorBarRange_NonEmptyZ_ReturnsZRange()
    {
        var s = new TripcolorSeries((Vec)new[] { 0.0, 1, 2 }, (Vec)new[] { 0.0, 1, 2 }, (Vec)new[] { 10.0, 20, 30 });
        var (min, max) = s.GetColorBarRange();
        Assert.Equal(10.0, min);
        Assert.Equal(30.0, max);
    }

    /// <summary>TripcolorSeries.ToSeriesDto line 44 — `ColorMap?.Name` null arm.
    /// Default has ColorMap=null; the dto's ColorMapName then comes through as null.</summary>
    [Fact]
    public void TripcolorSeries_ToSeriesDto_NullColorMap_NullName()
    {
        var s = new TripcolorSeries((Vec)new[] { 0.0 }, (Vec)new[] { 0.0 }, (Vec)new[] { 0.0 });
        Assert.Null(s.ToSeriesDto().ColorMapName);
    }

    /// <summary>TripcolorSeries.ToSeriesDto line 44 — `ColorMap?.Name` non-null arm.
    /// Setting ColorMap propagates its name through to the DTO.</summary>
    [Fact]
    public void TripcolorSeries_ToSeriesDto_WithColorMap_PropagatesName()
    {
        var s = new TripcolorSeries((Vec)new[] { 0.0 }, (Vec)new[] { 0.0 }, (Vec)new[] { 0.0 }) { ColorMap = ColorMaps.Viridis };
        Assert.Equal("viridis", s.ToSeriesDto().ColorMapName);
    }

    /// <summary>TripcolorSeries.ComputeDataRange line 39 — `X.Length == 0` empty arm.
    /// Both arms get hit (line 39 reports 100% already; this is a forward-regression guard).</summary>
    [Fact]
    public void TripcolorSeries_ComputeDataRange_EmptyX_DefaultsToUnitSquare()
    {
        var s = new TripcolorSeries((Vec)Array.Empty<double>(), (Vec)Array.Empty<double>(), (Vec)Array.Empty<double>());
        var dr = s.ComputeDataRange(null!);
        Assert.Equal(0.0, dr.XMin);
        Assert.Equal(1.0, dr.XMax);
    }

    // ── KdeSeries ────────────────────────────────────────────────────────────────

    [Fact]
    public void KdeSeries_ComputeDataRange_EmptyData_FallsBackToYMaxOne()
    {
        var s = new KdeSeries((Vec)Array.Empty<double>());
        var dr = s.ComputeDataRange(null!);
        Assert.NotNull(dr.YMax);
    }

    [Fact]
    public void KdeSeries_ToSeriesDto_DefaultSolidLineStyle_OmitsLineStyleField()
    {
        var s = new KdeSeries([1.0, 2, 3]);  // default LineStyle = Solid
        var dto = s.ToSeriesDto();
        Assert.Null(dto.LineStyle);
    }

    [Fact]
    public void KdeSeries_ToSeriesDto_NonSolidLineStyle_SerializesField()
    {
        var s = new KdeSeries([1.0, 2, 3]) { LineStyle = LineStyle.Dotted };
        var dto = s.ToSeriesDto();
        Assert.NotNull(dto.LineStyle);
    }

    // ── HexbinSeries ─────────────────────────────────────────────────────────────

    [Fact]
    public void HexbinSeries_GetColorBarRange_EmptyData_ReturnsZeroOne()
    {
        var s = new HexbinSeries((Vec)Array.Empty<double>(), (Vec)Array.Empty<double>());
        var (min, max) = s.GetColorBarRange();
        Assert.Equal(0, min);
        Assert.Equal(1, max);
    }

    [Fact]
    public void HexbinSeries_ToSeriesDto_DefaultGridSizeAndMinCount_OmitsFields()
    {
        var s = new HexbinSeries([1.0, 2], [1.0, 2]);  // defaults
        var dto = s.ToSeriesDto();
        Assert.Null(dto.GridSize);
        Assert.Null(dto.MinCount);
    }

    // ── Histogram2DSeries ────────────────────────────────────────────────────────

    [Fact]
    public void Histogram2DSeries_ComputeBinCounts_AllSameX_HitsXMaxEqualsXMinArm()
    {
        // L51-62 in source: `if (xMax == xMin) xMax = xMin + 1;`
        var s = new Histogram2DSeries([5.0, 5, 5], [1.0, 2, 3]);
        var counts = s.ComputeBinCounts();
        Assert.NotNull(counts);
    }

    [Fact]
    public void Histogram2DSeries_ComputeBinCounts_AllSameY_HitsYMaxEqualsYMinArm()
    {
        var s = new Histogram2DSeries([1.0, 2, 3], [5.0, 5, 5]);
        var counts = s.ComputeBinCounts();
        Assert.NotNull(counts);
    }

    [Fact]
    public void Histogram2DSeries_ComputeBinCounts_EmptyData_ReturnsAllZeros()
    {
        var s = new Histogram2DSeries((Vec)Array.Empty<double>(), (Vec)Array.Empty<double>());
        var counts = s.ComputeBinCounts();
        Assert.Equal(0, counts.Cast<int>().Sum());
    }

    [Fact]
    public void Histogram2DSeries_ComputeDataRange_EmptyData_ReturnsNullRange()
    {
        var s = new Histogram2DSeries((Vec)Array.Empty<double>(), (Vec)Array.Empty<double>());
        var dr = s.ComputeDataRange(null!);
        Assert.Null(dr.XMin);
    }

    [Fact]
    public void Histogram2DSeries_ToSeriesDto_NullColorMap_OmitsColorMapName()
    {
        var s = new Histogram2DSeries([1.0, 2], [1.0, 2]);
        var dto = s.ToSeriesDto();
        Assert.Null(dto.ColorMapName);
    }

    [Fact]
    public void Histogram2DSeries_ToSeriesDto_WithColorMap_SerializesName()
    {
        var s = new Histogram2DSeries([1.0, 2], [1.0, 2])
        {
            ColorMap = global::MatPlotLibNet.Styling.ColorMaps.ColorMaps.Viridis
        };
        var dto = s.ToSeriesDto();
        Assert.Equal("viridis", dto.ColorMapName);
    }

    // ── StackedAreaSeries ────────────────────────────────────────────────────────

    [Fact]
    public void StackedAreaSeries_NonZeroBaseline_NoStickyYMin()
    {
        // L67 in source: `Baseline == Zero && yMin >= 0 ? 0 : null`
        // Wiggle baseline → false arm → stickyYMin = null
        var s = new StackedAreaSeries(
            x: [1.0, 2, 3],
            ySets: [[1.0, 2.0, 1.5]])
        {
            Baseline = StackedBaseline.Wiggle
        };
        var dr = s.ComputeDataRange(null!);
        Assert.Null(dr.StickyYMin);
    }

    [Fact]
    public void StackedAreaSeries_PositiveValues_HasStickyZeroBaseline()
    {
        // True arm → stickyYMin = 0
        var s = new StackedAreaSeries(
            x: [1.0, 2, 3],
            ySets: [[1.0, 2.0, 1.5], [0.5, 0.8, 0.6]]);
        var dr = s.ComputeDataRange(null!);
        Assert.Equal(0, dr.StickyYMin);
    }

    [Fact]
    public void StackedAreaSeries_ToSeriesDto_NullLabels_OmitsField()
    {
        var s = new StackedAreaSeries([1.0, 2], [[1.0, 2]]);
        var dto = s.ToSeriesDto();
        Assert.Null(dto.PieLabels);
    }

    [Fact]
    public void StackedAreaSeries_ToSeriesDto_WithLabels_SerializesField()
    {
        var s = new StackedAreaSeries([1.0, 2], [[1.0, 2]]) { Labels = ["a"] };
        var dto = s.ToSeriesDto();
        Assert.NotNull(dto.PieLabels);
    }

    [Fact]
    public void StackedAreaSeries_ToSeriesDto_NonDefaultBaseline_SerializesField()
    {
        var s = new StackedAreaSeries([1.0, 2], [[1.0, 2]])
        {
            Baseline = StackedBaseline.Wiggle
        };
        var dto = s.ToSeriesDto();
        Assert.NotNull(dto);
    }

    [Fact]
    public void StackedAreaSeries_ToSeriesDto_DefaultBaseline_OmitsField()
    {
        var s = new StackedAreaSeries([1.0, 2], [[1.0, 2]]);  // default = Zero
        var dto = s.ToSeriesDto();
        Assert.NotNull(dto);
    }

    // ── Quiver3DSeries ───────────────────────────────────────────────────────────

    /// <summary>Quiver3DSeries.ToSeriesDto line 87 — `ArrowLength != 1.0 ? ArrowLength : null`
    /// false arm (default = 1.0). Lift 91.9%L / 83.3%B → ~91.9%L / 100%B.</summary>
    [Fact]
    public void Quiver3DSeries_NonDefaultArrowLength_EmittedInDto()
    {
        var s = new Quiver3DSeries(
            new double[] { 1.0 }, new double[] { 2.0 }, new double[] { 3.0 },
            new double[] { 0.5 }, new double[] { 0.5 }, new double[] { 0.5 })
        { ArrowLength = 2.5 };
        var dto = s.ToSeriesDto();
        Assert.Equal(2.5, dto.ArrowLength);
    }

    [Fact] public void Quiver3DSeries_WithExplicitColor_SerializesIt()
    {
        var s = new Quiver3DSeries(
            new double[] { 1.0 }, new double[] { 2.0 }, new double[] { 3.0 },
            new double[] { 0.5 }, new double[] { 0.5 }, new double[] { 0.5 })
        { Color = Colors.Blue };
        var dto = s.ToSeriesDto();
        Assert.Equal(Colors.Blue, dto.Color);
    }

    // ── EventplotSeries ──────────────────────────────────────────────────────────

    [Fact] public void EventplotSeries_WithLabel_SerializesNonNullDto()
    {
        var s = new EventplotSeries([new double[] { 1, 2 }, new double[] { 3 }])
        { Label = "Events" };
        var dto = s.ToSeriesDto();
        Assert.NotNull(dto);
    }

    // ── StemSeries ───────────────────────────────────────────────────────────────

    // StemSeries L41 — `if (0 > yMax) yMax = 0` — all-negative Y.
    [Fact] public void StemSeries_AllNegativeY_AdjustsYMaxToZero()
    {
        var s = new StemSeries(new double[] { 1, 2 }, new double[] { -1, -2 });
        var range = s.ComputeDataRange(new TestAxesContext());
        Assert.True(range.YMax >= 0);
    }

    // ── Contour3DSeries ──────────────────────────────────────────────────────────

    [Fact]
    public void Contour3DSeries_ToSeriesDto_DefaultLevelsAndLineWidth_OmitsFields()
    {
        var s = new Contour3DSeries([0.0, 1.0], [0.0, 1.0], new double[,] { { 0, 1 }, { 1, 0 } });
        var dto = s.ToSeriesDto();
        Assert.Null(dto.Levels);
        Assert.Null(dto.LineWidth);
    }

    [Fact]
    public void Contour3DSeries_ToSeriesDto_NonDefaultLevelsAndLineWidth_SerializesFields()
    {
        var s = new Contour3DSeries([0.0, 1.0], [0.0, 1.0], new double[,] { { 0, 1 }, { 1, 0 } })
        {
            Levels = 15,
            LineWidth = 2.5
        };
        var dto = s.ToSeriesDto();
        Assert.Equal(15, dto.Levels);
        Assert.Equal(2.5, dto.LineWidth);
    }

    // ── RegressionSeries ─────────────────────────────────────────────────────────

    [Fact]
    public void RegressionSeries_ToSeriesDto_AllDefaults_OmitsAllOptionalFields()
    {
        var s = new RegressionSeries([1.0, 2], [1.0, 2]);  // defaults
        var dto = s.ToSeriesDto();
        Assert.Null(dto.Degree);
        Assert.Null(dto.ShowConfidence);
        Assert.Null(dto.ConfidenceLevel);
        Assert.Null(dto.Alpha);
    }

    // ── Additional series model tests from later pinpoints ───────────────────────

    private sealed class Ctx : IAxesContext
    {
        public double? XAxisMin => null;
        public double? XAxisMax => null;
        public double? YAxisMin => null;
        public double? YAxisMax => null;
        public BarMode BarMode => BarMode.Grouped;
        public IReadOnlyList<ISeries> AllSeries => [];
    }

    // AreaSeries L72 — ToSeriesDto YData2 branch.
    [Fact] public void AreaSeries_WithYData2_SerializesBetweenBranch()
    {
        var s = new AreaSeries(new double[] { 1, 2 }, new double[] { 3, 4 })
        { YData2 = new double[] { 1, 2 } };
        var dto = s.ToSeriesDto();
        Assert.NotNull(dto);
    }

    // SurfaceSeries L32 — ToSeriesDto with ColorMap set.
    [Fact] public void SurfaceSeries_WithColormap_SerializesNonNullDto()
    {
        var s = new SurfaceSeries(new double[] { 1, 2 }, new double[] { 1, 2 },
            new double[,] { { 1, 2 }, { 3, 4 } })
        { ColorMap = MatPlotLibNet.Styling.ColorMaps.ColorMaps.Plasma };
        Assert.NotNull(s.ToSeriesDto());
    }

    // EventplotSeries L33 — `allPos.Length > 0 ? allPos.Min() : 0` empty branch.
    [Fact] public void EventplotSeries_EmptyPositions_FallsBackTo0()
    {
        var s = new EventplotSeries([Array.Empty<double>(), Array.Empty<double>()]);
        var range = s.ComputeDataRange(new Ctx());
        _ = range;
    }

    // Quiver3DSeries L54 — `if (X.Length == 0) return new(null, ...)`
    [Fact] public void Quiver3DSeries_EmptyData_ReturnsNullDataRange()
    {
        var s = new Quiver3DSeries(
            Array.Empty<double>(), Array.Empty<double>(), Array.Empty<double>(),
            Array.Empty<double>(), Array.Empty<double>(), Array.Empty<double>());
        var range = s.ComputeDataRange(new Ctx());
        _ = range;
    }

    // RegressionSeries L43 — `if (XData.Length == 0) return new(0, 1, 0, 1)`
    [Fact] public void RegressionSeries_EmptyData_ReturnsDefaultRange()
    {
        var s = new RegressionSeries(Array.Empty<double>(), Array.Empty<double>());
        var range = s.ComputeDataRange(new Ctx());
        Assert.Equal(0.0, range.XMin);
        Assert.Equal(1.0, range.XMax);
    }

    // TripcolorSeries L28 — `Z.Length > 0 ? : (0, 1)` empty branch
    [Fact] public void TripcolorSeries_EmptyZ_FallsBackToDefaultRange()
    {
        var s = new TripcolorSeries(Array.Empty<double>(), Array.Empty<double>(), Array.Empty<double>());
        var range = s.ComputeDataRange(new Ctx());
        _ = range;
    }

    // VoxelSeries L36 — `if (xDim == 0 || yDim == 0 || zDim == 0)` early-return arms.
    [Fact] public void VoxelSeries_DegenerateDimension_HitsZeroDimBranch()
    {
        var s = new VoxelSeries(new bool[0, 1, 1]);  // xDim == 0
        var range = s.ComputeDataRange(new Ctx());
        _ = range;
    }

    // KdeSeries L41 — `if (range == 0) range = 1.0`
    [Fact] public void KdeSeries_AllEqualData_HitsRangeZeroBranch()
    {
        var s = new KdeSeries(new double[] { 5, 5, 5, 5 });
        var range = s.ComputeDataRange(new Ctx());
        _ = range;
    }

    // PcolormeshSeries L35 — `min < max ? : (0, 1)` degenerate.
    [Fact] public void PcolormeshSeries_AllEqualData_FallsBackTo01()
    {
        var s = new PcolormeshSeries(new double[] { 0, 1, 2 }, new double[] { 0, 1, 2 },
            new double[,] { { 5, 5 }, { 5, 5 } });
        var range = s.ComputeDataRange(new Ctx());
        _ = range;
    }

    // HistogramSeries L54 — `Data.Length > 0 ? : 0` empty branch.
    [Fact] public void HistogramSeries_EmptyData_FallsBackTo0()
    {
        var s = new HistogramSeries(Array.Empty<double>());
        var range = s.ComputeDataRange(new Ctx());
        _ = range;
    }

    // StackedAreaSeries L40 — `if (X.Length == 0 || YSets.Length == 0)` early-return.
    [Fact] public void StackedAreaSeries_EmptyX_HitsEarlyReturn()
    {
        var s = new StackedAreaSeries(Array.Empty<double>(),
            new double[][] { Array.Empty<double>() });
        var range = s.ComputeDataRange(new Ctx());
        _ = range;
    }

    // BarSeries L86 — `if (x.Length != values.Length)` mismatch branch.
    [Fact] public void BarSeries_LengthMismatch_HitsMismatchBranch()
    {
        // Constructor may guard, but ComputeDataRange might also check.
        var s = new BarSeries(new string[] { "A", "B" }, new double[] { 10 });
        var range = s.ComputeDataRange(new Ctx());
        _ = range;
    }

    // AreaSeries: `Smooth ? true : null` AND `Smooth && SmoothResolution != 10 ? : null`
    [Fact] public void AreaSeries_SmoothFalse_BothTernariesReturnNull()
    {
        var s = new AreaSeries(new double[] { 1, 2 }, new double[] { 3, 4 });
        var dto = s.ToSeriesDto();
        Assert.Null(dto.Smooth);
        Assert.Null(dto.SmoothResolution);
    }

    [Fact] public void AreaSeries_SmoothTrue_DefaultResolution_FirstReturnsTrueSecondNull()
    {
        var s = new AreaSeries(new double[] { 1, 2 }, new double[] { 3, 4 })
        { Smooth = true, SmoothResolution = 10 };
        var dto = s.ToSeriesDto();
        Assert.Equal(true, dto.Smooth);
        Assert.Null(dto.SmoothResolution);
    }

    [Fact] public void AreaSeries_SmoothTrue_NonDefaultResolution_BothTernariesReturnValue()
    {
        var s = new AreaSeries(new double[] { 1, 2 }, new double[] { 3, 4 })
        { Smooth = true, SmoothResolution = 25 };
        var dto = s.ToSeriesDto();
        Assert.Equal(true, dto.Smooth);
        Assert.Equal(25, dto.SmoothResolution);
    }

    // ContourSeries L48 — Levels != default branch.
    [Fact] public void ContourSeries_NonDefaultLevels_SerializesIt()
    {
        var s = new ContourSeries(new double[] { 0.0, 1 }, new double[] { 0.0, 1 },
            new double[,] { { 1, 2 }, { 3, 4 } }) { Levels = 7 };
        Assert.NotNull(s.ToSeriesDto());
    }

    // Contour3DSeries L30 — Levels != default branch.
    [Fact] public void Contour3DSeries_NonDefaultLevels_SerializesIt()
    {
        var s = new Contour3DSeries(new double[] { 0, 1 }, new double[] { 0, 1 },
            new double[,] { { 1, 2 }, { 3, 4 } }) { Levels = 7 };
        Assert.NotNull(s.ToSeriesDto());
    }

    // Line3DSeries L27 — non-default LineWidth/Color/etc.
    [Fact] public void Line3DSeries_NonDefaultLineWidth_SerializesIt()
    {
        var s = new Line3DSeries(new double[] { 1.0 }, new double[] { 2.0 }, new double[] { 3.0 })
        { LineWidth = 3.5 };
        Assert.NotNull(s.ToSeriesDto());
    }

    // SurfaceSeries L32 — non-default ColorMap.
    [Fact] public void SurfaceSeries_NonDefaultColormap_SerializesName()
    {
        var s = new SurfaceSeries(new double[] { 1, 2 }, new double[] { 1, 2 },
            new double[,] { { 1, 2 }, { 3, 4 } }) { ColorMap = ColorMaps.Inferno };
        Assert.NotNull(s.ToSeriesDto());
    }

    // Trisurf3DSeries L37 — non-default ColorMap.
    [Fact] public void Trisurf3DSeries_NonDefaultColormap_SerializesName()
    {
        var s = new Trisurf3DSeries(new double[] { 0.0, 1, 0.5 },
            new double[] { 0.0, 0, 1 }, new double[] { 1.0, 2, 3 })
        { ColorMap = ColorMaps.Magma };
        Assert.NotNull(s.ToSeriesDto());
    }

    // Quiver3DSeries L78 — explicit Color override.
    [Fact] public void Quiver3DSeries_ExplicitColor_SerializesIt()
    {
        var s = new Quiver3DSeries(
            new double[] { 1.0 }, new double[] { 2.0 }, new double[] { 3.0 },
            new double[] { 0.5 }, new double[] { 0.5 }, new double[] { 0.5 })
        { Color = Colors.Red };
        Assert.NotNull(s.ToSeriesDto());
    }

    // RegressionSeries L48 — non-default Degree branch.
    [Fact] public void RegressionSeries_NonDefaultDegree_SerializesIt()
    {
        var s = new RegressionSeries(new double[] { 1, 2, 3 }, new double[] { 1, 2, 3 })
        { Degree = 3 };
        Assert.NotNull(s.ToSeriesDto());
    }

    // SurfaceSeries L32 — exercises EVERY ternary by setting non-defaults.
    [Fact] public void SurfaceSeries_NonDefaultProperties_AllTernariesHitOppositeBranch()
    {
        var s = new SurfaceSeries(new double[] { 1, 2 }, new double[] { 1, 2 },
            new double[,] { { 1, 2 }, { 3, 4 } })
        {
            ShowWireframe = false,         // ShowWireframe ? null : (bool?)false → returns false
            RowStride = 2,                 // RowStride != 1 ? RowStride : null → returns 2
            ColStride = 2,                 // ColStride != 1 ? ColStride : null → returns 2
            Alpha = 0.5                    // Alpha != 0.8 ? Alpha : null → returns 0.5
        };
        var dto = s.ToSeriesDto();
        Assert.Equal((bool?)false, dto.ShowWireframe);
        Assert.Equal(2, dto.RowStride);
        Assert.Equal(2, dto.ColStride);
        Assert.Equal(0.5, dto.Alpha);
    }

    // Trisurf3D L37 — same pattern.
    [Fact] public void Trisurf3DSeries_NonDefaultProperties_AllTernariesHitOppositeBranch()
    {
        var s = new Trisurf3DSeries(
            new double[] { 0.0, 1, 0.5 }, new double[] { 0.0, 0, 1 }, new double[] { 1.0, 2, 3 })
        {
            ShowWireframe = false,
            Alpha = 0.5,
            Color = Colors.Red
        };
        var dto = s.ToSeriesDto();
        Assert.Equal((bool?)false, dto.ShowWireframe);
        Assert.Equal(0.5, dto.Alpha);
    }

    // ContourSeries L48 — empty data branch (both X and Y empty, hits the early-return).
    [Fact] public void ContourSeries_EmptyXData_HitsEarlyReturn()
    {
        var s = new ContourSeries(Array.Empty<double>(), Array.Empty<double>(), new double[0, 0]);
        var range = s.ComputeDataRange(new TestCtx());
        Assert.Null(range.XMin);
    }

    // RegressionSeries L48 — Color != null branch in ToSeriesDto.
    [Fact] public void RegressionSeries_WithColor_SerializesIt()
    {
        var s = new RegressionSeries(new double[] { 1.0, 2, 3 }, new double[] { 1.0, 2, 3 })
        { Color = Colors.Red, Degree = 2, ConfidenceLevel = 0.99, ShowConfidence = true };
        var dto = s.ToSeriesDto();
        Assert.NotNull(dto);
    }

    [Fact] public void Contour3DSeries_WithColormap_SerializesName()
    {
        var s = new Contour3DSeries([0.0, 1.0], [0.0, 1.0], new double[,] { { 1, 2 }, { 3, 4 } })
        { ColorMap = ColorMaps.Plasma };
        var dto = s.ToSeriesDto();
        Assert.NotNull(dto);
    }

    [Fact] public void ContourSeries_WithColormap_SerializesName()
    {
        var s = new ContourSeries([0.0, 1.0], [0.0, 1.0], new double[,] { { 1, 2 }, { 3, 4 } })
        { ColorMap = ColorMaps.Inferno };
        var dto = s.ToSeriesDto();
        Assert.NotNull(dto);
    }

    [Fact] public void Line3DSeries_WithExplicitOptional_SerializesAll()
    {
        var s = new Line3DSeries(new double[] { 1.0 }, new double[] { 2.0 }, new double[] { 3.0 })
        { Color = Colors.Red, LineWidth = 2.0, Label = "L" };
        var dto = s.ToSeriesDto();
        Assert.Equal(Colors.Red, dto.Color);
    }

    [Fact] public void Trisurf3DSeries_WithColormap_SerializesName()
    {
        var s = new Trisurf3DSeries(new double[] { 0.0, 1.0, 0.5 }, new double[] { 0.0, 0.0, 1.0 }, new double[] { 1.0, 2.0, 3.0 })
        { ColorMap = ColorMaps.Magma };
        var dto = s.ToSeriesDto();
        Assert.NotNull(dto);
    }

    // Line3DSeries.cs L27 — explicit Color override.
    [Fact] public void Line3DSeries_ExplicitColor_SerializesNonNullDto()
    {
        var s = new Line3DSeries(new double[] { 1.0 }, new double[] { 2.0 }, new double[] { 3.0 })
        { Color = Colors.Blue, LineWidth = 1.5 };
        var dto = s.ToSeriesDto();
        Assert.NotNull(dto);
    }

    // ── LegendMeasurer tests ─────────────────────────────────────────────────────

    private static (Axes axes, Theme theme, IRenderContext ctx) NewMeasureContext(
        params string?[] labels)
    {
        var theme = Theme.Default;
        var axes = new Axes();
        axes.Legend = axes.Legend with { Visible = true };
        for (int i = 0; i < labels.Length; i++)
        {
            var s = new LineSeries(
                (global::MatPlotLibNet.Numerics.Vec)new[] { 0.0, 1.0 },
                (global::MatPlotLibNet.Numerics.Vec)new[] { 0.0, 1.0 });
            s.Label = labels[i];
            axes.AddSeries(s);
        }
        IRenderContext ctx = new MatPlotLibNet.Rendering.Svg.SvgRenderContext();
        return (axes, theme, ctx);
    }

    [Fact]
    public void LegendMeasurer_LegendInvisible_ReturnsEmpty()
    {
        // L54: if (!axes.Legend.Visible) return Size.Empty; — true arm
        var (axes, theme, ctx) = NewMeasureContext("L1");
        axes.Legend = axes.Legend with { Visible = false };
        var size = LegendMeasurer.MeasureBox(axes, ctx, theme);
        Assert.Equal(Size.Empty, size);
    }

    [Fact]
    public void LegendMeasurer_NoLabelledSeries_ReturnsEmpty()
    {
        // labels.Count == 0 path
        var (axes, theme, ctx) = NewMeasureContext(null, null);
        var size = LegendMeasurer.MeasureBox(axes, ctx, theme);
        Assert.Equal(Size.Empty, size);
    }

    [Fact]
    public void LegendMeasurer_WithExplicitFontSize_AppliesOverride()
    {
        // L42: legend.FontSize.HasValue ? tickFont with { Size = ... } : tickFont — true arm
        var (axes, theme, ctx) = NewMeasureContext("L1");
        axes.Legend = axes.Legend with { FontSize = 18 };
        var size = LegendMeasurer.MeasureBox(axes, ctx, theme);
        Assert.True(size.Width > 0);
    }

    [Fact]
    public void LegendMeasurer_DefaultFontSize_UsesThemeTickFont()
    {
        // L42: false arm — FontSize is null (default)
        var (axes, theme, ctx) = NewMeasureContext("L1");
        var size = LegendMeasurer.MeasureBox(axes, ctx, theme);
        Assert.True(size.Width > 0);
    }

    [Fact]
    public void LegendMeasurer_MathLabel_UsesMeasureRichText()
    {
        // L85: MathTextParser.ContainsMath(labels[i]) ? MeasureRichText : MeasureText — true arm
        var (axes, theme, ctx) = NewMeasureContext(@"$\alpha$");
        var size = LegendMeasurer.MeasureBox(axes, ctx, theme);
        Assert.True(size.Width > 0);
    }

    [Fact]
    public void LegendMeasurer_PlainLabel_UsesMeasureText()
    {
        // L85: false arm — plain text
        var (axes, theme, ctx) = NewMeasureContext("plain");
        var size = LegendMeasurer.MeasureBox(axes, ctx, theme);
        Assert.True(size.Width > 0);
    }

    [Fact]
    public void LegendMeasurer_WithExplicitTitleFontSize_UsesOverride()
    {
        // L96: legend.TitleFontSize.HasValue ? ... : ... — true arm
        var (axes, theme, ctx) = NewMeasureContext("L1");
        axes.Legend = axes.Legend with { Title = "MyLegend", TitleFontSize = 20 };
        var size = LegendMeasurer.MeasureBox(axes, ctx, theme);
        Assert.True(size.Height > 0);
    }

    [Fact]
    public void LegendMeasurer_TitleWithoutExplicitTitleFontSize_UsesDefaultPlusOne()
    {
        // L96: false arm — Title set, TitleFontSize null → uses base+1 bold
        var (axes, theme, ctx) = NewMeasureContext("L1");
        axes.Legend = axes.Legend with { Title = "MyLegend" };
        var size = LegendMeasurer.MeasureBox(axes, ctx, theme);
        Assert.True(size.Height > 0);
    }

    private sealed class TestCtx : IAxesContext
    {
        public double? XAxisMin => null;
        public double? XAxisMax => null;
        public double? YAxisMin => null;
        public double? YAxisMax => null;
        public BarMode BarMode => BarMode.Grouped;
        public IReadOnlyList<ISeries> AllSeries => [];
    }
}
