// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using SkiaSharp;
using Uno.WinUI.Graphics2DSK;
using Windows.Foundation;
using MatPlotLibNet.Models.Streaming;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Skia;

namespace MatPlotLibNet.Uno;

/// <summary>
/// A Uno Platform <see cref="SKCanvasElement"/> that renders a <see cref="StreamingFigure"/> with
/// automatic re-rendering when streamed data arrives. Subscribes to
/// <see cref="StreamingFigure.RenderRequested"/> and marshals invalidation via
/// <see cref="DispatcherQueue"/>.
/// </summary>
public sealed class MplStreamingChartElement : SKCanvasElement
{
    /// <summary>Identifies the <see cref="StreamingFigure"/> dependency property.</summary>
    public static readonly DependencyProperty StreamingFigureProperty =
        DependencyProperty.Register(nameof(StreamingFigure), typeof(StreamingFigure),
            typeof(MplStreamingChartElement),
            new PropertyMetadata(null, static (d, e) => ((MplStreamingChartElement)d).OnStreamingFigureChanged(
                e.OldValue as StreamingFigure, e.NewValue as StreamingFigure)));

    private StreamingFigure? _subscribedFigure;

    /// <summary>Gets or sets the <see cref="Models.Streaming.StreamingFigure"/> to render.</summary>
    public StreamingFigure? StreamingFigure
    {
        get => (StreamingFigure?)GetValue(StreamingFigureProperty);
        set => SetValue(StreamingFigureProperty, value);
    }

    /// <inheritdoc />
    protected override void RenderOverride(SKCanvas canvas, Windows.Foundation.Size area)
    {
        var sf = StreamingFigure;
        if (sf is null) return;
        sf.ApplyAxisScaling();
        var ctx = new SkiaRenderContext(canvas);
        ChartServices.Renderer.Render(sf.Figure, ctx);
    }

    private void OnStreamingFigureChanged(StreamingFigure? oldValue, StreamingFigure? newValue)
    {
        if (_subscribedFigure is not null)
        {
            _subscribedFigure.RenderRequested -= OnRenderRequested;
            _subscribedFigure = null;
        }

        if (newValue is not null)
        {
            _subscribedFigure = newValue;
            newValue.RenderRequested += OnRenderRequested;
        }
    }

    private void OnRenderRequested() =>
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, Invalidate);
}
