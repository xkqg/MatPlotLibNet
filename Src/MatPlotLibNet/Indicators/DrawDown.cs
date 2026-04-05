// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators;

/// <summary>Drawdown indicator. Shows the percentage decline from peak equity as a filled area below zero.</summary>
/// <remarks>Drawdown is always non-negative (0% = at peak, 20% = 20% below peak).
/// Best placed in a separate subplot. The area is filled from the drawdown line down to zero.</remarks>
public sealed class DrawDown : Indicator
{
    private readonly double[] _equity;

    /// <summary>Gets or sets the fill opacity for the drawdown area.</summary>
    public double Alpha { get; set; } = 0.4;

    /// <summary>Creates a new drawdown indicator from an equity curve.</summary>
    /// <param name="equity">The equity values over time (e.g., from <see cref="EquityCurve.Compute"/>).</param>
    public DrawDown(double[] equity)
    {
        _equity = equity;
        Label = "Drawdown";
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        var dd = Compute(_equity);
        var x = new double[dd.Length];
        for (int i = 0; i < dd.Length; i++) x[i] = i;

        // Negate for visual (drawdown plots below zero)
        var negDd = new double[dd.Length];
        for (int i = 0; i < dd.Length; i++) negDd[i] = -dd[i];

        var series = axes.FillBetween(x, negDd);
        series.Label = Label;
        series.Color = Color ?? Styling.Color.Red;
        series.Alpha = Alpha;

        axes.YAxis.Max = 0;
    }

    /// <summary>Computes the percentage drawdown from peak at each point.</summary>
    /// <returns>Array of non-negative drawdown percentages (0 = at peak).</returns>
    public static double[] Compute(double[] equity)
    {
        var dd = new double[equity.Length];
        double peak = equity[0];
        for (int i = 0; i < equity.Length; i++)
        {
            if (equity[i] > peak) peak = equity[i];
            dd[i] = peak > 0 ? (peak - equity[i]) / peak * 100 : 0;
        }
        return dd;
    }
}
