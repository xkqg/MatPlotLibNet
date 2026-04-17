// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Passive modifier that tracks the mouse position and exposes <see cref="ActiveCrosshair"/>
/// for controls to draw vertical + horizontal crosshair lines at data coordinates.
/// Does not claim any pointer events — runs alongside all other modifiers.</summary>
public sealed class CrosshairModifier : IInteractionModifier
{
    private readonly IChartLayout _layout;
    private CrosshairState? _state;

    /// <summary>Current crosshair state, or <c>null</c> when the mouse is outside the plot area.</summary>
    public CrosshairState? ActiveCrosshair => _state;

    /// <summary>Creates a crosshair modifier.</summary>
    public CrosshairModifier(IChartLayout layout) => _layout = layout;

    /// <summary>Updates crosshair state on every pointer move (called by controller in passive block).</summary>
    public void UpdatePosition(double pixelX, double pixelY)
    {
        var axesIndex = _layout.HitTestAxes(pixelX, pixelY);
        if (axesIndex is null) { _state = null; return; }

        var plotArea = _layout.GetPlotArea(axesIndex.Value);
        var range = _layout.GetDataRange(axesIndex.Value);

        double fracX = (pixelX - plotArea.X) / plotArea.Width;
        double fracY = 1.0 - (pixelY - plotArea.Y) / plotArea.Height;
        double dataX = range.XMin + fracX * (range.XMax - range.XMin);
        double dataY = range.YMin + fracY * (range.YMax - range.YMin);

        _state = new CrosshairState(pixelX, pixelY, dataX, dataY, axesIndex.Value, plotArea);
    }

    // Passive modifier — does not claim any events
    public bool HandlesPointerPressed(PointerInputArgs args) => false;
    public void OnPointerPressed(PointerInputArgs args) { }
    public void OnPointerMoved(PointerInputArgs args) { }
    public void OnPointerReleased(PointerInputArgs args) { }
    public bool HandlesScroll(ScrollInputArgs args) => false;
    public void OnScroll(ScrollInputArgs args) { }
    public bool HandlesKeyDown(KeyInputArgs args) => false;
    public void OnKeyDown(KeyInputArgs args) { }
}
