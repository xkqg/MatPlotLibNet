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
/// null publisher (the false arm of its null check), so we can drive every method here without
/// network.
///
/// IVT (set in <c>MatPlotLibNet.Interactive.csproj</c>) gives us access to the
/// internal ctor.</summary>
public class InteractiveFigureTests
{
    /// <summary>InteractiveFigure ctor + property surface.</summary>
    [Fact]
    public void Ctor_StoresChartIdAndFigure()
    {
        var fig = Plt.Create().Plot([1.0], [2.0]).Build();
        var iv = new InteractiveFigure("chart-x10a", fig);
        Assert.Equal("chart-x10a", iv.ChartId);
        Assert.Same(fig, iv.Figure);
    }

    /// <summary>UpdateAsync. Routes through ChartServer.Instance.UpdateFigureAsync;
    /// without EnsureStarted, that method stores the figure in the dictionary and the
    /// null-publisher arm short-circuits — no network, no exception.</summary>
    [Fact]
    public async Task UpdateAsync_WithoutStartedServer_NoOp()
    {
        var fig = Plt.Create().Plot([1.0], [2.0]).Build();
        using var iv = new InteractiveFigure("chart-update-x10a", fig);
        await iv.UpdateAsync();

        Assert.True(ChartServer.Instance.Holds(iv.ChartId));
    }

    /// <summary>CreateController returns a non-null controller for a generic
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

    /// <summary>AnimateAsync&lt;TState&gt; — runs a 1-frame animation that
    /// exercises PublishFrame. With Loop=false + Interval=Zero + 1 frame
    /// the controller publishes once then exits cleanly.</summary>
    [Fact]
    public async Task AnimateAsync_Generic_OneFrame_PublishesAndCompletes()
    {
        var fig = Plt.Create().Plot([1.0], [2.0]).Build();
        using var iv = new InteractiveFigure("chart-animate-x10a", fig);

        var animation = new TestAnimation(frameCount: 1);
        await iv.AnimateAsync(animation, TestContext.Current.CancellationToken);

        Assert.True(ChartServer.Instance.Holds(iv.ChartId));
    }

    /// <summary>AnimateAsync of an <see cref="AnimationBuilder"/> plays the builder as the animation of frame indexes it is,
    /// through the same controller as any other animation.</summary>
    [Fact]
    public async Task AnimateAsync_LegacyBuilder_OneFrame_PublishesAndCompletes()
    {
        var fig = Plt.Create().Plot([1.0], [2.0]).Build();
        using var iv = new InteractiveFigure("chart-legacy-x10a", fig);

        var builder = new AnimationBuilder(frameCount: 1, frameGenerator: i => fig)
        {
            Interval = TimeSpan.Zero,
            Loop = false,
        };
        await iv.AnimateAsync(builder, TestContext.Current.CancellationToken);

        Assert.True(ChartServer.Instance.Holds(iv.ChartId));
    }

    /// <summary>Pre-cancelled token causes the controller's PlayAsync to throw and catch
    /// OperationCanceledException, exiting cleanly. Pins the cancellation arm of the
    /// publish-frame loop without ever invoking the publish callback.</summary>
    [Fact]
    public async Task AnimateAsync_PreCancelledToken_ReturnsCleanly()
    {
        var fig = Plt.Create().Plot([1.0], [2.0]).Build();
        using var iv = new InteractiveFigure("chart-cancel-x10a", fig);

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var animation = new TestAnimation(frameCount: 5);
        await iv.AnimateAsync(animation, cts.Token);

        Assert.False(ChartServer.Instance.Holds(iv.ChartId));
    }

    /// <summary>A shown figure is held by the process' server so its page can be loaded again; disposing the handle lets it
    /// go, so a session that shows many figures does not keep every one of them.</summary>
    [Fact]
    public void Dispose_LetsTheServerGoOfTheFigure()
    {
        var fig = Plt.Create().Plot([1.0], [2.0]).Build();
        var server = ChartServer.Instance;
        var iv = new InteractiveFigure(server.RegisterFigure(fig), fig);
        Assert.True(server.Holds(iv.ChartId));

        iv.Dispose();
        iv.Dispose();   // a second dispose does nothing

        Assert.False(server.Holds(iv.ChartId));
    }

    /// <summary>A handle let go of pushes nothing: an update would hand the server the figure again for good.</summary>
    [Fact]
    public async Task AfterDispose_UpdatesAndAnimationsAreRefused()
    {
        var fig = Plt.Create().Plot([1.0], [2.0]).Build();
        var iv = new InteractiveFigure(ChartServer.Instance.RegisterFigure(fig), fig);
        iv.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(iv.UpdateAsync);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => iv.AnimateAsync(new TestAnimation(frameCount: 1), TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => iv.AnimateAsync(new AnimationBuilder(1, _ => fig), TestContext.Current.CancellationToken));
        Assert.Throws<ObjectDisposedException>(() => iv.CreateController(new TestAnimation(frameCount: 1)));
        Assert.False(ChartServer.Instance.Holds(iv.ChartId));
    }

    /// <summary>A controller made before the handle was disposed plays on without handing the server the figure again.</summary>
    [Fact]
    public async Task AControllerMadeBeforeDispose_PublishesNothingAfter()
    {
        var fig = Plt.Create().Plot([1.0], [2.0]).Build();
        var iv = new InteractiveFigure(ChartServer.Instance.RegisterFigure(fig), fig);
        var controller = iv.CreateController(new TestAnimation(frameCount: 2));
        iv.Dispose();

        await using (controller)
            await controller.PlayAsync(TestContext.Current.CancellationToken);

        Assert.False(ChartServer.Instance.Holds(iv.ChartId));
    }
}
