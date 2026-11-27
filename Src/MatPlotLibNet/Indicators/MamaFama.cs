// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Ehlers' MESA Adaptive Moving Average (MAMA) and its trailing companion (FAMA).
/// Uses the Hilbert transform to extract the instantaneous dominant cycle period
/// and adapts the EMA smoothing factor to it. Reference: Ehlers (2001),
/// <i>Rocket Science for Traders</i>, Ch. 15; canonical cross-check: TA-Lib <c>ta_MAMA.c</c>.</summary>
public sealed class MamaFama : PriceIndicator<MamaFamaResult>
{
    /// <summary>The <c>fastLimit</c> the indicator is made with when the caller gives none.</summary>
    public const double DefaultFastLimit = 0.5;

    /// <summary>The <c>slowLimit</c> the indicator is made with when the caller gives none.</summary>
    public const double DefaultSlowLimit = 0.05;

    private readonly double _fastLimit;
    private readonly double _slowLimit;

    /// <summary>Creates a new MAMA/FAMA indicator.</summary>
    /// <param name="prices">Price series (typically close, or median price for OHLC input).</param>
    /// <param name="fastLimit">Upper alpha bound in (0, 1]. Default <see cref="DefaultFastLimit"/>.</param>
    /// <param name="slowLimit">Lower alpha bound in (0, fastLimit). Default <see cref="DefaultSlowLimit"/>.</param>
    public MamaFama(double[] prices, double fastLimit = DefaultFastLimit, double slowLimit = DefaultSlowLimit) : base(prices, Spec(fastLimit, slowLimit))
    {
        _fastLimit = fastLimit;
        _slowLimit = slowLimit;
    }

    internal static IndicatorSpec Spec(double fastLimit, double slowLimit)
    {
        fastLimit.ThrowIfNot(fastLimit is > 0 and <= 1, nameof(fastLimit), "must be in (0, 1]");
        slowLimit.ThrowIfNotBetween(0, fastLimit, nameof(slowLimit));
        string label = $"MAMA({fastLimit.InvariantText("0.00")}/{slowLimit.InvariantText("0.00")})";
        return IndicatorSpec.Of(6, label, label, "FAMA");
    }

    /// <inheritdoc />
    public override MamaFamaResult Compute() =>
        FoldPrices<MamaFamaStep, MamaFamaPoint, Lines>(new(_fastLimit, _slowLimit), new(ValueCount)).Result;

    /// <summary>MAMA and FAMA, one entry per bar from the first value on.</summary>
    private readonly struct Lines(int count) : IOutputLines<MamaFamaPoint>
    {
        private readonly double[] _mama = new double[count], _fama = new double[count];

        internal MamaFamaResult Result => new(_mama, _fama);

        public void Set(int index, MamaFamaPoint output)
        {
            _mama[index] = output.Mama;
            _fama[index] = output.Fama;
        }
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        var result = Compute();
        PlotLine(axes, result.Mama);
        PlotLine(axes, result.Fama, label: LineLabel(1));
    }
}
