// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Tools;
using static MatPlotLibNet.Interaction.InteractionToolbar;

namespace MatPlotLibNet.Interaction;

/// <summary>Two-click trendline drawing: first click anchors p1, second click completes
/// the trendline and emits an <see cref="AddTrendlineEvent"/>. Active only when the
/// toolbar is set to <see cref="ToolMode.Trendline"/>.</summary>
public sealed class TrendlineDrawingModifier : InteractionModifier
{
    private readonly ToolGate _gate;

    private bool _waitingForSecondClick;
    private int _axesIndex;
    private double _x1, _y1;

    /// <summary>Creates a modifier that draws a trendline from two clicks while the trendline tool is active.</summary>
    /// <param name="chartId">The identifier of the chart that the emitted <see cref="AddTrendlineEvent"/> is addressed to.</param>
    /// <param name="layout">The chart layout used to find the axes under a click and to turn the click into data coordinates.</param>
    /// <param name="sink">Receives the event this modifier emits when the second click completes the trendline.</param>
    /// <param name="toolbar">The toolbar that says which tool is active; this modifier acts only while it is <see cref="ToolMode.Trendline"/>.</param>
    public TrendlineDrawingModifier(
        string chartId,
        IChartLayout layout,
        Action<FigureInteractionEvent> sink,
        InteractionToolbar toolbar)
        : base(chartId, layout, sink)
    {
        _gate = new ToolGate(toolbar, ToolMode.Trendline);
    }

    /// <inheritdoc />
    public override bool HandlesPointerPressed(PointerInputArgs args) => _gate.Opens(args, Layout);

    /// <inheritdoc />
    public override void OnPointerPressed(PointerInputArgs args)
    {
        if (Layout.PressAt(args.X, args.Y) is not { } press) return;

        if (!_waitingForSecondClick)
        {
            _axesIndex = press.AxesIndex;
            _x1 = press.Data.DataX;
            _y1 = press.Data.DataY;
            _waitingForSecondClick = true;
        }
        else
        {
            _waitingForSecondClick = false;
            var tool = new Trendline(_x1, _y1, press.Data.DataX, press.Data.DataY);
            Emit(new AddTrendlineEvent(ChartId, _axesIndex, tool));
        }
    }
}
