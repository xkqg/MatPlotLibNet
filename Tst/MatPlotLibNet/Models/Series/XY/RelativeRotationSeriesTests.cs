// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Models.Series.XY;

/// <summary>v1.11.0 — Verifies <see cref="RelativeRotationSeries"/> construction,
/// default properties, compute pipeline (all three formulas), and
/// <c>ToSeriesDto</c> emission.</summary>
public class RelativeRotationSeriesTests
{
    // ── Fixtures ──────────────────────────────────────────────────────────────

    private static double[] Flat(int n, double v = 100.0) => Enumerable.Repeat(v, n).ToArray();
    private static double[] Rising(int n, double start = 100.0, double step = 1.0) =>
        Enumerable.Range(0, n).Select(i => start + i * step).ToArray();

    private static RelativeRotationSeries Sample(int n = 30) =>
        new(
            assetCloses:    [Rising(n), Flat(n)],
            benchmarkCloses: Flat(n),
            assetLabels:     ["ETH", "BNB"]);

    // ── Construction ─────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_StoresAllInputs()
    {
        var s = Sample(30);
        Assert.Equal(2, s.AssetCloses.Count);
        Assert.Equal(30, s.BenchmarkCloses.Count);
        Assert.Equal(2, s.AssetLabels.Count);
    }

    [Fact]
    public void Constructor_AcceptsEmptyAssets()
    {
        var s = new RelativeRotationSeries([], [], []);
        Assert.Empty(s.AssetCloses);
    }

    [Fact]
    public void Constructor_Throws_WhenAssetLabelCountMismatch()
    {
        var ex = Record.Exception(() =>
            new RelativeRotationSeries([Flat(10)], Flat(10), ["ETH", "SOL"]));
        Assert.IsType<ArgumentException>(ex);
    }

    [Fact]
    public void Constructor_Throws_WhenAssetLengthMismatchesBenchmark()
    {
        var ex = Record.Exception(() =>
            new RelativeRotationSeries([Flat(5)], Flat(10), ["ETH"]));
        Assert.IsType<ArgumentException>(ex);
    }

    // ── Default properties ────────────────────────────────────────────────────

    [Fact]
    public void Formula_DefaultsDualEma()
    {
        Assert.Equal(RrgFormula.DualEma, Sample().Formula);
    }

    [Fact]
    public void ShortPeriod_Defaults10()
    {
        Assert.Equal(10, Sample().ShortPeriod);
    }

    [Fact]
    public void LongPeriod_Defaults26()
    {
        Assert.Equal(26, Sample().LongPeriod);
    }

    [Fact]
    public void MomentumLookback_Defaults10()
    {
        Assert.Equal(10, Sample().MomentumLookback);
    }

    [Fact]
    public void TailLength_Defaults8()
    {
        Assert.Equal(8, Sample().TailLength);
    }

    [Fact]
    public void ShowQuadrantGrid_DefaultsTrue()
    {
        Assert.True(Sample().ShowQuadrantGrid);
    }

    [Fact]
    public void ColorMap_DefaultsNull()
    {
        Assert.Null(Sample().ColorMap);
    }

    // ── Mutation ──────────────────────────────────────────────────────────────

    [Fact]
    public void Formula_CanBeSetToZScore()
    {
        var s = Sample();
        s.Formula = RrgFormula.ZScore;
        Assert.Equal(RrgFormula.ZScore, s.Formula);
    }

    [Fact]
    public void ColorMap_CanBeSet()
    {
        var s = Sample();
        s.ColorMap = ColorMaps.Plasma;
        Assert.NotNull(s.ColorMap);
    }

    // ── ToSeriesDto ───────────────────────────────────────────────────────────

    [Fact]
    public void ToSeriesDto_TypeIsRelativeRotation()
    {
        Assert.Equal("relativerotation", Sample().ToSeriesDto().Type);
    }

    [Fact]
    public void ToSeriesDto_DefaultProperties_NotEmitted()
    {
        var dto = Sample().ToSeriesDto();
        Assert.Null(dto.RrgFormula);                // DualEma = default → null
        Assert.Null(dto.RrgShortPeriod);            // 10 = default → null
        Assert.Null(dto.RrgLongPeriod);             // 26 = default → null
        Assert.Null(dto.RrgMomentumLookback);       // 10 = default → null
        Assert.Null(dto.RrgTailLength);             // 8  = default → null
        Assert.Null(dto.RrgShowQuadrantGrid);       // true = default → null
        Assert.Null(dto.ColorMapName);
    }

