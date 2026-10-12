// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Windows;
using MatPlotLibNet.Models.Streaming;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Skia;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;

namespace MatPlotLibNet.Wpf;

/// <summary>
/// A WPF control that renders a <see cref="Models.Streaming.StreamingFigure"/> and redraws itself when
/// streamed data arrives.
/// </summary>
/// <remarks>
/// <para>Bind the figure once and leave it there. The ordinary <see cref="MplChartControl"/> takes a
/// <see cref="Models.Figure"/> and redraws when that property changes — which, for a dependency property,
/// means when the <em>reference</em> changes. A streaming figure keeps its identity while its data moves, so
/// driving it through that control meant handing it <see langword="null"/> and the same figure again to fake
/// a change. This control listens to the figure instead.</para>
/// <para>The figure raises <see cref="StreamingFigure.RenderRequested"/> on a thread pool thread, so the
/// request is marshalled to the dispatcher before anything touches the control.</para>
/// </remarks>
public sealed class MplStreamingChartControl : SKElement
{
    /// <summary>Identifies the <see cref="StreamingFigure"/> dependency property.</summary>
    public static readonly DependencyProperty StreamingFigureProperty =
        DependencyProperty.Register(nameof(StreamingFigure), typeof(StreamingFigure),
            typeof(MplStreamingChartControl),
            new PropertyMetadata(null, static (d, e) => ((MplStreamingChartControl)d).OnStreamingFigureChanged(
                e.NewValue as StreamingFigure)));

    private StreamingFigure? _subscribedFigure;

    /// <summary>Gets or sets the <see cref="Models.Streaming.StreamingFigure"/> to render.</summary>
    public StreamingFigure? StreamingFigure
    {
        get => (StreamingFigure?)GetValue(StreamingFigureProperty);
        set => SetValue(StreamingFigureProperty, value);
    }

    /// <summary>Initializes a new streaming chart control and wires the <c>Unloaded</c> detach hook, so the
    /// subscription is released even when the control is torn down with its figure still attached — the
    /// figure outlives the control, and would otherwise keep invalidating a dead one.</summary>
    public MplStreamingChartControl()
    {
        Unloaded += OnUnloaded;
    }

    /// <inheritdoc />
    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        var sf = StreamingFigure;
        if (sf is null)
        {
            return;
        }

        // Before the draw, not after: the axis limits are computed from the data that is there now, and a
        // chart drawn against the previous limits is a chart whose data has left the plot area.
        sf.ApplyAxisScaling();

        var ctx = new SkiaRenderContext(e.Surface.Canvas);
        ChartServices.Renderer.Render(sf.Figure, ctx);
    }

    private void OnStreamingFigureChanged(StreamingFigure? newValue)
    {
        Unsubscribe();
        if (newValue is not null)
        {
            Subscribe(newValue);
        }
    }

    private void Subscribe(StreamingFigure sf)
    {
        _subscribedFigure = sf;
        sf.RenderRequested += OnRenderRequested;
    }

    private void Unsubscribe()
    {
        if (_subscribedFigure is not null)
        {
            _subscribedFigure.RenderRequested -= OnRenderRequested;
            _subscribedFigure = null;
        }
    }

    private void OnRenderRequested() =>
        Dispatcher.BeginInvoke(InvalidateVisual);

    private void OnUnloaded(object sender, RoutedEventArgs e) => Unsubscribe();
}
