// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Tools;
using static MatPlotLibNet.Interaction.InteractionToolbar;

namespace MatPlotLibNet.Interaction;

/// <summary>Two-click Fibonacci retracement drawing: first click anchors the price high,
/// second click anchors the price low and emits an <see cref="AddFibonacciRetracementEvent"/>.
/// The modifier swaps high/low automatically so high is always ≥ low.
/// Active only when the toolbar is set to <see cref="ToolMode.Fibonacci"/>.</summary>
public sealed class FibonacciDrawingModifier : IInteractionModifier
{
    private readonly string _chartId;
    private readonly IChartLayout _layout;
    private readonly Figure _figure;
    private readonly Action<FigureInteractionEvent> _sink;
    private readonly InteractionToolbar _toolbar;

    private bool _waitingForSecondClick;
    private int _axesIndex;
    private double _firstY;

    /// <summary>Creates a modifier that draws a Fibonacci retracement from two clicks while the Fibonacci tool is active.</summary>
    /// <param name="chartId">The identifier of the chart that the emitted <see cref="AddFibonacciRetracementEvent"/> is addressed to.</param>
    /// <param name="layout">The chart layout used to find the axes under a click and to turn the click into data coordinates.</param>
    /// <param name="figure">The figure being interacted with.</param>
    /// <param name="sink">Receives the event this modifier emits when the second click completes the retracement.</param>
    /// <param name="toolbar">The toolbar that says which tool is active; this modifier acts only while it is <see cref="ToolMode.Fibonacci"/>.</param>
    public FibonacciDrawingModifier(
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
        _toolbar.ActiveTool == ToolMode.Fibonacci &&
        args.Button == PointerButton.Left &&
        _layout.HitTestAxes(args.X, args.Y) is not null;

    /// <inheritdoc />
    public void OnPointerPressed(PointerInputArgs args)
    {
        int axesIndex = _layout.HitTestAxes(args.X, args.Y) ?? 0;
        var coords = _layout.PixelToData(args.X, args.Y, axesIndex);
        if (coords is null) return;

        if (!_waitingForSecondClick)
        {
            _axesIndex = axesIndex;
            _firstY = coords.Value.DataY;
            _waitingForSecondClick = true;
        }
        else
        {
            _waitingForSecondClick = false;
            double secondY = coords.Value.DataY;
            double high = Math.Max(_firstY, secondY);
            double low  = Math.Min(_firstY, secondY);
            var tool = new FibonacciRetracement(priceHigh: high, priceLow: low);
            _sink(new AddFibonacciRetracementEvent(_chartId, _axesIndex, tool));
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
