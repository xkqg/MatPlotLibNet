// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using MatPlotLibNet.Indicators;

namespace MatPlotLibNet.Benchmarks;

/// <summary>Benchmarks for all technical indicators. Uses instance Compute() where available (typed indicators),
/// static Compute() for untyped indicators (pending refactor to Indicator&lt;TResult&gt;).</summary>
[MemoryDiagnoser]
public class IndicatorBenchmarks
{
    private double[] _prices = default!;
    private double[] _high = default!;
    private double[] _low = default!;
    private double[] _close = default!;
    private double[] _volume = default!;

    [Params(1_000, 10_000, 100_000)]
    public int DataSize;

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);
        _close = new double[DataSize];
        _high = new double[DataSize];
        _low = new double[DataSize];
        _volume = new double[DataSize];

        double price = 100.0;
        for (int i = 0; i < DataSize; i++)
        {
            var change = (random.NextDouble() - 0.48) * 2.0;
            _high[i] = price + Math.Abs(change) + random.NextDouble();
            _low[i] = price - Math.Abs(change) - random.NextDouble();
            price += change;
            _close[i] = price;
            _volume[i] = random.NextDouble() * 1_000_000;
        }
        _prices = _close;
    }

    // --- Typed indicators (instance Compute() via Indicator<TResult>) ---

    [Benchmark(Baseline = true)]
    public double[] Sma_20() => new Sma(_prices, 20).Compute();

    [Benchmark]
    public double[] Ema_20() => new Ema(_prices, 20).Compute();

    [Benchmark]
    public BandsResult BollingerBands_20() => new BollingerBands(_prices, 20).Compute();

    [Benchmark]
    public BandsResult KeltnerChannels_20() => new KeltnerChannels(_high, _low, _close, 20).Compute();

    [Benchmark]
    public double[] Atr_14() => new Atr(_high, _low, _close, 14).Compute();

    [Benchmark]
    public double[] Adx_14() => new Adx(_high, _low, _close, 14).Compute();

    [Benchmark]
    public MacdResult Macd_12_26_9() => new Macd(_prices).Compute();

    [Benchmark]
    public StochasticResult Stochastic_14() => new Stochastic(_high, _low, _close).Compute();

    // --- All indicators now use instance Compute() ---

    [Benchmark]
    public double[] Rsi_14() => new Rsi(_prices, 14).Compute();

    [Benchmark]
    public double[] Vwap() => new Indicators.Vwap(_prices, _volume).Compute();

    [Benchmark]
    public double[] EquityCurve() => new Indicators.EquityCurve(_prices).Compute();

    [Benchmark]
    public double[] DrawDown() => new Indicators.DrawDown(_prices).Compute();

    // --- Phase F indicators (v0.6.0) ---

    [Benchmark]
    public double[] WilliamsR_14() => new Indicators.WilliamsR(_high, _low, _close, 14).Compute();

    [Benchmark]
    public double[] Obv() => new Indicators.Obv(_close, _volume).Compute();

    [Benchmark]
    public double[] Cci_20() => new Indicators.Cci(_high, _low, _close, 20).Compute();

    [Benchmark]
    public Indicators.ParabolicSarResult ParabolicSar_() => new Indicators.ParabolicSar(_high, _low).Compute();
}
