// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Interaction;

/// <summary>Resets the axes to their original limits on a double-click or <c>Home</c> key press.</summary>
public sealed class ResetModifier : IInteractionModifier
{
    private readonly string _chartId;
    private readonly IChartLayout _layout;
    private readonly Action<FigureInteractionEvent> _sink;
    /// <summary>Original data ranges captured when the modifier was created, keyed by axes index.</summary>
    private readonly Dictionary<int, DataRange> _originalRanges;

    /// <summary>Creates a reset modifier. The original axis limits are snapshotted at construction time.</summary>
    public ResetModifier(string chartId, IChartLayout layout, Action<FigureInteractionEvent> sink)
    {
        _chartId        = chartId;
        _layout         = layout;
        _sink           = sink;
        _originalRanges = new Dictionary<int, DataRange>();

        for (int i = 0; i < layout.AxesCount; i++)
            _originalRanges[i] = layout.GetDataRange(i);
    }

    /// <inheritdoc />
    public bool HandlesPointerPressed(PointerInputArgs args) =>
        args.ClickCount >= 2 && _layout.HitTestAxes(args.X, args.Y) is not null;

    /// <inheritdoc />
    public void OnPointerPressed(PointerInputArgs args)
    {
        int axesIndex = _layout.HitTestAxes(args.X, args.Y) ?? 0;
        EmitReset(axesIndex);
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
    public bool HandlesKeyDown(KeyInputArgs args) =>
        args.Key is "Home" or "Escape";

    /// <inheritdoc />
    public void OnKeyDown(KeyInputArgs args)
    {
        // Reset all axes on keyboard reset.
        for (int i = 0; i < _layout.AxesCount; i++)
            EmitReset(i);
    }

    private void EmitReset(int axesIndex)
    {
        if (!_originalRanges.TryGetValue(axesIndex, out var orig)) return;
        _sink(new ResetEvent(_chartId, axesIndex, orig.XMin, orig.XMax, orig.YMin, orig.YMax));
    }
}
