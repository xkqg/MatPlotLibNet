// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming Stochastic Oscillator (%K and %D). %K scans the last <c>k</c> highs and lows, O(k) per candle; %D is the
/// <see cref="RollingMeanStep"/> of the last <c>d</c> values of %K, the simple mean the batch <see cref="Stochastic"/> takes.</summary>
public sealed class StreamingStochastic : StreamingIndicator
{
    private readonly int _kPeriod;
    private readonly int _dPeriod;
    private readonly double[] _highs;
    private readonly double[] _lows;
    private int _ringIndex;
    private RollingMeanStep _d;

    /// <inheritdoc />
    public override int WarmupPeriod => _kPeriod + _dPeriod - 1;

    /// <summary>Creates streaming Stochastic. Two output series: %K and %D.</summary>
    public StreamingStochastic(int kPeriod = 14, int dPeriod = 3, int capacity = 10_000)
        : base([
            new StreamingLineSeries(capacity) { Label = $"%K({kPeriod})" },
            new StreamingLineSeries(capacity) { Label = $"%D({dPeriod})" },
        ])
    {
        _kPeriod = kPeriod;
        _dPeriod = dPeriod;
        _highs = new double[kPeriod];
        _lows = new double[kPeriod];
        _d = new RollingMeanStep(dPeriod);
        Label = $"Stoch({kPeriod},{dPeriod})";
    }

    /// <inheritdoc />
    public override void AppendCandle(OhlcBar bar)
    {
        ProcessedCount++;

        _highs[_ringIndex] = bar.High;
        _lows[_ringIndex] = bar.Low;
        _ringIndex = (_ringIndex + 1) % _kPeriod;

        double k = double.NaN;
        double d = double.NaN;
        if (ProcessedCount >= _kPeriod)
        {
            double high = double.MinValue, low = double.MaxValue;
            for (int i = 0; i < _kPeriod; i++)
            {
                high = Math.Max(high, _highs[i]);
                low = Math.Min(low, _lows[i]);
            }

            double range = high - low;
            k = range > 0 ? (bar.Close - low) / range * 100 : 50;
            d = _d.Next(k);
        }

        double x = ProcessedCount - 1;
        OutputSeries[0].AppendPoint(x, k);
        OutputSeries[1].AppendPoint(x, d);
    }

    /// <inheritdoc />
    protected override double ComputeNext(double price) => double.NaN; // use AppendCandle
}
