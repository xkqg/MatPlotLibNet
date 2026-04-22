// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Tools;

/// <summary>A single level within a <see cref="FibonacciRetracement"/>.</summary>
/// <param name="Ratio">Retracement ratio in [0, 1] (e.g. 0.236 = 23.6%).</param>
/// <param name="Price">Computed price at this level.</param>
public sealed record FibonacciLevel(double Ratio, double Price);

/// <summary>Represents a Fibonacci retracement overlay drawn between a price high and a price low.</summary>
public sealed class FibonacciRetracement
{
    private static readonly double[] Ratios = [0.0, 0.236, 0.382, 0.5, 0.618, 0.786, 1.0];

    public double PriceHigh { get; }
    public double PriceLow { get; }

    public Color? Color { get; set; }
    public double LineWidth { get; set; } = 1.0;
    public bool ShowLabels { get; set; } = true;

    /// <summary>The seven computed retracement levels (0 %, 23.6 %, 38.2 %, 50 %, 61.8 %, 78.6 %, 100 %).</summary>
    public IReadOnlyList<FibonacciLevel> Levels { get; }

    public FibonacciRetracement(double priceHigh, double priceLow)
    {
        PriceHigh = priceHigh;
        PriceLow = priceLow;
        var range = priceHigh - priceLow;
        Levels = Array.ConvertAll(Ratios, r => new FibonacciLevel(r, priceHigh - r * range));
    }
}
