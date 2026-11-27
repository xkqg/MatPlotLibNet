// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Animation;
using MatPlotLibNet.Models;
using MatPlotLibNet.Interactive;
using NSubstitute;

namespace MatPlotLibNet.Interactive.Tests;

/// <summary>Verifies <see cref="InteractiveExtensions"/> behavior.</summary>
[Collection("InteractiveDisplayGlobalState")]
public class InteractiveExtensionsTests : IDisposable
{
    private readonly IBrowserLauncher _original;

    public InteractiveExtensionsTests()
    {
        _original = InteractiveExtensions.Browser;
        InteractiveExtensions.Browser = Substitute.For<IBrowserLauncher>();
    }

    /// <summary>Verifies that ShowAsync returns an InteractiveFigure with a valid chart ID and the original figure.</summary>
    [Fact]
    public async Task ShowAsync_ReturnsInteractiveFigureWithChartId()
    {
        var figure = Plt.Create().WithTitle("Test").Plot([1.0], [2.0]).Build();
        var handle = await figure.ShowAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(handle);
        Assert.False(string.IsNullOrEmpty(handle.ChartId));
        Assert.Same(figure, handle.Figure);
    }

    /// <summary>Verifies that ShowAsync opens the browser with a URL containing the chart path.</summary>
    [Fact]
    public async Task ShowAsync_OpensBrowser()
    {
        var mock = Substitute.For<IBrowserLauncher>();
        mock.OpenAsync(Arg.Any<string>()).Returns(Task.CompletedTask);
        InteractiveExtensions.Browser = mock;

        var figure = Plt.Create().WithTitle("Browser Test").Build();
        await figure.ShowAsync(TestContext.Current.CancellationToken);

        await mock.Received(1).OpenAsync(Arg.Is<string>(url =>
            url.Contains("127.0.0.1") && url.Contains("/chart/")));
    }

    /// <summary>Verifies that the figure URL contains the /chart/ prefix and the chart ID.</summary>
    [Fact]
    public async Task Show_UrlContainsChartPrefix()
    {
        await using var server = new ChartServer();
        await server.EnsureStartedAsync(TestContext.Current.CancellationToken);

        var figure = Plt.Create().Build();
        var chartId = server.RegisterFigure(figure);
        var url = server.GetFigureUrl(chartId);

        Assert.Contains("/chart/", url);
        Assert.Contains(chartId, url);
    }

    /// <summary>A frame of a play that was stopped before its push is not pushed: the controller hands the push its run's token
    /// and the shown figure passes it on to the server, whose publisher gives up on it. Before, the shown figure dropped the
    /// token, the frame went out after the stop and <see cref="AnimationController{TState}.FrameReady"/> fired for it.</summary>
    [Fact]
    public async Task AFrameOfAPlayStoppedBeforeItsPush_IsNotPushed()
    {
        var shown = await Plt.Create().Plot([1.0], [2.0]).Build().ShowAsync(TestContext.Current.CancellationToken);
        AnimationController<int>? controller = null;
        var animation = new TestAnimation(1, _ =>
        {
            controller!.Stop();
            return Plt.Create().Plot([1.0], [2.0]).Build();
        });
        controller = shown.CreateController(animation);
        int pushed = 0;
        controller.FrameReady += (_, _) => pushed++;

        await controller.PlayAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, animation.FramesMade);
        Assert.Equal(0, pushed);
    }

    /// <summary>Verifies that InteractiveFigure correctly stores the chart ID and figure reference.</summary>
    [Fact]
    public void InteractiveFigure_StoresChartIdAndFigure()
    {
        var figure = Plt.Create().WithTitle("Stored").Build();
        var handle = new InteractiveFigure("my-id", figure);

        Assert.Equal("my-id", handle.ChartId);
        Assert.Same(figure, handle.Figure);
    }

    public void Dispose()
    {
        InteractiveExtensions.Browser = _original;
    }

    /// <summary>Browser setter — `value ?? throw` true arm: passing null
    /// must throw ArgumentNullException.</summary>
    [Fact]
    public void Browser_Setter_Null_ThrowsArgumentNullException()
    {
        var original = InteractiveExtensions.Browser;
        try
        {
            Assert.Throws<ArgumentNullException>(() => InteractiveExtensions.Browser = null!);
        }
        finally
        {
            InteractiveExtensions.Browser = original;
        }
    }

    /// <summary>Browser setter — false arm: passing a non-null instance
    /// stores it (already covered by existing tests; forward-regression guard).</summary>
    [Fact]
    public void Browser_Setter_NonNull_StoresInstance()
    {
        var original = InteractiveExtensions.Browser;
        try
        {
            var custom = new BrowserLauncher();
            InteractiveExtensions.Browser = custom;
            Assert.Same(custom, InteractiveExtensions.Browser);
        }
        finally
        {
            InteractiveExtensions.Browser = original;
        }
    }
}
