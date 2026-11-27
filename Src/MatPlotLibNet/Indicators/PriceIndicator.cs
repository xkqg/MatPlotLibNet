// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using MatPlotLibNet.Indicators.Steps;

namespace MatPlotLibNet.Indicators;

/// <summary>Base class for indicators that operate on a single price array.</summary>
/// <typeparam name="TResult">The typed computation result.</typeparam>
/// <remarks>Stores the price data once and provides the PriceSource resolution constructor.
/// Plotting helpers MakeX/PlotSignal are inherited from <see cref="Indicator"/>.
/// Excluded from coverage: concrete subclasses (Sma / Ema / Rsi / MamaFama / etc.) cover all
/// reachable production paths. The generic open-type body has an unmeasurable empty-method
/// line that disappears under JIT specialisation.</remarks>
[ExcludeFromCodeCoverage]
public abstract class PriceIndicator<TResult> : Indicator<TResult>
    where TResult : IIndicatorResult
{
    /// <summary>The single price array the indicator is computed from: the array given to the constructor, or the price source picked out of the OHLC data.</summary>
    protected double[] Prices { get; }

    /// <inheritdoc />
    private protected override int BarsIn => Prices.Length;

    /// <summary>Creates a new price indicator from a price array.</summary>
    protected PriceIndicator(double[] prices) : this(prices, IndicatorSpec.None) { }

    /// <summary>Creates a new price indicator from OHLC data with a selectable price source.</summary>
    protected PriceIndicator(double[] open, double[] high, double[] low, double[] close,
                             PriceSource source = PriceSource.Close)
        : this(PriceSources.Resolve(source, open, high, low, close)) { }

    /// <summary>An indicator of the library over a price array, named and placed by its spec.</summary>
    /// <param name="prices">The prices.</param>
    /// <param name="spec">What the batch class's <c>Spec</c> factory said of the parameters.</param>
    private protected PriceIndicator(double[] prices, IndicatorSpec spec) : base(spec)
    {
        Prices = prices;
    }

    /// <summary>The values of a step that reads one price a bar, from the indicator's first value on
    /// (<see cref="Indicator{TResult}.FoldBars{TFold, TOut, TLines}"/>).</summary>
    /// <typeparam name="TStep">The step.</typeparam>
    /// <param name="step">A new step.</param>
    /// <returns>One value per bar from the first value on.</returns>
    private protected double[] FoldPrices<TStep>(TStep step) where TStep : struct, IPriceStep<double> =>
        FoldPrices<TStep, double, ValueLine>(step, new ValueLine(ValueCount)).Values;

    /// <summary>The lines of a step that reads one price a bar and says a point of several lines, from the indicator's first value on.</summary>
    /// <typeparam name="TStep">The step.</typeparam>
    /// <typeparam name="TOut">The point it says.</typeparam>
    /// <typeparam name="TLines">The lines the points are written to.</typeparam>
    /// <param name="step">A new step.</param>
    /// <param name="lines">The lines, made for <see cref="Indicator{TResult}.ValueCount"/> entries.</param>
    /// <returns>The lines, written.</returns>
    private protected TLines FoldPrices<TStep, TOut, TLines>(TStep step, TLines lines)
        where TStep : struct, IPriceStep<TOut>
        where TLines : struct, IOutputLines<TOut> =>
        FoldBars<PriceFold<TStep, TOut>, TOut, TLines>(new(Prices, step), lines);

    /// <summary>A step over the prices: the step takes the price of each bar.</summary>
    private struct PriceFold<TStep, TOut>(double[] prices, TStep step) : IBarFold<TOut> where TStep : struct, IPriceStep<TOut>
    {
        private TStep _step = step;

        /// <inheritdoc />
        public TOut Next(int bar) => _step.Next(prices[bar]);
    }
}
