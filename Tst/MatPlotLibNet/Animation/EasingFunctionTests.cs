// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Animation;

namespace MatPlotLibNet.Tests.Animation;

/// <summary>Verifies <see cref="EasingFunction"/> pure math correctness.
/// All functions map t=0 → 0 and t=1 → 1, and are monotonic enough to be useful.</summary>
public class EasingFunctionTests
{
    private const double Eps = 1e-9;

    // ── Linear ────────────────────────────────────────────────────────────────

    [Fact]
    public void Linear_Zero_ReturnsZero() => Assert.Equal(0.0, EasingFunction.Linear(0), Eps);
    [Fact]
    public void Linear_One_ReturnsOne() => Assert.Equal(1.0, EasingFunction.Linear(1), Eps);
    [Fact]
    public void Linear_Half_ReturnsHalf() => Assert.Equal(0.5, EasingFunction.Linear(0.5), Eps);
    [Fact]
    public void Linear_IsIdentity() => Assert.Equal(0.75, EasingFunction.Linear(0.75), Eps);

    // ── EaseIn ────────────────────────────────────────────────────────────────

    [Fact]
    public void EaseIn_Zero_ReturnsZero() => Assert.Equal(0.0, EasingFunction.EaseIn(0), Eps);
    [Fact]
    public void EaseIn_One_ReturnsOne() => Assert.Equal(1.0, EasingFunction.EaseIn(1), Eps);
    [Fact]
    public void EaseIn_Half_LessThanHalf()
    {
        // Ease-in (quadratic) at t=0.5 is slower than linear at start
        Assert.True(EasingFunction.EaseIn(0.5) < 0.5);
    }

    // ── EaseOut ───────────────────────────────────────────────────────────────

    [Fact]
    public void EaseOut_Zero_ReturnsZero() => Assert.Equal(0.0, EasingFunction.EaseOut(0), Eps);
    [Fact]
    public void EaseOut_One_ReturnsOne() => Assert.Equal(1.0, EasingFunction.EaseOut(1), Eps);
    [Fact]
    public void EaseOut_Half_GreaterThanHalf()
    {
        // Ease-out (quadratic) at t=0.5 is faster than linear early
        Assert.True(EasingFunction.EaseOut(0.5) > 0.5);
    }

    // ── EaseInOut ─────────────────────────────────────────────────────────────

    [Fact]
    public void EaseInOut_Zero_ReturnsZero() => Assert.Equal(0.0, EasingFunction.EaseInOut(0), Eps);
    [Fact]
    public void EaseInOut_One_ReturnsOne() => Assert.Equal(1.0, EasingFunction.EaseInOut(1), Eps);
    [Fact]
    public void EaseInOut_Half_ReturnsHalf()
    {
        // S-curve is symmetric: t=0.5 → 0.5
        Assert.Equal(0.5, EasingFunction.EaseInOut(0.5), 1e-6);
    }
    [Fact]
    public void EaseInOut_Quarter_LessThanHalf()
    {
        // First half behaves like ease-in
        Assert.True(EasingFunction.EaseInOut(0.25) < 0.5);
    }

    // ── Bounce ────────────────────────────────────────────────────────────────

    [Fact]
    public void Bounce_Zero_ReturnsZero() => Assert.Equal(0.0, EasingFunction.Bounce(0), Eps);
    [Fact]
    public void Bounce_One_ReturnsOne() => Assert.Equal(1.0, EasingFunction.Bounce(1), Eps);
    [Fact]
    public void Bounce_StaysNonNegative()
    {
        for (double t = 0; t <= 1.0; t += 0.05)
            Assert.True(EasingFunction.Bounce(t) >= -0.001, $"Bounce({t}) was negative");
    }

    // ── Elastic ───────────────────────────────────────────────────────────────

    [Fact]
    public void Elastic_Zero_ReturnsZero() => Assert.Equal(0.0, EasingFunction.Elastic(0), Eps);
    [Fact]
    public void Elastic_One_ReturnsOne() => Assert.Equal(1.0, EasingFunction.Elastic(1), Eps);
    [Fact]
    public void Elastic_Mid_CanExceedOne()
    {
        // Elastic overshoots
        bool anyOver = false;
        for (double t = 0.01; t < 1.0; t += 0.02)
            if (EasingFunction.Elastic(t) > 1.0) anyOver = true;
        Assert.True(anyOver);
    }

    // ── Apply (dispatch by name) ──────────────────────────────────────────────

    [Fact]
    public void Apply_Linear_MatchesLinear()
        => Assert.Equal(EasingFunction.Linear(0.3), EasingFunction.Apply(EasingKind.Linear, 0.3), Eps);

    [Fact]
    public void Apply_EaseIn_MatchesEaseIn()
        => Assert.Equal(EasingFunction.EaseIn(0.3), EasingFunction.Apply(EasingKind.EaseIn, 0.3), Eps);

    [Fact]
    public void Apply_EaseOut_MatchesEaseOut()
        => Assert.Equal(EasingFunction.EaseOut(0.3), EasingFunction.Apply(EasingKind.EaseOut, 0.3), Eps);

    [Fact]
    public void Apply_EaseInOut_MatchesEaseInOut()
        => Assert.Equal(EasingFunction.EaseInOut(0.3), EasingFunction.Apply(EasingKind.EaseInOut, 0.3), Eps);

    [Fact]
    public void Apply_Bounce_MatchesBounce()
        => Assert.Equal(EasingFunction.Bounce(0.3), EasingFunction.Apply(EasingKind.Bounce, 0.3), Eps);

    [Fact]
    public void Apply_Elastic_MatchesElastic()
        => Assert.Equal(EasingFunction.Elastic(0.3), EasingFunction.Apply(EasingKind.Elastic, 0.3), Eps);
}
