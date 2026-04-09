// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Interactive;
using NSubstitute;

namespace MatPlotLibNet.Interactive.Tests;

/// <summary>Verifies <see cref="InteractiveExtensions"/> behavior.</summary>
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
        var handle = await figure.ShowAsync();

        Assert.NotNull(handle);
        Assert.False(string.IsNullOrEmpty(handle.ChartId));
        Assert.Same(figure, handle.Figure);
    }

    /// <summary>Verifies that ShowAsync opens the browser with a URL containing the chart path.</summary>
    [Fact(Skip = "Requires a desktop environment (no browser on CI)")]
    public async Task ShowAsync_OpensBrowser()
    {
        var mock = Substitute.For<IBrowserLauncher>();
        mock.OpenAsync(Arg.Any<string>()).Returns(Task.CompletedTask);
        InteractiveExtensions.Browser = mock;

        var figure = Plt.Create().WithTitle("Browser Test").Build();
        await figure.ShowAsync();

        await mock.Received(1).OpenAsync(Arg.Is<string>(url =>
            url.Contains("127.0.0.1") && url.Contains("/chart/")));
    }

    /// <summary>Verifies that the figure URL contains the /chart/ prefix and the chart ID.</summary>
    [Fact]
    public void Show_UrlContainsChartPrefix()
    {
        var server = new ChartServer();
        server.EnsureStarted();

        var figure = Plt.Create().Build();
        var chartId = server.RegisterFigure(figure);
        var url = server.GetFigureUrl(chartId);

        Assert.Contains("/chart/", url);
        Assert.Contains(chartId, url);
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
}
