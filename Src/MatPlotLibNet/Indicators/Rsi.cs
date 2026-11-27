// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Relative Strength Index indicator. Measures momentum on a 0-100 scale.</summary>
/// <remarks>RSI above 70 is typically considered overbought; below 30 is oversold.
/// Best placed in a separate subplot with <c>AxHLine(70)</c> and <c>AxHLine(30)</c> reference lines.</remarks>
public sealed class Rsi : PriceIndicator<SignalResult>
{
    /// <summary>The <c>period</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultPeriod = 14;

    private readonly int _period;

    /// <summary>Creates a new RSI indicator.</summary>
    /// <param name="prices">The price data (typically close prices).</param>
    /// <param name="period">The lookback period (default <see cref="DefaultPeriod"/>).</param>
    public Rsi(double[] prices, int period = DefaultPeriod) : base(prices, Spec(period))
    {
        _period = period;
    }

    internal static IndicatorSpec Spec(int period) => IndicatorSpec.OfWindow("RSI", period, period);

    /// <inheritdoc />
    public override SignalResult Compute() => FoldPrices(new RsiStep(_period));

    /// <summary>The range the values of the indicator live in: its pane, and the pane of <see cref="Streaming.StreamingRsi"/>, are pinned to it.</summary>
    internal static PaneRange Pane { get; } = PaneRange.Percent;

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        PlotLine(axes, Compute());
        Pane.Pin(axes);
    }
}
