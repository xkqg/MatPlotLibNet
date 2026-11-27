// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.AspNetCore;

/// <summary>Default <see cref="IChartPublisher"/> implementation that broadcasts chart updates via a SignalR hub.</summary>
/// <remarks>Made with the hub's <see cref="ChartSubscriptions"/> (as the container makes it), it sends each viewer only what
/// it does not hold yet: a viewer that subscribed with <see cref="ChartHub.SubscribeToParts"/> gets the whole document once,
/// every drawn part in a named group, and after that only the parts that changed (<see cref="IChartHubClient.UpdateChartParts"/>);
/// a viewer that subscribed the old way gets the plain document whenever something changed; and when nothing changed nobody is
/// sent anything, except a viewer that joined since and has no picture yet. That needs a writer that draws the document in its
/// parts, which only the library's own SVG writer does; with any other <see cref="ISvgRenderer"/>, or made without the
/// subscriptions, every publish sends the plain document to the chart's group, as before.
/// <para>What it keeps per chart (the last document sent, who holds it, the gate that keeps two sends of the chart apart) is let
/// go of when the chart ends (<see cref="FigureRegistry.UnregisterAsync"/>) and when its last viewer leaves; the next publish
/// of the chart then sends every viewer the whole picture.</para></remarks>
public sealed class ChartPublisher : IChartPublisher, IKeepingChartPublisher
{
    private readonly IHubContext<ChartHub, IChartHubClient> _hubContext;
    private readonly IChartSerializer _serializer;
    private readonly ISvgRenderer _svgRenderer;
    private readonly ChartSubscriptions? _subscriptions;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _gates = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Sent> _sent = new(StringComparer.Ordinal);

    /// <summary>A publisher that sends the plain document to the chart's group on every publish.</summary>
    public ChartPublisher(IHubContext<ChartHub, IChartHubClient> hubContext, IChartSerializer serializer, ISvgRenderer svgRenderer)
    {
        _hubContext = hubContext;
        _serializer = serializer;
        _svgRenderer = svgRenderer;
    }

    /// <summary>A publisher that sends each viewer only what changed, reading who views a chart, and how, from
    /// <paramref name="subscriptions"/>.</summary>
    public ChartPublisher(IHubContext<ChartHub, IChartHubClient> hubContext, IChartSerializer serializer, ISvgRenderer svgRenderer,
        ChartSubscriptions subscriptions) : this(hubContext, serializer, svgRenderer) => _subscriptions = subscriptions;

    /// <summary>The document last sent for a chart, and the connections that hold it.</summary>
    private sealed record Sent(SvgFigureDocument Document, HashSet<string> Viewers);

    /// <inheritdoc />
    public async Task PublishAsync(string chartId, Figure figure, CancellationToken ct = default)
    {
        var json = _serializer.ToJson(figure);
        await _hubContext.Clients.Group(chartId).UpdateChart(chartId, json);
    }

    /// <inheritdoc />
    public async Task PublishSvgAsync(string chartId, Figure figure, CancellationToken ct = default) =>
        _ = await PublishCoreAsync(chartId, figure, ct);

    /// <summary>Publishes as <see cref="PublishSvgAsync"/> does and hands back the plain SVG of the draw, written from the document
    /// that was published (one copy, no second draw).</summary>
    async Task<string?> IKeepingChartPublisher.PublishSvgAndHandOnAsync(string chartId, Figure figure, CancellationToken ct) =>
        (await PublishCoreAsync(chartId, figure, ct)).Plain();

