// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Indicators;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Coverage;

/// <summary>
/// Phase Z.8 (v1.7.2, 2026-04-19) — branch-only quick-fire batch lifting the
/// remaining ≤ 89%B classes by one or two facts each. Continues the
/// PinpointBranchTests1..12 series.
/// </summary>
public class PinpointBranchTests13
{
    // ── LogLocator (73.3L / 56.2B → ≥ 90/≥ 90) ───────────────────────────

    /// <summary>min ≤ 0 coercion arm (line 21).</summary>
    [Fact]
    public void LogLocator_MinLessThanZero_CoercesToTinyPositive()
    {
        var loc = new LogLocator();
        var ticks = loc.Locate(-5, 100);
        Assert.NotEmpty(ticks);
        Assert.All(ticks, t => Assert.True(t > 0));
    }

    /// <summary>max ≤ min returns [min] short-circuit (line 22).</summary>
    [Fact]
    public void LogLocator_MaxLessThanMin_ReturnsSingleMin()
    {
        var loc = new LogLocator();
        var ticks = loc.Locate(10, 5);
        Assert.Single(ticks);
        Assert.Equal(10, ticks[0]);
    }

    /// <summary>Multi-decade range — main loop hits both bounds inclusive (line 31).</summary>
    [Fact]
    public void LogLocator_MultiDecade_ProducesPowerOfTenTicks()
    {
        var loc = new LogLocator();
        var ticks = loc.Locate(1, 1000);
        Assert.Contains(1.0, ticks);
        Assert.Contains(10.0, ticks);
        Assert.Contains(100.0, ticks);
        Assert.Contains(1000.0, ticks);
    }

    /// <summary>Sub-decade range where lower-decade boundary IS in [min, max] — fallback arm 1
    /// (line 40 true).</summary>
    [Fact]
    public void LogLocator_SubDecadeWithLowerInRange_ReturnsLowerDecade()
    {
        var loc = new LogLocator();
        var ticks = loc.Locate(1.0, 5.0);
        Assert.Single(ticks);
        Assert.Equal(1.0, ticks[0]);
    }

    /// <summary>Sub-decade range where lower-decade boundary IS NOT in [min, max] —
    /// fallback arm 2 (line 40 false → line 43).</summary>
    [Fact]
    public void LogLocator_SubDecadeNoLowerInRange_FallsBackToMin()
    {
        var loc = new LogLocator();
        var ticks = loc.Locate(2.0, 5.0);
        Assert.Single(ticks);
        Assert.Equal(2.0, ticks[0]);
    }

    // ── PriceSources (73.3L / 60B → 100L/100B) ────────────────────────────

    [Theory]
    [InlineData(PriceSource.Close, 4.0)]
    [InlineData(PriceSource.Open, 1.0)]
    [InlineData(PriceSource.High, 5.0)]
    [InlineData(PriceSource.Low, 0.5)]
    public void PriceSources_DirectMappingFour_ReturnsCorrespondingArray(PriceSource src, double expectedFirst)
    {
        var open  = new[] { 1.0 };
        var high  = new[] { 5.0 };
        var low   = new[] { 0.5 };
        var close = new[] { 4.0 };
        var result = PriceSources.Resolve(src, open, high, low, close);
        Assert.Equal(expectedFirst, result[0]);
    }

    [Fact]
    public void PriceSources_HL2_AveragesHighAndLow()
    {
        var result = PriceSources.Resolve(PriceSource.HL2, [0], [10], [4], [0]);
        Assert.Equal(7.0, result[0]);
    }

    [Fact]
    public void PriceSources_HLC3_AveragesHighLowClose()
    {
        var result = PriceSources.Resolve(PriceSource.HLC3, [0], [9], [3], [6]);
        Assert.Equal(6.0, result[0]);
    }

