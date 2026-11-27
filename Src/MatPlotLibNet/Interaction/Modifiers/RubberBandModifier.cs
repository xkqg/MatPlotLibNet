// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>A modifier that draws a rubber band while a key is held: a left press on an axes with the key held opens it, it follows the
/// pointer, and the release hands the drag to the modifier, unless the drag is too short to count. The zoom rectangle
/// (<see cref="RectangleZoomModifier"/>), the span (<see cref="SpanSelectModifier"/>) and the brush (<see cref="BrushSelectModifier"/>)
/// are the three; each names only its key, how short a drag may be, the state it shows while dragging and the event it sends. Only the
/// library derives from it.</summary>
public abstract class RubberBandModifier : InteractionModifier
{
    /// <summary>The band being dragged.</summary>
    private readonly DragTrack _drag;

    /// <summary>Creates a rubber band for one chart.</summary>
    /// <param name="chartId">The identifier of the chart the events it sends are addressed to.</param>
    /// <param name="layout">The layout of the rendered figure.</param>
    /// <param name="sink">Receives every event the modifier sends.</param>
    /// <param name="key">The key that must be held for a press to open the band.</param>
    /// <param name="floor">The least a drag must cover to count.</param>
    private protected RubberBandModifier(string chartId, IChartLayout layout, Action<FigureInteractionEvent> sink, ModifierKeys key,
        DragFloor floor)
        : base(chartId, layout, sink) => _drag = new DragTrack(key, floor);

    /// <summary>The band while it is dragged: its axes, the pixel it started at and the pixel the pointer is at now.</summary>
    private protected DragTrack Band => _drag;

    /// <inheritdoc />
    public sealed override bool HandlesPointerPressed(PointerInputArgs args) => _drag.Opens(Layout, args);

    /// <inheritdoc />
    public sealed override void OnPointerPressed(PointerInputArgs args) => _drag.Begin(Layout, args);

    /// <inheritdoc />
    public sealed override void OnPointerMoved(PointerInputArgs args) => _drag.MoveTo(args);

    /// <inheritdoc />
    public sealed override void OnPointerReleased(PointerInputArgs args)
    {
        if (_drag.End(Layout, args) is { } drag) Released(drag);
    }

    /// <summary>Acts on a drag that counts: what the modifier sends for it.</summary>
    /// <param name="drag">The finished drag.</param>
    private protected abstract void Released(Drag drag);
}
