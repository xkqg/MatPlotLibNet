// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Avalonia;

/// <summary>
/// An Avalonia <see cref="Control"/> that renders a MatPlotLibNet <see cref="Figure"/> natively
/// via SkiaSharp. Set <see cref="IsInteractive"/> to <c>true</c> to enable pan / zoom / reset /
/// brush-select interaction without any JavaScript or SignalR dependency.
/// </summary>
public sealed class MplChartControl : Control
{
    /// <summary>Identifies the <see cref="Figure"/> styled property.</summary>
    public static readonly StyledProperty<Figure?> FigureProperty =
        AvaloniaProperty.Register<MplChartControl, Figure?>(nameof(Figure));

    /// <summary>Identifies the <see cref="IsInteractive"/> styled property.</summary>
    public static readonly StyledProperty<bool> IsInteractiveProperty =
        AvaloniaProperty.Register<MplChartControl, bool>(nameof(IsInteractive), defaultValue: false);

    static MplChartControl()
    {
        AffectsRender<MplChartControl>(FigureProperty);
        FocusableProperty.OverrideDefaultValue<MplChartControl>(true);
    }

    /// <summary>Gets or sets the <see cref="Models.Figure"/> rendered in this control.</summary>
    public Figure? Figure
    {
        get => GetValue(FigureProperty);
        set => SetValue(FigureProperty, value);
    }

    /// <summary>
    /// Gets or sets whether local pan / zoom / reset interaction is enabled.
    /// When <c>true</c> the control captures pointer and keyboard events and mutates
    /// the figure's axis limits directly, then calls <see cref="Control.InvalidateVisual"/>.
    /// </summary>
    public bool IsInteractive
    {
        get => GetValue(IsInteractiveProperty);
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

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        var figure = Figure;
        if (figure is null) return;
        context.Custom(new MplChartDrawOperation(new Rect(Bounds.Size), figure, this));
    }

    /// <summary>
    /// Called by <see cref="MplChartDrawOperation"/> (render thread) after each render pass.
    /// Marshals the layout update back to the UI thread so all interaction state is
    /// read and written from the same thread as pointer/keyboard events.
    /// </summary>
    internal void OnRenderCompleted(Figure figure, MatPlotLibNet.Rendering.LayoutResult layoutResult)
    {
        if (!IsInteractive) return;
        Dispatcher.UIThread.Post(() =>
        {
            var layout = ChartLayout.Create(figure, layoutResult);
            if (_controller is null)
            {
                _controller = _serverEventSink is not null
                    ? InteractionController.Create(figure, layout, _serverEventSink)
                    : InteractionController.CreateLocal(figure, layout);
                _controller.InvalidateRequested += InvalidateVisual;
            }
            else
            {
                _controller.UpdateLayout(layout);
            }
        });
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (_controller is null || !IsInteractive) return;
        _controller.HandlePointerPressed(AvaloniaInputAdapter.ToPointerArgs(e, this));
        e.Handled = true;
        Focus();
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_controller is null || !IsInteractive) return;
        _controller.HandlePointerMoved(AvaloniaInputAdapter.ToPointerArgs(e, this));

        var tooltip = _controller.ActiveTooltip;
        ToolTip.SetTip(this, tooltip?.Text);
        ToolTip.SetIsOpen(this, tooltip is not null);
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_controller is null || !IsInteractive) return;
        _controller.HandlePointerReleased(AvaloniaInputAdapter.ToPointerArgs(e, this));
    }

    /// <inheritdoc />
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (_controller is null || !IsInteractive) return;
        _controller.HandleScroll(AvaloniaInputAdapter.ToScrollArgs(e, this));
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (_controller is null || !IsInteractive) return;
        _controller.HandleKeyDown(AvaloniaInputAdapter.ToKeyArgs(e));
    }
}
