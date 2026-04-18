// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Animation;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Interaction;

/// <summary>Phase 6 — interaction + animation edge cases. Targets:
/// <list type="bullet">
///   <item>AnimationController&lt;T&gt; (was 19.3%)</item>
///   <item>SpanSelectModifier (26.4%) — kept minimal because state-machine API
///         requires private internal types</item>
///   <item>LegacyAnimationAdapter (42.8%)</item>
/// </list>
/// </summary>
public class InteractionAndAnimationEdgeCaseTests
{
    // ── LegacyAnimationAdapter ───────────────────────────────────────────────

    [Fact]
    public void LegacyAdapter_ForwardsAllProperties()
    {
        var fig = new Figure();
        var builder = new AnimationBuilder(3, _ => fig)
        {
            Interval = TimeSpan.FromMilliseconds(50),
            Loop = true,
        };
        var adapter = new LegacyAnimationAdapter(builder);

        Assert.Equal(3, adapter.FrameCount);
        Assert.Equal(TimeSpan.FromMilliseconds(50), adapter.Interval);
        Assert.True(adapter.Loop);
    }

    [Fact]
    public void LegacyAdapter_PropertySettersWriteThrough()
    {
        var builder = new AnimationBuilder(5, _ => new Figure());
        var adapter = new LegacyAnimationAdapter(builder);

        adapter.Interval = TimeSpan.FromSeconds(1);
        adapter.Loop = true;

        Assert.Equal(TimeSpan.FromSeconds(1), builder.Interval);
        Assert.True(builder.Loop);
    }

    [Fact]
    public void LegacyAdapter_AdvanceReturnsFrameIndex_IgnoresState()
    {
        var adapter = new LegacyAnimationAdapter(new AnimationBuilder(5, _ => new Figure()));
        Assert.Equal(0, adapter.CreateInitialState());
        // State is ignored — Advance(any, frameIndex) returns frameIndex.
        Assert.Equal(7,   adapter.Advance(currentState: 999, frameIndex: 7));
        Assert.Equal(42,  adapter.Advance(currentState: 0,   frameIndex: 42));
        Assert.Equal(-1,  adapter.Advance(currentState: 100, frameIndex: -1));
    }

    [Fact]
    public void LegacyAdapter_GenerateFrame_DelegatesToBuilder()
    {
        var fig0 = new Figure { Title = "F0" };
        var fig1 = new Figure { Title = "F1" };
        var fig2 = new Figure { Title = "F2" };
        var figs = new[] { fig0, fig1, fig2 };
        var builder = new AnimationBuilder(3, i => figs[i]);
        var adapter = new LegacyAnimationAdapter(builder);

        // GenerateFrame ignores state, uses frameIndex only
        Assert.Same(fig1, adapter.GenerateFrame(state: 0, frameIndex: 1));
        Assert.Same(fig2, adapter.GenerateFrame(state: 999, frameIndex: 2));
    }

    // ── AnimationController ──────────────────────────────────────────────────

    [Fact]
    public async Task Controller_StopBeforePlay_IsNoOp()
    {
        var fig = new Figure();
        var ctrl = new AnimationController<int>(
            new LegacyAnimationAdapter(new AnimationBuilder(3, _ => fig)),
            (_, _) => Task.CompletedTask);

        // Stop before any Play call must not throw.
        ctrl.Stop();
        Assert.Equal(AnimationPlaybackState.Stopped, ctrl.State);
    }

    [Fact]
    public async Task Controller_PlayThenComplete_NormalPath()
    {
        var fig = new Figure();
        int frames = 0;
        var ctrl = new AnimationController<int>(
            new LegacyAnimationAdapter(new AnimationBuilder(3, _ => fig)
            { Interval = TimeSpan.FromMilliseconds(1), Loop = false }),
            (_, _) => { frames++; return Task.CompletedTask; });

        // Use TestContext.Current.CancellationToken (xUnit v3) so the test cancels
        // promptly if the runner aborts -- avoids xUnit1051 warning.
        await ctrl.PlayAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, frames);
        Assert.Equal(AnimationPlaybackState.Stopped, ctrl.State);
    }

    [Fact]
    public async Task Controller_ExternalCancellation_StopsPlayback()
    {
        var fig = new Figure();
        var ctrl = new AnimationController<int>(
            new LegacyAnimationAdapter(new AnimationBuilder(1000, _ => fig)
            { Interval = TimeSpan.FromMilliseconds(100), Loop = true }),
            (_, _) => Task.CompletedTask);

        using var externalCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        await ctrl.PlayAsync(externalCts.Token);

        Assert.Equal(AnimationPlaybackState.Stopped, ctrl.State);
    }

    [Fact]
    public async Task Controller_DisposeAsync_StopsAndIsIdempotent()
    {
        var fig = new Figure();
        var ctrl = new AnimationController<int>(
            new LegacyAnimationAdapter(new AnimationBuilder(3, _ => fig)),
            (_, _) => Task.CompletedTask);

        await ctrl.DisposeAsync();
        await ctrl.DisposeAsync();   // second dispose must not throw
    }
}
