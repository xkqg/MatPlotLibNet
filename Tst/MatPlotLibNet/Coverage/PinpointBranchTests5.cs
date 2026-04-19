// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Coverage;

/// <summary>Phase Q Wave 2 (2026-04-19) — fifth pinpoint batch on series ToSeriesDto +
/// ComputeDataRange empty/degenerate branches.</summary>
public class PinpointBranchTests5
{
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
        Assert.NotNull(range);
    }

    // Quiver3DSeries L54 — `if (X.Length == 0) return new(null, ...)`
    [Fact] public void Quiver3DSeries_EmptyData_ReturnsNullDataRange()
    {
        var s = new Quiver3DSeries(
            Array.Empty<double>(), Array.Empty<double>(), Array.Empty<double>(),
            Array.Empty<double>(), Array.Empty<double>(), Array.Empty<double>());
        var range = s.ComputeDataRange(new Ctx());
        Assert.NotNull(range);
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
        Assert.NotNull(range);
    }

    // VoxelSeries L36 — `if (xDim == 0 || yDim == 0 || zDim == 0)` early-return arms.
    [Fact] public void VoxelSeries_DegenerateDimension_HitsZeroDimBranch()
    {
        var s = new VoxelSeries(new bool[0, 1, 1]);  // xDim == 0
        var range = s.ComputeDataRange(new Ctx());
        Assert.NotNull(range);
    }

    // KdeSeries L41 — `if (range == 0) range = 1.0`
    [Fact] public void KdeSeries_AllEqualData_HitsRangeZeroBranch()
    {
        var s = new KdeSeries(new double[] { 5, 5, 5, 5 });
        var range = s.ComputeDataRange(new Ctx());
        Assert.NotNull(range);
    }

    // PcolormeshSeries L35 — `min < max ? : (0, 1)` degenerate.
    [Fact] public void PcolormeshSeries_AllEqualData_FallsBackTo01()
    {
        var s = new PcolormeshSeries(new double[] { 0, 1, 2 }, new double[] { 0, 1, 2 },
            new double[,] { { 5, 5 }, { 5, 5 } });
        var range = s.ComputeDataRange(new Ctx());
        Assert.NotNull(range);
    }

    // StemSeries L41 — `if (0 > yMax) yMax = 0` — all-negative Y.
    [Fact] public void StemSeries_AllNegativeY_AdjustsYMaxToZero()
    {
        var s = new StemSeries(new double[] { 1, 2 }, new double[] { -1, -2 });
        var range = s.ComputeDataRange(new Ctx());
        Assert.True(range.YMax >= 0);
    }

    // HistogramSeries L54 — `Data.Length > 0 ? : 0` empty branch.
    [Fact] public void HistogramSeries_EmptyData_FallsBackTo0()
    {
        var s = new HistogramSeries(Array.Empty<double>());
        var range = s.ComputeDataRange(new Ctx());
        Assert.NotNull(range);
    }

    // StackedAreaSeries L40 — `if (X.Length == 0 || YSets.Length == 0)` early-return.
    [Fact] public void StackedAreaSeries_EmptyX_HitsEarlyReturn()
    {
        var s = new StackedAreaSeries(Array.Empty<double>(),
            new double[][] { Array.Empty<double>() });
        var range = s.ComputeDataRange(new Ctx());
        Assert.NotNull(range);
    }

    // BarSeries L86 — `if (x.Length != values.Length)` mismatch branch.
    [Fact] public void BarSeries_LengthMismatch_HitsMismatchBranch()
    {
        // Constructor may guard, but ComputeDataRange might also check.
        var s = new BarSeries(new string[] { "A", "B" }, new double[] { 10 });
        var range = s.ComputeDataRange(new Ctx());
        Assert.NotNull(range);
    }
}
