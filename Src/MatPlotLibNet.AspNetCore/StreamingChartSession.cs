// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models.Streaming;

namespace MatPlotLibNet.AspNetCore;

/// <summary>Server-side streaming session that subscribes to a <see cref="StreamingFigure"/>'s
/// <see cref="StreamingFigure.RenderRequested"/> event and publishes the rendered SVG to all
/// connected clients via <see cref="IChartPublisher"/>. Enables server-pushed streaming charts
/// to Blazor WASM, Angular, React, and Vue clients over SignalR.
/// <para>It also reads what the viewers do to the chart: a <see cref="ChartSession"/> drains their gestures onto the same
/// figure the stream writes. The two share one gate, so a gesture is never applied while a frame is being scaled and
/// rendered, nor the other way round; and while a viewer has moved an axes the scale mode leaves it alone
/// (<see cref="StreamingFigure.ApplyAxisScaling"/>) until Home gives it back.</para></summary>
internal sealed class StreamingChartSession : IAsyncDisposable
{
    private readonly object _gate = new();
    private readonly IChartPublisher _publisher;
    private readonly StreamingFigure _streamingFigure;
    private readonly ChartSession _gestures;
    private readonly string _chartId;
    private long _faultedPublishes;
    private bool _disposed;

    public StreamingChartSession(string chartId, StreamingFigure streamingFigure, IChartPublisher publisher)
        : this(chartId, streamingFigure, publisher, NullCallerPublisher.Instance, new ChartSessionOptions())
    {
    }

    public StreamingChartSession(
        string chartId,
        StreamingFigure streamingFigure,
        IChartPublisher publisher,
        ICallerPublisher callerPublisher,
        ChartSessionOptions options)
    {
        _chartId = chartId;
        _streamingFigure = streamingFigure;
        _publisher = publisher;
        _gestures = new ChartSession(chartId, streamingFigure.Figure, publisher, callerPublisher, options, _gate);
        _streamingFigure.RenderRequested += OnRenderRequested;
    }

    /// <summary>Where the gestures of the viewers are written; the session applies them in order.</summary>
    internal System.Threading.Channels.ChannelWriter<FigureInteractionEvent> Gestures => _gestures.Events.Writer;

    /// <summary>How many publishes of a frame failed. A failure costs that frame; the next one is published as usual.</summary>
    internal long FaultedPublishCount => Interlocked.Read(ref _faultedPublishes);

    private void OnRenderRequested()
    {
        Task publish;
        lock (_gate)
        {
            _streamingFigure.ApplyAxisScaling();
            publish = _publisher.PublishSvgAsync(_chartId, _streamingFigure.Figure);
        }

        // The send is not awaited, because the stream must not wait on a slow client, but it is observed: a send that
        // fails is counted instead of surfacing, much later, as an unobserved exception.
        _ = ObserveAsync(publish);
    }

    private async Task ObserveAsync(Task publish)
    {
        try
        {
            await publish.ConfigureAwait(false);
        }
        catch (Exception)
        {
            Interlocked.Increment(ref _faultedPublishes);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        _streamingFigure.RenderRequested -= OnRenderRequested;
        await _gestures.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
}
