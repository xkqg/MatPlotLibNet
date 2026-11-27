// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Interaction;

/// <summary>Default implementation of <see cref="IInteractionController"/>.
/// Gives each gesture to the first of its modifiers that claims it, in the order they were given, tells every other one of each
/// move of the pointer (<see cref="InteractionModifier.OnPointerTracked"/>), and shows what its modifiers show: the rubber band,
/// the crosshair and the tooltip. It is made from an <see cref="InteractionContext"/> and a list of modifiers; the list a native
/// control uses is <see cref="DefaultModifiers"/>, the fifteen built-in gestures, which a caller can edit before making the
/// controller from it (see <see cref="InteractionHost.ControllerFactory"/>). Supports two event sinks:
/// <list type="bullet">
///   <item><b>Local mode</b> (default): <see cref="CreateLocal"/> — applies events directly to
///   the figure in-process and fires <see cref="InvalidateRequested"/>.</item>
///   <item><b>Custom sink</b>: <see cref="Create(Figure, IChartLayout, Action{FigureInteractionEvent})"/> — caller provides any
///   <c>Action&lt;FigureInteractionEvent&gt;</c> (e.g. publish to SignalR).</item>
/// </list>
/// </summary>
public sealed class InteractionController : IInteractionController
{
    private readonly InteractionContext _context;
    private readonly InteractionModifier[] _modifiers;
    private InteractionModifier? _activeModifier;

    private InteractionController(InteractionContext context, IEnumerable<InteractionModifier> modifiers)
    {
        _context = context;
        // A copy: the list the caller edited is theirs again once the controller is made.
        _modifiers = [.. modifiers];
    }

    /// <summary>The toolbar managed by this controller (its context's). Activate drawing tools via
    /// <see cref="InteractionToolbar.Activate"/>.</summary>
    public InteractionToolbar Toolbar => _context.Toolbar;

    /// <inheritdoc />
    public event Action? InvalidateRequested;

    /// <inheritdoc cref="InteractionHost.DataPointClicked"/>
    /// <remarks>It is raised in both hosting modes because a click is a question about the data, not a change to it: the context's
    /// sink raises it after it has handed the click on, so a handler that throws cannot stop the event reaching the sink.</remarks>
    public event Action<PinnedAnnotation>? DataPointClicked
    {
        add => _context.DataPointClicked += value;
        remove => _context.DataPointClicked -= value;
    }

    /// <summary>Raised for every event this controller produced, after the sink has applied it or sent it and after
    /// <see cref="DataPointClicked"/> (for a click).</summary>
    public event Action<FigureInteractionEvent>? EventHandled
    {
        add => _context.EventHandled += value;
        remove => _context.EventHandled -= value;
    }

    /// <inheritdoc />
    public BrushSelectState? ActiveBrushSelect => _modifiers.Select(m => m.ActiveBrushSelect).FirstOrDefault(s => s is not null);

    /// <inheritdoc />
    public HoverTooltipContent? ActiveTooltip => _modifiers.Select(m => m.ActiveTooltip).FirstOrDefault(s => s is not null);

    /// <inheritdoc />
    public CrosshairState? ActiveCrosshair => _modifiers.Select(m => m.ActiveCrosshair).FirstOrDefault(s => s is not null);

    /// <summary>The fifteen built-in gestures over <paramref name="context"/>, in the order they claim a gesture: the first whose
    /// <c>Handles*</c> says yes takes it. A new list each time, the caller's to edit (take a gesture out, put one of your own in
    /// its place, add one) before making the controller from it with <see cref="Create(InteractionContext, IEnumerable{InteractionModifier})"/>.</summary>
    /// <param name="context">The context the gestures are made over.</param>
    /// <returns>The legend toggle, home, 3-D rotation, rectangle zoom, brush, span, the three drawing tools, the drawing
    /// selection, the data cursor, pan, wheel zoom, crosshair and hover.</returns>
    public static List<InteractionModifier> DefaultModifiers(InteractionContext context)
    {
        var (id, layout, sink, figure, toolbar) = (context.ChartId, context.Layout, context.Sink, context.Figure, context.Toolbar);
        // Order matters — first modifier whose Handles* returns true claims the event:
        //   1. LegendToggle: click on legend item (specific hit-test, highest priority for clicks)
        //   2. Home: double-click wins before Pan (both claim left-button; ClickCount distinguishes)
        //   3. Rotate3D: right-drag on 3D axes, arrow keys/Home for 3D
        //   4. RectangleZoom: Ctrl+drag draws zoom box
        //   5. BrushSelect: Shift+drag before Pan (Shift distinguishes)
        //   6. SpanSelect: Alt+drag selects X-range
        //   7. Drawing tools: toolbar-mode-gated, claim left-click only when active
        //   8. DrawingToolSelection: Delete key for selected drawing tool
        //   9. DataCursor: proximity click, before Pan so the hit test wins; a miss falls through to Pan
        //  10. Pan: plain left-drag
        //  11. Zoom: scroll wheel
        //  12. Crosshair and Hover: claim nothing, follow every move
        var pan = new PanModifier(id, layout, sink);
        return
        [
            new LegendToggleModifier(id, layout, sink),
            new HomeModifier(id, layout, sink),
            new Rotate3DModifier(id, layout, sink, figure),
            new RectangleZoomModifier(id, layout, sink),
            new BrushSelectModifier(id, layout, sink),
            new SpanSelectModifier(id, layout, sink),
            new TrendlineDrawingModifier(id, layout, sink, toolbar),
            new LevelDrawingModifier(id, layout, sink, toolbar),
            new FibonacciDrawingModifier(id, layout, sink, toolbar),
            new DrawingToolSelectionModifier(id, layout, sink),
            new DataCursorModifier(id, layout, figure, sink, pan),
            pan,
            new ZoomModifier(id, layout, sink),
            new CrosshairModifier(layout),
            new HoverModifier(id, layout, figure, sink),
        ];
    }

