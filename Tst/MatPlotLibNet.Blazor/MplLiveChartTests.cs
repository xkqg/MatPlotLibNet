// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Bunit;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Blazor.Tests;

/// <summary>What <see cref="MplLiveChart"/> does with its client: it takes the one it is given, renders an update for its own chart
/// and passes over one for another chart. An in-memory client records the callbacks, so no hub is needed.</summary>
public class MplLiveChartTests : BunitContext
{
    /// <summary>Client parameter set explicitly — the `Client ?? ...` skips
    /// the default-construction arm. Forward-regression guard for the test-injection
    /// hook added in Phase J.1.</summary>
    [Fact]
    public void OnInitialized_WithExplicitClient_UsesProvidedInstance()
    {
        var client = new RecordingSubscriptionClient();
        var cut = Render<MplLiveChart>(p =>
        {
            p.Add(x => x.ChartId, "c1");
            p.Add(x => x.Client, client);
        });
        // After OnAfterRenderAsync (firstRender=true), Connect + Subscribe were called.
        Assert.True(client.IsConnected);
        Assert.Contains("c1", client.Subscribed);
    }

    /// <summary>OnSvgUpdated callback fires with a NON-MATCHING chartId — the
    /// `id == ChartId` false arm. _svgContent must NOT update.</summary>
    [Fact]
    public void OnSvgUpdated_DifferentChartId_DoesNotUpdateContent()
    {
        var client = new RecordingSubscriptionClient();
        var cut = Render<MplLiveChart>(p =>
        {
            p.Add(x => x.ChartId, "c1");
            p.Add(x => x.Client, client);
        });

        client.FireSvg("c2", "<svg>other</svg>");
        cut.Render();
        Assert.DoesNotContain("other", cut.Markup);
    }

    /// <summary>OnChartUpdated callback fires with a NON-MATCHING chartId — the id check
    /// false arm. JSON deserialisation is skipped.</summary>
    [Fact]
    public void OnChartUpdated_DifferentChartId_DoesNotDeserialiseJson()
    {
        var client = new RecordingSubscriptionClient();
        var cut = Render<MplLiveChart>(p =>
        {
            p.Add(x => x.ChartId, "c1");
            p.Add(x => x.Client, client);
        });

        // Invalid JSON for the wrong chartId — must NOT throw because the if-guard
        // short-circuits before FromJson runs.
        client.FireJson("c2", "not valid json at all");
    }

    // ── Wave J.2 — remaining MplLiveChart branches ───────────────────────────

    /// <summary>InitialFigure is null (default) — OnParametersSet
    /// skips the SVG pre-render. Markup should contain only the wrapper div.</summary>
    [Fact]
    public void OnParametersSet_NoInitialFigure_SvgContentEmpty()
    {
        var client = new RecordingSubscriptionClient();
        var cut = Render<MplLiveChart>(p =>
        {
            p.Add(x => x.ChartId, "c1");
            p.Add(x => x.Client, client);
        });
        Assert.DoesNotContain("<svg", cut.Markup);
    }

    /// <summary>!firstRender == true (second call to OnAfterRenderAsync
    /// with firstRender=false) → returns immediately without re-subscribing.</summary>
    [Fact]
    public void OnAfterRenderAsync_SubsequentRender_DoesNotResubscribe()
    {
        var client = new RecordingSubscriptionClient();
        var cut = Render<MplLiveChart>(p =>
        {
            p.Add(x => x.ChartId, "c1");
            p.Add(x => x.Client, client);
        });
        int subscribedBefore = client.Subscribed.Count;
        cut.Render(); // triggers OnAfterRenderAsync(firstRender=false)
        Assert.Equal(subscribedBefore, client.Subscribed.Count);
    }

    /// <summary>OnSvgUpdated fires with the MATCHING chartId →
    /// _svgContent updates and the markup reflects the new SVG.</summary>
    [Fact]
    public void OnSvgUpdated_MatchingChartId_UpdatesContent()
    {
        var client = new RecordingSubscriptionClient();
        var cut = Render<MplLiveChart>(p =>
        {
            p.Add(x => x.ChartId, "c1");
            p.Add(x => x.Client, client);
        });

        client.FireSvg("c1", "<svg><text>updated</text></svg>");
        cut.Render();
        Assert.Contains("updated", cut.Markup);
    }

    // ── v1.13.0 Wave — ownership arms (_ownsClient / DisposeAsync) ───────────────

