// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Animation;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Interactive.Tests;

/// <summary>Phase X.10.a (v1.7.2, 2026-04-19) — drives <see cref="InteractiveFigure"/>'s
/// public surface. Pre-X.10.a the class was at 26.7%L because no test exercised the
/// internal ctor or the publish-frame callback. <c>ChartServer.Instance</c> is a
/// process-wide singleton that initialises lazily without starting Kestrel; UpdateFigureAsync
/// against an unstarted server stores the figure in the dictionary and skips the
/// null publisher (line 90 false arm), so we can drive every method here without
/// network.
///
/// IVT (set in <c>MatPlotLibNet.Interactive.csproj</c>) gives us access to the
/// internal ctor.</summary>
public class InteractiveFigureTests
{
    /// <summary>InteractiveFigure ctor + property surface (lines 18-22).</summary>
    [Fact]
    public void Ctor_StoresChartIdAndFigure()
    {
        var fig = Plt.Create().Plot([1.0], [2.0]).Build();
        var iv = new InteractiveFigure("chart-x10a", fig);
        Assert.Equal("chart-x10a", iv.ChartId);
        Assert.Same(fig, iv.Figure);
    }

    /// <summary>UpdateAsync (lines 25-28). Routes through ChartServer.Instance.UpdateFigureAsync;
    /// without EnsureStarted, that method stores the figure in the dictionary and the
    /// null-publisher arm short-circuits — no network, no exception.</summary>
    [Fact]
    public async Task UpdateAsync_WithoutStartedServer_NoOp()
    {
        var fig = Plt.Create().Plot([1.0], [2.0]).Build();
        var iv = new InteractiveFigure("chart-update-x10a", fig);
        await iv.UpdateAsync();
    }

    /// <summary>CreateController (line 47) returns a non-null controller for a generic
    /// state animation. Disposing immediately exercises the controller's IAsyncDisposable.</summary>
    [Fact]
    public async Task CreateController_ReturnsNonNullController()
    {
        var fig = Plt.Create().Plot([1.0], [2.0]).Build();
        var iv = new InteractiveFigure("chart-controller-x10a", fig);

        var animation = new TestAnimation(frameCount: 1);
        var ctrl = iv.CreateController(animation);
        Assert.NotNull(ctrl);
        await ctrl.DisposeAsync();
    }

    /// <summary>AnimateAsync&lt;TState&gt; (lines 39-43) — runs a 1-frame animation that
    /// exercises PublishFrame (line 49-50). With Loop=false + Interval=Zero + 1 frame
    /// the controller publishes once then exits cleanly.</summary>
    [Fact]
    public async Task AnimateAsync_Generic_OneFrame_PublishesAndCompletes()
    {
        var fig = Plt.Create().Plot([1.0], [2.0]).Build();
        var iv = new InteractiveFigure("chart-animate-x10a", fig);

        var animation = new TestAnimation(frameCount: 1);
        await iv.AnimateAsync(animation, TestContext.Current.CancellationToken);
    }

    /// <summary>AnimateAsync (legacy AnimationBuilder) — lines 31-36 wrap AnimationBuilder
    /// in LegacyAnimationAdapter and play it through the same controller.</summary>
    [Fact]
    public async Task AnimateAsync_LegacyBuilder_OneFrame_PublishesAndCompletes()
    {
        var fig = Plt.Create().Plot([1.0], [2.0]).Build();
        var iv = new InteractiveFigure("chart-legacy-x10a", fig);

        var builder = new AnimationBuilder(frameCount: 1, frameGenerator: i => fig)
        {
            Interval = TimeSpan.Zero,
            Loop = false,
        };
        await iv.AnimateAsync(builder, TestContext.Current.CancellationToken);
    }

    /// <summary>Pre-cancelled token causes the controller's PlayAsync to throw and catch
    /// OperationCanceledException, exiting cleanly. Pins the cancellation arm of the
    /// publish-frame loop without ever invoking the publish callback.</summary>
    [Fact]
    public async Task AnimateAsync_PreCancelledToken_ReturnsCleanly()
    {
        var fig = Plt.Create().Plot([1.0], [2.0]).Build();
        var iv = new InteractiveFigure("chart-cancel-x10a", fig);

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var animation = new TestAnimation(frameCount: 5);
        await iv.AnimateAsync(animation, cts.Token);
    }

    /// <summary>Minimal IAnimation&lt;int&gt; for testing — frame count configurable, state is
    /// just an integer counter, and GenerateFrame returns the same constant figure so we
    /// don't invoke any external rendering pipeline.</summary>
    private sealed class TestAnimation : IAnimation<int>
    {
        public int FrameCount { get; }
        public TimeSpan Interval { get; set; } = TimeSpan.Zero;
        public bool Loop { get; set; } = false;

        public TestAnimation(int frameCount) => FrameCount = frameCount;

        public int CreateInitialState() => 0;
        public int Advance(int currentState, int frameIndex) => currentState + 1;
        public Figure GenerateFrame(int state, int frameIndex) =>
            Plt.Create().Plot([1.0], [2.0]).Build();
    }
}