    /// <summary>Creates the controller of <paramref name="context"/> with the fifteen built-in gestures
    /// (<see cref="DefaultModifiers"/>): the one a native control makes unless it is given another.</summary>
    /// <param name="context">The context the controller acts in.</param>
    public static InteractionController Create(InteractionContext context) => Create(context, DefaultModifiers(context));

    /// <summary>Creates the controller of <paramref name="context"/> with the gestures of <paramref name="modifiers"/>, in their
    /// order: typically <see cref="DefaultModifiers"/> edited.</summary>
    /// <param name="context">The context the controller acts in.</param>
    /// <param name="modifiers">The gestures, in the order they claim one. Made over the same context.</param>
    public static InteractionController Create(InteractionContext context, IEnumerable<InteractionModifier> modifiers) =>
        new(context, modifiers);

    /// <summary>Creates a controller in local mode: events are applied directly to the figure
    /// and <see cref="InvalidateRequested"/> is raised after each mutation.</summary>
    public static InteractionController CreateLocal(Figure figure, IChartLayout layout)
    {
        InteractionController? ctrl = null;
        ctrl = Create(InteractionContext.For(figure, layout, null, () => ctrl!.InvalidateRequested?.Invoke()));
        return ctrl;
    }

    /// <summary>Creates a controller with a custom event sink.
    /// The sink is responsible for applying or publishing the event.</summary>
    public static InteractionController Create(
        Figure figure,
        IChartLayout layout,
        Action<FigureInteractionEvent> sink)
        => Create(new InteractionContext(figure, layout, sink));

    /// <inheritdoc />
    /// <remarks>Only the layout the modifiers read is replaced (<see cref="InteractionContext.UpdateLayout"/>). They are not built
    /// again: a rubber band or a drag that is going on keeps going across the repaint, and "home" is the view the figure remembers
    /// (see <see cref="ResetEvent"/>), not whatever range the latest repaint drew.</remarks>
    public void UpdateLayout(IChartLayout layout) => _context.UpdateLayout(layout);

    /// <inheritdoc />
    public void HandlePointerPressed(PointerInputArgs args)
    {
        _activeModifier = _modifiers.FirstOrDefault(m => m.HandlesPointerPressed(args));
        _activeModifier?.OnPointerPressed(args);
    }

    /// <inheritdoc />
    /// <remarks>The modifier dragging is told through <see cref="InteractionModifier.OnPointerMoved"/>, and while it shows a
    /// rubber band the control is asked to repaint so it draws it; every other modifier is told through
    /// <see cref="InteractionModifier.OnPointerTracked"/>.</remarks>
    public void HandlePointerMoved(PointerInputArgs args)
    {
        if (_activeModifier is { } active)
        {
            active.OnPointerMoved(args);
            if (active.ActiveBrushSelect is not null)
                InvalidateRequested?.Invoke();
        }

        foreach (var m in _modifiers)
        {
            if (m != _activeModifier)
                m.OnPointerTracked(args);
        }
    }

    /// <inheritdoc />
    public void HandlePointerReleased(PointerInputArgs args)
    {
        _activeModifier?.OnPointerReleased(args);
        _activeModifier = null;
    }

    /// <inheritdoc />
    public void HandleScroll(ScrollInputArgs args) => _modifiers.FirstOrDefault(m => m.HandlesScroll(args))?.OnScroll(args);

    /// <inheritdoc />
    public void HandleKeyDown(KeyInputArgs args) => _modifiers.FirstOrDefault(m => m.HandlesKeyDown(args))?.OnKeyDown(args);
}