    /// <summary>No <c>Client</c> parameter supplied → OnInitialized's
    /// <c>_ownsClient = Client is null; _client = Client ?? new ChartSubscriptionClient();</c>
    /// TRUE arm: the component constructs its own <see cref="ChartSubscriptionClient"/>. That
    /// assignment completes successfully during <c>OnInitialized</c>; it's only the
    /// subsequent <c>OnAfterRenderAsync</c> — which every other test in this file bypasses by
    /// injecting a fake <c>Client</c> — that then attempts a genuine SignalR negotiation via
    /// the real <see cref="MatPlotLibNet.Blazor.ChartSubscriptionClient"/>, and faults with a
    /// <see cref="UriFormatException"/> before any network I/O. bUnit's renderer surfaces
    /// that as the <c>Render</c> call's own exception. Teardown
    /// (<see cref="BunitContext.Dispose"/>, run by xunit after this method returns) then
    /// disposes every component the renderer ever mounted — including this one — which
    /// drives <c>DisposeAsync</c> through its <c>if (_ownsClient)</c> branch.
    /// <para>The HubUrl is pinned to <c>"http://"</c> (structurally invalid: empty authority)
    /// instead of relying on the default relative <c>"/charts-hub"</c>: Uri parsing of rooted
    /// paths is PLATFORM-DEPENDENT — Windows rejects <c>"/charts-hub"</c> with
    /// <see cref="UriFormatException"/>, but Unix accepts it as an absolute <c>file://</c>
    /// path, which made this test red on Linux CI while green on Windows. Scheme-level
    /// invalidity throws identically on every platform.</para></summary>
    [Fact]
    public void NoClientProvided_OwnsClientConstructionArmFires()
    {
        Assert.Throws<UriFormatException>(() =>
            Render<MplLiveChart>(p => p
                .Add(x => x.ChartId, "c1")
                .Add(x => x.HubUrl, "http://")));
    }

    /// <summary>A relative <c>HubUrl</c> (the component default <c>"/charts-hub"</c>) is resolved against the
    /// app's base URI before it reaches the subscription client, and lands on an <b>HTTP</b> scheme.
    /// <para>The scheme assertion is the whole point of this test, and its absence is what let a real defect
    /// ship. Rooted-path URI parsing is platform-dependent: <c>Uri.TryCreate("/charts-hub", UriKind.Absolute,
    /// out _)</c> is <see langword="false"/> on Windows but <see langword="true"/> on Unix, where it yields
    /// <c>file:///charts-hub</c>. An earlier version of this test asserted only "absolute" and "ends with
    /// /charts-hub" — and <c>file:///charts-hub</c> satisfies both. It passed on Linux CI while the component
    /// was sending a Blazor Server app off to connect to a <i>file URI</i>. A test that cannot tell a hub from
    /// a file is not a test of a hub.</para></summary>
    [Fact]
    public void RelativeHubUrl_IsResolvedAgainstTheBaseUri()
    {
        var client = new RecordingSubscriptionClient();

        Render<MplLiveChart>(p => p
            .Add(x => x.ChartId, "c1")
            .Add(x => x.Client, client));

        Assert.NotNull(client.HubUrl);
        Assert.True(Uri.TryCreate(client.HubUrl, UriKind.Absolute, out var uri), $"not absolute: {client.HubUrl}");
        Assert.True(uri!.Scheme is "http" or "https", $"hub URL is not an HTTP address: {client.HubUrl}");
        Assert.EndsWith("/charts-hub", client.HubUrl);
    }

    /// <summary>A <c>file://</c> hub URL is not a hub address — it is what a rooted path degrades into on Unix.
    /// It is resolved against the base URI like any other non-HTTP input, never handed to SignalR as-is.
    /// <para>This pins the platform split on EVERY platform: Windows cannot produce <c>file:///charts-hub</c>
    /// from a rooted path, so the defect was invisible here. Feeding the degraded form in directly makes the
    /// regression reproducible everywhere.</para></summary>
    [Fact]
    public void AFileSchemeHubUrl_IsResolvedNotPassedThrough()
    {
        var client = new RecordingSubscriptionClient();

        Render<MplLiveChart>(p => p
            .Add(x => x.ChartId, "c1")
            .Add(x => x.HubUrl, "file:///charts-hub")
            .Add(x => x.Client, client));

        Assert.StartsWith("http", client.HubUrl!);
        Assert.DoesNotContain("file:", client.HubUrl!);
    }

    /// <summary>A <c>ws://</c> hub URL is a legitimate hub address, and so is an absolute one (cross-origin hub hosting): both pass through untouched.</summary>
    [Theory]
    [InlineData("wss://charts.example.com/charts-hub")]
    [InlineData("http://charts.example.com/charts-hub")]
    public void AWebSocketOrAbsoluteHubUrl_IsPassedThroughUnchanged(string hubUrl)
    {
        var client = new RecordingSubscriptionClient();

        Render<MplLiveChart>(p => p
            .Add(x => x.ChartId, "c1")
            .Add(x => x.HubUrl, hubUrl)
            .Add(x => x.Client, client));

        Assert.Equal(hubUrl, client.HubUrl);
    }
}
