// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Interaction;

/// <summary>The interaction of one native chart control: it owns the <see cref="InteractionController"/> and keeps it
/// aimed at the figure the control is showing, so a control only forwards its input and hands over the layout of each
/// paint. The WPF, Avalonia, Uno and MAUI controls derive from four different base classes, so what they share is this
/// object, composed, and not a class they could inherit.
/// <para>Nothing here is thread-safe: a control calls it on its UI thread, the thread its pointer and key events
/// arrive on.</para></summary>
public sealed class InteractionHost
{
    private InteractionController? _controller;
    private Figure? _figure;
    private Action<FigureInteractionEvent>? _serverEventSink;

    /// <summary>Raised when a gesture changed the figure and the control has to paint again.</summary>
    public event Action? InvalidateRequested;

    /// <summary>Raised when the reader clicks a data point, with the point they clicked.</summary>
    public event Action<PinnedAnnotation>? DataPointClicked;

    /// <summary>When set, the gestures are sent through this sink (typically
    /// <see cref="SignalREventSink.Create"/>) and applied by whoever receives them; when <see langword="null"/> they are
    /// applied to the figure in place. Setting it drops the controller, which is built again, for the sink now set, by
    /// the next <see cref="Update"/>.</summary>
    public Action<FigureInteractionEvent>? ServerEventSink
    {
        get => _serverEventSink;
        set
        {
            _serverEventSink = value;
            _controller = null;
        }
    }

    /// <summary>Told of every gesture once it has reached the figure (or the server sink), whichever kind it is: the same
    /// <see cref="IChartEventHandler"/> a server session takes, so one class reacts to a chart wherever it is shown.</summary>
    public IChartEventHandler? Handler { get; set; }

    /// <summary>Raised when <see cref="Handler"/> fails. A control does not stop for it: the gesture has been applied, and
    /// the next one is taken as usual.</summary>
    public event Action<Exception>? HandlerFaulted;

    /// <summary>Whether a paint has given the host a figure to act on.</summary>
    public bool IsAttached => _controller is not null;

    /// <summary>The rubber band of the brush-select drag in progress, or <see langword="null"/>.</summary>
    public BrushSelectState? ActiveBrushSelect => _controller?.ActiveBrushSelect;

    /// <summary>The tooltip for the point under the pointer, or <see langword="null"/>.</summary>
    public HoverTooltipContent? ActiveTooltip => _controller?.ActiveTooltip;

    /// <summary>The crosshair under the pointer, or <see langword="null"/>.</summary>
    public CrosshairState? ActiveCrosshair => _controller?.ActiveCrosshair;

    /// <summary>Gives the host what the latest paint drew. The first paint of a figure builds the controller for it; a
    /// paint of a different figure builds a new one, so the next gesture acts on the figure now on screen; a repaint of
    /// the same figure only replaces the layout, and a gesture that is going on keeps going.</summary>
    /// <param name="figure">The figure that was painted.</param>
    /// <param name="layout">The layout of that paint.</param>
    public void Update(Figure figure, IChartLayout layout)
    {
        if (_controller is null || !ReferenceEquals(_figure, figure))
        {
            _figure = figure;
            _controller = _serverEventSink is not null
                ? InteractionController.Create(figure, layout, _serverEventSink)
                : InteractionController.CreateLocal(figure, layout);
            _controller.InvalidateRequested += () => InvalidateRequested?.Invoke();
            _controller.DataPointClicked += point => DataPointClicked?.Invoke(point);
            _controller.EventHandled += evt => Deliver(figure, evt);
            return;
        }

        _controller.UpdateLayout(layout);
    }

    /// <summary>Tells <see cref="Handler"/> of a gesture. A handler that completes at once costs nothing; one that goes on
    /// asynchronously is watched in the background, and a failure of either is reported through
    /// <see cref="HandlerFaulted"/>, never thrown into the control.</summary>
    private void Deliver(Figure figure, FigureInteractionEvent evt)
    {
        if (Handler is not { } handler) return;

        // A brush over rows with keys is also said as the rows it covers, right after the brush itself.
        if (evt is BrushSelectEvent brush && figure.RowSelectionOf(brush) is { } rows)
        {
            DeliverOne(handler, evt);
            DeliverOne(handler, rows);
            return;
        }

        DeliverOne(handler, evt);
    }

    private void DeliverOne(IChartEventHandler handler, FigureInteractionEvent evt)
    {
        ValueTask pending;
        try
        {
            pending = handler.OnEventAsync(evt, default);
        }
        catch (Exception ex)
        {
            HandlerFaulted?.Invoke(ex);
            return;
        }

        if (pending.IsCompletedSuccessfully) return;
        _ = WatchAsync(pending);
    }

    private async Task WatchAsync(ValueTask pending)
    {
        try
        {
            await pending.ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            HandlerFaulted?.Invoke(ex);
        }
    }

    /// <summary>A pointer button went down.</summary>
    /// <param name="args">The pointer position, button and held keys.</param>
    public void HandlePointerPressed(PointerInputArgs args) => _controller?.HandlePointerPressed(args);

    /// <summary>The pointer moved.</summary>
    /// <param name="args">The pointer position, button and held keys.</param>
    public void HandlePointerMoved(PointerInputArgs args) => _controller?.HandlePointerMoved(args);

    /// <summary>A pointer button went up.</summary>
    /// <param name="args">The pointer position, button and held keys.</param>
    public void HandlePointerReleased(PointerInputArgs args) => _controller?.HandlePointerReleased(args);

    /// <summary>The wheel turned.</summary>
    /// <param name="args">The pointer position, the delta and the held keys.</param>
    public void HandleScroll(ScrollInputArgs args) => _controller?.HandleScroll(args);

    /// <summary>A key went down.</summary>
    /// <param name="args">The key and the held keys.</param>
    public void HandleKeyDown(KeyInputArgs args) => _controller?.HandleKeyDown(args);
}
