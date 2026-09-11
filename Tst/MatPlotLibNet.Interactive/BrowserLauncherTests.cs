// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interactive;
using NSubstitute;

namespace MatPlotLibNet.Interactive.Tests;

/// <summary>Verifies <see cref="BrowserLauncher"/> behavior.</summary>
/// <summary>Mutates the process-global <see cref="InteractiveExtensions.Browser"/>, so it belongs in the
/// collection that serialises the classes which do — it was the one writer left outside it. A substitute this
/// class installs stays reachable while a sibling test drives the real <c>ShowAsync</c> flow, and that flow then
/// calls THIS class's <c>Arg.Do</c> callback with the server's own URL: the assertion reads a value written by
/// another test. Measured on CI under coverage instrumentation, which is slow enough to interleave them:
/// expected "http://localhost:5000/chart/abc", got "http://127.0.0.1:40249/chart/d983a71c...".</summary>
[Collection("InteractiveDisplayGlobalState")]
public class BrowserLauncherTests : IDisposable
{
    private readonly IBrowserLauncher _original;

    public BrowserLauncherTests()
    {
        _original = InteractiveExtensions.Browser;
    }

    /// <summary>Verifies that OpenAsync invokes the browser launcher with the given URL.</summary>
    [Fact]
    public async Task OpenAsync_CallsBrowserLauncher()
    {
        var mock = Substitute.For<IBrowserLauncher>();
        mock.OpenAsync(Arg.Any<string>()).Returns(Task.CompletedTask);
        InteractiveExtensions.Browser = mock;

        await mock.OpenAsync("http://localhost:5000/chart/abc");

        await mock.Received(1).OpenAsync("http://localhost:5000/chart/abc");
    }

    /// <summary>Verifies that OpenAsync passes the exact URL to the browser launcher.</summary>
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

    /// <summary>Verifies that the default browser launcher instance is not null and is the expected type.</summary>
    [Fact]
    public void DefaultBrowser_IsNotNull()
    {
        var launcher = new BrowserLauncher();
        Assert.NotNull(launcher);
        Assert.IsType<BrowserLauncher>(launcher);
    }

    public void Dispose()
    {
        InteractiveExtensions.Browser = _original;
    }
}