    [Fact]
    public void ToSeriesDto_NonDefaultFormula_Emitted()
    {
        var s = Sample();
        s.Formula = RrgFormula.ZScore;
        Assert.Equal("ZScore", s.ToSeriesDto().RrgFormula);
    }

    [Fact]
    public void ToSeriesDto_EmitsAssetData()
    {
        var dto = Sample(10).ToSeriesDto();
        Assert.NotNull(dto.RrgAssetCloses);
        Assert.NotNull(dto.RrgBenchmarkCloses);
        Assert.NotNull(dto.RrgAssetLabels);
        Assert.Equal(2, dto.RrgAssetCloses!.Count);
        Assert.Equal(10, dto.RrgBenchmarkCloses!.Count);
    }

    // ── ComputeRsData — DualEma ───────────────────────────────────────────────

    [Fact]
    public void ComputeRsData_DualEma_FlatRelativeStrength_RsRatioNear100()
    {
        // When RS is constant (asset moves identically to benchmark),
        // EMA(RS,short) == EMA(RS,long) → RsRatio == 100.
        var s = new RelativeRotationSeries(
            [Flat(40)], Flat(40), ["A"])
        {
            Formula     = RrgFormula.DualEma,
            ShortPeriod = 5,
            LongPeriod  = 10,
        };

        var (rsRatio, _) = s.ComputeRsData()[0];
        var valid = rsRatio.Where(v => !double.IsNaN(v)).ToArray();
        Assert.True(valid.Length > 0);
        foreach (var v in valid)
            Assert.Equal(100.0, v, precision: 8);
    }

    [Fact]
    public void ComputeRsData_DualEma_RisingAsset_RsRatioAbove100()
    {
        // Rising asset vs. flat benchmark → RS is rising → short EMA > long EMA → RsRatio > 100.
        var s = new RelativeRotationSeries(
            [Rising(60, 100, 1)], Flat(60), ["ETH"])
        {
            Formula     = RrgFormula.DualEma,
            ShortPeriod = 5,
            LongPeriod  = 10,
        };

        var (rsRatio, _) = s.ComputeRsData()[0];
        // Skip the leading NaN (first LongPeriod-1 elements); the rest should be > 100.
        var valid = rsRatio.Where(v => !double.IsNaN(v)).ToArray();
        Assert.True(valid.Length > 0);
        Assert.All(valid, v => Assert.True(v > 100.0, $"Expected >100 but got {v}"));
    }

    [Fact]
    public void ComputeRsData_DualEma_OutputLength_MatchesInput()
    {
        var s = new RelativeRotationSeries([Rising(30)], Flat(30), ["A"])
        {
            Formula = RrgFormula.DualEma, ShortPeriod = 3, LongPeriod = 5,
        };
        var (rsRatio, rsMom) = s.ComputeRsData()[0];
        Assert.Equal(30, rsRatio.Length);
        Assert.Equal(30, rsMom.Length);
    }

    [Fact]
    public void ComputeRsData_DualEma_TwoAssets_IndependentResults()
    {
        var s = new RelativeRotationSeries(
            [Rising(40, 100, 1), Rising(40, 100, -1)],
            Flat(40),
            ["Rising", "Falling"])
        {
            Formula = RrgFormula.DualEma, ShortPeriod = 5, LongPeriod = 10,
        };

        var results = s.ComputeRsData();
        var (risingRatio, _)  = results[0];
        var (fallingRatio, _) = results[1];

        var risingValid  = risingRatio.Where(v => !double.IsNaN(v)).ToArray();
        var fallingValid = fallingRatio.Where(v => !double.IsNaN(v)).ToArray();

        Assert.All(risingValid,  v => Assert.True(v > 100.0, $"Rising asset RsRatio {v} should be >100"));
        Assert.All(fallingValid, v => Assert.True(v < 100.0, $"Falling asset RsRatio {v} should be <100"));
    }

