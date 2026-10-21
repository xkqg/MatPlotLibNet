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
        // The value under the pointer by the rule every other modifier converts with: the range and scale drawn.
        var data = _layout.ViewOf(axesIndex.Value).PixelToData(new Rendering.Point(pixelX, pixelY));

        _state = new CrosshairState(pixelX, pixelY, data.X, data.Y, axesIndex.Value, plotArea);
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
