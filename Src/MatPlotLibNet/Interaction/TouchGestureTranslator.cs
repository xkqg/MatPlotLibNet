// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Turns the gestures a touch surface reports into the pointer input an <see cref="InteractionHost"/> understands.
/// A finger has no wheel, no modifier keys and no button, only a drag, a pinch and a tap, and the interaction controller is
/// written for the mouse, so: a drag is whatever <see cref="DragAction"/> says (a pan, a rectangle zoom, a band, a span, a
/// rotation), a pinch is the wheel, a double tap is Home, a tap is a click and a finger moving without pressing is a hover.
/// <para>It holds no figure and draws nothing, so it is the same for every UI framework: a control forwards what its gesture
/// recognizers report, and what each gesture does is decided and tested here. Call it on the UI thread, the thread the host
/// is called on.</para></summary>
/// <param name="host">The host the translated input is given to.</param>
public sealed class TouchGestureTranslator(InteractionHost host)
{
    /// <summary>What one notch of the mouse wheel zooms by (the zoom modifier's 15 % of the range).</summary>
    private const double NotchFraction = 0.15;

    /// <summary>A pinch is the wheel: it adds up in the logarithm of its scale until it is a whole notch, so many small steps
    /// zoom as much as one big one and a step too small to be a notch is not lost. A notch in shrinks the range to 85 %, a
    /// notch out widens it to 115 %.</summary>
    private static readonly double LogNotchIn = -Math.Log(1.0 - NotchFraction);

    private static readonly double LogNotchOut = Math.Log(1.0 + NotchFraction);

    private DragGestureAction _action;
    private bool _dragging;
    private double _startX;
    private double _startY;
    private double _pinched;

    /// <summary>What a one-finger drag does. It is read when a drag starts; a drag in progress keeps the action it began with.</summary>
    public DragGestureAction DragAction { get; set; } = DragGestureAction.Pan;

    /// <summary>A finger went down and began to drag at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    /// <param name="x">Where the drag began, in the control's pixels.</param>
    /// <param name="y">Where the drag began.</param>
    public void DragStarted(double x, double y)
    {
        if (_dragging) DragCancelled();

        _action = DragAction;
        _dragging = true;
        _startX = x;
        _startY = y;
        host.HandlePointerPressed(MouseFor(_action, x, y));
    }

    /// <summary>The finger moved. The gesture recognizers of a touch surface report the distance from where the drag began,
    /// so both are taken.</summary>
    /// <param name="startX">Where the drag began.</param>
    /// <param name="startY">Where the drag began.</param>
    /// <param name="totalX">How far the finger is from there, horizontally.</param>
    /// <param name="totalY">How far the finger is from there, vertically.</param>
    public void DragMoved(double startX, double startY, double totalX, double totalY)
    {
        if (!_dragging) return;

        host.HandlePointerMoved(MouseFor(_action, startX + totalX, startY + totalY));
    }

    /// <summary>The finger lifted.</summary>
    /// <param name="startX">Where the drag began.</param>
    /// <param name="startY">Where the drag began.</param>
    /// <param name="totalX">How far the finger got, horizontally.</param>
    /// <param name="totalY">How far the finger got, vertically.</param>
    public void DragEnded(double startX, double startY, double totalX, double totalY)
    {
        if (!_dragging) return;

        _dragging = false;
        host.HandlePointerReleased(MouseFor(_action, startX + totalX, startY + totalY));
    }

    /// <summary>The system took the drag away (a call, another gesture). What it had begun is ended where it began: a pan has
    /// moved nothing further, a rectangle zoom and a span are smaller than the two pixels they need to count, so none of
    /// them selects or zooms anything. A brush ends as the empty band it is at that point, as a click with Shift held does.</summary>
    public void DragCancelled()
    {
        if (!_dragging) return;

        _dragging = false;
        host.HandlePointerReleased(MouseFor(_action, _startX, _startY));
    }

    /// <summary>Two fingers pinched. The scale is that of this step, relative to the one before it, as touch recognizers
    /// report it: more than one when the fingers move apart (zoom in), less than one when they move together.</summary>
    /// <param name="scale">This step's scale relative to the last.</param>
    /// <param name="originX">The point between the fingers, which stays where it is.</param>
    /// <param name="originY">The point between the fingers.</param>
    public void Pinched(double scale, double originX, double originY)
    {
        if (!double.IsFinite(scale) || scale <= 0) return;

        _pinched += Math.Log(scale);
        while (_pinched >= LogNotchIn)
        {
            host.HandleScroll(new ScrollInputArgs(originX, originY, 0, -1));
            _pinched -= LogNotchIn;
        }

        while (_pinched <= -LogNotchOut)
        {
            host.HandleScroll(new ScrollInputArgs(originX, originY, 0, 1));
            _pinched += LogNotchOut;
        }
    }

    /// <summary>The fingers lifted: what was left over of the pinch, less than a notch, is dropped.</summary>
    public void PinchEnded() => _pinched = 0;

    /// <summary>A tap: a click, which pins the data point under it or toggles the legend entry.</summary>
    /// <param name="x">Where the finger touched.</param>
    /// <param name="y">Where the finger touched.</param>
    public void Tapped(double x, double y) => Click(x, y, 1);

    /// <summary>A double tap: Home, the view the chart started from.</summary>
    /// <param name="x">Where the finger touched.</param>
    /// <param name="y">Where the finger touched.</param>
    public void DoubleTapped(double x, double y) => Click(x, y, 2);

    /// <summary>A pointer over the chart without pressing, where the platform reports one (a pen, a mouse on a touch
    /// laptop): a hover, which shows the tooltip of the point under it.</summary>
    /// <param name="x">Where the pointer is.</param>
    /// <param name="y">Where the pointer is.</param>
    public void Hovered(double x, double y) =>
        host.HandlePointerMoved(new PointerInputArgs(x, y, PointerButton.None, ModifierKeys.None));

    private void Click(double x, double y, int count)
    {
        var press = new PointerInputArgs(x, y, PointerButton.Left, ModifierKeys.None, count);
        host.HandlePointerPressed(press);
        host.HandlePointerReleased(press);
    }

    /// <summary>The mouse input a drag of the given action stands for: the button and the keys held at that pixel.</summary>
    private static PointerInputArgs MouseFor(DragGestureAction action, double x, double y) => action switch
    {
        DragGestureAction.RectangleZoom => new PointerInputArgs(x, y, PointerButton.Left, ModifierKeys.Ctrl),
        DragGestureAction.BrushSelect => new PointerInputArgs(x, y, PointerButton.Left, ModifierKeys.Shift),
        DragGestureAction.SpanSelect => new PointerInputArgs(x, y, PointerButton.Left, ModifierKeys.Alt),
        DragGestureAction.Rotate => new PointerInputArgs(x, y, PointerButton.Right, ModifierKeys.None),
        _ => new PointerInputArgs(x, y, PointerButton.Left, ModifierKeys.None),
    };
}
