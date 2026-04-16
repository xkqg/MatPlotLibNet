// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Fires a <see cref="LegendToggleEvent"/> when the user clicks a legend entry.
/// Relies on <see cref="IChartLayout.HitTestLegendItem"/> to determine which legend item was
/// clicked based on the pixel-space bounds computed by the renderer.</summary>
public sealed class LegendToggleModifier : IInteractionModifier
{
    private readonly string _chartId;
    private readonly IChartLayout _layout;
    private readonly Action<FigureInteractionEvent> _sink;

    /// <summary>Creates a legend-toggle modifier for the given chart.</summary>
    public LegendToggleModifier(string chartId, IChartLayout layout, Action<FigureInteractionEvent> sink)
    {
        _chartId = chartId;
        _layout  = layout;
        _sink    = sink;
    }

    /// <inheritdoc />
    public bool HandlesPointerPressed(PointerInputArgs args)
    {
        if (args.Button != PointerButton.Left) return false;
        int? axesIndex = _layout.HitTestAxes(args.X, args.Y);
        if (axesIndex is null) return false;
        return _layout.HitTestLegendItem(args.X, args.Y, axesIndex.Value) is not null;
    }

    /// <inheritdoc />
    public void OnPointerPressed(PointerInputArgs args)
    {
        int axesIndex  = _layout.HitTestAxes(args.X, args.Y) ?? 0;
        int? seriesIdx = _layout.HitTestLegendItem(args.X, args.Y, axesIndex);
        if (seriesIdx is null) return;
        _sink(new LegendToggleEvent(_chartId, axesIndex, seriesIdx.Value));
    }

    /// <inheritdoc />
    public void OnPointerMoved(PointerInputArgs args) { }
    /// <inheritdoc />
    public void OnPointerReleased(PointerInputArgs args) { }

    /// <inheritdoc />
    public bool HandlesScroll(ScrollInputArgs args) => false;
    /// <inheritdoc />
    public void OnScroll(ScrollInputArgs args) { }

    /// <inheritdoc />
    public bool HandlesKeyDown(KeyInputArgs args) => false;
    /// <inheritdoc />
    public void OnKeyDown(KeyInputArgs args) { }
}
