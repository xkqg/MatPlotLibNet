// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Ctrl+left-drag draws a zoom rectangle; on release, zooms the axes to that rectangle.
/// Exposes <see cref="ActiveZoomRect"/> for overlay rendering by platform controls.</summary>
public sealed class RectangleZoomModifier : IInteractionModifier
{
    private readonly string _chartId;
    private readonly IChartLayout _layout;
    private readonly Action<FigureInteractionEvent> _sink;

    private bool _active;
    private int _activeAxes;
    private double _startPixelX, _startPixelY;
    private double _currentPixelX, _currentPixelY;
    private double _startDataX, _startDataY;

    /// <summary>Creates a rectangle-zoom modifier.</summary>
    public RectangleZoomModifier(string chartId, IChartLayout layout, Action<FigureInteractionEvent> sink)
    {
        _chartId = chartId;
        _layout = layout;
        _sink = sink;
    }

    /// <summary>Returns the current zoom rectangle while a Ctrl+drag is in progress, or <c>null</c>.</summary>
    public RectangleZoomState? ActiveZoomRect =>
        _active
            ? new RectangleZoomState(_startPixelX, _startPixelY, _currentPixelX, _currentPixelY, _activeAxes)
            : null;

    /// <inheritdoc />
    public bool HandlesPointerPressed(PointerInputArgs args) =>
        args.Button == PointerButton.Left &&
        (args.Modifiers & ModifierKeys.Ctrl) != 0 &&
        _layout.HitTestAxes(args.X, args.Y) is not null;

    /// <inheritdoc />
    public void OnPointerPressed(PointerInputArgs args)
    {
        _activeAxes = _layout.HitTestAxes(args.X, args.Y) ?? 0;
        var coords = ((ChartLayout)_layout).PixelToData(args.X, args.Y, _activeAxes);
        if (coords is null) return;

        _active = true;
        _startPixelX = _currentPixelX = args.X;
        _startPixelY = _currentPixelY = args.Y;
        _startDataX = coords.Value.DataX;
        _startDataY = coords.Value.DataY;
    }

    /// <inheritdoc />
    public void OnPointerMoved(PointerInputArgs args)
    {
        if (!_active) return;
        _currentPixelX = args.X;
        _currentPixelY = args.Y;
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
        double yMin = Math.Min(_startDataY, endCoords.Value.DataY);
        double yMax = Math.Max(_startDataY, endCoords.Value.DataY);

        // Ignore tiny drags (< 2px in either direction)
        if (Math.Abs(args.X - _startPixelX) < 2 && Math.Abs(args.Y - _startPixelY) < 2) return;

        _sink(new RectangleZoomEvent(_chartId, _activeAxes, xMin, xMax, yMin, yMax));
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
