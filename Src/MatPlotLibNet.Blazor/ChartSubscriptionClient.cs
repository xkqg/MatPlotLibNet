// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Text.Json;
using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Interaction;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Blazor;

/// <summary>SignalR-based implementation of <see cref="IChartSubscriptionClient"/> for Blazor and .NET clients. It is
/// also what a live chart's script reaches the server through (<see cref="IChartGestureSender"/>) and hears about the
/// other viewers from (<see cref="IChartMessageSource"/>): one connection per viewer, owned here.</summary>
public sealed class ChartSubscriptionClient : IChartSubscriptionClient, IChartGestureSender, IChartMessageSource
{
    private readonly IHubConnector _connector;

    private HubConnection? _hub;
    private IDisposable? _svgUpdatedToken;
    private IDisposable? _chartUpdatedToken;
    private IDisposable? _partsToken;
    private Func<string, string, Task>? _onSvgUpdated;
    private Func<string, string, Task>? _onChartUpdated;
    private bool _disposed;

    // Several callbacks can listen at once (two components showing one chart through this client), and a connection that
    // came back must join its charts again, so both are kept as sets that a callback can leave.
    private readonly ConcurrentDictionary<Guid, Func<string, string, Task>> _svgSubscribers = new();
    private readonly ConcurrentDictionary<Guid, Func<string, JsonElement, Task>> _messageSubscribers = new();
    private readonly ConcurrentDictionary<string, byte> _subscribedCharts = new();

    // The last whole document of every chart the server sends in parts: a part that arrives is put in place in it, and the
    // whole document is handed on, so a component never sees a part.
    private readonly ConcurrentDictionary<string, SvgPartsDocument> _documents = new();
    private readonly List<IDisposable> _messageTokens = [];

    /// <summary>Creates a client that is not yet connected; call <see cref="ConnectAsync"/> with the hub's URL to open its one connection.</summary>
    public ChartSubscriptionClient() : this(new SignalRHubConnector()) { }

    /// <summary>A client that connects and listens through <paramref name="connector"/>: a test hands it one that watches, or
    /// stands in for, the connection; every other client is made with SignalR's.</summary>
    internal ChartSubscriptionClient(IHubConnector connector) => _connector = connector;

    /// <inheritdoc/>
    public bool IsConnected => _hub?.State == HubConnectionState.Connected;

    /// <summary>The server's name for this connection, or <see langword="null"/> while not connected.</summary>
    internal string? ConnectionId => _hub?.ConnectionId;

    /// <inheritdoc/>
    public async Task ConnectAsync(string hubUrl, CancellationToken ct = default)
    {
        // BUG B1(a): a prior _hub must be fully torn down before rebuilding — otherwise
        // it is silently orphaned and, thanks to WithAutomaticReconnect, keeps
        // reconnecting forever (zombie connection, resource leak).
        await DisposeCurrentHubAsync();

        _hub = _connector.Connect(hubUrl);

        // BUG B1(b): capture the On(...) tokens so they can be explicitly unregistered
        // (DisposeCurrentHubAsync) instead of being discarded, which made them
        // impossible to unregister short of disposing the whole hub.
        _svgUpdatedToken = _connector.Listen(_hub, HubMethodNames.UpdateChartSvg, HandleSvgUpdatedAsync);
        _chartUpdatedToken = _connector.Listen(_hub, HubMethodNames.UpdateChart, HandleChartUpdatedAsync);
        _partsToken = _hub.On<string, SvgPartsMessage>(HubMethodNames.UpdateChartParts, HandlePartsAsync);
        RegisterMessageHandlers(_hub);
        _hub.Reconnected += _ => HandleReconnectedAsync();

        await _hub.StartAsync(ct);
    }

