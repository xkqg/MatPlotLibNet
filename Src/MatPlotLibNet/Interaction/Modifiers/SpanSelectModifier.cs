// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Alt+left-drag selects a horizontal X-range, fires <see cref="SpanSelectEvent"/>.
/// Exposes <see cref="ActiveSpan"/> for controls to draw a full-height shaded band overlay.</summary>
public sealed class SpanSelectModifier : IInteractionModifier
{
    private readonly string _chartId;
    private readonly IChartLayout _layout;
    private readonly Action<FigureInteractionEvent> _sink;

    private bool _active;
    private int _activeAxes;
    private double _startPixelX;
    private double _currentPixelX;
    private double _startDataX;

    /// <summary>Creates a span-select modifier.</summary>
    public SpanSelectModifier(string chartId, IChartLayout layout, Action<FigureInteractionEvent> sink)
    {
        _chartId = chartId;
        _layout = layout;
        _sink = sink;
    }

    /// <summary>Returns the span state during an Alt+drag, or <c>null</c>.</summary>
    public SpanSelectState? ActiveSpan =>
        _active
            ? new SpanSelectState(_startPixelX, _currentPixelX, _activeAxes, _layout.GetPlotArea(_activeAxes))
            : null;

    /// <inheritdoc />
    public bool HandlesPointerPressed(PointerInputArgs args) =>
        args.Button == PointerButton.Left &&
        (args.Modifiers & ModifierKeys.Alt) != 0 &&
        _layout.HitTestAxes(args.X, args.Y) is not null;

    /// <inheritdoc />
    public void OnPointerPressed(PointerInputArgs args)
    {
        _activeAxes = _layout.HitTestAxes(args.X, args.Y) ?? 0;
        var coords = ((ChartLayout)_layout).PixelToData(args.X, args.Y, _activeAxes);
        if (coords is null) return;

        _active = true;
        _startPixelX = _currentPixelX = args.X;
        _startDataX = coords.Value.DataX;
    }

    /// <inheritdoc />
    public void OnPointerMoved(PointerInputArgs args)
    {
        if (!_active) return;
        _currentPixelX = args.X;
    }

    /// <inheritdoc />
    public void OnPointerReleased(PointerInputArgs args)
    {
        if (!_active) return;
        _active = false;

        var endCoords = ((ChartLayout)_layout).PixelToData(args.X, args.Y, _activeAxes);
        if (endCoords is null) return;

        double xMin = Math.Min(_startDataX, endCoords.Value.DataX);
        double xMax = Math.Max(_startDataX, endCoords.Value.DataX);

        if (Math.Abs(args.X - _startPixelX) < 2) return; // ignore tiny drags

        _sink(new SpanSelectEvent(_chartId, _activeAxes, xMin, xMax));
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
