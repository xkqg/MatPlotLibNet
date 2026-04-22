// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Tools;
using static MatPlotLibNet.Interaction.InteractionToolbar;

namespace MatPlotLibNet.Interaction;

/// <summary>Two-click trendline drawing: first click anchors p1, second click completes
/// the trendline and emits an <see cref="AddTrendlineEvent"/>. Active only when the
/// toolbar is set to <see cref="ToolMode.Trendline"/>.</summary>
public sealed class TrendlineDrawingModifier : IInteractionModifier
{
    private readonly string _chartId;
    private readonly IChartLayout _layout;
    private readonly Figure _figure;
    private readonly Action<FigureInteractionEvent> _sink;
    private readonly InteractionToolbar _toolbar;

    private bool _waitingForSecondClick;
    private int _axesIndex;
    private double _x1, _y1;

    public TrendlineDrawingModifier(
        string chartId,
        IChartLayout layout,
        Figure figure,
        Action<FigureInteractionEvent> sink,
        InteractionToolbar toolbar)
    {
        _chartId = chartId;
        _layout  = layout;
        _figure  = figure;
        _sink    = sink;
        _toolbar = toolbar;
    }

    /// <inheritdoc />
    public bool HandlesPointerPressed(PointerInputArgs args) =>
        _toolbar.ActiveTool == ToolMode.Trendline &&
        args.Button == PointerButton.Left &&
        _layout.HitTestAxes(args.X, args.Y) is not null;

    /// <inheritdoc />
    public void OnPointerPressed(PointerInputArgs args)
    {
        int axesIndex = _layout.HitTestAxes(args.X, args.Y) ?? 0;
        var coords = ((ChartLayout)_layout).PixelToData(args.X, args.Y, axesIndex);
        if (coords is null) return;

        if (!_waitingForSecondClick)
        {
            _axesIndex = axesIndex;
            _x1 = coords.Value.DataX;
            _y1 = coords.Value.DataY;
            _waitingForSecondClick = true;
        }
        else
        {
            _waitingForSecondClick = false;
            var tool = new Trendline(_x1, _y1, coords.Value.DataX, coords.Value.DataY);
            _sink(new AddTrendlineEvent(_chartId, _axesIndex, tool));
        }
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
    public bool HandlesKeyDown(KeyInputArgs args) => false;
    /// <inheritdoc />
    public void OnKeyDown(KeyInputArgs args) { }
}