    /// <summary>The messages about the other viewers and the tooltip the server computes for this one, each handed to the
    /// message subscribers as the name of the client method and its arguments.</summary>
    private void RegisterMessageHandlers(HubConnection hub)
    {
        _messageTokens.Add(hub.On<string, string>(HubMethodNames.ReceiveTooltipContent,
            (chartId, html) => DeliverMessageAsync(HubMethodNames.ReceiveTooltipContent, chartId, html)));
        _messageTokens.Add(hub.On<string, string, string, int, double, double>(HubMethodNames.ReceiveCursor,
            (chartId, viewerId, viewerName, axesIndex, x, y) =>
                DeliverMessageAsync(HubMethodNames.ReceiveCursor, chartId, viewerId, viewerName, axesIndex, x, y)));
        _messageTokens.Add(hub.On<string, string, string>(HubMethodNames.ViewChanged,
            (chartId, viewerId, viewerName) => DeliverMessageAsync(HubMethodNames.ViewChanged, chartId, viewerId, viewerName)));
        _messageTokens.Add(hub.On<string, string>(HubMethodNames.ViewerLeft,
            (chartId, viewerId) => DeliverMessageAsync(HubMethodNames.ViewerLeft, chartId, viewerId)));
    }

    private async Task DeliverMessageAsync(string method, params object[] args)
    {
        if (_messageSubscribers.IsEmpty) return;

        var json = JsonSerializer.SerializeToElement(args);
        foreach (var subscriber in _messageSubscribers.Values)
            await subscriber(method, json);
    }

    /// <summary>A lost connection that came back is a new connection to the server: it has none of the groups the old one
    /// joined. Every chart this client had subscribed to is subscribed to again, so a chart does not go silent for good
    /// after a network blip.</summary>
    internal async Task HandleReconnectedAsync()
    {
        var hub = _hub;
        if (hub is null) return;

        foreach (var chartId in _subscribedCharts.Keys)
        {
            try
            {
                await SubscribeOnAsync(hub, chartId, CancellationToken.None);
            }
            catch (Exception ex)
            {
                ChartDiagnostics.Emit(new ChartDiagnostic(
                    nameof(ChartSubscriptionClient),
                    $"Subscribing to '{chartId}' again after a reconnect failed: {ex.Message}",
                    ex));
            }
        }
    }

    /// <summary>Disposes the current hub's subscription tokens and the hub itself (if
    /// present), then clears both fields. Shared by <see cref="ConnectAsync"/> (before
    /// rebuilding a new hub) and <see cref="DisposeAsync"/> (final teardown).</summary>
    private async Task DisposeCurrentHubAsync()
    {
        _svgUpdatedToken?.Dispose();
        _svgUpdatedToken = null;
        _chartUpdatedToken?.Dispose();
        _chartUpdatedToken = null;
        _partsToken?.Dispose();
        _partsToken = null;
        _documents.Clear();
        foreach (var token in _messageTokens) token.Dispose();
        _messageTokens.Clear();

        if (_hub is not null)
        {
            var hub = _hub;
            _hub = null;
            try
            {
                await hub.DisposeAsync();
            }
            catch (Exception ex)
            {
                // Kept as catch-all (not narrowed to e.g. ObjectDisposedException/
                // InvalidOperationException) on purpose: Dispose/DisposeAsync must never throw
                // (standard .NET guidance — callers use `await using`/finally blocks that assume
                // teardown cannot fail), and an already-closing/closed hub racing with this dispose
                // is legitimate, not a bug. Narrowing here would let an unanticipated hub-internal
                // exception escape a teardown path, which is strictly worse than today. What
                // changes is observability: report the race via ChartDiagnostics so operators can
                // see it instead of it vanishing silently.
                ChartDiagnostics.Emit(new ChartDiagnostic(
                    nameof(ChartSubscriptionClient),
                    $"Hub dispose raced during teardown and was suppressed: {ex.Message}",
                    ex));
            }
        }
    }

    internal async Task HandleSvgUpdatedAsync(string id, string svg)
    {
        if (SvgPartsDocument.IsWrapped(svg))
        {
            var document = new SvgPartsDocument(svg);
            _documents[id] = document;
            await DeliverSvgAsync(id, document.Plain);
            return;
        }

        _documents.TryRemove(id, out _);
        await DeliverSvgAsync(id, svg);
    }

    /// <summary>Puts the parts that changed in place in the last whole document of the chart and hands that document on. A
    /// part for a chart this client holds no whole document of, or one it cannot place, is left: the server sends the whole
    /// document again on the next change that needs it.</summary>
    internal Task HandlePartsAsync(string id, SvgPartsMessage parts) =>
        _documents.TryGetValue(id, out var document) && document.Apply(parts) ? DeliverSvgAsync(id, document.Plain) : Task.CompletedTask;

