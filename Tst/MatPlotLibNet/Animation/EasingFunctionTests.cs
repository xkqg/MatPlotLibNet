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

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(0.5)]
    [InlineData(0.75)]
    public void Linear_IsIdentity(double t) => Assert.Equal(t, EasingFunction.Linear(t), Eps);

    /// <summary>Every eased function starts at 0 and ends at 1, as the straight one does.</summary>
    [Theory]
    [InlineData(EasingKind.EaseIn)]
    [InlineData(EasingKind.EaseOut)]
    [InlineData(EasingKind.EaseInOut)]
    [InlineData(EasingKind.Bounce)]
    [InlineData(EasingKind.Elastic)]
    public void AnEasedFunction_MapsZeroToZero_AndOneToOne(EasingKind kind)
    {
        Assert.Equal(0.0, EasingFunction.Apply(kind, 0), Eps);
        Assert.Equal(1.0, EasingFunction.Apply(kind, 1), Eps);
    }

    // ── EaseIn ────────────────────────────────────────────────────────────────

    /// <summary>An ease that starts slowly is behind the straight line early on: ease-in (quadratic) at t=0.5 is slower than linear at the
    /// start, and the first half of ease-in-out behaves like ease-in.</summary>
    public static TheoryData<Func<double, double>, double> SlowStarts => new()
    {
        { EasingFunction.EaseIn, 0.5 },
        { EasingFunction.EaseInOut, 0.25 },
    };

    [Theory]
    [MemberData(nameof(SlowStarts))]
    public void AnEaseThatStartsSlowly_IsBehindTheLine_EarlyOn(Func<double, double> ease, double t) => Assert.True(ease(t) < 0.5);

    // ── EaseOut ───────────────────────────────────────────────────────────────

    [Fact]
    public void EaseOut_Half_GreaterThanHalf()
    {
        // Ease-out (quadratic) at t=0.5 is faster than linear early
        Assert.True(EasingFunction.EaseOut(0.5) > 0.5);
    }

    // ── EaseInOut ─────────────────────────────────────────────────────────────

    [Fact]
    public void EaseInOut_Half_ReturnsHalf()
    {
        // S-curve is symmetric: t=0.5 → 0.5
        Assert.Equal(0.5, EasingFunction.EaseInOut(0.5), 1e-6);
    }

    // ── Bounce ────────────────────────────────────────────────────────────────

    [Fact]
    public void Bounce_StaysNonNegative()
    {
        for (double t = 0; t <= 1.0; t += 0.05)
            Assert.True(EasingFunction.Bounce(t) >= -0.001, $"Bounce({t}) was negative");
    }

    // ── Elastic ───────────────────────────────────────────────────────────────

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

    /// <summary>Apply runs the function of the kind it is given.</summary>
    [Theory]
    [InlineData(EasingKind.Linear)]
    [InlineData(EasingKind.EaseIn)]
    [InlineData(EasingKind.EaseOut)]
    [InlineData(EasingKind.EaseInOut)]
    [InlineData(EasingKind.Bounce)]
    [InlineData(EasingKind.Elastic)]
    public void Apply_RunsTheFunctionOfTheKind(EasingKind kind)
    {
        Func<double, double> function = kind switch
        {
            EasingKind.Linear => EasingFunction.Linear,
            EasingKind.EaseIn => EasingFunction.EaseIn,
            EasingKind.EaseOut => EasingFunction.EaseOut,
            EasingKind.EaseInOut => EasingFunction.EaseInOut,
            EasingKind.Bounce => EasingFunction.Bounce,
            _ => EasingFunction.Elastic,
        };

        Assert.Equal(function(0.3), EasingFunction.Apply(kind, 0.3), Eps);
    }

}
