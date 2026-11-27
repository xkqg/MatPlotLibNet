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
public sealed class FibonacciDrawingModifier : InteractionModifier
{
    private readonly ToolGate _gate;

    private bool _waitingForSecondClick;
    private int _axesIndex;
    private double _firstY;

    /// <summary>Creates a modifier that draws a Fibonacci retracement from two clicks while the Fibonacci tool is active.</summary>
    /// <param name="chartId">The identifier of the chart that the emitted <see cref="AddFibonacciRetracementEvent"/> is addressed to.</param>
    /// <param name="layout">The chart layout used to find the axes under a click and to turn the click into data coordinates.</param>
    /// <param name="sink">Receives the event this modifier emits when the second click completes the retracement.</param>
    /// <param name="toolbar">The toolbar that says which tool is active; this modifier acts only while it is <see cref="ToolMode.Fibonacci"/>.</param>
    public FibonacciDrawingModifier(
        string chartId,
        IChartLayout layout,
        Action<FigureInteractionEvent> sink,
        InteractionToolbar toolbar)
        : base(chartId, layout, sink)
    {
        _gate = new ToolGate(toolbar, ToolMode.Fibonacci);
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
            _firstY = press.Data.DataY;
            _waitingForSecondClick = true;
        }
        else
        {
            _waitingForSecondClick = false;
            double secondY = press.Data.DataY;
            double high = Math.Max(_firstY, secondY);
            double low  = Math.Min(_firstY, secondY);
            var tool = new FibonacciRetracement(priceHigh: high, priceLow: low);
            Emit(new AddFibonacciRetracementEvent(ChartId, _axesIndex, tool));
        }
    }
}
