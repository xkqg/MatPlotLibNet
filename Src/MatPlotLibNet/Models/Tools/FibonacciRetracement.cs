// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Tools;

/// <summary>A single level within a <see cref="FibonacciRetracement"/>.</summary>
/// <param name="Ratio">Retracement ratio in [0, 1] (e.g. 0.236 = 23.6%).</param>
/// <param name="Price">Computed price at this level.</param>
public sealed record FibonacciLevel(double Ratio, double Price);

/// <summary>Represents a Fibonacci retracement overlay drawn between a price high and a price low.</summary>
public sealed class FibonacciRetracement : IChangeTracked, IDrawingTool
{
    private long _stamp;

    /// <summary>Stores <paramref name="value"/> and marks this retracement changed, unless it already holds an equal value.</summary>
    private void Change<T>(ref T slot, T value) => ChangeClock.Change(ref slot, value, ref _stamp);

    long IChangeTracked.LatestChange => _stamp;

    /// <summary>The ratios, 0 % at the high to 100 % at the low: the one table of the levels and of the Fibonacci indicator's lines.</summary>
    internal static readonly double[] Ratios = [0.0, 0.236, 0.382, 0.5, 0.618, 0.786, 1.0];

    /// <summary>The price a ratio of a move stands at (0 at the high, 1 at the low): the high less that ratio of the move.</summary>
    internal static double PriceAt(double ratio, double priceHigh, double priceLow) => priceHigh - ratio * (priceHigh - priceLow);

    /// <summary>The price at the top of the retracement, where the 0 % level stands.</summary>
    public double PriceHigh { get; }
    /// <summary>The price at the bottom of the retracement, where the 100 % level stands.</summary>
    public double PriceLow { get; }

    /// <summary>The colour of the level lines and their labels. <see langword="null"/> (the default) takes the theme's
    /// neutral colour.</summary>
    public Color? Color { get; set => Change(ref field, value); }
    /// <summary>The width of the level lines, in pixels. Default 1; the lines are always dashed.</summary>
    public double LineWidth { get; set => Change(ref field, value); } = 1.0;

    /// <summary>The level lines are always dashed.</summary>
    LineStyle IStrokedTool.LineStyle => LineStyle.Dashed;
    /// <summary>Whether each level line carries a label with its percentage and price at the right end. On by default.</summary>
    public bool ShowLabels { get; set => Change(ref field, value); } = true;

    /// <summary>The seven computed retracement levels (0 %, 23.6 %, 38.2 %, 50 %, 61.8 %, 78.6 %, 100 %).</summary>
    public IReadOnlyList<FibonacciLevel> Levels { get; }

    /// <summary>Creates a retracement between a high and a low price and computes its seven levels.</summary>
    /// <param name="priceHigh">The price at the top of the move, where the 0 % level stands.</param>
    /// <param name="priceLow">The price at the bottom of the move, where the 100 % level stands.</param>
    public FibonacciRetracement(double priceHigh, double priceLow)
    {
        PriceHigh = priceHigh;
        PriceLow = priceLow;
        Levels = Array.ConvertAll(Ratios, r => new FibonacciLevel(r, PriceAt(r, priceHigh, priceLow)));
    }

    void IDrawingTool.AddTo(AxesOverlays overlays) => overlays.FibonacciRetracements.Add(this);
    void IDrawingTool.RemoveFrom(AxesOverlays overlays) => overlays.FibonacciRetracements.Remove(this);
}
