// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;
using System.Numerics.Tensors;

namespace MatPlotLibNet.Indicators;

/// <summary>Bollinger Bands indicator. Plots upper and lower bands at N standard deviations from a moving average.</summary>
public sealed class BollingerBands : PriceIndicator<BandsResult>
{
    /// <summary>The <c>period</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultPeriod = 20;

    /// <summary>The <c>stdDevMultiplier</c> the indicator is made with when the caller gives none.</summary>
    public const double DefaultStdDevMultiplier = 2.0;

    private readonly int _period;
    private readonly double _stdDevMultiplier;

    /// <summary>The opacity of the shaded band between the upper and lower lines, from 0 (invisible) to 1 (solid). Default <see cref="IndicatorLook.BandOpacity"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 0, above 1 or not a number.</exception>
    public double Alpha { get; set => field = value.ThrowIfNotOpacity(nameof(Alpha)); } = IndicatorLook.BandOpacity;

    /// <summary>Creates a new Bollinger Bands indicator from a price array.</summary>
    public BollingerBands(double[] prices, int period = DefaultPeriod, double stdDevMultiplier = DefaultStdDevMultiplier) : base(prices, Spec(period, stdDevMultiplier))
    {
        _period = period;
        _stdDevMultiplier = stdDevMultiplier;
    }

    internal static IndicatorSpec Spec(int period, double stdDevMultiplier)
    {
        string label = string.Invariant($"BB({period},{stdDevMultiplier.InvariantText()})");
        return IndicatorSpec.Of(period - 1, label, label, $"{label} Upper", $"{label} Lower");
    }

    /// <inheritdoc />
    public override BandsResult Compute()
    {
        double[] sma = new Sma(Prices, _period).Compute();
        int n = sma.Length;
        var stdDev = new double[n];
        Prices.RollingStdDev(_period, sma, stdDev);
        // upper = sma + mult*stdDev;  lower = sma - mult*stdDev
        var scaledStd = new double[n];
        TensorPrimitives.Multiply(stdDev, _stdDevMultiplier, scaledStd);
        var upper = new double[n];
        var lower = new double[n];
        TensorPrimitives.Add(sma, scaledStd, upper);
        TensorPrimitives.Subtract(sma, scaledStd, lower);
        return new BandsResult(sma, upper, lower);
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotBand(axes, Compute(), Alpha);
}
