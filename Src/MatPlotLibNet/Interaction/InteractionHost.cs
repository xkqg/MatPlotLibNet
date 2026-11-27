// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Interaction;

/// <summary>The interaction of one native chart control: it owns the <see cref="InteractionController"/> and keeps it
/// aimed at the figure the control is showing, so a control only forwards its input and hands over the layout of each
/// paint. The WPF, Avalonia, Uno and MAUI controls derive from four different base classes, so what they share is this
/// object, composed, and not a class they could inherit; inside one package the chart control and the streaming control derive
/// from the one surface that composes it (<c>MplChartSurface</c>; on MAUI the streaming view derives from the chart view).
/// <para>The host talks to its controller through <see cref="IInteractionController"/> only (and hears the clicked point
/// and every handled event through an internal interface beside it).</para>
/// <para>A control gives the host the facts that are its own when it makes it: whether it is interactive, how it is painted
/// again after a gesture changed the figure, and how it reaches its UI thread. The host then answers whether a gesture may act (<c>Listening</c>), takes the layout of each
/// paint (<c>Painted</c>) and routes the gestures to a server (<c>RouteToServer</c>), so no control writes any of them.</para>
/// <para>Nothing here is thread-safe: a control calls it on its UI thread, the thread its pointer and key events
/// arrive on.</para></summary>
public sealed class InteractionHost
{
    /// <summary>The gate of a host made with no control: it has no interactive switch, so the figure it is given is the only
    /// thing it waits for.</summary>
    private static readonly Func<bool> AlwaysInteractive = static () => true;

    /// <summary>The repaint of a host made with no control: nothing is drawn again.</summary>
    private static readonly Action NoRepaint = static () => { };

    private readonly InteractionControllerFactory _createController;
    private readonly Func<bool> _isInteractive;
    private readonly Action _repaint;
    private readonly UiThreadDispatch? _post;
    private IHostedInteractionController? _controller;
    private Figure? _figure;
    private Action<FigureInteractionEvent>? _serverEventSink;

    /// <summary>Creates a host that draws nothing again after a gesture, which builds an <see cref="InteractionController"/> for each
    /// figure it is shown: the figure changes in place, for whoever draws it next.</summary>
    public InteractionHost() : this(InteractionController.For, AlwaysInteractive, NoRepaint, null)
    {
    }

    /// <summary>Creates the host of one control, which builds an <see cref="InteractionController"/> for each figure it is
    /// shown.</summary>
    /// <param name="repaint">How the control is painted again (its invalidate), called when a gesture changed the figure.</param>
    public InteractionHost(Action repaint) : this(InteractionController.For, AlwaysInteractive, repaint, null)
    {
    }

    /// <summary>Creates a host that gets the controller for a figure from <paramref name="createController"/>.</summary>
    /// <param name="createController">Makes the controller for a figure, its layout and the server sink set then.</param>
    /// <param name="repaint">How the control is painted again, or <see langword="null"/> for nothing.</param>
    internal InteractionHost(InteractionControllerFactory createController, Action? repaint = null)
        : this(createController, AlwaysInteractive, repaint ?? NoRepaint, null)
    {
    }

    /// <summary>Creates the host of a native control.</summary>
    /// <param name="isInteractive">Whether the control is interactive now: the control's own property, read when a gesture
    /// arrives and when a paint is handed over, so the property stays the one place that says it.</param>
    /// <param name="repaint">How the control is painted again after a gesture changed the figure (its invalidate; a streaming
    /// control's follower sets the limits from the data first).</param>
    /// <param name="post">How the control runs work on its UI thread, for a control that paints on another thread (Avalonia
    /// renders on its own thread, Uno on its render pass); <see langword="null"/> for a control that paints on its UI thread.</param>
    internal InteractionHost(Func<bool> isInteractive, Action repaint, UiThreadDispatch? post = null)
        : this(InteractionController.For, isInteractive, repaint, post)
    {
    }

    private InteractionHost(InteractionControllerFactory createController, Func<bool> isInteractive, Action repaint, UiThreadDispatch? post)
    {
        _createController = createController;
        _isInteractive = isInteractive;
        _repaint = repaint;
        _post = post;
    }

    /// <summary>Raised when the reader clicks a data point (or taps it, on a touch screen), with the point they clicked: the series
    /// it belongs to, its value, where it is on screen and which axes it is in. Subscribe to open a detail panel, follow a link or
    /// select a row beside the chart. It is raised whether the gestures are applied to the figure in place or sent to a server, and
    /// on a native control it needs the control's <c>IsInteractive</c>.</summary>
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
    /// <see cref="IChartEventHandler"/> a server session takes, so one class reacts to a chart wherever it is shown. On a native
    /// control it needs the control's <c>IsInteractive</c>.</summary>
    public IChartEventHandler? Handler { get; set; }

    /// <summary>Raised when <see cref="Handler"/> fails. A control does not stop for it: the gesture has been applied, and
    /// the next one is taken as usual, so the failure is reported here and never thrown into the control.</summary>
    public event Action<Exception>? HandlerFaulted;

    /// <summary>Whether a paint has given the host a figure to act on.</summary>
    public bool IsAttached => _controller is not null;

    /// <summary>Whether a gesture may act now: a paint has given the host a figure and the control is interactive. A native
    /// control asks it before it forwards an input, so the question is written here and nowhere else.</summary>
    internal bool Listening => IsAttached && _isInteractive();

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
            _controller = _createController(figure, layout, _serverEventSink);
            _controller.InvalidateRequested += _repaint;
            _controller.DataPointClicked += point => DataPointClicked?.Invoke(point);
            _controller.EventHandled += evt => Deliver(figure, evt);
            return;
        }

        _controller.UpdateLayout(layout);
    }

    /// <summary>Takes what a paint drew, from a control that is interactive: the layout of the paint becomes the
    /// <see cref="ChartLayout"/> the host aims its controller with (<see cref="Update"/>), on the control's UI thread when the
    /// control paints on another one. A control that is not interactive gives the host nothing, and no layout is built for it.</summary>
    /// <param name="figure">The figure that was painted.</param>
    /// <param name="layout">The layout of that paint.</param>
    internal void Painted(Figure figure, LayoutResult layout)
    {
        if (!_isInteractive()) return;

        // No post, no closure: a control that paints on its UI thread pays nothing for the thread hop another one needs.
        if (_post is null)
            Aim(figure, layout);
        else
            _post(() => Aim(figure, layout));
    }

    private void Aim(Figure figure, LayoutResult layout) => Update(figure, ChartLayout.Create(figure, layout));

    /// <summary>Tells <see cref="Handler"/> of a gesture. A handler that completes at once costs nothing; one that goes on
    /// asynchronously is watched in the background, and a failure of either is reported through
    /// <see cref="HandlerFaulted"/>, never thrown into the control.</summary>
    private void Deliver(Figure figure, FigureInteractionEvent evt)
    {
        if (Handler is not { } handler) return;

        // A brush over rows with keys is also said as the rows it covers, right after the brush itself.
        if (evt.RowsCoveredOn(figure) is { } rows)
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
