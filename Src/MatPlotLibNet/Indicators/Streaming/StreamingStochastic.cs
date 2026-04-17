// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming Stochastic Oscillator (%K and %D). Uses rolling min/max deque. O(1) amortized.</summary>
public sealed class StreamingStochastic : StreamingIndicatorBase
{
    private readonly int _kPeriod;
    private readonly int _dPeriod;
    private readonly double[] _highs;
    private readonly double[] _lows;
    private readonly double[] _closes;
    private int _ringIndex;
    private double _dSum;
    private int _dCount;
    private double _dEma;

    /// <inheritdoc />
    public override int WarmupPeriod => _kPeriod;

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
        _closes = new double[kPeriod];
        Label = $"Stoch({kPeriod},{dPeriod})";
    }

    /// <inheritdoc />
    public override void AppendCandle(OhlcBar bar)
    {
        ProcessedCount++;

        _highs[_ringIndex] = bar.High;
        _lows[_ringIndex] = bar.Low;
        _closes[_ringIndex] = bar.Close;
        _ringIndex = (_ringIndex + 1) % _kPeriod;

        double x = ProcessedCount - 1;

        if (ProcessedCount < _kPeriod)
        {
            OutputSeries[0].AppendPoint(x, double.NaN);
            OutputSeries[1].AppendPoint(x, double.NaN);
            return;
        }

        int count = Math.Min(ProcessedCount, _kPeriod);
        double high = double.MinValue, low = double.MaxValue;
        for (int i = 0; i < count; i++)
        {
            high = Math.Max(high, _highs[i]);
            low = Math.Min(low, _lows[i]);
        }

        double k = high == low ? 50 : (bar.Close - low) / (high - low) * 100;
        OutputSeries[0].AppendPoint(x, k);

        // %D = SMA of %K
        _dCount++;
        if (_dCount <= _dPeriod) _dSum += k;
        if (_dCount == _dPeriod) _dEma = _dSum / _dPeriod;
        else if (_dCount > _dPeriod) _dEma = (_dEma * (_dPeriod - 1) + k) / _dPeriod;

        OutputSeries[1].AppendPoint(x, _dCount >= _dPeriod ? _dEma : double.NaN);
    }

    /// <inheritdoc />
    protected override double ComputeNext(double price) => double.NaN; // use AppendCandle
}