    [Fact]
    public void ComputeRsData_DualEma_ShortPeriodGreaterThanData_ReturnsAllNaN()
    {
        var s = new RelativeRotationSeries([Flat(5)], Flat(5), ["A"])
        {
            Formula = RrgFormula.DualEma, ShortPeriod = 20, LongPeriod = 26,
        };
        var (rsRatio, _) = s.ComputeRsData()[0];
        Assert.All(rsRatio, v => Assert.True(double.IsNaN(v)));
    }

    // ── ComputeRsData — ZScore ────────────────────────────────────────────────

    [Fact]
    public void ComputeRsData_ZScore_ConstantRS_Returns100()
    {
        // Constant RS → stddev = 0 → falls back to 100.
        var s = new RelativeRotationSeries([Flat(40)], Flat(40), ["A"])
        {
            Formula = RrgFormula.ZScore, ShortPeriod = 5, MomentumLookback = 3,
        };
        var (rsRatio, _) = s.ComputeRsData()[0];
        var valid = rsRatio.Where(v => !double.IsNaN(v)).ToArray();
        Assert.True(valid.Length > 0);
        foreach (var v in valid)
            Assert.Equal(100.0, v, precision: 8);
    }

    [Fact]
    public void ComputeRsData_ZScore_OutputLength_MatchesInput()
    {
        var s = new RelativeRotationSeries([Rising(30)], Flat(30), ["A"])
        {
            Formula = RrgFormula.ZScore, ShortPeriod = 5, MomentumLookback = 3,
        };
        var (rsRatio, rsMom) = s.ComputeRsData()[0];
        Assert.Equal(30, rsRatio.Length);
        Assert.Equal(30, rsMom.Length);
    }

    [Fact]
    public void ComputeRsData_ZScore_ShortData_ReturnsNaNMomentum()
    {
        // n (5) <= MomentumLookback (10) → Roc returns empty → rsMomentum all NaN.
        var s = new RelativeRotationSeries([Flat(5)], Flat(5), ["A"])
        {
            Formula = RrgFormula.ZScore, ShortPeriod = 3, MomentumLookback = 10,
        };
        var (_, rsMom) = s.ComputeRsData()[0];
        Assert.All(rsMom, v => Assert.True(double.IsNaN(v)));
    }

    // ── ComputeRsData — LogReturn ─────────────────────────────────────────────

    [Fact]
    public void ComputeRsData_LogReturn_OutputLength_MatchesInput()
    {
        var s = new RelativeRotationSeries([Rising(40)], Flat(40), ["A"])
        {
            Formula = RrgFormula.LogReturn, ShortPeriod = 5, LongPeriod = 10,
        };
        var (rsRatio, rsMom) = s.ComputeRsData()[0];
        Assert.Equal(40, rsRatio.Length);
        Assert.Equal(40, rsMom.Length);
    }

    [Fact]
    public void ComputeRsData_LogReturn_ShortData_ReturnsNaNMomentum()
    {
        // n (5) <= LongPeriod (10) → Roc(longP) returns empty → rsMomentum all NaN.
        var s = new RelativeRotationSeries([Flat(5)], Flat(5), ["A"])
        {
            Formula = RrgFormula.LogReturn, ShortPeriod = 3, LongPeriod = 10,
        };
        var (_, rsMom) = s.ComputeRsData()[0];
        Assert.All(rsMom, v => Assert.True(double.IsNaN(v)));
    }

    // ── ComputeDataRange ──────────────────────────────────────────────────────

    [Fact]
    public void ComputeDataRange_EmptyAssets_ReturnsNullRange()
    {
        var s = new RelativeRotationSeries([], [], []);
        var r = s.ComputeDataRange(null!);
        Assert.Null(r.XMin);
        Assert.Null(r.XMax);
    }

    [Fact]
    public void ComputeDataRange_ValidAssets_ReturnsBoundedRange()
    {
        var s = Sample(30);
        s.Formula     = RrgFormula.DualEma;
        s.ShortPeriod = 3;
        s.LongPeriod  = 5;
        var r = s.ComputeDataRange(null!);
        Assert.NotNull(r.XMin);
        Assert.NotNull(r.XMax);
        Assert.NotNull(r.YMin);
        Assert.NotNull(r.YMax);
    }
}
