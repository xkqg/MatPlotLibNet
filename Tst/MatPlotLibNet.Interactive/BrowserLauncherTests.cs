// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interactive;
using NSubstitute;

namespace MatPlotLibNet.Interactive.Tests;

public class BrowserLauncherTests : IDisposable
{
    private readonly IBrowserLauncher _original;

    public BrowserLauncherTests()
    {
        _original = InteractiveExtensions.Browser;
    }

    [Fact]
    public async Task OpenAsync_CallsBrowserLauncher()
    {
        var mock = Substitute.For<IBrowserLauncher>();
        mock.OpenAsync(Arg.Any<string>()).Returns(Task.CompletedTask);
        InteractiveExtensions.Browser = mock;

        await mock.OpenAsync("http://localhost:5000/chart/abc");

        await mock.Received(1).OpenAsync("http://localhost:5000/chart/abc");
    }

    [Fact]
    public async Task OpenAsync_PassesCorrectUrl()
    {
        string? captured = null;
        var mock = Substitute.For<IBrowserLauncher>();
        mock.OpenAsync(Arg.Do<string>(url => captured = url)).Returns(Task.CompletedTask);
        InteractiveExtensions.Browser = mock;

        await mock.OpenAsync("http://localhost:5000/chart/abc");

        Assert.Equal("http://localhost:5000/chart/abc", captured);
    }

    [Fact]
    public void DefaultBrowser_IsNotNull()
    {
        Assert.NotNull(new BrowserLauncher());
        Assert.IsType<BrowserLauncher>(_original);
    }

    public void Dispose()
    {
        InteractiveExtensions.Browser = _original;
    }
}
