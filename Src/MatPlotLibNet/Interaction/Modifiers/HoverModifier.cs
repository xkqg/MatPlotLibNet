// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Fires a <see cref="HoverEvent"/> when the cursor moves over a plot area with no button held.
/// Fire-and-forget notification: the figure is never mutated.</summary>
public sealed class HoverModifier : IInteractionModifier
{
    private readonly string _chartId;
    private readonly IChartLayout _layout;
    private readonly Action<FigureInteractionEvent> _sink;

    /// <summary>Creates a hover modifier for the given chart.</summary>
    public HoverModifier(string chartId, IChartLayout layout, Action<FigureInteractionEvent> sink)
    {
        _chartId = chartId;
        _layout  = layout;
        _sink    = sink;
    }

    /// <inheritdoc />
    public bool HandlesPointerPressed(PointerInputArgs args) => false;
    /// <inheritdoc />
    public void OnPointerPressed(PointerInputArgs args) { }
    /// <inheritdoc />
    public void OnPointerReleased(PointerInputArgs args) { }

    /// <inheritdoc />
    public void OnPointerMoved(PointerInputArgs args)
    {
        if (args.Button != PointerButton.None) return;

        int? axesIndex = _layout.HitTestAxes(args.X, args.Y);
        if (axesIndex is null) return;

        var coords = ((ChartLayout)_layout).PixelToData(args.X, args.Y, axesIndex.Value);

        _sink(new HoverEvent(_chartId, axesIndex.Value, coords!.Value.DataX, coords.Value.DataY));
    }

    /// <inheritdoc />
    public bool HandlesScroll(ScrollInputArgs args) => false;
    /// <inheritdoc />
    public void OnScroll(ScrollInputArgs args) { }

    /// <inheritdoc />
    public bool HandlesKeyDown(KeyInputArgs args) => false;
    /// <inheritdoc />
    public void OnKeyDown(KeyInputArgs args) { }
}
