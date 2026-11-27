// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Tools;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Serialization;

/// <summary>Data-transfer object for a <see cref="FibonacciRetracement"/>: an entry of an axes' <c>fibonacciRetracements</c>.
/// The colour travels as a hex string, absent when the retracement takes the theme's.</summary>
internal sealed record FibonacciRetracementDto
{
    public double PriceHigh { get; init; }
    public double PriceLow { get; init; }
    public string? Color { get; init; }
    public double LineWidth { get; init; } = 1.0;
    public bool ShowLabels { get; init; } = true;

    /// <summary>The JSON's form of <paramref name="retracement"/>.</summary>
    /// <param name="retracement">The Fibonacci retracement.</param>
    internal static FibonacciRetracementDto From(FibonacciRetracement retracement) => new()
    {
        PriceHigh = retracement.PriceHigh,
        PriceLow = retracement.PriceLow,
        Color = retracement.Color is { } color ? color.ToHex() : null,
        LineWidth = retracement.LineWidth,
        ShowLabels = retracement.ShowLabels,
    };

    /// <summary>Adds the retracement this record describes to <paramref name="axes"/>.</summary>
    /// <param name="axes">The axes being read.</param>
    internal void AddTo(Axes axes)
    {
        var retracement = axes.AddFibonacci(PriceHigh, PriceLow);
        if (Color is { } hex) retracement.Color = Styling.Color.FromHex(hex);
        retracement.LineWidth = LineWidth;
        retracement.ShowLabels = ShowLabels;
    }
}