    [Fact]
    public void PriceSources_OHLC4_AveragesAllFour()
    {
        var result = PriceSources.Resolve(PriceSource.OHLC4, [4], [8], [2], [6]);
        Assert.Equal(5.0, result[0]);
    }

    [Fact]
    public void PriceSources_UnknownEnumValue_FallsBackToClose()
    {
        var close = new[] { 99.0 };
        var result = PriceSources.Resolve((PriceSource)999, [0], [0], [0], close);
        Assert.Equal(99.0, result[0]);
    }

    // ── TwoSlopeNormalizer (100L / 83.3B → 100/100) ───────────────────────

    [Fact]
    public void TwoSlopeNormalizer_LowerHalfWithZeroLowerRange_Returns05()
    {
        var n = new TwoSlopeNormalizer(center: 5.0);
        // min == center: lowerRange = 5-5 = 0
        Assert.Equal(0.5, n.Normalize(3, min: 5, max: 10));
    }

    [Fact]
    public void TwoSlopeNormalizer_UpperHalfWithZeroUpperRange_Returns05()
    {
        var n = new TwoSlopeNormalizer(center: 5.0);
        // max == center: upperRange = 5-5 = 0
        Assert.Equal(0.5, n.Normalize(7, min: 0, max: 5));
    }

    [Fact]
    public void TwoSlopeNormalizer_LowerHalfNormal_Scales()
    {
        var n = new TwoSlopeNormalizer(center: 0.0);
        Assert.Equal(0.25, n.Normalize(-5, min: -10, max: 10));
    }

    [Fact]
    public void TwoSlopeNormalizer_UpperHalfNormal_Scales()
    {
        var n = new TwoSlopeNormalizer(center: 0.0);
        Assert.Equal(0.75, n.Normalize(5, min: -10, max: 10));
    }

    // ── FacetGridFigure / FacetedFigure (81L / 57.9B → ≥ 90/≥ 90) ────────

    /// <summary>Build with no Title and no explicit size — both null arms in Build().</summary>
    [Fact]
    public void FacetedFigure_BuildWithoutTitleOrSize_StillProducesFigure()
    {
        var fig = new FacetGridFigure(
            x: [1.0, 2, 3, 4],
            y: [10.0, 20, 30, 40],
            category: ["A", "B", "A", "B"],
            plotFunc: (ax, fx, fy) => ax.Scatter(fx, fy)).Build();
        Assert.NotNull(fig);
    }

    /// <summary>Build with Title + explicit size — both true arms (BuildCore may
    /// resize, but the title is preserved).</summary>
    [Fact]
    public void FacetedFigure_BuildWithTitleAndSize_AppliesBoth()
    {
        var fb = new FacetGridFigure(
            x: [1.0, 2, 3, 4],
            y: [10.0, 20, 30, 40],
            category: ["A", "B", "A", "B"],
            plotFunc: (ax, fx, fy) => ax.Scatter(fx, fy))
        {
            Title = "facets",
            Width = 800,
            Height = 600
        }.Build();
        var fig = fb.Build();
        Assert.Equal("facets", fig.Title);
    }

    /// <summary>JointPlot with hue labels — exercises HueGrouper-driven branches.</summary>
    [Fact]
    public void JointPlotFigure_WithHueLabels_BuildsWithoutError()
    {
        var fb = new JointPlotFigure([1.0, 2, 3, 4], [10.0, 20, 30, 40])
        {
            Hue = ["a", "b", "a", "b"]
        }.Build();
        Assert.NotEmpty(fb.Build().SubPlots);
    }

    /// <summary>PairPlot smoke — exercises PairPlotFigure.BuildCore.</summary>
    [Fact]
    public void PairPlotFigure_TwoColumns_BuildsGrid()
    {
        var fb = new PairPlotFigure([
            [1.0, 2, 3],
            [4.0, 5, 6]
        ]).Build();
        var fig = fb.Build();
        Assert.NotEmpty(fig.SubPlots);
    }

