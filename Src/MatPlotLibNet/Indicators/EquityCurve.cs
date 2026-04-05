// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Equity curve indicator. Plots cumulative P&amp;L as a line starting from initial capital.</summary>
/// <remarks>Best placed in a separate subplot below the price chart to show portfolio value over time.</remarks>
public sealed class EquityCurve : Indicator
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
    public override void Apply(Axes axes)
    {
        var equity = Compute(_returns, _startingCapital);
        var x = new double[equity.Length];
        for (int i = 0; i < equity.Length; i++) x[i] = i;
        var series = axes.Plot(x, equity);
        series.Label = Label;
        if (Color.HasValue) series.Color = Color.Value;
        series.LineWidth = LineWidth;
    }

    /// <summary>Computes the cumulative equity curve.</summary>
    /// <returns>Array of length <c>returns.Length + 1</c> starting at <paramref name="startingCapital"/>.</returns>
    public static double[] Compute(double[] returns, double startingCapital = 10000)
    {
        var equity = new double[returns.Length + 1];
        equity[0] = startingCapital;
        for (int i = 0; i < returns.Length; i++)
            equity[i + 1] = equity[i] + returns[i];
        return equity;
    }
}
