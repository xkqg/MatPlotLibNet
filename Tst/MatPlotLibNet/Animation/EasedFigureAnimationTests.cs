// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Animation;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Animation;

/// <summary>Verifies <see cref="EasedFigureAnimation"/> easing math and
/// <see cref="AnimationBuilder"/> extension.</summary>
public class EasedFigureAnimationTests
{
    // ── FrameCount / Interval / Loop ─────────────────────────────────────────

    [Fact]
    public void FrameCount_MatchesConstructorArg()
    {
        var anim = new EasedFigureAnimation(7, _ => new Figure(), intervalMs: 20);
        Assert.Equal(7, anim.FrameCount);
    }

    [Fact]
    public void Interval_MatchesConstructorMs()
    {
        var anim = new EasedFigureAnimation(5, _ => new Figure(), intervalMs: 33);
        Assert.Equal(TimeSpan.FromMilliseconds(33), anim.Interval);
    }

    [Fact]
    public void Loop_DefaultsFalse()
    {
        var anim = new EasedFigureAnimation(5, _ => new Figure());
        Assert.False(anim.Loop);
    }

    [Fact]
    public void Loop_CanBeSetTrue()
    {
        var anim = new EasedFigureAnimation(5, _ => new Figure(), loop: true);
        Assert.True(anim.Loop);
    }

    // ── CreateInitialState ────────────────────────────────────────────────────

    [Fact]
    public void CreateInitialState_ReturnsZero()
    {
        var anim = new EasedFigureAnimation(5, _ => new Figure());
        Assert.Equal(0.0, anim.CreateInitialState());
    }

    // ── Advance — easing applied to normalized frame index ────────────────────

    [Fact]
    public void Advance_FirstFrame_ReturnsLinearZero()
    {
        var anim = new EasedFigureAnimation(5, _ => new Figure(), EasingKind.Linear);
        Assert.Equal(0.0, anim.Advance(0.0, 0));
    }

    [Fact]
    public void Advance_LastFrame_ReturnsOne()
    {
        var anim = new EasedFigureAnimation(5, _ => new Figure(), EasingKind.Linear);
        Assert.Equal(1.0, anim.Advance(0.0, 4), precision: 10);
    }

    [Fact]
    public void Advance_MidFrame_Linear_IsHalf()
    {
        // 5 frames: indices 0–4; index 2 → t = 2/4 = 0.5
        var anim = new EasedFigureAnimation(5, _ => new Figure(), EasingKind.Linear);
        Assert.Equal(0.5, anim.Advance(0.0, 2), precision: 10);
    }

    [Fact]
    public void Advance_MidFrame_EaseIn_IsLessThanHalf()
    {
        // EaseIn(0.5) = 0.25 < 0.5
        var anim = new EasedFigureAnimation(5, _ => new Figure(), EasingKind.EaseIn);
        Assert.True(anim.Advance(0.0, 2) < 0.5);
    }

    [Fact]
    public void Advance_MidFrame_EaseOut_IsGreaterThanHalf()
    {
        // EaseOut(0.5) = 0.75 > 0.5
        var anim = new EasedFigureAnimation(5, _ => new Figure(), EasingKind.EaseOut);
        Assert.True(anim.Advance(0.0, 2) > 0.5);
    }

    [Fact]
    public void Advance_SingleFrame_AlwaysReturnsOne()
    {
        // Single-frame edge case: t = 1.0 regardless of easing
        var anim = new EasedFigureAnimation(1, _ => new Figure(), EasingKind.EaseIn);
        Assert.Equal(1.0, anim.Advance(0.0, 0), precision: 10);
    }

    // ── GenerateFrame — frame generator receives eased state ──────────────────

    [Fact]
    public void GenerateFrame_PassesEasedStateToGenerator()
    {
        double receivedT = double.NaN;
        var anim = new EasedFigureAnimation(5, t => { receivedT = t; return new Figure(); }, EasingKind.EaseIn);
        double easedT = anim.Advance(0.0, 2); // EaseIn(0.5) = 0.25
        anim.GenerateFrame(easedT, 2);
        Assert.Equal(easedT, receivedT, precision: 10);
    }

    // ── AnimationBuilder.ToController extension ───────────────────────────────

    [Fact]
    public void ToController_ReturnsAnimationController()
    {
        var builder = new AnimationBuilder(3, i => new Figure { Title = $"F{i}" });
        var ctrl = builder.ToController();
        Assert.NotNull(ctrl);
        Assert.IsType<AnimationController<int>>(ctrl);
    }

    [Fact]
    public async Task ToController_PlayAsync_DeliversAllFrames()
    {
        var frames = new List<Figure>();
        var builder = new AnimationBuilder(3, i => new Figure { Title = $"F{i}" })
        {
            Interval = TimeSpan.FromMilliseconds(1),
            Loop = false,
        };
        var ctrl = builder.ToController();
        ctrl.FrameReady += (_, fig) => frames.Add(fig);
        await ctrl.PlayAsync(TestContext.Current.CancellationToken);
        Assert.Equal(3, frames.Count);
    }

    // ── Plt.Animate factory ────────────────────────────────────────────────────

    [Fact]
    public void Plt_Animate_ReturnsAnimationController()
    {
        var ctrl = Plt.Animate(3, t => new Figure { Title = $"t={t:F2}" }, EasingKind.EaseOut, intervalMs: 1);
        Assert.NotNull(ctrl);
        Assert.Equal(AnimationPlaybackState.Stopped, ctrl.State);
    }

    [Fact]
    public async Task Plt_Animate_PlaysThroughAllFrames()
    {
        var frames = new List<Figure>();
        var ctrl = Plt.Animate(3, t => new Figure { Title = $"F{t:F2}" }, EasingKind.Linear, intervalMs: 1);
        ctrl.FrameReady += (_, fig) => frames.Add(fig);
        await ctrl.PlayAsync(TestContext.Current.CancellationToken);
        Assert.Equal(3, frames.Count);
    }
}
