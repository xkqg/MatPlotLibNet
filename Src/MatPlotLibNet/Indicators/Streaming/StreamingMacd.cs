// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming MACD (Moving Average Convergence Divergence). Three outputs: MACD line, signal, histogram.</summary>
public sealed class StreamingMacd : StreamingIndicatorBase
{
    private readonly int _fastPeriod;
    private readonly int _slowPeriod;
    private readonly int _signalPeriod;
    private double _fastEma;
    private double _slowEma;
    private double _signalEma;
    private double _fastSum;
    private double _slowSum;
    private double _signalSum;
    private int _signalCount;
    private readonly double _fastMult;
    private readonly double _slowMult;
    private readonly double _signalMult;

    /// <inheritdoc />
    public override int WarmupPeriod => _slowPeriod + _signalPeriod;

    /// <summary>Creates streaming MACD. Three output series: MACD line, signal line, histogram.</summary>
    public StreamingMacd(int fastPeriod = 12, int slowPeriod = 26, int signalPeriod = 9, int capacity = 10_000)
        : base([
            new StreamingLineSeries(capacity) { Label = "MACD" },
            new StreamingLineSeries(capacity) { Label = "Signal" },
            new StreamingLineSeries(capacity) { Label = "Histogram" },
        ])
    {
        _fastPeriod = fastPeriod;
        _slowPeriod = slowPeriod;
        _signalPeriod = signalPeriod;
        _fastMult = 2.0 / (fastPeriod + 1);
        _slowMult = 2.0 / (slowPeriod + 1);
        _signalMult = 2.0 / (signalPeriod + 1);
        Label = $"MACD({fastPeriod},{slowPeriod},{signalPeriod})";
    }

    /// <inheritdoc />
    protected override double ComputeNext(double price)
    {
        // Accumulate for initial SMA seeds
        if (ProcessedCount <= _fastPeriod) _fastSum += price;
        if (ProcessedCount <= _slowPeriod) _slowSum += price;

        // Seed fast EMA
        if (ProcessedCount == _fastPeriod) _fastEma = _fastSum / _fastPeriod;
        else if (ProcessedCount > _fastPeriod) _fastEma = (price - _fastEma) * _fastMult + _fastEma;

        // Seed slow EMA
        if (ProcessedCount == _slowPeriod) _slowEma = _slowSum / _slowPeriod;
        else if (ProcessedCount > _slowPeriod) _slowEma = (price - _slowEma) * _slowMult + _slowEma;

        if (ProcessedCount < _slowPeriod) return double.NaN;

        double macdLine = _fastEma - _slowEma;

        // Signal line EMA
        _signalCount++;
        if (_signalCount <= _signalPeriod) _signalSum += macdLine;
        if (_signalCount == _signalPeriod) _signalEma = _signalSum / _signalPeriod;
        else if (_signalCount > _signalPeriod) _signalEma = (macdLine - _signalEma) * _signalMult + _signalEma;

        double x = ProcessedCount - 1;
        if (_signalCount >= _signalPeriod)
        {
            AppendToOutput(1, x, _signalEma);
            AppendToOutput(2, x, macdLine - _signalEma);
        }

        return macdLine;
    }
}
