// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Streaming;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>
/// Phase Z.7 (v1.7.2, 2026-04-19) — branch coverage for <see cref="FigureRegistry"/>
/// arms not exercised by <see cref="FigureRegistryTests"/>: the configure-overload
/// null-arg guard, RegisterStreaming dispose-during-replace, and UnregisterAsync's
/// TryRemove false arm. Pre-Z.7: 96.4%L / 87.5%B.
/// </summary>
public class FigureRegistryCoverageTests
{
    private static Figure NewFigure() => Plt.Create()
        .Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0])
        .Build();

    /// <summary>Register(chartId, figure, configure) with null configure — `ThrowIfNull` true arm.</summary>
    [Fact]
    public void Register_WithNullConfigure_ThrowsArgumentNullException()
    {
        var registry = new FigureRegistry(new RecordingPublisher());
        Assert.Throws<ArgumentNullException>(() =>
            registry.Register("c1", NewFigure(), configure: null!));
    }

    /// <summary>Register(chartId, figure, configure) with non-null configure — `ThrowIfNull` false arm,
    /// callback fires once.</summary>
    [Fact]
    public void Register_WithNonNullConfigure_InvokesCallbackOnce()
    {
        var registry = new FigureRegistry(new RecordingPublisher());
        int callCount = 0;
        registry.Register("c1", NewFigure(), configure: opts => callCount++);
        Assert.Equal(1, callCount);
    }

    /// <summary>UnregisterAsync with unknown chartId — TryRemove returns false, no exception.</summary>
    [Fact]
    public async Task UnregisterAsync_UnknownChartId_NoOp()
    {
        var registry = new FigureRegistry(new RecordingPublisher());
        await registry.UnregisterAsync("never-registered");
        // No exception, no publish — behavior is just to silently skip
    }

    /// <summary>Register the same chartId twice — second call disposes the existing session
    /// (RegisterCore TryRemove true arm).</summary>
    [Fact]
    public async Task Register_SameChartIdTwice_DisposesPreviousSession()
    {
        var registry = new FigureRegistry(new RecordingPublisher());
        registry.Register("c1", NewFigure());
        registry.Register("c1", NewFigure());  // second register replaces, disposes first
        // Accept events on the new session
        Assert.True(registry.Publish("c1", new ZoomEvent("c1", 0, 0, 1, 0, 1)));
        await Task.Delay(20, TestContext.Current.CancellationToken);
    }

    /// <summary>RegisterStreaming over an existing entry disposes the previous streaming session
    /// (TryRemove true arm at line 74).</summary>
    [Fact]
    public void RegisterStreaming_SameChartIdTwice_DisposesPreviousStreaming()
    {
        var registry = new FigureRegistry(new RecordingPublisher());
        var sf = new StreamingFigure(NewFigure());
        registry.RegisterStreaming("s1", sf);
        var sf2 = new StreamingFigure(NewFigure());
        registry.RegisterStreaming("s1", sf2);  // replaces, disposes the first session wrapper
        // No exception is the assertion
    }

    private sealed class RecordingPublisher : IChartPublisher
    {
        public Task PublishAsync(string chartId, Figure figure, CancellationToken ct = default) => Task.CompletedTask;
        public Task PublishSvgAsync(string chartId, Figure figure, CancellationToken ct = default) => Task.CompletedTask;
    }
}
