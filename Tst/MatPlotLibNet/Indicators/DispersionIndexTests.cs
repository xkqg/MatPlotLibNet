// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="DispersionIndex"/>. Covers all 7 branches enumerated in
/// docs/contrib/indicator-tier-2a.md §3.</summary>
public class DispersionIndexTests
{
    // ── Branch 1 — Empty input → empty output ──
    [Fact]
    public void Compute_EmptyInput_ReturnsEmpty()
    {
        var d = new DispersionIndex([]);
        Assert.Empty(d.Compute().Values);
    }

    // ── Branch 2 — FeatureCount < 2 throws ──
    [Fact]
    public void Constructor_SingleSignalPerRow_Throws()
    {
        Assert.Throws<ArgumentException>(() => new DispersionIndex([[0.5]]));
    }

    [Fact]
    public void Constructor_ZeroSignalsPerRow_Throws()
    {
        // 1 row, 0 features per row.
        Assert.Throws<ArgumentException>(() => new DispersionIndex([Array.Empty<double>()]));
    }

    // ── Branch 3 — FeatureCount == 2 boundary ──
    [Fact]
    public void Compute_TwoSignals_BinarySplit_ReturnsHalf()
    {
        // Pop stddev of [0, 1]: mean=0.5, variance=((0.25)+(0.25))/2=0.25, stddev=0.5.
        var d = new DispersionIndex([[0.0, 1.0]]);
        var r = d.Compute().Values;
        Assert.Single(r);
        Assert.Equal(0.5, r[0], precision: 12);
    }

    // ── Branch 4 — Perfect agreement → dispersion = 0 for every row ──
    [Fact]
    public void Compute_PerfectAgreement_AllZero()
    {
        var d = new DispersionIndex([
            [0.7, 0.7, 0.7],
            [0.3, 0.3, 0.3],
            [0.0, 0.0, 0.0],
            [1.0, 1.0, 1.0]
        ]);
        var r = d.Compute().Values;
        Assert.Equal(4, r.Length);
        Assert.All(r, v =>
        {
            Assert.Equal(0.0, v, precision: 12);
            Assert.False(double.IsNaN(v));
        });
    }

    // ── Branch 5 — Maximum disagreement (K=2, signals at [0, 1]) → 0.5 ──
    [Fact]
    public void Compute_MaxDisagreement_TwoSignals_Returns_0_5()
    {
        var d = new DispersionIndex([
            [0.0, 1.0],
            [1.0, 0.0]
        ]);
        var r = d.Compute().Values;
        Assert.Equal(2, r.Length);
        Assert.All(r, v => Assert.Equal(0.5, v, precision: 12));
    }

    // ── Branch 6 — Non-rectangular feature matrix throws (via base class) ──
    [Fact]
    public void Constructor_NonRectangular_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new DispersionIndex([new[] { 0.5, 0.5 }, new[] { 0.3, 0.3, 0.3 }]));
    }

    // ── Branch 7 — Normal multi-bar path ──
    [Fact]
    public void Compute_MixedSignals_MatchesPopulationStddev()
    {
        // Signals [0.2, 0.3, 0.7, 0.8]: mean=0.5, variance (pop) = 0.26/4 = 0.065,
        // stddev = √0.065 ≈ 0.254951.
        var d = new DispersionIndex([[0.2, 0.3, 0.7, 0.8]]);
        var r = d.Compute().Values;
        Assert.Single(r);
        Assert.Equal(Math.Sqrt(0.065), r[0], precision: 12);
    }

    [Fact]
    public void Compute_MultiBarMixed_ReturnsExpectedLength()
    {
        var d = new DispersionIndex([
            [0.5, 0.5, 0.5],
            [0.0, 1.0, 0.5],
            [1.0, 0.0, 0.5]
        ]);
        var r = d.Compute().Values;
        Assert.Equal(3, r.Length);
        Assert.Equal(0.0, r[0], precision: 12);
        // For [0, 1, 0.5]: mean=0.5, variance = (0.25 + 0.25 + 0)/3 ≈ 0.16667, stddev ≈ 0.408248
        Assert.Equal(Math.Sqrt((0.25 + 0.25 + 0.0) / 3.0), r[1], precision: 12);
        Assert.Equal(Math.Sqrt((0.25 + 0.25 + 0.0) / 3.0), r[2], precision: 12);
    }

    // ── Apply / label / styling ──
    [Fact]
    public void Apply_AddsLineSeries()
    {
        var axes = new Axes();
        new DispersionIndex([[0.2, 0.3], [0.5, 0.5]]).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }

    [Fact]
    public void Apply_SetsExpectedLabel()
    {
        var axes = new Axes();
        new DispersionIndex([[0.2, 0.3, 0.4], [0.5, 0.5, 0.5]]).Apply(axes);
        Assert.Equal("Dispersion(3)", axes.Series[0].Label);
    }

    // ── Type system ──
    [Fact]
    public void InheritsMultivariateIndicator()
    {
        var d = new DispersionIndex([[0.5, 0.5]]);
        Assert.IsAssignableFrom<MultivariateIndicator<SignalResult>>(d);
        Assert.IsAssignableFrom<IIndicator>(d);
    }
}
