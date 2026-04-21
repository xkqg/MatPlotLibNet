// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Bunit;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Blazor.Tests;

/// <summary>Phase Y.7 (v1.7.2, 2026-04-19) — branch coverage for
/// <see cref="MplLiveChart"/> arms not exercised by
/// <see cref="MplLiveChartLifecycleTests"/>:
///   - line 28: `Client ?? new ChartSubscriptionClient()` — explicit-Client arm
///   - lines 43, 53: `id == ChartId` false arm (callback fires for a DIFFERENT chartId)
///
/// Uses an in-memory test client (records callback invocations) instead of a real
/// SignalR HubConnection so tests don't need a live server.</summary>
public class MplLiveChartCoverageTests : BunitContext
{
    /// <summary>Client parameter set explicitly — line 28's `Client ?? ...` skips
    /// the default-construction arm. Forward-regression guard for the test-injection
    /// hook added in Phase J.1.</summary>
    [Fact]
    public void OnInitialized_WithExplicitClient_UsesProvidedInstance()
    {
        var client = new RecordingClient();
        var cut = Render<MplLiveChart>(p =>
        {
            p.Add(x => x.ChartId, "c1");
            p.Add(x => x.Client, client);
        });
        // After OnAfterRenderAsync (firstRender=true), Connect + Subscribe were called.
        Assert.True(client.Connected);
        Assert.Contains("c1", client.Subscribed);
    }

    /// <summary>OnSvgUpdated callback fires with a NON-MATCHING chartId — line 43
    /// `id == ChartId` false arm. _svgContent must NOT update.</summary>
    [Fact]
    public void OnSvgUpdated_DifferentChartId_DoesNotUpdateContent()
    {
        var client = new RecordingClient();
        var cut = Render<MplLiveChart>(p =>
        {
            p.Add(x => x.ChartId, "c1");
            p.Add(x => x.Client, client);
        });

        client.FireSvgUpdated("c2", "<svg>other</svg>");
        cut.Render();
        Assert.DoesNotContain("other", cut.Markup);
    }

    /// <summary>OnChartUpdated callback fires with a NON-MATCHING chartId — line 53
    /// false arm. JSON deserialisation is skipped.</summary>
    [Fact]
    public void OnChartUpdated_DifferentChartId_DoesNotDeserialiseJson()
    {
        var client = new RecordingClient();
        var cut = Render<MplLiveChart>(p =>
        {
            p.Add(x => x.ChartId, "c1");
            p.Add(x => x.Client, client);
        });

        // Invalid JSON for the wrong chartId — must NOT throw because the if-guard
        // short-circuits before FromJson runs.
        client.FireChartUpdated("c2", "not valid json at all");
    }

    // ── Wave J.2 — remaining MplLiveChart branches ───────────────────────────

    /// <summary>L33 FALSE arm — InitialFigure is null (default), OnParametersSet
    /// skips the SVG pre-render. Markup should contain only the wrapper div.</summary>
    [Fact]
    public void OnParametersSet_NoInitialFigure_SvgContentEmpty()
    {
        var client = new RecordingClient();
        var cut = Render<MplLiveChart>(p =>
        {
            p.Add(x => x.ChartId, "c1");
            p.Add(x => x.Client, client);
        });
        Assert.DoesNotContain("<svg", cut.Markup);
    }

    /// <summary>L39 TRUE arm — !firstRender == true (second call to OnAfterRenderAsync
    /// with firstRender=false) → returns immediately without re-subscribing.</summary>
    [Fact]
    public void OnAfterRenderAsync_SubsequentRender_DoesNotResubscribe()
    {
        var client = new RecordingClient();
        var cut = Render<MplLiveChart>(p =>
        {
            p.Add(x => x.ChartId, "c1");
            p.Add(x => x.Client, client);
        });
        int subscribedBefore = client.Subscribed.Count;
        cut.Render(); // triggers OnAfterRenderAsync(firstRender=false)
        Assert.Equal(subscribedBefore, client.Subscribed.Count);
    }

    /// <summary>L43 TRUE arm — OnSvgUpdated fires with the MATCHING chartId →
    /// _svgContent updates and the markup reflects the new SVG.</summary>
    [Fact]
    public void OnSvgUpdated_MatchingChartId_UpdatesContent()
    {
        var client = new RecordingClient();
        var cut = Render<MplLiveChart>(p =>
        {
            p.Add(x => x.ChartId, "c1");
            p.Add(x => x.Client, client);
        });

        client.FireSvgUpdated("c1", "<svg><text>updated</text></svg>");
        cut.Render();
        Assert.Contains("updated", cut.Markup);
    }

    /// <summary>L53 TRUE arm — OnChartUpdated fires with the MATCHING chartId →
    /// JSON is deserialised, rendered, and the SVG content updated.</summary>
    [Fact]
    public void OnChartUpdated_MatchingChartId_DeserializesAndRenders()
    {
        var client = new RecordingClient();
        var cut = Render<MplLiveChart>(p =>
        {
            p.Add(x => x.ChartId, "c1");
            p.Add(x => x.Client, client);
        });

        var json = ChartServices.Serializer.ToJson(
            Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build());
        client.FireChartUpdated("c1", json);
        cut.Render();
        Assert.Contains("<svg", cut.Markup);
    }

    /// <summary>Recording client — captures Connect/Subscribe + exposes the registered
    /// callbacks so tests can fire synthetic UpdateChartSvg / UpdateChart events.</summary>
    private sealed class RecordingClient : IChartSubscriptionClient
    {
        private Func<string, string, Task>? _onSvg;
        private Func<string, string, Task>? _onJson;

        public bool Connected { get; private set; }
        public bool IsConnected => Connected;
        public List<string> Subscribed { get; } = new();

        public Task ConnectAsync(string hubUrl, CancellationToken ct = default)
        {
            Connected = true;
            return Task.CompletedTask;
        }

        public Task SubscribeAsync(string chartId, CancellationToken ct = default)
        {
            Subscribed.Add(chartId);
            return Task.CompletedTask;
        }

        public Task UnsubscribeAsync(string chartId, CancellationToken ct = default) =>
            Task.CompletedTask;

        public void OnSvgUpdated(Func<string, string, Task> handler) => _onSvg = handler;
        public void OnChartUpdated(Func<string, string, Task> handler) => _onJson = handler;

        public void FireSvgUpdated(string id, string svg) => _onSvg?.Invoke(id, svg).Wait();
        public void FireChartUpdated(string id, string json) => _onJson?.Invoke(id, json).Wait();

        public ValueTask DisposeAsync() => default;
    }
}
