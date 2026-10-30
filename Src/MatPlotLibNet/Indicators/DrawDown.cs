// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators;

/// <summary>Drawdown indicator. Shows the percentage decline from peak equity as a filled area below zero.</summary>
/// <remarks>Drawdown is always non-negative (0% = at peak, 20% = 20% below peak).
/// Best placed in a separate subplot. The area is filled from the drawdown line down to zero.</remarks>
public sealed class DrawDown : Indicator<SignalResult>
{
    private readonly double[] _equity;

    /// <summary>The opacity of the filled drawdown area, from 0 (invisible) to 1 (solid). Default 0.4.</summary>
    public double Alpha { get; set; } = 0.4;

    /// <summary>Creates a new drawdown indicator from an equity curve.</summary>
    /// <param name="equity">The equity values over time (e.g., from <see cref="EquityCurve.Compute"/>).</param>
    public DrawDown(double[] equity)
    {
        _equity = equity;
        Label = "Drawdown";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        var dd = new double[_equity.Length];
        var step = new DrawDownStep();
        for (int i = 0; i < _equity.Length; i++)
            dd[i] = step.Next(_equity[i]);
        return dd;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        double[] dd = Compute();
        var x = MakeX(dd.Length, 0);
        // Negate for visual (drawdown plots below zero)
        var negDd = new double[dd.Length];
        VectorMath.Negate(dd, negDd);

        var series = axes.FillBetween(x, negDd);
        series.Label = Label;
        series.Color = Color ?? Colors.Red;
        series.Alpha = Alpha;

        axes.YAxis.Max = 0;
    }
}
