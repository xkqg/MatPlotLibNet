// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Interaction;

/// <summary>Default implementation of <see cref="IInteractionController"/>.
/// Composes the six standard <see cref="IInteractionModifier"/> implementations and dispatches
/// input events to the first matching one. Supports two event sinks:
/// <list type="bullet">
///   <item><b>Local mode</b> (default): <see cref="CreateLocal"/> — applies events directly to
///   the figure in-process and fires <see cref="InvalidateRequested"/>.</item>
///   <item><b>Custom sink</b>: <see cref="Create"/> — caller provides any
///   <c>Action&lt;FigureInteractionEvent&gt;</c> (e.g. publish to SignalR).</item>
/// </list>
/// </summary>
public sealed class InteractionController : IInteractionController
{
    private IReadOnlyList<IInteractionModifier> _modifiers;
    private IInteractionModifier? _activeModifier;
    private readonly Figure _figure;
    private readonly Action<FigureInteractionEvent> _sink;
    private IChartLayout _layout;
    private HoverTooltipContent? _activeTooltip;

    /// <inheritdoc />
    public event Action? InvalidateRequested;

    /// <inheritdoc />
    public BrushSelectState? ActiveBrushSelect
    {
        get
        {
            foreach (var m in _modifiers)
            {
                if (m is BrushSelectModifier bsm)
                    return bsm.ActiveBrush;
            }
            return null;
        }
    }

    /// <inheritdoc />
    public HoverTooltipContent? ActiveTooltip => _activeTooltip;

    private InteractionController(
        Figure figure,
        IChartLayout layout,
        Action<FigureInteractionEvent> sink)
    {
        _figure    = figure;
        _layout    = layout;
        _sink      = sink;
        _modifiers = BuildModifiers(figure.ChartId ?? string.Empty, layout, sink, figure);
    }

    /// <summary>Creates a controller in local mode: events are applied directly to the figure
    /// and <see cref="InvalidateRequested"/> is raised after each mutation.</summary>
    public static InteractionController CreateLocal(Figure figure, IChartLayout layout)
    {
        InteractionController? ctrl = null;
        Action<FigureInteractionEvent> localSink = evt =>
        {
            evt.ApplyTo(figure);
            // Notification events have a sealed no-op ApplyTo — they don't mutate.
            // Only fire InvalidateRequested for mutation events.
            if (evt is not FigureNotificationEvent)
                ctrl!.InvalidateRequested?.Invoke();
        };
        ctrl = new InteractionController(figure, layout, localSink);
        return ctrl;
    }

    /// <summary>Creates a controller with a custom event sink.
    /// The sink is responsible for applying or publishing the event.</summary>
    public static InteractionController Create(
        Figure figure,
        IChartLayout layout,
        Action<FigureInteractionEvent> sink)
        => new(figure, layout, sink);

    /// <inheritdoc />
    public void UpdateLayout(IChartLayout layout)
    {
        _layout = layout;
        _modifiers = BuildModifiers(_figure.ChartId ?? string.Empty, layout, _sink, _figure);
    }

    /// <inheritdoc />
    public void HandlePointerPressed(PointerInputArgs args)
    {
        _activeModifier = null;
        foreach (var m in _modifiers)
        {
            if (m.HandlesPointerPressed(args))
            {
                _activeModifier = m;
                m.OnPointerPressed(args);
                return;
            }
        }
    }

    /// <inheritdoc />
    public void HandlePointerMoved(PointerInputArgs args)
    {
        // Active drag modifier gets priority; hover modifier always processes moves.
        _activeModifier?.OnPointerMoved(args);

        // If the active modifier is a brush select, repaint so the control draws the rubber band.
        if (_activeModifier is BrushSelectModifier)
            InvalidateRequested?.Invoke();

        // If no active modifier (or active is not the hover modifier), let hover run too.
        bool hoverRan = false;
        foreach (var m in _modifiers)
        {
            if (m is HoverModifier && m != _activeModifier)
            {
                m.OnPointerMoved(args);
                hoverRan = true;
                break;
            }
        }

        // Compute hover tooltip from the nearest data point when no drag is active.
        if (hoverRan && _activeModifier is null)
        {
            int? axesIndex = _layout.HitTestAxes(args.X, args.Y);
            if (axesIndex is not null && _layout is ChartLayout cl)
            {
                var coords = cl.PixelToData(args.X, args.Y, axesIndex.Value);
                if (coords is { } c)
                {
                    var nearest = NearestPointFinder.Find(_figure, axesIndex.Value, c.DataX, c.DataY, _layout);
                    _activeTooltip = nearest is not null
                        ? new HoverTooltipContent(
                            $"{nearest.SeriesLabel}: ({nearest.DataX:G5}, {nearest.DataY:G5})",
                            args.X, args.Y)
                        : null;
                }
                else
                {
                    _activeTooltip = null;
                }
            }
            else
            {
                _activeTooltip = null;
            }
        }
    }

    /// <inheritdoc />
    public void HandlePointerReleased(PointerInputArgs args)
    {
        _activeModifier?.OnPointerReleased(args);
        _activeModifier = null;
    }

    /// <inheritdoc />
    public void HandleScroll(ScrollInputArgs args)
    {
        foreach (var m in _modifiers)
        {
            if (m.HandlesScroll(args))
            {
                m.OnScroll(args);
                return;
            }
        }
    }

    /// <inheritdoc />
    public void HandleKeyDown(KeyInputArgs args)
    {
        foreach (var m in _modifiers)
        {
            if (m.HandlesKeyDown(args))
            {
                m.OnKeyDown(args);
                return;
            }
        }
    }

    private static IReadOnlyList<IInteractionModifier> BuildModifiers(
        string chartId,
        IChartLayout layout,
        Action<FigureInteractionEvent> sink,
        Figure figure)
    {
        // Order matters — first modifier whose Handles* returns true claims the event:
        //   1. LegendToggle: click on legend item (specific hit-test, highest priority for clicks)
        //   2. Reset: double-click wins before Pan (both claim left-button; ClickCount distinguishes)
        //   3. Rotate3D: right-drag on 3D axes, arrow keys/Home for 3D (v1.4.1)
        //   4. RectangleZoom: Ctrl+drag draws zoom box (v1.4.1)
        //   5. BrushSelect: Shift+drag before Pan (Shift distinguishes)
        //   6. SpanSelect: Alt+drag selects X-range (v1.4.1)
        //   7. Pan: plain left-drag
        //   8. Zoom: scroll wheel
        //   9. Crosshair: passive — every move (v1.4.1)
        //  10. Hover: passive, always last
        return
        [
            new LegendToggleModifier(chartId, layout, sink),
            new ResetModifier(chartId, layout, sink),
            new Rotate3DModifier(chartId, layout, sink, figure),
            new RectangleZoomModifier(chartId, layout, sink),
            new BrushSelectModifier(chartId, layout, sink),
            new SpanSelectModifier(chartId, layout, sink),
            new PanModifier(chartId, layout, sink),
            new ZoomModifier(chartId, layout, sink),
            new HoverModifier(chartId, layout, sink),
        ];
    }
}
