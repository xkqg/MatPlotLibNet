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

    /// <summary>One asset against a flat benchmark, under a formula whose lookback is the one it reads: the z-score's momentum
    /// lookback, the log return's long period.</summary>
    private static RelativeRotationSeries WithLookback(RrgFormula formula, double[] asset, int shortPeriod, int lookback)
    {
        var series = new RelativeRotationSeries([asset], Flat(asset.Length), ["A"]) { Formula = formula, ShortPeriod = shortPeriod };
        if (formula == RrgFormula.ZScore) series.MomentumLookback = lookback; else series.LongPeriod = lookback;
        return series;
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

    // ── Input that cannot be drawn is refused where it is given ──────────────

    /// <summary>A period, a lookback and a tail are a count of bars, at least one: a window of no bars has no average and a tail
    /// of none no head. Each formula reads another of them, so the setter refuses, whatever the formula is.</summary>
    [Theory]
    [InlineData(nameof(RelativeRotationSeries.ShortPeriod), 0)]
    [InlineData(nameof(RelativeRotationSeries.ShortPeriod), -1)]
    [InlineData(nameof(RelativeRotationSeries.LongPeriod), 0)]
    [InlineData(nameof(RelativeRotationSeries.LongPeriod), -1)]
    [InlineData(nameof(RelativeRotationSeries.MomentumLookback), 0)]
    [InlineData(nameof(RelativeRotationSeries.MomentumLookback), -1)]
    [InlineData(nameof(RelativeRotationSeries.TailLength), 0)]
    [InlineData(nameof(RelativeRotationSeries.TailLength), -1)]
    public void ACountOfBarsBelowOne_IsRefusedByName(string property, int bars)
    {
        var s = Sample();
        var setter = typeof(RelativeRotationSeries).GetProperty(property)!;
        object before = setter.GetValue(s)!;

        var refused = Assert.Throws<System.Reflection.TargetInvocationException>(() => setter.SetValue(s, bars));

        Assert.Equal(property, Assert.IsType<ArgumentOutOfRangeException>(refused.InnerException).ParamName);
        Assert.Equal(before, setter.GetValue(s));
    }

    [Theory]
    [InlineData(nameof(RelativeRotationSeries.AbsorptionRatioPerBar), 29)]
    [InlineData(nameof(RelativeRotationSeries.AbsorptionRatioPerBar), 0)]
    [InlineData(nameof(RelativeRotationSeries.EnbPerBar), 29)]
    [InlineData(nameof(RelativeRotationSeries.EnbPerBar), 0)]
    public void AValuePerBarThatIsNotOneForEachClose_IsRefusedByName(string property, int length)
    {
        var s = Sample(30);
        var setter = typeof(RelativeRotationSeries).GetProperty(property)!;

        var refused = Assert.Throws<System.Reflection.TargetInvocationException>(() => setter.SetValue(s, Flat(length, 0.5)));

        Assert.Equal(property, Assert.IsType<ArgumentException>(refused.InnerException).ParamName);
        Assert.Null(setter.GetValue(s));
    }

    /// <summary>The log-return momentum is the log of the long return less that of the short one, at each bar where both have a
    /// value: from the later of the two lookbacks on, whichever of the two periods is the longer.</summary>
    [Fact]
    public void ALogReturnRotationWhoseShortPeriodIsTheLonger_DrawsItsMomentumFromTheLaterLookback()
    {
        var s = new RelativeRotationSeries([Rising(40, 100, 0.7)], Rising(40, 100, 0.3), ["A"])
        {
            Formula = RrgFormula.LogReturn, ShortPeriod = 12, LongPeriod = 4,
        };

        var momentum = s.ComputeRsData()[0].RsMomentum;
        _ = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.AddSeries(s)).Build().ToSvg();

        Assert.All(momentum[..12], value => Assert.True(double.IsNaN(value)));
        Assert.Contains(momentum[12..], double.IsFinite);
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

    /// <summary>A formula that reads a lookback (the z-score its momentum lookback, the log return its long period) is run for as many bars
    /// as it was given: the ratio and the momentum are as long as the asset.</summary>
    [Theory]
    [InlineData(RrgFormula.ZScore, 30, 3)]
    [InlineData(RrgFormula.LogReturn, 40, 10)]
    public void ComputeRsData_OutputLength_MatchesInput(RrgFormula formula, int bars, int lookback)
    {
        var (rsRatio, rsMom) = WithLookback(formula, Rising(bars), 5, lookback).ComputeRsData()[0];
        Assert.Equal(bars, rsRatio.Length);
        Assert.Equal(bars, rsMom.Length);
    }

    /// <summary>Fewer bars than the lookback leave the momentum all NaN: the rate of change over the lookback comes back empty.</summary>
    [Theory]
    [InlineData(RrgFormula.ZScore)]
    [InlineData(RrgFormula.LogReturn)]
    public void ComputeRsData_ShortData_ReturnsNaNMomentum(RrgFormula formula)
    {
        var (_, rsMom) = WithLookback(formula, Flat(5), 3, 10).ComputeRsData()[0];
        Assert.All(rsMom, v => Assert.True(double.IsNaN(v)));
    }

    // ── ComputeRsData — LogReturn ─────────────────────────────────────────────

    // ── ComputeRsData — return type ───────────────────────────────────────────

    [Fact]
    public void ComputeRsData_ReturnsRrsPointArray()
    {
        // Verifies that ComputeRsData() returns RrsPoint[] (not a named-tuple array).
        // Deconstruction must still work so the other tests remain valid.
        var s = new RelativeRotationSeries(
            [Rising(40), Flat(40)],
            Flat(40),
            ["BTC", "ETH"])
        {
            Formula = RrgFormula.DualEma, ShortPeriod = 5, LongPeriod = 10,
        };

        var result = s.ComputeRsData();

        Assert.IsType<RrsPoint[]>(result);
        Assert.Equal(2, result.Length);
        Assert.Equal(40, result[0].RsRatio.Length);
        Assert.Equal(40, result[0].RsMomentum.Length);
        Assert.Equal(40, result[1].RsRatio.Length);
        Assert.Equal(40, result[1].RsMomentum.Length);
    }

    // ── the rotation the range and the drawing read ──────────────────────────

    /// <summary>The range of the axes and the renderer both read the rotation, so a draw asks for it at least twice: the series
    /// computes it once and keeps it.</summary>
    [Fact]
    public void TheRotation_AskedTwice_IsComputedOnce()
    {
        var series = Sample(40);

        Assert.Same(series.Rotation(), series.Rotation());
    }

    /// <summary>A figure drawn again with its series unchanged (here its title changed, so the subplot is drawn) reads the
    /// rotation the draw before computed: one computation per change of the series, not two or three per draw.</summary>
    [Fact]
    public void TheRotation_ADrawUsed_IsTheOneTheNextDrawUses()
    {
        RelativeRotationSeries series = null!;
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.RelativeRotation([Rising(40), Flat(40)], Flat(40), ["ETH", "BNB"],
            s => series = s)).Build();
        string first = figure.ToSvg();
        var drawn = series.Rotation();

        figure.SubPlots[0].Title = "drawn again";
        string second = figure.ToSvg();

        Assert.Same(drawn, series.Rotation());
        Assert.NotEqual(first, second);
    }

    /// <summary>A close changed in place passes no setter; the rotation is computed again all the same, as a fresh series
    /// computes it, because the copy of the data it was computed from no longer matches.</summary>
    [Fact]
    public void TheRotation_AfterACloseChangedInPlace_IsComputedAgain()
    {
        double[] asset = Rising(40);
        var series = new RelativeRotationSeries([asset], Flat(40), ["A"]) { ShortPeriod = 3, LongPeriod = 5 };
        var before = series.Rotation();

        asset[^1] = 180;
        var after = series.Rotation();

        Assert.NotSame(before, after);
        var fresh = new RelativeRotationSeries([[.. asset]], Flat(40), ["A"]) { ShortPeriod = 3, LongPeriod = 5 }.Rotation();
        Assert.Equal(fresh[0].RsRatio, after[0].RsRatio);
        Assert.Equal(fresh[0].RsMomentum, after[0].RsMomentum);
    }

    /// <summary>A setting the formulas read moves the series' change mark, and the rotation is computed again with it.</summary>
    [Fact]
    public void TheRotation_AfterTheFormulaChanged_IsComputedAgain()
    {
        var series = Sample(40);
        var before = series.Rotation();

        series.Formula = RrgFormula.ZScore;
        var after = series.Rotation();

        Assert.NotSame(before, after);
        var fresh = new RelativeRotationSeries([Rising(40), Flat(40)], Flat(40), ["ETH", "BNB"]) { Formula = RrgFormula.ZScore }.Rotation();
        Assert.Equal(fresh[0].RsRatio, after[0].RsRatio);
        Assert.Equal(fresh[0].RsMomentum, after[0].RsMomentum);
    }

    /// <summary>What <see cref="RelativeRotationSeries.ComputeRsData"/> hands out is the caller's own: a fresh copy of the
    /// values the series keeps, so writing into it leaves the drawing alone.</summary>
    [Fact]
    public void ComputeRsData_HandsOutAFreshCopy_ThatAWriteLeavesTheKeptRotationAlone()
    {
        var series = Sample(40);
        var kept = series.Rotation();

        var handedOut = series.ComputeRsData();
        handedOut[0].RsRatio[^1] = -1;

        Assert.NotSame(kept, handedOut);
        Assert.NotSame(kept[0].RsRatio, handedOut[0].RsRatio);
        Assert.Same(kept, series.Rotation());
        Assert.NotEqual(-1, series.Rotation()[0].RsRatio[^1]);
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

    // ── the centre is always in the range ────────────────────────────────────

    private static RelativeRotationSeries OneSided(bool above, int n = 60)
    {
        // A strengthening (or weakening) asset against a flat benchmark: its ratio and its momentum stay on one side of 100.
        double[] asset = [.. Enumerable.Range(0, n).Select(i => 100 + (above ? 1 : -1) * i * i * 0.02)];
        return new([asset], Flat(n), ["ONE"]) { ShortPeriod = 3, LongPeriod = 5, TailLength = 8 };
    }

    /// <summary>Measured before the change: the range was the extent of the data alone, so a chart whose assets all stand on one
    /// side of the 100/100 centre (all leading, all lagging) had no centre in its plot, no crosshair and quadrants of no area,
    /// where the quadrants are what the chart is read by. The range now holds the centre, as StockCharts' does, and the empty
    /// space it leaves is accepted.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ComputeDataRange_WithAssetsOnOneSideOf100_HoldsTheCentre(bool above)
    {
        var series = OneSided(above);
        series.ShowQuadrantGrid = false;
        var data = series.ComputeDataRange(null!);
        Assert.True(above ? data.XMin > 100 && data.YMin > 100 : data.XMax < 100 && data.YMax < 100, "the premise: the data stands on one side");
        series.ShowQuadrantGrid = true;

        var range = series.ComputeDataRange(null!);

        Assert.Equal(Math.Min(data.XMin!.Value, RelativeRotationSeries.Centre), range.XMin!.Value);
        Assert.Equal(Math.Max(data.XMax!.Value, RelativeRotationSeries.Centre), range.XMax!.Value);
        Assert.Equal(Math.Min(data.YMin!.Value, RelativeRotationSeries.Centre), range.YMin!.Value);
        Assert.Equal(Math.Max(data.YMax!.Value, RelativeRotationSeries.Centre), range.YMax!.Value);
        Assert.InRange(RelativeRotationSeries.Centre, range.XMin!.Value, range.XMax!.Value);
        Assert.InRange(RelativeRotationSeries.Centre, range.YMin!.Value, range.YMax!.Value);
    }

    [Fact]
    public void TheCentre_IsOneHundred() => Assert.Equal(100.0, RelativeRotationSeries.Centre);

    /// <summary>Without the quadrant grid nothing is drawn at the centre, so the range holds the data alone.</summary>
    [Fact]
    public void ComputeDataRange_WithTheQuadrantGridOff_HoldsOnlyTheData()
    {
        var series = OneSided(above: true);
        series.ShowQuadrantGrid = false;

        var range = series.ComputeDataRange(null!);

        Assert.True(range.XMin > 100);
        Assert.True(range.YMin > 100);
    }

    [Fact]
    public void ComputeDataRange_WithAssetsOnBothSidesOf100_IsTheDataRange()
    {
        var series = Sample(60);
        series.ShortPeriod = 3;
        series.LongPeriod = 5;
        series.ShowQuadrantGrid = false;
        var data = series.ComputeDataRange(null!);
        Assert.True(data.XMin <= 100 && data.XMax >= 100 && data.YMin <= 100 && data.YMax >= 100, "the premise: the data holds the centre");
        series.ShowQuadrantGrid = true;

        Assert.Equal(data, series.ComputeDataRange(null!));
    }

    [Fact]
    public void ComputeDataRange_OfNothingToDraw_StaysEmpty_WhateverTheGrid()
    {
        var series = new RelativeRotationSeries([], [], []);

        var range = series.ComputeDataRange(null!);

        Assert.Null(range.XMin);
        Assert.Null(range.YMax);
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

    // ── The formulas: an asset's RS-Ratio and RS-Momentum against its benchmark, held to the values the series computed when the
    // formulas were first written, taken at the first bar each formula can give and at two bars after it. ──

    private static readonly double[] Asset = [.. Enumerable.Range(0, 60).Select(i => 100 + 10 * Math.Sin(i * 0.3) + i * 0.5)];
    private static readonly double[] Benchmark = [.. Enumerable.Range(0, 60).Select(i => 100 + i * 0.2)];

    /// <summary>The rotation of <paramref name="assets"/> against <paramref name="benchmark"/> under <paramref name="formula"/>, with a
    /// short period of 5, a long period of 12 and a momentum lookback of 4.</summary>
    private static RrsPoint[] RotationOf(RrgFormula formula, double[][] assets, double[] benchmark) =>
        new RelativeRotationSeries(assets, benchmark, [.. assets.Select((_, i) => $"a{i}")])
        {
            Formula = formula, ShortPeriod = 5, LongPeriod = 12, MomentumLookback = 4,
        }.ComputeRsData();

    [Theory]
    [InlineData(RrgFormula.DualEma, 11, 22, 102.2104396689337, 101.02156013272759, 97.73470754893067, 98.7520757402458)]
    [InlineData(RrgFormula.ZScore, 4, 8, 98.2800571989275, 98.61409082561009, 100.53619837278032, 101.61693500679046)]
    [InlineData(RrgFormula.LogReturn, 4, 16, 98.2800571989275, 100.75100153234206, 100.53619837278032, 98.69379341269237)]
    public void EachFormula_GivesTheValuesTheSeriesGaveBefore(RrgFormula formula, int firstRatio, int firstMomentum,
        double ratioAt30, double momentumAt30, double ratioAt59, double momentumAt59)
    {
        var point = RotationOf(formula, [Asset], Benchmark)[0];

        Assert.Equal(firstRatio, Array.FindIndex(point.RsRatio, v => !double.IsNaN(v)));
        Assert.Equal(firstMomentum, Array.FindIndex(point.RsMomentum, v => !double.IsNaN(v)));
        Assert.Equal(ratioAt30, point.RsRatio[30], 1e-9);
        Assert.Equal(momentumAt30, point.RsMomentum[30], 1e-9);
        Assert.Equal(ratioAt59, point.RsRatio[59], 1e-9);
        Assert.Equal(momentumAt59, point.RsMomentum[59], 1e-9);
    }

    [Fact]
    public void NoAsset_GivesNoPoint() => Assert.Empty(RotationOf(RrgFormula.DualEma, [], Benchmark));

    /// <summary>A benchmark of zero has no ratio to it: every bar of both coordinates is missing.</summary>
    [Theory]
    [InlineData(RrgFormula.DualEma)]
    [InlineData(RrgFormula.ZScore)]
    [InlineData(RrgFormula.LogReturn)]
    public void AZeroBenchmark_GivesNoValue(RrgFormula formula)
    {
        var point = RotationOf(formula, [Asset], new double[Asset.Length])[0];

        Assert.All(point.RsRatio, v => Assert.True(double.IsNaN(v)));
        Assert.All(point.RsMomentum, v => Assert.True(double.IsNaN(v)));
    }

    /// <summary>An asset worth nothing gives a long average of zero, which no ratio is taken over.</summary>
    [Fact]
    public void AnAssetWorthNothing_GivesNoDualEmaValue()
    {
        var point = RotationOf(RrgFormula.DualEma, [new double[Asset.Length]], Benchmark)[0];

        Assert.All(point.RsRatio, v => Assert.True(double.IsNaN(v)));
        Assert.All(point.RsMomentum, v => Assert.True(double.IsNaN(v)));
    }

    /// <summary>Fourteen bars give the dual-EMA ratio from the twelfth on, three ratios: too few for its long average, so the
    /// momentum has no value yet.</summary>
    [Fact]
    public void BarsEnoughForTheRatio_ButNotForItsMomentum_GiveTheRatioAlone()
    {
        var point = RotationOf(RrgFormula.DualEma, [Asset[..14]], Benchmark[..14])[0];

        point.RsRatio[13].AssertFinite();
        Assert.All(point.RsMomentum, v => Assert.True(double.IsNaN(v)));
    }

    /// <summary>Fewer bars than the window of the z-score give no ratio at all.</summary>
    [Theory]
    [InlineData(RrgFormula.ZScore)]
    [InlineData(RrgFormula.LogReturn)]
    public void FewerBarsThanTheWindow_GiveNoZScore(RrgFormula formula)
    {
        var point = RotationOf(formula, [Asset[..3]], Benchmark[..3])[0];

        Assert.All(point.RsRatio, v => Assert.True(double.IsNaN(v)));
        Assert.All(point.RsMomentum, v => Assert.True(double.IsNaN(v)));
    }
}
