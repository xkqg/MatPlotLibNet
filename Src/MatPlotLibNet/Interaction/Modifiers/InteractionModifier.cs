// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>The common base of the modifiers: it holds the chart id, the layout and the event sink every modifier
/// is made with, and answers "I claim nothing" and "I do nothing" to every gesture until a derived class overrides
/// the ones it cares about. A modifier that only presses and drags overrides the pointer members and leaves the scroll
/// and key members as they are.
/// <para>Beyond the gestures it claims, a controller asks every modifier for a role: what it does on each move of the pointer
/// (<see cref="OnPointerTracked"/>, the crosshair and the hover) and what it shows (<see cref="ActiveBrushSelect"/>,
/// <see cref="ActiveCrosshair"/>, <see cref="ActiveTooltip"/>). A modifier of your own derives from this class and overrides the
/// ones it takes, so it can stand in the place of a built-in one in <see cref="InteractionController.DefaultModifiers"/>.</para></summary>
public abstract class InteractionModifier : IInteractionModifier
{
    private readonly Action<FigureInteractionEvent> _sink;

    /// <summary>Creates a modifier for one chart.</summary>
    /// <param name="chartId">The identifier of the chart the events it sends are addressed to.</param>
    /// <param name="layout">The layout of the rendered figure: where the axes are and which range they show.</param>
    /// <param name="sink">Receives every event the modifier sends.</param>
    protected InteractionModifier(string chartId, IChartLayout layout, Action<FigureInteractionEvent> sink)
    {
        ChartId = chartId;
        Layout = layout;
        _sink = sink;
    }

    /// <summary>Creates a modifier that only reads the layout: it has no chart to address and sends nothing.</summary>
    /// <param name="layout">The layout of the rendered figure.</param>
    protected InteractionModifier(IChartLayout layout) : this(string.Empty, layout, static _ => { })
    {
    }

    /// <summary>The identifier of the chart the events of this modifier are addressed to.</summary>
    protected string ChartId { get; }

    /// <summary>The layout of the rendered figure.</summary>
    protected IChartLayout Layout { get; }

    /// <summary>Hands <paramref name="e"/> to the sink the modifier was made with.</summary>
    /// <param name="e">The event to send.</param>
    protected void Emit(FigureInteractionEvent e) => _sink(e);

    /// <inheritdoc />
    public virtual bool HandlesPointerPressed(PointerInputArgs args) => false;
    /// <inheritdoc />
    public virtual void OnPointerPressed(PointerInputArgs args) { }
    /// <inheritdoc />
    public virtual void OnPointerMoved(PointerInputArgs args) { }
    /// <inheritdoc />
    public virtual void OnPointerReleased(PointerInputArgs args) { }

    /// <inheritdoc />
    public virtual bool HandlesScroll(ScrollInputArgs args) => false;
    /// <inheritdoc />
    public virtual void OnScroll(ScrollInputArgs args) { }

    /// <inheritdoc />
    public virtual bool HandlesKeyDown(KeyInputArgs args) => false;
    /// <inheritdoc />
    public virtual void OnKeyDown(KeyInputArgs args) { }

    /// <summary>Told of every move of the pointer, whichever modifier is dragging (the one dragging is told through
    /// <see cref="OnPointerMoved"/> instead). The crosshair follows the pointer here and the hover names the point under it.
    /// Does nothing unless overridden.</summary>
    /// <param name="args">The pointer position, button and held keys.</param>
    public virtual void OnPointerTracked(PointerInputArgs args) { }

    /// <summary>The rubber band this modifier is dragging, for the control to draw, or <see langword="null"/> for none.</summary>
    public virtual BrushSelectState? ActiveBrushSelect => null;

    /// <summary>The crosshair this modifier shows under the pointer, or <see langword="null"/> for none.</summary>
    public virtual CrosshairState? ActiveCrosshair => null;

    /// <summary>The tooltip this modifier shows for the point under the pointer, or <see langword="null"/> for none.</summary>
    public virtual HoverTooltipContent? ActiveTooltip => null;
}
