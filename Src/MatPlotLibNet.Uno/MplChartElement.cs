// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SkiaSharp;
using SkiaSharp.Views.Windows;
using Uno.WinUI.Graphics2DSK;
using Windows.Foundation;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Skia;

namespace MatPlotLibNet.Uno;

/// <summary>
/// A Uno Platform / WinUI 3 <see cref="SKCanvasElement"/> that renders a MatPlotLibNet
/// <see cref="Figure"/> natively via SkiaSharp. Set <see cref="IsInteractive"/> to
/// <c>true</c> to enable pan / zoom / reset / brush-select interaction.
/// </summary>
public sealed class MplChartElement : SKCanvasElement
{
    /// <summary>Identifies the <see cref="Figure"/> dependency property.</summary>
    public static readonly DependencyProperty FigureProperty =
        DependencyProperty.Register(nameof(Figure), typeof(Figure), typeof(MplChartElement),
            new PropertyMetadata(null, static (d, _) => ((MplChartElement)d).Invalidate()));

    /// <summary>Identifies the <see cref="IsInteractive"/> dependency property.</summary>
    public static readonly DependencyProperty IsInteractiveProperty =
        DependencyProperty.Register(nameof(IsInteractive), typeof(bool), typeof(MplChartElement),
            new PropertyMetadata(false));

    /// <summary>Gets or sets the <see cref="Models.Figure"/> rendered in this element.</summary>
    public Figure? Figure
    {
        get => (Figure?)GetValue(FigureProperty);
        set => SetValue(FigureProperty, value);
    }

    /// <summary>
    /// Gets or sets whether local pan / zoom / reset interaction is enabled.
    /// When <c>true</c> the element captures pointer and keyboard events and mutates
    /// the figure's axis limits directly, then calls <see cref="SKCanvasElement.Invalidate"/>.
    /// </summary>
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

    private readonly InteractionHost _host = new();

    /// <summary>When set, the interaction controller routes events through this sink (typically
    /// created via <see cref="SignalREventSink.Create"/>) instead of applying them locally.
    /// Set via <see cref="ServerInteractionExtensions.WithServerInteraction"/>.</summary>
    internal Action<FigureInteractionEvent>? ServerEventSink
    {
        get => _host.ServerEventSink;
        set => _host.ServerEventSink = value;
    }

    /// <summary>Returns the active brush select state for overlay rendering, or <c>null</c>.</summary>
    internal BrushSelectState? ActiveBrushSelect => _host.ActiveBrushSelect;

    /// <summary>Raised when the reader clicks a data point, with the point they clicked: the series it belongs to,
    /// its value, where it is on screen and which axes it is in. Subscribe to open a detail panel, select a row in a
    /// grid beside the chart, or navigate. It needs <see cref="IsInteractive"/>, and it fires whether the chart runs
    /// locally or through a server sink.</summary>
    public event Action<PinnedAnnotation>? DataPointClicked;

    /// <summary>Initializes a new chart element.</summary>
    public MplChartElement()
    {
        // SKCanvasElement extends FrameworkElement, not Control, so input events
        // are wired as handlers rather than overrides.
        _host.InvalidateRequested += Invalidate;
        _host.HandlerFaulted += failure => ChartEventHandlerFaulted?.Invoke(failure);
        _host.DataPointClicked += point => DataPointClicked?.Invoke(point);
        PointerPressed += OnElementPointerPressed;
        PointerMoved += OnElementPointerMoved;
        PointerReleased += OnElementPointerReleased;
        PointerWheelChanged += OnElementPointerWheelChanged;
        KeyDown += OnElementKeyDown;
    }

    /// <inheritdoc />
    protected override void RenderOverride(SKCanvas canvas, Windows.Foundation.Size area)
    {
        var figure = Figure;
        if (figure is null) return;

        var ctx = new SkiaRenderContext(canvas);
        ChartRendererDefault.Current.Render(figure, ctx);

        if (IsInteractive)
        {
            var layoutResult = ChartRendererDefault.Current.ComputeLayout(figure, ctx);
            PostLayoutUpdate(figure, layoutResult);
        }

        // Draw rubber-band selection rectangle if a brush select drag is in progress.
        var brushState = ActiveBrushSelect;
        if (brushState is { } bs)
            canvas.DrawBrushSelect(bs);
    }

    private void PostLayoutUpdate(Figure figure, MatPlotLibNet.Rendering.LayoutResult layoutResult)
    {
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            _host.Update(figure, ChartLayout.Create(figure, layoutResult)));
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

        var tooltip = _host.ActiveTooltip;
        ToolTipService.SetToolTip(this, tooltip?.Text);
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
}
