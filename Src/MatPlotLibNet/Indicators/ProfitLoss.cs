// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;
using System.Numerics.Tensors;

namespace MatPlotLibNet.Indicators;

/// <summary>Profit/Loss indicator. Displays per-trade or per-period returns as colored bars.</summary>
/// <remarks>Positive returns are shown in green, negative in red. Best placed in a separate subplot.</remarks>
public sealed class ProfitLoss : Indicator<SignalResult>
{
    private readonly double[] _returns;
    private readonly string[]? _labels;

    /// <summary>The colour of the bars for periods with a positive return. Until one is given it reads green, and the bars are drawn
    /// green under the library's own themes and in the next colour of the cycle under the matplotlib themes, as matplotlib draws a bar
    /// that names no colour; a colour that is given is drawn as given, green included.</summary>
    public Color ProfitColor { get => GivenProfitColor ?? IndicatorLook.Library.Moves.Up; set => GivenProfitColor = value; }

    /// <summary>The value of <see cref="ProfitColor"/> its author gave, <see langword="null"/> while none was.</summary>
    internal Color? GivenProfitColor { get; private set; }

    /// <summary>The colour of the bars for periods with a negative return. Until one is given it reads red, and the bars are drawn red
    /// under the library's own themes and in the next colour of the cycle under the matplotlib themes; a colour that is given is drawn
    /// as given.</summary>
    public Color LossColor { get => GivenLossColor ?? IndicatorLook.Library.Moves.Down; set => GivenLossColor = value; }

    /// <summary>The value of <see cref="LossColor"/> its author gave, <see langword="null"/> while none was.</summary>
    internal Color? GivenLossColor { get; private set; }

    /// <summary>Creates a new P&amp;L indicator.</summary>
    /// <param name="returns">Per-period returns (positive = profit, negative = loss).</param>
    /// <param name="labels">Optional labels for each period.</param>
    public ProfitLoss(double[] returns, string[]? labels = null) : base(Spec())
    {
        _returns = returns;
        _labels = labels;
    }

    internal static IndicatorSpec Spec() => IndicatorSpec.Of(0, "P&L");

    /// <inheritdoc />
    public override SignalResult Compute() => new(_returns);

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        var categories = _labels ?? new string[_returns.Length];
        if (_labels is null)
            for (int i = 0; i < _returns.Length; i++) categories[i] = i.InvariantText();

        var profits = new double[_returns.Length];
        var losses = new double[_returns.Length];
        TensorPrimitives.Max(_returns, 0.0, profits);
        TensorPrimitives.Min(_returns, 0.0, losses);

        var profitSeries = axes.Bar(categories, profits);
        WearColor(profitSeries, GivenProfitColor, ProfitColor);
        profitSeries.Label = "Profit";

        var lossSeries = axes.Bar(categories, losses);
        WearColor(lossSeries, GivenLossColor, LossColor);
        lossSeries.Label = "Loss";

        // The pane reaches a tenth below the deepest loss; its top is left to the data.
        PaneRange.AtLeast(_returns.Min() * 1.1).Pin(axes);
    }
}
