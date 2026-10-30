// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SkiaSharp;
using Uno.WinUI.Graphics2DSK;
using Windows.Foundation;
using MatPlotLibNet.Interaction;
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

    /// <summary>Identifies the <see cref="IsInteractive"/> dependency property.</summary>
    public static readonly DependencyProperty IsInteractiveProperty =
        DependencyProperty.Register(nameof(IsInteractive), typeof(bool), typeof(MplStreamingChartElement),
            new PropertyMetadata(false));

    private readonly InteractionHost _host = new();
    private StreamingFigure? _subscribedFigure;

    /// <summary>Gets or sets whether the reader can pan, zoom and rotate the chart. A chart the reader has moved stays
    /// where they put it while data keeps arriving, until Home returns it to the scale mode that follows the data.</summary>
    public bool IsInteractive
    {
        get => (bool)GetValue(IsInteractiveProperty);
        set => SetValue(IsInteractiveProperty, value);
    }

    /// <summary>Told of every gesture of the reader once it has reached the figure (or the server sink), whichever kind it
    /// is: the same <see cref="IChartEventHandler"/> a server session takes, so one class reacts to a chart wherever it is
    /// shown. It needs <see cref="IsInteractive"/>.</summary>
    public IChartEventHandler? ChartEventHandler
    {
        get => _host.Handler;
        set => _host.Handler = value;
    }

    /// <summary>Raised when <see cref="ChartEventHandler"/> fails. The gesture has been applied and the next one is taken as
    /// usual, so the failure is reported here and never thrown into the control.</summary>
    public event Action<Exception>? ChartEventHandlerFaulted;

    /// <summary>Gets or sets the <see cref="Models.Streaming.StreamingFigure"/> to render.</summary>
    public StreamingFigure? StreamingFigure
    {
        get => (StreamingFigure?)GetValue(StreamingFigureProperty);
        set => SetValue(StreamingFigureProperty, value);
    }

    /// <summary>Initializes a new streaming chart element and wires the <c>Unloaded</c>
    /// detach hook, so the subscription to <see cref="StreamingFigure.RenderRequested"/> is
    /// always released even if the element is torn down without its StreamingFigure changing.</summary>
    public MplStreamingChartElement()
    {
        Unloaded += OnUnloaded;
        _host.InvalidateRequested += Invalidate;
        _host.HandlerFaulted += failure => ChartEventHandlerFaulted?.Invoke(failure);
        PointerPressed += OnElementPointerPressed;
        PointerMoved += OnElementPointerMoved;
        PointerReleased += OnElementPointerReleased;
        PointerWheelChanged += OnElementPointerWheelChanged;
        KeyDown += OnElementKeyDown;
    }

    /// <inheritdoc />
    protected override void RenderOverride(SKCanvas canvas, Windows.Foundation.Size area)
    {
        var sf = StreamingFigure;
        if (sf is null) return;
        sf.ApplyAxisScaling();
        var ctx = new SkiaRenderContext(canvas);
        ChartRendererDefault.Current.Render(sf.Figure, ctx);

        if (IsInteractive)
        {
            var figure = sf.Figure;
            var layoutResult = ChartRendererDefault.Current.ComputeLayout(figure, ctx);
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                _host.Update(figure, ChartLayout.Create(figure, layoutResult)));
        }

        if (_host.ActiveBrushSelect is { } band)
            canvas.DrawBrushSelect(band);
    }

    private void OnElementPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (!_host.IsAttached || !IsInteractive) return;
        _host.HandlePointerPressed(UnoInputAdapter.ToPointerArgs(e, this));
        e.Handled = true;
        Focus(FocusState.Pointer);
    }

    private void OnElementPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_host.IsAttached || !IsInteractive) return;
        _host.HandlePointerMoved(UnoInputAdapter.ToPointerArgs(e, this));
        ToolTipService.SetToolTip(this, _host.ActiveTooltip?.Text);
    }

    private void OnElementPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_host.IsAttached || !IsInteractive) return;
        _host.HandlePointerReleased(UnoInputAdapter.ToPointerArgs(e, this));
    }

    private void OnElementPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        if (!_host.IsAttached || !IsInteractive) return;
        _host.HandleScroll(UnoInputAdapter.ToScrollArgs(e, this));
        e.Handled = true;
    }

    private void OnElementKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (!_host.IsAttached || !IsInteractive) return;
        _host.HandleKeyDown(UnoInputAdapter.ToKeyArgs(e));
    }

    private void OnStreamingFigureChanged(StreamingFigure? oldValue, StreamingFigure? newValue)
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
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, Invalidate);

    private void OnUnloaded(object sender, RoutedEventArgs e) => Unsubscribe();
}
