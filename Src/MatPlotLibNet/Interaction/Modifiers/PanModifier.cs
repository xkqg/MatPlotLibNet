// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Interaction;

/// <summary>Translates the visible axes range when the user drags with the left mouse button
/// (without Shift, which is claimed by <see cref="BrushSelectModifier"/>).</summary>
public sealed class PanModifier : InteractionModifier
{
    private bool   _active;
    private int    _activeAxes;
    /// <summary>Where the pointer was when the last pan was sent (or the button went down): the next move pans by the
    /// distance from here.</summary>
    private double _lastPixelX;
    private double _lastPixelY;

    /// <summary>Creates a pan modifier for the given chart.</summary>
    public PanModifier(string chartId, IChartLayout layout, Action<FigureInteractionEvent> sink)
        : base(chartId, layout, sink)
    {
    }

    /// <inheritdoc />
    public override bool HandlesPointerPressed(PointerInputArgs args) =>
        (args.Modifiers & ModifierKeys.Shift) == 0 && Layout.IsLeftPressOnAxes(args);

    /// <inheritdoc />
    public override void OnPointerPressed(PointerInputArgs args)
    {
        if (Layout.PressAt(args.X, args.Y) is not { } press) return;

        _active     = true;
        _activeAxes = press.AxesIndex;
        _lastPixelX = args.X;
        _lastPixelY = args.Y;
    }

    /// <inheritdoc />
    /// <remarks>The pointer's move on screen becomes a shift of the range in the space each axis is drawn in (data
    /// units on a linear axis, decades on a log axis, with the axis' breaks cut out), the unit <see cref="PanEvent"/> takes:
    /// the point that was under the pointer moves with it, on every scale and across a break. Each move sends the distance
    /// since the last one, so a move that arrives before the control has repainted still pans by exactly that move.</remarks>
    public override void OnPointerMoved(PointerInputArgs args)
    {
        if (!_active) return;

        var coords = Layout.PixelToData(args.X, args.Y, _activeAxes);
        if (coords is null) return;

        var view = Layout.ViewOf(_activeAxes);
        var area = Layout.GetPlotArea(_activeAxes);
        double dyPixels = args.Y - _lastPixelY;
        // Drawn units per pixel; negative on a reversed axis, so the shift still follows the pointer.
        double dx = (_lastPixelX - args.X) / area.Width * (view.DataXMax - view.DataXMin);
        double dy = dyPixels / area.Height * (view.DataYMax - view.DataYMin);

        if (dx == 0 && dy == 0) return;

        Emit(new PanEvent(ChartId, _activeAxes, dx, dy)
        {
            InView = Layout.GetDataRange(_activeAxes),
            SecondaryRanges = SecondaryRangesFor(area, dyPixels),
        });

        _lastPixelX = args.X;
        _lastPixelY = args.Y;
    }

    /// <summary>The new range of every Y axis stacked past the primary after this move, each in ITS OWN data units: the plot's
    /// lower and upper edges moved by the pointer's <paramref name="dyPixels"/>, read back through that axis' own transform
    /// (<see cref="DataTransform.PixelToData"/>), the rule a zoom reads its edges by. The same pixel move is a different shift
    /// on an axis with a different span or scale, and a log axis moves by decades.</summary>
    private List<SecondaryAxisRange>? SecondaryRangesFor(Rect area, double dyPixels)
    {
        var low = new Point(area.X, area.Y + area.Height - dyPixels);
        var high = new Point(area.X + area.Width, area.Y - dyPixels);
        return Layout.SecondaryRangesBetween(_activeAxes, low, high, DataRange.FromCorners);
    }

    /// <inheritdoc />
    public override void OnPointerReleased(PointerInputArgs args) => _active = false;
}
