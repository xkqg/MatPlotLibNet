// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace MatPlotLibNet.Blazor.Tests;

/// <summary>A live chart that a viewer can zoom and pan. The script inside the SVG runs in the page, the connection to the
/// server is held by the .NET client, and between them sits a small module: the component hands it a bridge, the script
/// sends its gestures through the module's proxy, the bridge forwards them to the client, and the messages about the other
/// viewers come back the same way. A redrawn picture is not mounted while the viewer is in the middle of a drag.</summary>
public class MplLiveChartGestureTests : BunitContext
{
    private const string Module = "./_content/MatPlotLibNet.Blazor/mplLiveChart.js";

    /// <summary>A client that also sends gestures and hands on messages: what the Blazor client is.</summary>
    private sealed class LiveClient : IChartSubscriptionClient, IChartGestureSender, IChartMessageSource
    {
        private readonly List<Func<string, string, Task>> _svg = [];
        private readonly List<Func<string, JsonElement, Task>> _messages = [];

        public List<(string Method, string Payload)> Sent { get; } = [];
        public int SvgSubscribers => _svg.Count;
        public int MessageSubscribers => _messages.Count;
        public bool Disposed { get; private set; }
        public bool IsConnected => true;

        public Task ConnectAsync(string hubUrl, CancellationToken ct = default) => Task.CompletedTask;
        public Task SubscribeAsync(string chartId, CancellationToken ct = default) => Task.CompletedTask;
        public Task UnsubscribeAsync(string chartId, CancellationToken ct = default) => Task.CompletedTask;
        public void OnSvgUpdated(Func<string, string, Task> handler) { }
        public void OnChartUpdated(Func<string, string, Task> handler) { }

        public IDisposable SubscribeSvgUpdates(Func<string, string, Task> handler)
        {
            _svg.Add(handler);
            return new Token(() => _svg.Remove(handler));
        }

        public IDisposable SubscribeMessages(Func<string, JsonElement, Task> handler)
        {
            _messages.Add(handler);
            return new Token(() => _messages.Remove(handler));
        }

        public Task SendGestureAsync(string method, JsonElement payload, CancellationToken ct = default)
        {
            Sent.Add((method, payload.GetRawText()));
            return Task.CompletedTask;
        }

        public Task PushSvgAsync(string id, string svg) => Task.WhenAll(_svg.ToArray().Select(h => h(id, svg)));
        public Task PushMessageAsync(string method, string argsJson)
        {
            using var doc = JsonDocument.Parse(argsJson);
            var args = doc.RootElement.Clone();
            return Task.WhenAll(_messages.ToArray().Select(h => h(method, args)));
        }

        public ValueTask DisposeAsync() { Disposed = true; return ValueTask.CompletedTask; }

        private sealed class Token(Action remove) : IDisposable { public void Dispose() => remove(); }
    }

    private BunitJSModuleInterop Module_() => JSInterop.SetupModule(Module);

    private (IRenderedComponent<MplLiveChart> Cut, LiveClient Client, LiveChartBridge Bridge, BunitJSModuleInterop Js, Func<bool> Busy)
        Mount(string chartId = "chart-1")
    {
        var client = new LiveClient();
        bool busy = false;
        var js = Module_();
        js.SetupVoid("attach", _ => true).SetVoidResult();
        js.Setup<bool>("isBusy", _ => true).SetResult(false);
        js.SetupVoid("deliver", _ => true).SetVoidResult();

        var cut = Render<MplLiveChart>(p => p.Add(c => c.ChartId, chartId).Add(c => c.Client, client));

        // The component attaches to the page after its first render, in a continuation.
        cut.WaitForAssertion(() => Assert.Equal(1, client.MessageSubscribers));
        var attach = js.Invocations["attach"].Single();
        var bridge = ((DotNetObjectReference<LiveChartBridge>)attach.Arguments[1]!).Value;
        return (cut, client, bridge, js, () => busy);
    }

    [Fact]
    public void TheHostElement_IsMarkedAsTheChartsLiveHost_AndTheModuleIsAttachedToIt()
    {
        var (cut, _, _, js, _) = Mount("chart-7");

        Assert.Contains("data-mpl-live=\"chart-7\"", cut.Markup);
        Assert.Single(js.Invocations["attach"]);
    }

    [Fact]
    public async Task AGestureTheScriptSends_IsForwardedToTheClient()
    {
        var (_, client, bridge, _, _) = Mount();
        using var doc = JsonDocument.Parse("{\"chartId\":\"chart-1\",\"axesIndex\":0,\"xMin\":1,\"xMax\":2,\"yMin\":3,\"yMax\":4}");

        await bridge.Send("OnZoom", doc.RootElement.Clone());

        var (method, payload) = Assert.Single(client.Sent);
        Assert.Equal("OnZoom", method);
        Assert.Contains("\"xMax\":2", payload);
    }

    [Fact]
    public async Task AMessageAboutAnotherViewer_IsPassedOnToThePageWithItsArguments()
    {
        var (_, client, _, js, _) = Mount();

        await client.PushMessageAsync("ReceiveCursor", "[\"chart-1\",\"v2\",\"Viewer 2\",0,1.5,2.5]");

        var call = Assert.Single(js.Invocations["deliver"]);
        Assert.Equal("ReceiveCursor", call.Arguments[1]);
    }

