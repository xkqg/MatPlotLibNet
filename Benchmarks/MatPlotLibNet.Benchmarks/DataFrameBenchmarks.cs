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
    public double[] Sma_20() => _df.Sma("Close", 20);

    [Benchmark]
    public double[] Ema_20() => _df.Ema("Close", 20);

    [Benchmark]
    public double[] Rsi_14() => _df.Rsi("Close");

    [Benchmark]
    public MacdResult Macd_12_26_9() => _df.Macd("Close");

    [Benchmark]
    public BandsResult BollingerBands_20() => _df.BollingerBands("Close", 20);

    [Benchmark]
    public double[] DrawDown() => _df.DrawDown("Close");

    [Benchmark]
    public double[] Obv() => _df.Obv("Close", "Volume");

    // --- OHLCV indicators ---

    [Benchmark]
    public double[] Atr_14() => _df.Atr("High", "Low", "Close");

    [Benchmark]
    public double[] Adx_14() => _df.Adx("High", "Low", "Close");

    [Benchmark]
    public AdxResult AdxFull_14() => _df.AdxFull("High", "Low", "Close");

    [Benchmark]
    public double[] Cci_20() => _df.Cci("High", "Low", "Close");

    [Benchmark]
    public StochasticResult Stochastic_14() => _df.Stochastic("High", "Low", "Close");

    [Benchmark]
    public double[] WilliamsR_14() => _df.WilliamsR("High", "Low", "Close");

    [Benchmark]
    public BandsResult KeltnerChannels_20() => _df.KeltnerChannels("High", "Low", "Close");

    [Benchmark]
    public double[] Vwap() => _df.Vwap("High", "Low", "Close", "Volume");

    [Benchmark]
    public double[] ParabolicSar() => _df.ParabolicSar("High", "Low");

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
