// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Streaming;

namespace MatPlotLibNet.AspNetCore;

/// <summary>Server-side streaming session that subscribes to a <see cref="StreamingFigure"/>'s
/// <see cref="StreamingFigure.RenderRequested"/> event and publishes the rendered SVG to all
/// connected clients via <see cref="IChartPublisher"/>. Enables server-pushed streaming charts
/// to Blazor WASM, Angular, React, and Vue clients over SignalR.</summary>
internal sealed class StreamingChartSession : IAsyncDisposable
{
    private readonly IChartPublisher _publisher;
    private readonly StreamingFigure _streamingFigure;
    private readonly string _chartId;
    private bool _disposed;

    public StreamingChartSession(string chartId, StreamingFigure streamingFigure, IChartPublisher publisher)
    {
        _chartId = chartId;
        _streamingFigure = streamingFigure;
        _publisher = publisher;
        _streamingFigure.RenderRequested += OnRenderRequested;
    }

    private void OnRenderRequested()
    {
        _streamingFigure.ApplyAxisScaling();
        _ = _publisher.PublishSvgAsync(_chartId, _streamingFigure.Figure);
    }

    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            DisposeCore();
        }
        return ValueTask.CompletedTask;
    }

    private void DisposeCore()
    {
        _streamingFigure.RenderRequested -= OnRenderRequested;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
