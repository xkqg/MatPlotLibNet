// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Bunit;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Blazor.Tests;

/// <summary>Phase J.1 of the v1.7.2 follow-on plan — behavioural coverage for
/// <see cref="MplLiveChart"/> SignalR subscription lifecycle. Pre-J.1 only
/// static render tests of <see cref="MplChart"/> existed; the live-chart
/// subscription / dispose path had zero coverage.
///
/// <para>Uses <see cref="RecordingSubscriptionClient"/> injected via the
/// <c>Client</c> parameter so tests can drive the callback path and observe
/// re-render without a running SignalR server.</para></summary>
public class MplLiveChartLifecycleTests : BunitContext
{
    /// <summary>A rooted <c>HubUrl</c> reaches the client RESOLVED against the base URI, not verbatim.
    /// This assertion used to pin <c>"/hub"</c> — pinning a defect: SignalR feeds the value to
    /// <c>new Uri(...)</c>, which throws <see cref="UriFormatException"/> on Windows for a rooted path
    /// (and yields a bogus <c>file://</c> URI on Unix), so a live chart on the component's own default
    /// <c>"/charts-hub"</c> never connected — the initial figure rendered and nothing ever updated.</summary>
    [Fact]
    public void OnAfterRender_CallsConnectAndSubscribe()
    {
        var fake = new RecordingSubscriptionClient();
        var cut = Render<MplLiveChart>(parameters => parameters
            .Add(p => p.ChartId, "chart-1")
            .Add(p => p.HubUrl, "/hub")
            .Add(p => p.Client, fake));

        Assert.Equal("http://localhost/hub", fake.HubUrl);
        Assert.Equal("chart-1", fake.Subscribed[^1]);
    }

    [Fact]
    public void ReceiveSvg_ForMatchingChartId_UpdatesDom()
    {
        var fake = new RecordingSubscriptionClient();
        var cut = Render<MplLiveChart>(parameters => parameters
            .Add(p => p.ChartId, "chart-1")
            .Add(p => p.Client, fake));

        // Simulate server push.
        var svg = "<svg data-test=\"updated\"><text>hello</text></svg>";
        fake.FireSvg("chart-1", svg);
        cut.Render();  // flush

        Assert.Contains("data-test=\"updated\"", cut.Markup);
    }

    [Fact]
    public void ReceiveSvg_ForDifferentChartId_IsIgnored()
    {
        var fake = new RecordingSubscriptionClient();
        var cut = Render<MplLiveChart>(parameters => parameters
            .Add(p => p.ChartId, "chart-1")
            .Add(p => p.Client, fake));

        fake.FireSvg("chart-2", "<svg data-unrelated=\"true\" />");
        cut.Render();

        Assert.DoesNotContain("data-unrelated", cut.Markup);
    }

    /// <summary>BUG B4 repro (council-main-a5fe781-20260704.stap3.md, blocking #4) —
    /// the component must NOT dispose a Client that was injected (unowned) via the
    /// Client parameter; ownership belongs to the DI container/host app. Pre-fix,
    /// DisposeAsync unconditionally disposed whatever client was in use, injected
    /// or not — this test replaces the old (buggy-behavior-pinning) DisposeAsync_
    /// DisposesClient test, which asserted the opposite (Disposed == true) for an
    /// injected client.</summary>
    [Fact]
    public async Task MplLiveChart_InjectedClient_NotDisposedOnTeardown()
    {
        var fake = new RecordingSubscriptionClient();
        var cut = Render<MplLiveChart>(parameters => parameters
            .Add(p => p.ChartId, "chart-1")
            .Add(p => p.Client, fake));

        await ((IAsyncDisposable)cut.Instance).DisposeAsync();
        Assert.False(fake.Disposed);
    }

    /// <summary>The initial figure is drawn before the hub pushes anything.</summary>
    [Fact]
    public void InitialFigure_IsShownBeforeAnyPush()
    {
        var figure = Plt.Create().WithTitle("first view").Plot([1.0, 2.0], [3.0, 4.0]).Build();

        var cut = Render<MplLiveChart>(parameters => parameters
            .Add(p => p.ChartId, "chart-1")
            .Add(p => p.InitialFigure, figure)
            .Add(p => p.Client, new RecordingSubscriptionClient()));

        Assert.Contains(">first view<", cut.Markup);
    }
}
