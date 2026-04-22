// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Tools;
using static MatPlotLibNet.Interaction.InteractionToolbar;

namespace MatPlotLibNet.Interaction;

/// <summary>Single-click level drawing: click anywhere on the axes to place a
/// <see cref="HorizontalLevel"/> at that Y value. Active only when the toolbar is
/// set to <see cref="ToolMode.Level"/>.</summary>
public sealed class LevelDrawingModifier : IInteractionModifier
{
    private readonly string _chartId;
    private readonly IChartLayout _layout;
    private readonly Figure _figure;
    private readonly Action<FigureInteractionEvent> _sink;
    private readonly InteractionToolbar _toolbar;

    public LevelDrawingModifier(
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
        _toolbar.ActiveTool == ToolMode.Level &&
        args.Button == PointerButton.Left &&
        _layout.HitTestAxes(args.X, args.Y) is not null;

    /// <inheritdoc />
    public void OnPointerPressed(PointerInputArgs args)
    {
        int axesIndex = _layout.HitTestAxes(args.X, args.Y) ?? 0;
        var coords = ((ChartLayout)_layout).PixelToData(args.X, args.Y, axesIndex);
        if (coords is null) return;

        var tool = new HorizontalLevel(coords.Value.DataY);
        _sink(new AddHorizontalLevelEvent(_chartId, axesIndex, tool));
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