    /// <summary>The one publish of an SVG: drawn once, sent to whoever needs it.</summary>
    private async Task<PublishedSvg> PublishCoreAsync(string chartId, Figure figure, CancellationToken ct)
    {
        // A writer that draws no document in parts (any but the library's own) says so itself; its document goes whole.
        if (_subscriptions is null || _svgRenderer.RenderDocument(figure) is not { } document)
        {
            string plain = _svgRenderer.Render(figure);
            await _hubContext.Clients.Group(chartId).UpdateChartSvg(chartId, plain);
            return new PublishedSvg(plain, null);
        }

        // The gate of the chart's sends. One taken after the chart was forgotten while this waited on it is no longer the
        // chart's: it is let go of and the chart's gate as it is now is taken instead, so two sends of one chart never overlap.
        SemaphoreSlim gate;
        while (true)
        {
            gate = _gates.GetOrAdd(chartId, static _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync(ct);
            if (_gates.TryGetValue(chartId, out var current) && ReferenceEquals(current, gate)) break;
            gate.Release();
        }

        try
        {
            return new PublishedSvg(await SendAsync(chartId, document), document);
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>Lets go of what is kept for <paramref name="chartId"/>, after the send in progress, if any.</summary>
    async Task IKeepingChartPublisher.ForgetAsync(string chartId)
    {
        if (!_gates.TryGetValue(chartId, out var gate)) return;
        await gate.WaitAsync();
        try
        {
            _sent.TryRemove(chartId, out _);
            _gates.TryRemove(new KeyValuePair<string, SemaphoreSlim>(chartId, gate));
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>What a publish drew: the plain SVG when it was written, and the document it was written from. A publish that wrote
    /// no plain SVG always drew a document, so at least one of the two is set.</summary>
    private readonly record struct PublishedSvg(string? Written, SvgFigureDocument? Document)
    {
        /// <summary>The plain SVG of the draw: the one written for the viewers, or, when nobody needed it, written now.</summary>
        internal string Plain() => Written ?? Document!.ToSvg();
    }

    /// <summary>Sends every viewer of the chart what it does not hold of <paramref name="document"/> yet.</summary>
    /// <returns>The plain SVG when it was written for a viewer, otherwise <see langword="null"/>.</returns>
    private async Task<string?> SendAsync(string chartId, SvgFigureDocument document)
    {
        var viewers = _subscriptions!.ConnectionsOf(chartId);
        var patching = _subscriptions.PartsConnectionsOf(chartId);
        _sent.TryGetValue(chartId, out var last);

        bool sameFrame = last is not null && SameFrame(last.Document, document);
        var patch = sameFrame ? PatchOf(last!.Document, document) : null;
        bool changed = !sameFrame || patch is not null;
        var holding = last?.Viewers ?? [];
        var clients = _hubContext.Clients;

        string? plain = null;
        if (changed)
        {
            plain = document.ToSvg();
            // A chart nobody is recorded as viewing still sends to its group, as it always did: whoever joined it some other
            // way keeps getting the picture.
            if (patching.Count == 0) await clients.Group(chartId).UpdateChartSvg(chartId, plain);
            else await clients.GroupExcept(chartId, patching).UpdateChartSvg(chartId, plain);
        }
        else
        {
            foreach (var viewer in viewers)
                if (!holding.Contains(viewer) && !patching.Contains(viewer))
                    await clients.Client(viewer).UpdateChartSvg(chartId, plain ??= document.ToSvg());
        }

        string? wrapped = null;
        foreach (var viewer in patching)
        {
            if (!sameFrame || !holding.Contains(viewer))
                await clients.Client(viewer).UpdateChartSvg(chartId, wrapped ??= document.ToWrappedSvg());
            else if (patch is not null)
                await clients.Client(viewer).UpdateChartParts(chartId, patch);
        }

        _sent[chartId] = new Sent(document, [.. viewers]);
        return plain;
    }

    /// <summary>Whether <paramref name="now"/> has the frame of <paramref name="before"/>: the same title and description, the
    /// same scripts and as many subplots, so a viewer holding <paramref name="before"/> can be brought to <paramref name="now"/>
    /// part by part.</summary>
    private static bool SameFrame(SvgFigureDocument before, SvgFigureDocument now) =>
        before.HeadBody == now.HeadBody && before.Axes.Count == now.Axes.Count && before.Scripts.SequenceEqual(now.Scripts);

    /// <summary>The parts of <paramref name="now"/> that differ from <paramref name="before"/>, or <see langword="null"/> when
    /// none does. A subplot that was handed out again is the same context; one drawn again that came out the same is equal in
    /// its markup; either is left out.</summary>
    private static ChartSvgParts? PatchOf(SvgFigureDocument before, SvgFigureDocument now)
    {
        string? root = before.RootTag == now.RootTag ? null : now.RootTag;
        string? background = SameMarkup(before.Background, now.Background) ? null : now.WrappedBackground();
        string? overlay = SameMarkup(before.Overlay, now.Overlay) ? null : now.WrappedOverlay();
        var axes = new List<ChartSvgAxesPart>();
        for (int i = 0; i < now.Axes.Count; i++)
            if (!SameMarkup(before.Axes[i], now.Axes[i]))
                axes.Add(new ChartSvgAxesPart(i, now.WrappedAxes(i)));

        return root is null && background is null && overlay is null && axes.Count == 0
            ? null
            : new ChartSvgParts(root, background, overlay, axes);
    }

    private static bool SameMarkup(SvgRenderContext before, SvgRenderContext now) =>
        ReferenceEquals(before, now) || before.Markup.Equals(now.Markup);

    /// <summary>Whether anything is kept for <paramref name="chartId"/>: the last document sent or the gate of its sends.</summary>
    internal bool Holds(string chartId) => _sent.ContainsKey(chartId) || _gates.ContainsKey(chartId);

    /// <inheritdoc />
    public Task PublishViewChangeAsync(string chartId, string viewerId, string viewerName, CancellationToken ct = default) =>
        _hubContext.Clients.Group(chartId).ViewChanged(chartId, viewerId, viewerName);
}
