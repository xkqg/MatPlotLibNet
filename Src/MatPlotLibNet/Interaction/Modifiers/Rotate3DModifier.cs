// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Interaction;

/// <summary>Rotates a 3D axes camera on right-mouse-drag. Arrow keys rotate ±5°.
/// Home key resets to the default camera position (elevation 30°, azimuth -60°).
/// Only activates on axes with <see cref="CoordinateSystem.ThreeD"/>.</summary>
public sealed class Rotate3DModifier : IInteractionModifier
{
    private const double DegreesPerPixel = 0.5;
    private const double ArrowKeyDegrees = 5.0;
    private const double DefaultElevation = 30.0;
    private const double DefaultAzimuth = -60.0;

    private readonly string _chartId;
    private readonly IChartLayout _layout;
    private readonly Action<FigureInteractionEvent> _sink;
    private readonly Figure _figure;

    private bool _active;
    private int _activeAxes;
    private double _lastX;
    private double _lastY;

    /// <summary>Creates a 3D rotation modifier.</summary>
    public Rotate3DModifier(string chartId, IChartLayout layout,
        Action<FigureInteractionEvent> sink, Figure figure)
    {
        _chartId = chartId;
        _layout = layout;
        _sink = sink;
        _figure = figure;
    }

    /// <inheritdoc />
    public bool HandlesPointerPressed(PointerInputArgs args)
    {
        if (args.Button != PointerButton.Right) return false;
        var axesIndex = _layout.HitTestAxes(args.X, args.Y);
        if (axesIndex is null) return false;
        return IsThreeD(axesIndex.Value);
    }

    /// <inheritdoc />
    public void OnPointerPressed(PointerInputArgs args)
    {
        _activeAxes = _layout.HitTestAxes(args.X, args.Y) ?? 0;
        _active = true;
        _lastX = args.X;
        _lastY = args.Y;
    }

    /// <inheritdoc />
    public void OnPointerMoved(PointerInputArgs args)
    {
        if (!_active) return;

        double dx = args.X - _lastX;
        double dy = args.Y - _lastY;
        _lastX = args.X;
        _lastY = args.Y;

        if (dx == 0 && dy == 0) return;

        _sink(new Rotate3DEvent(_chartId, _activeAxes,
            dx * DegreesPerPixel, -dy * DegreesPerPixel));
    }

    /// <inheritdoc />
    public void OnPointerReleased(PointerInputArgs args) => _active = false;

    /// <inheritdoc />
    public bool HandlesScroll(ScrollInputArgs args) => false;

    /// <inheritdoc />
    public void OnScroll(ScrollInputArgs args) { }

    /// <inheritdoc />
    public bool HandlesKeyDown(KeyInputArgs args)
    {
        if (!HasAnyThreeD()) return false;
        return args.Key is "Left" or "Right" or "Up" or "Down" or "Home";
    }

    /// <inheritdoc />
    public void OnKeyDown(KeyInputArgs args)
    {
        int axesIndex = FindFirstThreeD();
        if (axesIndex < 0) return;

        var (da, de) = args.Key switch
        {
            "Left" => (-ArrowKeyDegrees, 0.0),
            "Right" => (ArrowKeyDegrees, 0.0),
            "Up" => (0.0, ArrowKeyDegrees),
            "Down" => (0.0, -ArrowKeyDegrees),
            "Home" => (DefaultAzimuth - _figure.SubPlots[axesIndex].Azimuth,
                       DefaultElevation - _figure.SubPlots[axesIndex].Elevation),
            _ => (0.0, 0.0)
        };

        if (da != 0 || de != 0)
            _sink(new Rotate3DEvent(_chartId, axesIndex, da, de));
    }

    private bool IsThreeD(int axesIndex) =>
        axesIndex < _figure.SubPlots.Count &&
        _figure.SubPlots[axesIndex].CoordinateSystem == CoordinateSystem.ThreeD;

    private bool HasAnyThreeD()
    {
        foreach (var ax in _figure.SubPlots)
            if (ax.CoordinateSystem == CoordinateSystem.ThreeD) return true;
        return false;
    }

    private int FindFirstThreeD()
    {
        for (int i = 0; i < _figure.SubPlots.Count; i++)
            if (_figure.SubPlots[i].CoordinateSystem == CoordinateSystem.ThreeD) return i;
        return -1;
    }
}