    [Fact]
    public async Task ARedrawnPicture_WaitsForTheViewerToFinishTheirDrag_AndTheLatestWins()
    {
        var (cut, client, bridge, js, _) = Mount();
        js.Setup<bool>("isBusy", _ => true).SetResult(true);

        await client.PushSvgAsync("chart-1", "<svg data-test=\"first\"/>");
        await client.PushSvgAsync("chart-1", "<svg data-test=\"second\"/>");
        cut.Render();
        Assert.DoesNotContain("data-test=\"first\"", cut.Markup);
        Assert.DoesNotContain("data-test=\"second\"", cut.Markup);

        await bridge.OnIdle();
        cut.Render();

        Assert.Contains("data-test=\"second\"", cut.Markup);
        Assert.DoesNotContain("data-test=\"first\"", cut.Markup);
    }

    [Fact]
    public async Task ARedrawnPicture_WhenTheChartIsIdle_IsMountedAtOnce()
    {
        var (cut, client, _, _, _) = Mount();

        await client.PushSvgAsync("chart-1", "<svg data-test=\"now\"/>");
        cut.Render();

        Assert.Contains("data-test=\"now\"", cut.Markup);
    }

    [Fact]
    public async Task IdleWithNothingHeldBack_ChangesNothing()
    {
        var (cut, _, bridge, _, _) = Mount();
        var before = cut.Markup;

        await bridge.OnIdle();

        Assert.Equal(before, cut.Markup);
    }

    [Fact]
    public async Task WhenTheComponentGoesAway_ItLeavesNothingBehindOnTheSharedClient()
    {
        var (cut, client, _, js, _) = Mount();
        Assert.Equal(1, client.SvgSubscribers);
        Assert.Equal(1, client.MessageSubscribers);

        await cut.Instance.DisposeAsync();

        Assert.Equal(0, client.SvgSubscribers);
        Assert.Equal(0, client.MessageSubscribers);
        Assert.False(client.Disposed);   // the client is the host application's: only a client the component made is disposed
    }

    [Fact]
    public async Task AClosedCircuit_DoesNotMakeTheDisposeThrow()
    {
        var (cut, _, _, js, _) = Mount();
        js.Mode = JSRuntimeMode.Loose;

        var thrown = await Record.ExceptionAsync(async () => await cut.Instance.DisposeAsync());

        Assert.Null(thrown);
    }

    [Fact]
    public async Task APictureForAPageThatIsGone_IsMountedAnyway()
    {
        var (cut, client, _, js, _) = Mount();
        js.Setup<bool>("isBusy", _ => true).SetException(new JSDisconnectedException("the circuit closed"));

        await client.PushSvgAsync("chart-1", "<svg data-test=\"late\"/>");
        cut.Render();

        Assert.Contains("data-test=\"late\"", cut.Markup);
    }

    [Fact]
    public async Task AMessageForAPageThatIsGone_IsDropped()
    {
        var (_, client, _, js, _) = Mount();
        js.SetupVoid("deliver", _ => true).SetException(new JSDisconnectedException("the circuit closed"));

        var thrown = await Record.ExceptionAsync(() => client.PushMessageAsync("ViewerLeft", "[\"chart-1\",\"v2\"]"));

        Assert.Null(thrown);
    }

    [Fact]
    public async Task ACircuitThatClosesBeforeTheModuleIsDisposed_DoesNotMakeTheDisposeThrow()
    {
        Services.AddSingleton<IJSRuntime>(new GoneAtTeardownRuntime());
        var client = new LiveClient();
        var cut = Render<MplLiveChart>(p => p.Add(c => c.ChartId, "chart-1").Add(c => c.Client, client));
        cut.WaitForAssertion(() => Assert.Equal(1, client.MessageSubscribers));

        var thrown = await Record.ExceptionAsync(async () => await cut.Instance.DisposeAsync());

        Assert.Null(thrown);
    }

    [Fact]
    public async Task AModuleThatLetsGoLater_IsWaitedFor()
    {
        Services.AddSingleton<IJSRuntime>(new GoneAtTeardownRuntime(slow: true));
        var client = new LiveClient();
        var cut = Render<MplLiveChart>(p => p.Add(c => c.ChartId, "chart-1").Add(c => c.Client, client));
        cut.WaitForAssertion(() => Assert.Equal(1, client.MessageSubscribers));

        await cut.Instance.DisposeAsync();

        Assert.Equal(0, client.MessageSubscribers);
    }

    [Fact]
    public async Task AComponentThatWasNeverShown_HasNothingToTakeAway()
    {
        var thrown = await Record.ExceptionAsync(async () => await new MplLiveChart().DisposeAsync());

        Assert.Null(thrown);
    }

    /// <summary>A page whose module imports and attaches, and whose circuit is gone by the time the component lets the module go.</summary>
    private sealed class GoneAtTeardownRuntime(bool slow = false) : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            new((TValue)(object)new GoneAtTeardownModule(slow));

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            InvokeAsync<TValue>(identifier, args);
    }

    private sealed class GoneAtTeardownModule(bool slow) : IJSObjectReference
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => default;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => default;

        public ValueTask DisposeAsync() =>
            slow ? new ValueTask(Task.Delay(20)) : throw new JSDisconnectedException("the circuit closed");
    }
}
