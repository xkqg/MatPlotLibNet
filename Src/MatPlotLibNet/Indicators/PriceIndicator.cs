// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators;

/// <summary>Base class for indicators that operate on a single price array.</summary>
/// <typeparam name="TResult">The typed computation result.</typeparam>
/// <remarks>Stores the price data once and provides the PriceSource resolution constructor.
/// Plotting helpers MakeX/PlotSignal are inherited from <see cref="Indicator"/>.</remarks>
public abstract class PriceIndicator<TResult> : Indicator<TResult>
    where TResult : IIndicatorResult
{
    protected double[] Prices { get; }

    /// <summary>Creates a new price indicator from a price array.</summary>
    protected PriceIndicator(double[] prices)
    {
        Prices = prices;
    }

    /// <summary>Creates a new price indicator from OHLC data with a selectable price source.</summary>
    protected PriceIndicator(double[] open, double[] high, double[] low, double[] close,
                             PriceSource source = PriceSource.Close)
        : this(PriceSources.Resolve(source, open, high, low, close)) { }
}
