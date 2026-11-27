// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>What an indicator's batch form and its streaming form both say from the parameters they are made with is said once, by
/// the batch class's <c>Spec</c> factory: the label, the labels of its lines and how many bars come before its first value.</summary>
public sealed class IndicatorSpecTests
{
    [Fact]
    public void AOneLineSpec_NamesItsLineByTheLabel_AndWarmsUpOneBarAfterItsFirstValueIndex()
    {
        var spec = IndicatorSpec.Of(14, "RSI(14)");

        Assert.Equal("RSI(14)", spec.Label);
        Assert.Equal(["RSI(14)"], spec.Lines.ToArray());
        Assert.Equal(14, spec.FirstValueIndex);
        Assert.Equal(15, spec.Warmup);
    }

    [Fact]
    public void ASpecOfSeveralLines_KeepsTheirLabelsInTheOrderOfTheOutputs()
    {
        var spec = IndicatorSpec.Of(27, "ADX(14)", "ADX(14)", "+DI", "-DI");

        Assert.Equal("ADX(14)", spec.Label);
        Assert.Equal(["ADX(14)", "+DI", "-DI"], spec.Lines.ToArray());
    }

    [Fact]
    public void AFirstValueIndexBelowZero_IsRefused() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => IndicatorSpec.Of(-1, "X"));

    [Fact]
    public void ALabelOfOneNumber_IsTheNameAndTheNumberInBrackets() =>
        Assert.Equal("ADX(14)", IndicatorSpec.Labelled("ADX", 14));

    [Theory]
    [InlineData("RSI", 14, 14)]
    [InlineData("SMA", 20, 19)]
    [InlineData("%R", 14, 13)]
    public void AWindowSpec_IsLabelledByItsNameAndPeriod_AndKeepsTheFirstValueIndexItWasGiven(string name, int period, int first)
    {
        var spec = IndicatorSpec.OfWindow(name, period, first);

        Assert.Equal($"{name}({period})", spec.Label);
        Assert.Equal([spec.Label], spec.Lines.ToArray());
        Assert.Equal(first, spec.FirstValueIndex);
    }

    [Fact]
    public void ACheckedWindowSpec_IsTheWindowSpec_OfAPeriodAtLeastItsLeast()
    {
        var spec = IndicatorSpec.OfCheckedWindow("Aroon", 25, 2, 25);

        Assert.Equal("Aroon(25)", spec.Label);
        Assert.Equal(25, spec.FirstValueIndex);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(-5)]
    public void ACheckedWindowSpec_RefusesAPeriodBelowItsLeast_NamingTheArgumentTheCallerPassed(int smoothingPeriod)
    {
        var refused = Assert.Throws<ArgumentOutOfRangeException>(() => IndicatorSpec.OfCheckedWindow("AdaptStoch", smoothingPeriod, 2, 6));

        Assert.Equal("smoothingPeriod", refused.ParamName);
        Assert.Contains("must be >= 2", refused.Message, StringComparison.Ordinal);
    }
}
