// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Each factory of <see cref="OhlcvBars"/> refuses arrays that do not pair up, whichever of the arrays it reads is the short
/// one, so no indicator is made over bars of two lengths.</summary>
public sealed class OhlcvBarsTests
{
    private static readonly double[] Three = [1, 2, 3];

    private static readonly double[] Two = [1, 2];

    private static readonly Dictionary<string, Func<OhlcvBars>> ShortArray = new()
    {
        ["Hlc, low"] = () => OhlcvBars.Hlc(Three, Two, Three),
        ["Hlc, close"] = () => OhlcvBars.Hlc(Three, Three, Two),
        ["Hl, low"] = () => OhlcvBars.Hl(Three, Two),
        ["Hlv, low"] = () => OhlcvBars.Hlv(Three, Two, Three),
        ["Hlv, volume"] = () => OhlcvBars.Hlv(Three, Three, Two),
        ["Cv, volume"] = () => OhlcvBars.Cv(Three, Two),
        ["Ohlc, high"] = () => OhlcvBars.Ohlc(Three, Two, Three, Three),
        ["Ohlc, low"] = () => OhlcvBars.Ohlc(Three, Three, Two, Three),
        ["Ohlc, close"] = () => OhlcvBars.Ohlc(Three, Three, Three, Two),
        ["Hlcv, low"] = () => OhlcvBars.Hlcv(Three, Two, Three, Three),
        ["Hlcv, close"] = () => OhlcvBars.Hlcv(Three, Three, Two, Three),
        ["Hlcv, volume"] = () => OhlcvBars.Hlcv(Three, Three, Three, Two),
    };

    public static TheoryData<string> Calls() => new(ShortArray.Keys);

    [Theory]
    [MemberData(nameof(Calls))]
    public void AFactory_RefusesAnArrayShorterThanTheOthers(string call) =>
        Assert.Equal(3.RefusalAgainst(2), Assert.Throws<ArgumentException>(() => ShortArray[call]()).Message);
}
