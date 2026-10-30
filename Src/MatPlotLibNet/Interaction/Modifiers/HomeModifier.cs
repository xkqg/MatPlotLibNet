// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Sends a <see cref="ResetEvent"/> for an axes on a double-click and for every axes on the <c>Home</c> or
/// <c>Escape</c> key. The event takes the axes back to the view the figure remembers from before the reader first moved
/// it; the range the event carries is read from the layout when the key is pressed, and is used only for an axes the
/// figure remembers nothing for.</summary>
internal sealed class HomeModifier : IInteractionModifier
{
    private readonly string _chartId;
    private readonly IChartLayout _layout;
    private readonly Action<FigureInteractionEvent> _sink;

    /// <summary>Creates the modifier for the given chart.</summary>
    internal HomeModifier(string chartId, IChartLayout layout, Action<FigureInteractionEvent> sink)
    {
        _chartId = chartId;
        _layout  = layout;
        _sink    = sink;
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
        for (int i = 0; i < _layout.AxesCount; i++)
            EmitReset(i);
    }

    private void EmitReset(int axesIndex)
    {
        if ((uint)axesIndex >= (uint)_layout.AxesCount) return;

        var range = _layout.GetDataRange(axesIndex);
        _sink(new ResetEvent(_chartId, axesIndex, range.XMin, range.XMax, range.YMin, range.YMax)
        {
            SecondaryRanges = _layout.GetSecondaryDataRanges(axesIndex),
        });
    }
}
