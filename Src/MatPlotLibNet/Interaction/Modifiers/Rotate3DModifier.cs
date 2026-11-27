// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Interaction;

/// <summary>Rotates a 3D axes camera on right-mouse-drag. Arrow keys rotate ±5°.
/// Home key turns it back to the camera at rest (elevation <see cref="Camera3D.RestingElevation"/>, azimuth
/// <see cref="Camera3D.RestingAzimuth"/>). Only activates on axes with <see cref="CoordinateSystem.ThreeD"/>.</summary>
public sealed class Rotate3DModifier : InteractionModifier
{
    private const double DegreesPerPixel = 0.5;
    private const double ArrowKeyDegrees = 5.0;

    private readonly Figure _figure;

    private bool _active;
    private int _activeAxes;
    private double _lastX;
    private double _lastY;

    /// <summary>Creates a 3D rotation modifier.</summary>
    public Rotate3DModifier(string chartId, IChartLayout layout,
        Action<FigureInteractionEvent> sink, Figure figure)
        : base(chartId, layout, sink)
    {
        _figure = figure;
    }

    /// <inheritdoc />
    public override bool HandlesPointerPressed(PointerInputArgs args)
    {
        if (args.Button != PointerButton.Right) return false;
        var axesIndex = Layout.HitTestAxes(args.X, args.Y);
        if (axesIndex is null) return false;
        return IsThreeD(axesIndex.Value);
    }

    /// <inheritdoc />
    public override void OnPointerPressed(PointerInputArgs args)
    {
        _activeAxes = Layout.AxesOrFirstAt(args.X, args.Y);
        _active = true;
        _lastX = args.X;
        _lastY = args.Y;
    }

    /// <inheritdoc />
    public override void OnPointerMoved(PointerInputArgs args)
    {
        if (!_active) return;

        double dx = args.X - _lastX;
        double dy = args.Y - _lastY;
        _lastX = args.X;
        _lastY = args.Y;

        if (dx == 0 && dy == 0) return;

        Emit(new Rotate3DEvent(ChartId, _activeAxes,
            dx * DegreesPerPixel, -dy * DegreesPerPixel));
    }

    /// <inheritdoc />
    public override void OnPointerReleased(PointerInputArgs args) => _active = false;

    /// <inheritdoc />
    public override bool HandlesKeyDown(KeyInputArgs args)
    {
        if (!HasAnyThreeD()) return false;
        return args.Key is "Left" or "Right" or "Up" or "Down" or "Home";
    }

    /// <inheritdoc />
    public override void OnKeyDown(KeyInputArgs args)
    {
        int axesIndex = FindFirstThreeD();
        if (axesIndex < 0) return;

        var (da, de) = args.Key switch
        {
            "Left" => (-ArrowKeyDegrees, 0.0),
            "Right" => (ArrowKeyDegrees, 0.0),
            "Up" => (0.0, ArrowKeyDegrees),
            "Down" => (0.0, -ArrowKeyDegrees),
            "Home" => (Camera3D.RestingAzimuth - _figure.SubPlots[axesIndex].Azimuth,
                       Camera3D.RestingElevation - _figure.SubPlots[axesIndex].Elevation),
            _ => (0.0, 0.0)
        };

        if (da != 0 || de != 0)
            Emit(new Rotate3DEvent(ChartId, axesIndex, da, de));
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