    private async Task DeliverSvgAsync(string id, string svg)
    {
        if (_onSvgUpdated is not null)
            await _onSvgUpdated(id, svg);

        foreach (var subscriber in _svgSubscribers.Values)
            await subscriber(id, svg);
    }

    internal async Task HandleChartUpdatedAsync(string id, string json)
    {
        if (_onChartUpdated is not null)
            await _onChartUpdated(id, json);
    }

    /// <inheritdoc/>
    public async Task SubscribeAsync(string chartId, CancellationToken ct = default)
    {
        if (_hub is not null)
        {
            await SubscribeOnAsync(_hub, chartId, ct);
            _subscribedCharts[chartId] = 0;
        }
    }

    /// <inheritdoc/>
    public async Task UnsubscribeAsync(string chartId, CancellationToken ct = default)
    {
        if (_hub is not null)
        {
            _subscribedCharts.TryRemove(chartId, out _);
            _documents.TryRemove(chartId, out _);
            await _hub.InvokeAsync(HubMethodNames.Unsubscribe, chartId, ct);
        }
    }

    /// <summary>Subscribes as a viewer that patches; a server from before parts existed has no such method, and is subscribed
    /// to the old way, whole documents only.</summary>
    private static async Task SubscribeOnAsync(HubConnection hub, string chartId, CancellationToken ct)
    {
        try
        {
            await hub.InvokeAsync(HubMethodNames.SubscribeToParts, chartId, ct);
        }
        catch (HubException)
        {
            await hub.InvokeAsync(HubMethodNames.Subscribe, chartId, ct);
        }
    }

    /// <inheritdoc/>
    public void OnSvgUpdated(Func<string, string, Task> handler) => _onSvgUpdated = handler;

    /// <inheritdoc/>
    public IDisposable SubscribeSvgUpdates(Func<string, string, Task> handler)
    {
        var key = Guid.NewGuid();
        _svgSubscribers[key] = handler;
        return new Unsubscription(() => _svgSubscribers.TryRemove(key, out _));
    }

    /// <inheritdoc/>
    public IDisposable SubscribeMessages(Func<string, JsonElement, Task> handler)
    {
        var key = Guid.NewGuid();
        _messageSubscribers[key] = handler;
        return new Unsubscription(() => _messageSubscribers.TryRemove(key, out _));
    }

    /// <inheritdoc/>
    public Task SendGestureAsync(string method, JsonElement payload, CancellationToken ct = default) =>
        _hub is { State: HubConnectionState.Connected } hub ? hub.SendAsync(method, payload, ct) : Task.CompletedTask;

    private sealed class Unsubscription(Action remove) : IDisposable
    {
        public void Dispose() => remove();
    }

