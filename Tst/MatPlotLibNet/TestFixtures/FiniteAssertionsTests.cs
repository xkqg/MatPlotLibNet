// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Rendering;
using Xunit.Sdk;

namespace MatPlotLibNet.Tests;

/// <summary>How the tests say a number, a sequence of numbers, a point or a rectangle is finite: the refusal names the value, where
/// it stands and what it is.</summary>
public sealed class FiniteAssertionsTests
{
    [Fact]
    public void AFiniteNumber_Passes_AndEveryNumberThatIsNotFinite_IsRefusedByName()
    {
        1.5.AssertFinite();
        double.MaxValue.AssertFinite("big");

        foreach (double bad in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            var ex = Assert.ThrowsAny<XunitException>(() => bad.AssertFinite("the width"));
            Assert.Contains($"the width: {bad.ToString(CultureInfo.InvariantCulture)} is not finite", ex.Message, StringComparison.Ordinal);
        }
        Assert.Contains("NaN is not finite", Assert.ThrowsAny<XunitException>(() => double.NaN.AssertFinite()).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ASequence_IsFiniteWhenEveryNumberIs_AnEmptyOneToo_AndTheRefusalNamesTheFirstIndexThatIsNot()
    {
        new double[] { 1, -2, 0.5 }.AssertAllFinite();
        Array.Empty<double>().AssertAllFinite("nothing");

        var ex = Assert.ThrowsAny<XunitException>(() => new[] { 1.0, double.PositiveInfinity, double.NaN }.AssertAllFinite("the ratio"));
        Assert.Contains("the ratio[1]: Infinity is not finite", ex.Message, StringComparison.Ordinal);
        Assert.Contains("[0]: NaN is not finite", Assert.ThrowsAny<XunitException>(() => new[] { double.NaN }.AssertAllFinite()).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void APointAndARectangle_AreFiniteWhenEveryNumberOfThemIs()
    {
        new Point(1, 2).AssertFinite();
        new Rect(1, 2, 3, 4).AssertFinite("the plot");

        Assert.Contains("p[1]: Infinity", Assert.ThrowsAny<XunitException>(() => new Point(0, double.PositiveInfinity).AssertFinite("p")).Message, StringComparison.Ordinal);
        Assert.Contains("plot[3]: NaN", Assert.ThrowsAny<XunitException>(() => new Rect(0, 0, 1, double.NaN).AssertFinite("plot")).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ATextWithNoMissingValueAndNoInfinity_Passes_AndEveryOneThatHasOne_IsRefusedWithTheTokenItCarries()
    {
        "<svg><path d=\"M 1 2 L 3 4\"/><text>x = 6.50</text></svg>".AssertNoNonFiniteNumber();
        "<svg><path d=\"M 1 2\"/><script>if (isNaN(x) || x === Infinity) return;</script></svg>".AssertNoNonFiniteNumber("the picture");

        foreach (string bad in new[] { "<circle cx=\"NaN\"/>", "<rect width=\"-Infinity\"/>", "<text>x = Infinity</text>" })
        {
            var ex = Assert.ThrowsAny<XunitException>(() => bad.AssertNoNonFiniteNumber("the picture"));
            Assert.Contains("the picture carries a missing value or an infinity: ", ex.Message, StringComparison.Ordinal);
            Assert.Contains(bad.Contains("NaN", StringComparison.Ordinal) ? "NaN" : "Infinity", ex.Message, StringComparison.Ordinal);
        }
        Assert.Contains("the text carries", Assert.ThrowsAny<XunitException>(() => "NaN".AssertNoNonFiniteNumber()).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ASequenceOfTexts_IsHeldToTheSame_AndTheRefusalNamesTheIndexOfTheFirstThatIsNot()
    {
        new[] { "a = 1", "b = 2" }.AssertNoNonFiniteNumber();
        Array.Empty<string>().AssertNoNonFiniteNumber("nothing");

        var ex = Assert.ThrowsAny<XunitException>(() => new[] { "a = 1", "b = NaN", "c = Infinity" }.AssertNoNonFiniteNumber("the labels"));
        Assert.Contains("the labels[1] carries", ex.Message, StringComparison.Ordinal);
    }
}
