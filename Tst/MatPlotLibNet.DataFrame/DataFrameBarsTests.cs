// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Data.Analysis;
using MatPlotLibNet.Indicators;
using MsDataFrame = Microsoft.Data.Analysis.DataFrame;

namespace MatPlotLibNet.DataFrame.Tests;

/// <summary>An indicator reads named columns through one call per shape of bars (and one for a single column): each call here
/// reaches one shape, and the indicator made from it says what it says over the arrays of those columns.</summary>
public class DataFrameBarsTests
{
    private static readonly double[] O = [.. Enumerable.Range(0, 40).Select(i => 100.0 + (i % 5))];
    private static readonly double[] H = [.. O.Select(o => o + 2)];
    private static readonly double[] L = [.. O.Select(o => o - 2)];
    private static readonly double[] C = [.. O.Select((o, i) => o + (i % 3) - 1)];
    private static readonly double[] V = [.. Enumerable.Range(0, 40).Select(i => 1000.0 + 10 * i)];

    private static MsDataFrame Frame() => new(
        new PrimitiveDataFrameColumn<double>("o", O),
        new PrimitiveDataFrameColumn<double>("h", H),
        new PrimitiveDataFrameColumn<double>("l", L),
        new PrimitiveDataFrameColumn<double>("c", C),
        new PrimitiveDataFrameColumn<double>("v", V));

    [Fact]
    public void Values_ReadsOneColumn() =>
        Assert.Equal(new Sma(C, 5).Compute().Values, new Sma(Frame().Values("c"), 5).Compute().Values);

    [Fact]
    public void Hlc_ReadsTheHighLowAndCloseColumns() =>
        Assert.Equal(new Atr(OhlcvBars.Hlc(H, L, C)).Compute().Values, new Atr(Frame().Hlc("h", "l", "c")).Compute().Values);

    [Fact]
    public void Hlcv_ReadsTheVolumeToo() =>
        Assert.Equal(new TwiggsMoneyFlow(OhlcvBars.Hlcv(H, L, C, V)).Compute().Values,
            new TwiggsMoneyFlow(Frame().Hlcv("h", "l", "c", "v")).Compute().Values);

    [Fact]
    public void Ohlc_ReadsTheOpenToo() =>
        Assert.Equal(new GarmanKlass(OhlcvBars.Ohlc(O, H, L, C)).Compute().Values,
            new GarmanKlass(Frame().Ohlc("o", "h", "l", "c")).Compute().Values);

    [Fact]
    public void Cv_Hl_AndHlv_ReadTheColumnsTheirShapeNames()
    {
        var frame = Frame();
        Assert.Equal(new ForceIndex(OhlcvBars.Cv(C, V)).Compute().Values, new ForceIndex(frame.Cv("c", "v")).Compute().Values);
        Assert.Equal(new AroonOscillator(OhlcvBars.Hl(H, L)).Compute().Values, new AroonOscillator(frame.Hl("h", "l")).Compute().Values);
        Assert.Equal(new EaseOfMovement(OhlcvBars.Hlv(H, L, V)).Compute().Values,
            new EaseOfMovement(frame.Hlv("h", "l", "v")).Compute().Values);
    }

    [Fact]
    public void AColumnTheFrameDoesNotHave_IsRefusedByName()
    {
        var error = Assert.Throws<ArgumentException>(() => Frame().Hlc("h", "x", "c"));
        Assert.Contains("'x'", error.Message);
        Assert.Throws<ArgumentException>(() => Frame().Values("x"));
    }
}
