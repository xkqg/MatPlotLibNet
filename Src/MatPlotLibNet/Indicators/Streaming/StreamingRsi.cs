// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming Relative Strength Index (Wilder's smoothing). O(1) per append.</summary>
public sealed class StreamingRsi : StreamingIndicatorBase
{
    private readonly int _period;
    private double _avgGain;
    private double _avgLoss;
    private double _prevPrice;
    private bool _hasPrev;

    /// <inheritdoc />
    public override int WarmupPeriod => _period + 1; // need period+1 prices to get period changes

    /// <summary>Creates a streaming RSI indicator.</summary>
    public StreamingRsi(int period = 14, int capacity = 10_000) : base(capacity)
    {
        _period = period;
        Label = $"RSI({period})";
    }

    /// <inheritdoc />
    protected override double ComputeNext(double price)
    {
        if (!_hasPrev)
        {
            _prevPrice = price;
            _hasPrev = true;
            return double.NaN;
        }

        double change = price - _prevPrice;
        _prevPrice = price;
        double gain = change > 0 ? change : 0;
        double loss = change < 0 ? -change : 0;

        if (ProcessedCount <= _period)
        {
            _avgGain += gain;
            _avgLoss += loss;

            if (ProcessedCount == _period + 1)
            {
                _avgGain /= _period;
                _avgLoss /= _period;
            }
            else
            {
                return double.NaN;
            }
        }
        else
        {
            _avgGain = (_avgGain * (_period - 1) + gain) / _period;
            _avgLoss = (_avgLoss * (_period - 1) + loss) / _period;
        }

        if (_avgLoss == 0) return 100.0;
        double rs = _avgGain / _avgLoss;
        return 100.0 - 100.0 / (1.0 + rs);
    }
}
