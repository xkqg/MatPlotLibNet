// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Equity curve indicator. Plots cumulative P&amp;L as a line starting from initial capital.</summary>
/// <remarks>Best placed in a separate subplot below the price chart to show portfolio value over time.</remarks>
public sealed class EquityCurve : Indicator<SignalResult>
{
    private readonly double[] _returns;
    private readonly double _startingCapital;

    /// <summary>Creates a new equity curve indicator.</summary>
    /// <param name="tradeReturns">Per-period returns (positive = profit, negative = loss).</param>
    /// <param name="startingCapital">Initial capital (default 10000).</param>
    public EquityCurve(double[] tradeReturns, double startingCapital = 10000)
    {
        _returns = tradeReturns;
        _startingCapital = startingCapital;
        Label = "Equity";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        var equity = new double[_returns.Length + 1];
        equity[0] = _startingCapital;
        var step = new EquityCurveStep(_startingCapital);
        for (int i = 0; i < _returns.Length; i++)
            equity[i + 1] = step.Next(_returns[i]);
        return equity;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), 0);
}
