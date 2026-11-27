// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Sends a <see cref="ResetEvent"/> for an axes on a double-click and for every axes on the <c>Home</c> or
/// <c>Escape</c> key. The event takes the axes back to the view the figure remembers from before the reader first moved
/// it; the range the event carries is read from the layout when the key is pressed, and is used only for an axes the
/// figure remembers nothing for.</summary>
public sealed class HomeModifier : InteractionModifier
{
    /// <summary>Creates the modifier for the given chart.</summary>
    public HomeModifier(string chartId, IChartLayout layout, Action<FigureInteractionEvent> sink)
        : base(chartId, layout, sink)
    {
    }

    /// <inheritdoc />
    public override bool HandlesPointerPressed(PointerInputArgs args) =>
        args.ClickCount >= 2 && Layout.IsOnAxes(args.X, args.Y);

    /// <inheritdoc />
    public override void OnPointerPressed(PointerInputArgs args)
    {
        EmitReset(Layout.AxesOrFirstAt(args.X, args.Y));
    }

    /// <inheritdoc />
    public override bool HandlesKeyDown(KeyInputArgs args) =>
        args.Key is "Home" or "Escape";

    /// <inheritdoc />
    public override void OnKeyDown(KeyInputArgs args)
    {
        for (int i = 0; i < Layout.AxesCount; i++)
            EmitReset(i);
    }

    private void EmitReset(int axesIndex)
    {
        if ((uint)axesIndex >= (uint)Layout.AxesCount) return;

        var range = Layout.GetDataRange(axesIndex);
        Emit(new ResetEvent(ChartId, axesIndex, range.XMin, range.XMax, range.YMin, range.YMax)
        {
            SecondaryRanges = Layout.GetSecondaryDataRanges(axesIndex),
        });
    }
}
