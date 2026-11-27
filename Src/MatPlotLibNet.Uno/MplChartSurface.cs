// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SkiaSharp;
using Uno.WinUI.Graphics2DSK;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Skia;

namespace MatPlotLibNet.Uno;

/// <summary>What an Uno Platform / WinUI 3 chart element is, apart from where its figure comes from: an
/// <see cref="SKCanvasElement"/> that paints the figure natively through SkiaSharp and, with <see cref="IsInteractive"/> set, lets
/// the reader pan, zoom, reset and brush-select through its <see cref="InteractionHost"/>. <see cref="MplChartElement"/> shows a
/// <see cref="Figure"/> and <see cref="MplStreamingChartElement"/> a streaming figure; only the library derives from it.</summary>
public abstract class MplChartSurface : SKCanvasElement, IServerInteractive
{
    /// <summary>Identifies the <see cref="IsInteractive"/> dependency property.</summary>
    public static readonly DependencyProperty IsInteractiveProperty =
        DependencyProperty.Register(nameof(IsInteractive), typeof(bool), typeof(MplChartSurface), new PropertyMetadata(false));

    private readonly InteractionHost _host;
    private readonly SkiaChartPainter _painter = new();
    private readonly ClickCounter _clicks = new();

    /// <summary>Only the library makes a surface. <see cref="SKCanvasElement"/> extends <c>FrameworkElement</c>, not
    /// <c>Control</c>, so its input events are wired as handlers rather than overrides; it lets go of what it holds when it is
    /// unloaded.</summary>
    private protected MplChartSurface()
    {
        // A paint runs on the render pass: the host reads the layout on the dispatcher's thread, where the pointer and key events arrive.
        _host = new InteractionHost(() => IsInteractive, Repaint, work => DispatcherQueue.Post(work));
        _host.HandlerFaulted += failure => ChartEventHandlerFaulted?.Invoke(failure);
        _host.DataPointClicked += point => DataPointClicked?.Invoke(point);
        PointerPressed += OnElementPointerPressed;
        PointerMoved += OnElementPointerMoved;
        PointerReleased += OnElementPointerReleased;
        PointerWheelChanged += OnElementPointerWheelChanged;
        KeyDown += OnElementKeyDown;
        Unloaded += (_, _) => Release();
    }

    /// <summary>Gets or sets whether the reader can work the chart: pan, zoom, reset and brush-select. Off by default.</summary>
    public bool IsInteractive
    {
        get => (bool)GetValue(IsInteractiveProperty);
        set => SetValue(IsInteractiveProperty, value);
    }

    /// <inheritdoc cref="InteractionHost.Handler"/>
    public IChartEventHandler? ChartEventHandler
    {
        get => _host.Handler;
        set => _host.Handler = value;
    }

    /// <inheritdoc cref="InteractionHost.ControllerFactory"/>
    public InteractionControllerFactory ControllerFactory
    {
        get => _host.ControllerFactory;
        set => _host.ControllerFactory = value;
    }

    /// <inheritdoc cref="InteractionHost.HandlerFaulted"/>
    public event Action<Exception>? ChartEventHandlerFaulted;

    /// <inheritdoc cref="InteractionHost.DataPointClicked"/>
    public event Action<PinnedAnnotation>? DataPointClicked;

    /// <inheritdoc />
    InteractionHost IServerInteractive.Host => _host;

    /// <summary>The figure the surface shows now, or <see langword="null"/> for none: the one thing each leaf says of itself.</summary>
    internal abstract Figure? Shown { get; }

    /// <summary>How the surface is painted again after a gesture changed the figure: its invalidate. A streaming element sets the
    /// limits from the data first.</summary>
    private protected virtual void Repaint() => Invalidate();

    /// <summary>What the surface lets go of when it is unloaded: what its painter recorded of the figure, so shown again it paints
    /// from scratch. A streaming element lets go of its figure too, which outlives it.</summary>
    internal virtual void Release() => _painter.Dispose();

    /// <inheritdoc />
    /// <remarks>The paint only draws: it writes nothing into the figure (a streaming element set the limits from the data before
    /// it asked to be drawn). It draws only the subplots that changed since the last paint (the others are replayed as they were
    /// recorded), with the rubber band of a brush-select drag in progress over them.</remarks>
    protected override void RenderOverride(SKCanvas canvas, Windows.Foundation.Size area)
    {
        if (Shown is not { } figure) return;

        _host.Painted(figure, _painter.Paint(canvas, figure, _host.ActiveBrushSelect));
    }

    private void OnElementPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (!_host.Listening) return;
        var point = e.GetCurrentPoint(this);
        _host.HandlePointerPressed(UnoInputAdapter.ToPointerArgs(e, this, _clicks.Press(point.Timestamp, point.Position.X, point.Position.Y)));
        e.Handled = true;
        Focus(FocusState.Pointer);
    }

    private void OnElementPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_host.Listening) return;
        _host.HandlePointerMoved(UnoInputAdapter.ToPointerArgs(e, this));
        ToolTipService.SetToolTip(this, _host.ActiveTooltip?.Text);
    }

    private void OnElementPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_host.Listening) return;
        _host.HandlePointerReleased(UnoInputAdapter.ToPointerArgs(e, this));
    }

    private void OnElementPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        if (!_host.Listening) return;
        _host.HandleScroll(UnoInputAdapter.ToScrollArgs(e, this));
        e.Handled = true;
    }

    private void OnElementKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (!_host.Listening) return;
        _host.HandleKeyDown(UnoInputAdapter.ToKeyArgs(e));
    }
}
