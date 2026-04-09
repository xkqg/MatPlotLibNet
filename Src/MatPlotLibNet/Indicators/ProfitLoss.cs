// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators;

/// <summary>Profit/Loss indicator. Displays per-trade or per-period returns as colored bars.</summary>
/// <remarks>Positive returns are shown in green, negative in red. Best placed in a separate subplot.</remarks>
public sealed class ProfitLoss : Indicator<SignalResult>
{
    private readonly double[] _returns;
    private readonly string[]? _labels;

    /// <summary>Gets or sets the color for profitable periods.</summary>
    public Color ProfitColor { get; set; } = Colors.Green;

    /// <summary>Gets or sets the color for losing periods.</summary>
    public Color LossColor { get; set; } = Colors.Red;

    /// <summary>Creates a new P&amp;L indicator.</summary>
    /// <param name="returns">Per-period returns (positive = profit, negative = loss).</param>
    /// <param name="labels">Optional labels for each period.</param>
    public ProfitLoss(double[] returns, string[]? labels = null)
    {
        _returns = returns;
        _labels = labels;
        Label = "P&L";
    }

    /// <inheritdoc />
    public override SignalResult Compute() => new(_returns);

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        var categories = _labels ?? new string[_returns.Length];
        if (_labels is null)
            for (int i = 0; i < _returns.Length; i++) categories[i] = i.ToString();

        var profits = new double[_returns.Length];
        var losses = new double[_returns.Length];
        VectorMath.SplitPositiveNegative(_returns, profits, losses);

        var profitSeries = axes.Bar(categories, profits);
        profitSeries.Color = ProfitColor;
        profitSeries.Label = "Profit";

        var lossSeries = axes.Bar(categories, losses);
        lossSeries.Color = LossColor;
        lossSeries.Label = "Loss";

        axes.YAxis.Min = _returns.Min() * 1.1;
    }
}