    // ── MathTextParser (100L / 80.8B → 100/≥ 90) ──────────────────────────

    [Fact]
    public void MathTextParser_EmptyDelimiter_ReturnsEmptyOrPlain()
    {
        var rt = MathTextParser.Parse("$$");
        Assert.NotNull(rt);
    }

    [Fact]
    public void MathTextParser_UnbalancedBrace_DoesNotThrow()
    {
        var rt = MathTextParser.Parse(@"$\frac{a$");
        Assert.NotNull(rt);
    }

    [Fact]
    public void MathTextParser_PlainText_NoMathDelimiter_RoundTrips()
    {
        var rt = MathTextParser.Parse("hello world");
        Assert.NotNull(rt);
        Assert.NotEmpty(rt.Spans);
    }

    [Fact]
    public void MathTextParser_GreekLetter_ProducesUnicodeSpan()
    {
        var rt = MathTextParser.Parse(@"$\alpha$");
        Assert.NotNull(rt);
        // α is U+03B1
        Assert.Contains(rt.Spans, s => s.Text.Contains('\u03B1'));
    }

    // ── EnumerableFigureExtensions (100L / 85B → ≥ 90/≥ 90) ───────────────

    private record Sample(double X, double Y, string Group);

    [Fact]
    public void EnumerableFigureExtensions_Line_NoHue_BuildsSingleSeries()
    {
        var data = new[] { new Sample(1, 10, "a"), new Sample(2, 20, "a"), new Sample(3, 30, "b") };
        var fb = data.Line(s => s.X, s => s.Y);
        Assert.Single(fb.Build().SubPlots[0].Series);
    }

    [Fact]
    public void EnumerableFigureExtensions_Line_WithHue_GroupsIntoMultipleSeries()
    {
        var data = new[] { new Sample(1, 10, "a"), new Sample(2, 20, "b"), new Sample(3, 30, "a") };
        var fb = data.Line(s => s.X, s => s.Y, hue: s => s.Group);
        // Two unique hue groups → two series
        Assert.Equal(2, fb.Build().SubPlots[0].Series.Count);
    }

    [Fact]
    public void EnumerableFigureExtensions_Hist_WithHueAndPalette_OverlapsSeriesPerHue()
    {
        var data = new[] { new Sample(0, 1.0, "a"), new Sample(0, 2.0, "b"), new Sample(0, 3.0, "a") };
        var palette = new[] { Colors.Red, Colors.Blue };
        var fb = data.Hist(s => s.Y, bins: 5, hue: s => s.Group, palette: palette);
        Assert.Equal(2, fb.Build().SubPlots[0].Series.Count);
    }

    // ── LeastSquares (90.8L / 76.4B — singular-input arms) ────────────────

    [Fact]
    public void LeastSquares_PolyFitDegree1TwoPoints_GivesExactLine()
    {
        var coeffs = LeastSquares.PolyFit([0.0, 1.0], [0.0, 2.0], degree: 1);
        Assert.Equal(0.0, coeffs[0], precision: 6); // intercept
        Assert.Equal(2.0, coeffs[1], precision: 6); // slope
    }

    [Fact]
    public void LeastSquares_PolyFitDegree2_FitsParabola()
    {
        // y = x² → coefficients ≈ [0, 0, 1]
        var coeffs = LeastSquares.PolyFit([-2.0, -1, 0, 1, 2], [4.0, 1, 0, 1, 4], degree: 2);
        Assert.Equal(3, coeffs.Length);
        Assert.Equal(1.0, coeffs[2], precision: 6);
    }

    [Fact]
    public void LeastSquares_PolyFitDegreeOutOfRange_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => LeastSquares.PolyFit([1.0], [1.0], degree: 20));
    }

    [Fact]
    public void LeastSquares_PolyFitEmpty_Throws()
    {
        Assert.Throws<ArgumentException>(() => LeastSquares.PolyFit([], [], degree: 1));
    }
}
