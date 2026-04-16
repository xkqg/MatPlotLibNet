// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SkiaSharp;
using SkiaSharp.Views.Windows;
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

    private InteractionController? _controller;
    private Action<FigureInteractionEvent>? _serverEventSink;

    /// <summary>When set, the interaction controller routes events through this sink (typically
    /// created via <see cref="SignalREventSink.Create"/>) instead of applying them locally.
    /// Set via <see cref="ServerInteractionExtensions.WithServerInteraction"/>.</summary>
    internal Action<FigureInteractionEvent>? ServerEventSink
    {
        get => _serverEventSink;
        set { _serverEventSink = value; _controller = null; }
    }

    /// <summary>Returns the active brush select state for overlay rendering, or <c>null</c>.</summary>
    internal BrushSelectState? ActiveBrushSelect => _controller?.ActiveBrushSelect;

    /// <summary>Initializes a new chart element.</summary>
    public MplChartElement()
    {
        IsTabStop = true;
    }

    /// <inheritdoc />
    protected override void RenderOverride(SKCanvas canvas, Windows.Foundation.Size area)
    {
        var figure = Figure;
        if (figure is null) return;

        var ctx = new SkiaRenderContext(canvas);
        ChartServices.Renderer.Render(figure, ctx);

        if (IsInteractive)
        {
            var layoutResult = ChartServices.Renderer.ComputeLayout(figure, ctx);
            PostLayoutUpdate(figure, layoutResult);
        }

        // Draw rubber-band selection rectangle if a brush select drag is in progress.
        var brushState = ActiveBrushSelect;
        if (brushState is { } bs)
        {
            var rect = new SKRect(
                (float)Math.Min(bs.StartPixelX, bs.CurrentPixelX),
                (float)Math.Min(bs.StartPixelY, bs.CurrentPixelY),
                (float)Math.Max(bs.StartPixelX, bs.CurrentPixelX),
                (float)Math.Max(bs.StartPixelY, bs.CurrentPixelY));

            using var paint = new SKPaint { Color = new SKColor(30, 144, 255, 50), Style = SKPaintStyle.Fill };
            canvas.DrawRect(rect, paint);

            paint.Color = new SKColor(30, 144, 255, 180);
            paint.Style = SKPaintStyle.Stroke;
            paint.StrokeWidth = 1.5f;
            canvas.DrawRect(rect, paint);
        }
    }

    private void PostLayoutUpdate(Figure figure, MatPlotLibNet.Rendering.LayoutResult layoutResult)
    {
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
        {
            var layout = ChartLayout.Create(figure, layoutResult);
            if (_controller is null)
            {
                _controller = _serverEventSink is not null
                    ? InteractionController.Create(figure, layout, _serverEventSink)
                    : InteractionController.CreateLocal(figure, layout);
                _controller.InvalidateRequested += Invalidate;
            }
            else
            {
                _controller.UpdateLayout(layout);
            }
        });
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (_controller is null || !IsInteractive) return;
        _controller.HandlePointerPressed(UnoInputAdapter.ToPointerArgs(e, this));
        e.Handled = true;
        Focus(FocusState.Pointer);
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerRoutedEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_controller is null || !IsInteractive) return;
        _controller.HandlePointerMoved(UnoInputAdapter.ToPointerArgs(e, this));

        var tooltip = _controller.ActiveTooltip;
        ToolTipService.SetToolTip(this, tooltip?.Text);
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_controller is null || !IsInteractive) return;
        _controller.HandlePointerReleased(UnoInputAdapter.ToPointerArgs(e, this));
    }

    /// <inheritdoc />
    protected override void OnPointerWheelChanged(PointerRoutedEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (_controller is null || !IsInteractive) return;
        _controller.HandleScroll(UnoInputAdapter.ToScrollArgs(e, this));
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        base.OnKeyDown(e);
        if (_controller is null || !IsInteractive) return;
        _controller.HandleKeyDown(UnoInputAdapter.ToKeyArgs(e));
    }
}