    /// <inheritdoc/>
    public void OnChartUpdated(Func<string, string, Task> handler) => _onChartUpdated = handler;

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await DisposeCurrentHubAsync();
    }

    /// <summary>The parts of a chart's SVG that changed, as the server's <c>UpdateChartParts</c> carries them: each part is the
    /// whole <c>&lt;g data-mpl-part="…"&gt;</c> element, and a part that did not change is <see langword="null"/> or absent.</summary>
    /// <param name="RootTag">The root element's new opening tag with its line break, or <see langword="null"/>.</param>
    /// <param name="Background">The new background part, or <see langword="null"/>.</param>
    /// <param name="Overlay">The new part the figure draws above its subplots, or <see langword="null"/>.</param>
    /// <param name="Axes">The subplots that changed.</param>
    internal sealed record SvgPartsMessage(string? RootTag, string? Background, string? Overlay, IReadOnlyList<SvgAxesPartMessage>? Axes);

    /// <summary>One subplot that changed: its index and its new part.</summary>
    /// <param name="Index">The subplot's index.</param>
    /// <param name="Markup">The new part, the whole <c>&lt;g data-mpl-part="axes-…"&gt;</c> element.</param>
    internal readonly record struct SvgAxesPartMessage(int Index, string Markup);

    /// <summary>The last whole document of a chart, wrapped in its parts, into which every part the server sends is put in place
    /// of the part of the same name, so a component is handed a whole SVG while the wire carries only what changed. Every part
    /// of the document runs from its own <c>&lt;g data-mpl-part&gt;</c> to the next one; the last (the overlay) to the first
    /// script after it, or to the end of the document: text in a part is written escaped, so no part holds a script tag.</summary>
    internal sealed class SvgPartsDocument
    {
        /// <summary>A document to patch, as the server sent it whole.</summary>
        internal SvgPartsDocument(string wrapped) => Svg = wrapped;

        /// <summary>The document as it stands.</summary>
        internal string Svg { get; private set; }

        /// <summary>The document without its part groups: what the figure's <c>ToSvg</c> writes, byte for byte, so a component
        /// is handed the same picture whether the server sent it whole or in parts. Each group's opening line and its closing
        /// <c>&lt;/g&gt;</c> line are taken out with the line breaks the server wrote.</summary>
        internal string Plain
        {
            get
            {
                var text = new System.Text.StringBuilder(Svg.Length);
                int at = 0;
                for (int start = Svg.IndexOf(SvgPartMarker.Opening, StringComparison.Ordinal); start >= 0;
                     start = Svg.IndexOf(SvgPartMarker.Opening, at, StringComparison.Ordinal))
                {
                    int end = EndOfPart(Svg, start);
                    text.Append(Svg, at, start - at);
                    int body = Svg.IndexOf('\n', start) + 1;
                    int close = Svg.LastIndexOf(SvgPartMarker.Closing, end - 1, end - body, StringComparison.Ordinal);
                    text.Append(Svg, body, close - body);
                    at = end;
                }

                return text.Append(Svg, at, Svg.Length - at).ToString();
            }
        }

        /// <summary>Whether <paramref name="svg"/> is a document in parts, which can be patched.</summary>
        internal static bool IsWrapped(string svg) => svg.Contains(SvgPartMarker.Opening, StringComparison.Ordinal);

        /// <summary>Puts every part of <paramref name="parts"/> in place of the part of the same name.</summary>
        /// <returns><see langword="false"/>, leaving the document as it was, when a part names one the document does not have:
        /// the document then waits for the server's next whole one.</returns>
        internal bool Apply(SvgPartsMessage parts)
        {
            string text = Svg;
            if (parts.RootTag is { } root)
            {
                int rootEnd = text.IndexOf('\n', StringComparison.Ordinal) + 1;
                text = root + text[rootEnd..];
            }

            var replacements = new List<string>();
            if (parts.Background is { } background) replacements.Add(background);
            foreach (var axes in parts.Axes ?? []) replacements.Add(axes.Markup);
            if (parts.Overlay is { } overlay) replacements.Add(overlay);

            foreach (var part in replacements)
            {
                if (NameOf(part) is not { } name) return false;
                int start = text.IndexOf(SvgPartMarker.Opening + name + "\"", StringComparison.Ordinal);
                if (start < 0) return false;
                int end = EndOfPart(text, start);
                text = text[..start] + part + text[end..];
            }

            Svg = text;
            return true;
        }

        /// <summary>The name a part's opening tag gives it, or <see langword="null"/> for markup that is not a part.</summary>
        private static string? NameOf(string part)
        {
            if (!part.StartsWith(SvgPartMarker.Opening, StringComparison.Ordinal)) return null;
            int close = part.IndexOf('"', SvgPartMarker.Opening.Length);
            return close < 0 ? null : part[SvgPartMarker.Opening.Length..close];
        }

        /// <summary>Where the part that begins at <paramref name="start"/> ends: at the next part, or at the first script after it,
        /// or at the end of the document.</summary>
        private static int EndOfPart(string text, int start)
        {
            int next = text.IndexOf(SvgPartMarker.Opening, start + SvgPartMarker.Opening.Length, StringComparison.Ordinal);
            if (next >= 0) return next;
            int script = text.IndexOf("<script", start, StringComparison.Ordinal);
            return script >= 0 ? script : text.LastIndexOf("</svg>", StringComparison.Ordinal);
        }
    }
}
