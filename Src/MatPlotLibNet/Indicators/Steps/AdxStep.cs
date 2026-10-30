// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Steps;

/// <summary>The three lines of the average directional index at one bar.</summary>
/// <param name="Adx">The average directional index: the mean of the last <c>period</c> directional indices.</param>
/// <param name="PlusDi">The positive directional indicator, in percent.</param>
/// <param name="MinusDi">The negative directional indicator, in percent.</param>
internal readonly record struct AdxPoint(double Adx, double PlusDi, double MinusDi);

/// <summary>One step of the average directional index: the recursion that the batch <see cref="Adx"/> folds over its bars and
/// <see cref="Streaming.StreamingAdx"/> takes one bar at a time. The true range and the two directional movements of a bar are
/// summed over the first <c>period</c> bars; from the next bar Wilder's smoothing carries them, and the directional indicators and
/// the directional index follow. The ADX is the simple mean of the last <c>period</c> directional indices
/// (a <see cref="RollingMeanStep"/>), so the first value comes with the bar <c>2 × period</c> and the step says NaN before.</summary>
internal struct AdxStep
{
    private readonly int _period;
    private RollingMeanStep _mean;
    private double _previousHigh;
    private double _previousLow;
    private double _previousClose;
    private double _smoothTr;
    private double _smoothPlusDm;
    private double _smoothMinusDm;
    private int _count;

    /// <summary>A step over the given period.</summary>
    /// <param name="period">The period of the smoothing and of the mean; at least 1.</param>
    internal AdxStep(int period)
    {
        _period = period;
        _mean = new RollingMeanStep(period);
    }

    /// <summary>Takes one bar and returns the three lines, NaN before the bar <c>2 × period</c>.</summary>
    /// <param name="high">The high of the bar.</param>
    /// <param name="low">The low of the bar.</param>
    /// <param name="close">The close of the bar.</param>
    internal AdxPoint Next(double high, double low, double close)
    {
        int bar = _count++;
        double previousHigh = _previousHigh, previousLow = _previousLow, previousClose = _previousClose;
        _previousHigh = high;
        _previousLow = low;
        _previousClose = close;
        if (bar == 0) return new AdxPoint(double.NaN, double.NaN, double.NaN);

        int i = bar - 1;
        double tr = PriceSources.TrueRange(high, low, previousClose);
        double upMove = high - previousHigh;
        double downMove = previousLow - low;
        double plusDm = upMove > downMove && upMove > 0 ? upMove : 0;
        double minusDm = downMove > upMove && downMove > 0 ? downMove : 0;

        if (i < _period)
        {
            _smoothTr += tr;
            _smoothPlusDm += plusDm;
            _smoothMinusDm += minusDm;
            return new AdxPoint(double.NaN, double.NaN, double.NaN);
        }

        _smoothTr = _smoothTr - _smoothTr / _period + tr;
        _smoothPlusDm = _smoothPlusDm - _smoothPlusDm / _period + plusDm;
        _smoothMinusDm = _smoothMinusDm - _smoothMinusDm / _period + minusDm;
        double pdi = _smoothTr > 0 ? _smoothPlusDm / _smoothTr * 100 : 0;
        double mdi = _smoothTr > 0 ? _smoothMinusDm / _smoothTr * 100 : 0;
        double diSum = pdi + mdi;
        double dx = diSum > 0 ? Math.Abs(pdi - mdi) / diSum * 100 : 0;

        double adx = _mean.Next(dx);
        return double.IsNaN(adx)
            ? new AdxPoint(double.NaN, double.NaN, double.NaN)
            : new AdxPoint(adx, pdi, mdi);
    }
}
