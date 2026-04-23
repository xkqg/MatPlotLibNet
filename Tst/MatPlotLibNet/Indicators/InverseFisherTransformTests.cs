// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="InverseFisherTransform"/>. Covers all 8 branches in
/// docs/contrib/indicator-tier-3b.md §3.</summary>
public class InverseFisherTransformTests
{
    // ── Branch 1 — Null input → throw ──
    [Fact]
    public void Constructor_NullInput_Throws()
    {
        Assert.Throws<ArgumentException>(() => new InverseFisherTransform(null!));
    }

    // ── Branch 2 — Empty input → throw ──
    [Fact]
    public void Constructor_EmptyInput_Throws()
    {
        Assert.Throws<ArgumentException>(() => new InverseFisherTransform([]));
    }

    // ── Branch 3 — scale <= 0 → throw ──
    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    public void Constructor_NonPositiveScale_Throws(double scale)
    {
        Assert.Throws<ArgumentException>(() => new InverseFisherTransform([0.5], scale));
    }

    // ── Branch 4 — Single value → length-1 output ──
    [Fact]
    public void Compute_SingleValue_ReturnsSingle()
    {
        var ift = new InverseFisherTransform([0.5]).Compute().Values;
        Assert.Single(ift);
        Assert.Equal(Math.Tanh(0.5), ift[0], precision: 9);
    }

    // ── Branch 5 — Very large positive input → output → +1 ──
    [Fact]
    public void Compute_LargePositive_ApproachesOne()
    {
        var ift = new InverseFisherTransform([100.0]).Compute().Values;
        Assert.Equal(1.0, ift[0], precision: 6);
    }

    // ── Branch 6 — Very large negative input → output → -1 ──
    [Fact]
    public void Compute_LargeNegative_ApproachesMinusOne()
    {
        var ift = new InverseFisherTransform([-100.0]).Compute().Values;
        Assert.Equal(-1.0, ift[0], precision: 6);
    }

    // ── Branch 7 — Zero input → output = 0 ──
    [Fact]
    public void Compute_Zero_ReturnsZero()
    {
        var ift = new InverseFisherTransform([0.0]).Compute().Values;
        Assert.Equal(0.0, ift[0], precision: 10);
    }

    // ── Branch 8 — Normal mixed input + scale > 1 ──
    [Fact]
    public void Compute_KnownTanhValues_Match()
    {
        var ift = new InverseFisherTransform([0.0, 0.5, 1.0, -0.5, -1.0]).Compute().Values;
        Assert.Equal(5, ift.Length);
        Assert.Equal(0.0, ift[0], precision: 9);
        Assert.Equal(Math.Tanh(0.5), ift[1], precision: 9);
        Assert.Equal(Math.Tanh(1.0), ift[2], precision: 9);
        Assert.Equal(Math.Tanh(-0.5), ift[3], precision: 9);
        Assert.Equal(Math.Tanh(-1.0), ift[4], precision: 9);
    }

    [Fact]
    public void Compute_ScaleSteepensTransition()
    {
        var ift = new InverseFisherTransform([0.5], scale: 2.0).Compute().Values;
        Assert.Equal(Math.Tanh(2.0 * 0.5), ift[0], precision: 9);
    }

    // ── Apply / label / styling ──
    [Fact]
    public void Apply_AddsLineSeriesAndSetsYRange()
    {
        var axes = new Axes();
        new InverseFisherTransform([0.0, 0.5, 1.0, -0.5, -1.0]).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
        Assert.Equal(-1, axes.YAxis.Min);
        Assert.Equal(1, axes.YAxis.Max);
    }

    [Fact]
    public void DefaultLabel_HasScale()
    {
        var ift = new InverseFisherTransform([0.5]);
        Assert.Equal("IFT(scale=1)", ift.Label);
    }

    [Fact]
    public void InheritsIndicator()
    {
        var ift = new InverseFisherTransform([0.5]);
        Assert.IsAssignableFrom<Indicator<SignalResult>>(ift);
        Assert.IsAssignableFrom<IIndicator>(ift);
    }
}
