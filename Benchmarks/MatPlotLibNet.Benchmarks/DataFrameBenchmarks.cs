// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using MatPlotLibNet.DataFrame;
using MatPlotLibNet.Indicators;
using MatPlotLibNet.Numerics;
using Microsoft.Data.Analysis;
using MsDataFrame = Microsoft.Data.Analysis.DataFrame;

namespace MatPlotLibNet.Benchmarks;

/// <summary>Benchmarks for MatPlotLibNet.DataFrame extension methods — column reader, financial indicators,
/// polynomial numerics, and figure builder integration, all over Microsoft.Data.Analysis.DataFrame.</summary>
[MemoryDiagnoser]
public class DataFrameBenchmarks
{
    private MsDataFrame _df = default!;
    private double[] _polyCoeffs = default!;
    private double[] _evalX = default!;
    private static readonly string[] TickerValues = ["AAPL", "MSFT", "GOOG"];

    [Params(1_000, 10_000, 100_000)]
    public int DataSize;

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);
        var open   = new double[DataSize];
        var high   = new double[DataSize];
        var low    = new double[DataSize];
        var close  = new double[DataSize];
        var volume = new double[DataSize];

        double price = 100.0;
        for (int i = 0; i < DataSize; i++)
        {
            double change = (rng.NextDouble() - 0.48) * 2.0;
            open[i]   = price;
            high[i]   = price + Math.Abs(change) + rng.NextDouble();
            low[i]    = price - Math.Abs(change) - rng.NextDouble();
            price    += change;
            close[i]  = price;
            volume[i] = rng.NextDouble() * 1_000_000;
        }

        var tickers = new string[DataSize];
        for (int i = 0; i < DataSize; i++)
            tickers[i] = TickerValues[i % 3];

        _df = new MsDataFrame(
            new DoubleDataFrameColumn("Open",   open),
            new DoubleDataFrameColumn("High",   high),
            new DoubleDataFrameColumn("Low",    low),
            new DoubleDataFrameColumn("Close",  close),
            new DoubleDataFrameColumn("Volume", volume),
            new StringDataFrameColumn("Ticker", tickers));

        _polyCoeffs = _df.PolyFit("Open", "Close", 3);
        _evalX      = DataFrameColumnReader.ToDoubleArray(_df["Open"]);
    }

    // --- Column reader ---

    [Benchmark(Baseline = true)]
    public double[] ToDoubleArray_Close() => DataFrameColumnReader.ToDoubleArray(_df["Close"]);

    [Benchmark]
    public string[] ToStringArray_Ticker() => DataFrameColumnReader.ToStringArray(_df["Ticker"]);

    // --- Price indicators ---

    [Benchmark]
    public double[] Sma_20() => new Sma(_df.Values("Close"), 20).Compute();

    [Benchmark]
    public double[] Ema_20() => new Ema(_df.Values("Close"), 20).Compute();

    [Benchmark]
    public double[] Rsi_14() => new Rsi(_df.Values("Close")).Compute();

    [Benchmark]
    public MacdResult Macd_12_26_9() => new Macd(_df.Values("Close")).Compute();

    [Benchmark]
    public BandsResult BollingerBands_20() => new BollingerBands(_df.Values("Close"), 20).Compute();

    [Benchmark]
    public double[] DrawDown() => new DrawDown(_df.Values("Close")).Compute();

    [Benchmark]
    public double[] Obv() => new Obv(_df.Values("Close"), _df.Values("Volume")).Compute();

    // --- OHLCV indicators ---

    [Benchmark]
    public double[] Atr_14() => new Atr(_df.Hlc("High", "Low", "Close")).Compute();

    [Benchmark]
    public double[] Adx_14() => new Adx(_df.Hlc("High", "Low", "Close")).Compute();

    [Benchmark]
    public AdxResult AdxFull_14() => new Adx(_df.Hlc("High", "Low", "Close")).ComputeFull();

    [Benchmark]
    public double[] Cci_20() => new Cci(_df.Hlc("High", "Low", "Close")).Compute();

    [Benchmark]
    public StochasticResult Stochastic_14() => new Stochastic(_df.Hlc("High", "Low", "Close")).Compute();

    [Benchmark]
    public double[] WilliamsR_14() => new WilliamsR(_df.Hlc("High", "Low", "Close")).Compute();

    [Benchmark]
    public BandsResult KeltnerChannels_20() => new KeltnerChannels(_df.Hlc("High", "Low", "Close")).Compute();

    [Benchmark]
    public double[] Vwap() => new Vwap(_df.Hlcv("High", "Low", "Close", "Volume")).Compute();

    [Benchmark]
    public double[] ParabolicSar() => new ParabolicSar(_df.Values("High"), _df.Values("Low")).Compute().Sar;

    // --- Numerics extensions ---

    [Benchmark]
    public double[] PolyFit_Degree3() => _df.PolyFit("Open", "Close", 3);

    [Benchmark]
    public double[] PolyEval_Degree3() => _df.PolyEval("Open", _polyCoeffs);

    [Benchmark]
    public ConfidenceBand ConfidenceBand_95() =>
        _df.ConfidenceBand("Open", "Close", _polyCoeffs, _evalX);

    // --- Figure builder extensions — no hue ---

    [Benchmark]
    public FigureBuilder Line_Close() => _df.Line("Open", "Close");

    [Benchmark]
    public FigureBuilder Scatter_Close() => _df.Scatter("Open", "Close");

    [Benchmark]
    public FigureBuilder Hist_Close_30bins() => _df.Hist("Close", 30);

    // --- Figure builder extensions — with hue (3-group split overhead) ---

    [Benchmark]
    public FigureBuilder Line_WithHue() => _df.Line("Open", "Close", hue: "Ticker");

    [Benchmark]
    public FigureBuilder Scatter_WithHue() => _df.Scatter("Open", "Close", hue: "Ticker");

    [Benchmark]
    public FigureBuilder Hist_WithHue() => _df.Hist("Close", 30, hue: "Ticker");
}
