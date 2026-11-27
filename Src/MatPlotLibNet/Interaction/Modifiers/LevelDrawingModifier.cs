// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Tools;
using static MatPlotLibNet.Interaction.InteractionToolbar;

namespace MatPlotLibNet.Interaction;

/// <summary>Single-click level drawing: click anywhere on the axes to place a
/// <see cref="HorizontalLevel"/> at that Y value. Active only when the toolbar is
/// set to <see cref="ToolMode.Level"/>.</summary>
public sealed class LevelDrawingModifier : InteractionModifier
{
    private readonly ToolGate _gate;

    /// <summary>Creates a modifier that draws a horizontal level at the Y value of a click while the level tool is active.</summary>
    /// <param name="chartId">The identifier of the chart that the emitted <see cref="AddHorizontalLevelEvent"/> is addressed to.</param>
    /// <param name="layout">The chart layout used to find the axes under a click and to turn the click into data coordinates.</param>
    /// <param name="sink">Receives the event this modifier emits when a level is placed.</param>
    /// <param name="toolbar">The toolbar that says which tool is active; this modifier acts only while it is <see cref="ToolMode.Level"/>.</param>
    public LevelDrawingModifier(
        string chartId,
        IChartLayout layout,
        Action<FigureInteractionEvent> sink,
        InteractionToolbar toolbar)
        : base(chartId, layout, sink)
    {
        _gate = new ToolGate(toolbar, ToolMode.Level);
    }

    /// <inheritdoc />
    public override bool HandlesPointerPressed(PointerInputArgs args) => _gate.Opens(args, Layout);

    /// <inheritdoc />
    public override void OnPointerPressed(PointerInputArgs args)
    {
        if (Layout.PressAt(args.X, args.Y) is not { } press) return;

        var tool = new HorizontalLevel(press.Data.DataY);
        Emit(new AddHorizontalLevelEvent(ChartId, press.AxesIndex, tool));
    }
}
