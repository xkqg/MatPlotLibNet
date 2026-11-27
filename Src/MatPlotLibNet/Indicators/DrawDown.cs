// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;
using System.Numerics.Tensors;

namespace MatPlotLibNet.Indicators;

/// <summary>Drawdown indicator. Shows the percentage decline from peak equity as a filled area below zero.</summary>
/// <remarks>Drawdown is always non-negative (0% = at peak, 20% = 20% below peak).
/// Best placed in a separate subplot. The area is filled from the drawdown line down to zero.</remarks>
public sealed class DrawDown : Indicator<SignalResult>
{
    private readonly double[] _equity;

    /// <inheritdoc />
    private protected override int BarsIn => _equity.Length;

    /// <summary>The opacity of the filled drawdown area, from 0 (invisible) to 1 (solid). Default 0.4.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 0, above 1 or not a number.</exception>
    public double Alpha { get; set => field = value.ThrowIfNotOpacity(nameof(Alpha)); } = 0.4;

    /// <summary>Creates a new drawdown indicator from an equity curve.</summary>
    /// <param name="equity">The equity values over time (e.g., from <see cref="EquityCurve.Compute"/>).</param>
    public DrawDown(double[] equity) : base(Spec())
    {
        _equity = equity;
    }

    internal static IndicatorSpec Spec() => IndicatorSpec.Of(0, "Drawdown");

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        var dd = new double[_equity.Length];
        var step = new DrawDownStep();
        for (int i = 0; i < _equity.Length; i++)
            dd[i] = step.Next(_equity[i]);
        return dd;
    }

    /// <summary>The range a drawdown is drawn in: below zero, its depth left to the data. Its streaming form draws the drawdown itself,
    /// above zero, and pins nothing.</summary>
    internal static PaneRange Pane { get; } = PaneRange.AtMost(0);

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        double[] dd = Compute();
        var x = Positions(dd.Length);
        // Negate for visual (drawdown plots below zero)
        var negDd = new double[dd.Length];
        TensorPrimitives.Negate(dd, negDd);

        var series = axes.AddSeries(AreaSeries.Band(x, negDd, null, Alpha));
        series.Label = Label;
        WearColor(series, Color, IndicatorLook.Library.Moves.Down);

        Pane.Pin(axes);
    }
}
