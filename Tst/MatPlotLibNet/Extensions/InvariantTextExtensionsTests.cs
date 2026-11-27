// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace MatPlotLibNet.Tests.Extensions;

/// <summary>Verifies the one owner of a number's text: written the same in every culture (a point for the decimals, no group
/// separator of the reader's culture), in the shortest form that reads back to the same number or in a format, into a string or
/// into a span, and read back from text the same way.</summary>
public class InvariantTextExtensionsTests
{
    /// <summary>Runs <paramref name="check"/> under a culture that writes a comma for the decimals and a point between groups.</summary>
    private static void UnderACommaCulture(Action check)
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nl-NL");
            check();
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void InvariantText_WritesANumberTheSameInEveryCulture_WithOrWithoutAFormat() => UnderACommaCulture(() =>
    {
        Assert.Equal("1.5", 1.5.InvariantText());
        Assert.Equal("0.07", 0.07.InvariantText("0.00"));
        Assert.Equal("3", 3.0.InvariantText("0.#"));
        Assert.Equal("1234567", 1234567.InvariantText());
        Assert.Equal("1,234", 1234.InvariantText("N0"));
        Assert.Equal("-12", (-12L).InvariantText());
        Assert.Equal("2026-11-18", new DateTime(2026, 11, 18).InvariantText("yyyy-MM-dd"));
    });

    [Theory]
    [InlineData(0.1)]
    [InlineData(1.0 / 3.0)]
    [InlineData(-1.7976931348623157E+308)]
    [InlineData(4.9E-324)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    public void InvariantText_WithoutAFormat_IsTheShortestFormThatReadsBack_TheGeneralForm(double value)
    {
        Assert.Equal(value.ToString("G", CultureInfo.InvariantCulture), value.InvariantText());
        Assert.Equal(value.ToString("R", CultureInfo.InvariantCulture), value.InvariantText());
    }

    [Theory]
    [InlineData(0.1, null)]
    [InlineData(-1.7976931348623157E+308, null)]
    [InlineData(1.0 / 3.0, "G6")]
    [InlineData(1.0 / 3.0, "G4")]
    [InlineData(12.5, "0.##")]
    public void FormatInvariant_WritesIntoASpan_TheTextInvariantTextReturns(double value, string? format) => UnderACommaCulture(() =>
    {
        Span<char> digits = stackalloc char[32];

        int written = value.FormatInvariant(digits, format);

        Assert.Equal(value.InvariantText(format), digits[..written].ToString());
    });

    /// <summary>A value of a reference type is written as its own formatting writes it: a version.</summary>
    [Fact]
    public void FormatInvariant_OfAValueOfAReferenceType_WritesItsText()
    {
        Span<char> text = stackalloc char[32];

        int written = new Version(1, 2, 3).FormatInvariant(text);

        Assert.Equal("1.2.3", text[..written].ToString());
    }

    [Fact]
    public void FormatInvariant_OfACompositeFormat_PutsTheNumberInItsHole_InEveryCulture() => UnderACommaCulture(() =>
        Assert.Equal("12.3%", "{0:F1}%".FormatInvariant(12.34)));

    [Fact]
    public void Invariant_ComposesAnInterpolatedText_WithEveryNumberInItInTheInvariantCulture() => UnderACommaCulture(() =>
    {
        double width = 1234.5;
        int count = -3;
        string name = "bars";

        string text = string.Invariant(
            $"{name}: {width} by {0.07:0.00}, {count} of {1234:N0}, {new DateTime(2026, 11, 18):yyyy-MM-dd}, [{1.5,5}] [{2.5,6:0.00}] [{"ab".AsSpan()}]");

        Assert.Equal("bars: 1234.5 by 0.07, -3 of 1,234, 2026-11-18, [  1.5] [  2.50] [ab]", text);
    });

    [Fact]
    public void AppendInvariant_AppendsTheTextInvariantTextReturns_WithOrWithoutAFormat() => UnderACommaCulture(() =>
    {
        var text = new System.Text.StringBuilder("n=");

        text.AppendInvariant(-3).Append(' ').AppendInvariant(1234.5).Append(' ').AppendInvariant(0.5, "0.00");

        Assert.Equal("n=-3 1234.5 0.50", text.ToString());
    });

    [Theory]
    [InlineData("1.5", 1.5)]
    [InlineData("-2.5e3", -2500.0)]
    [InlineData("1e400", double.PositiveInfinity)]
    public void TryReadInvariant_ReadsAPointForTheDecimals_InEveryCulture(string text, double expected) => UnderACommaCulture(() =>
    {
        Assert.True(text.AsSpan().TryReadInvariant(NumberStyles.Float, out double value));
        Assert.Equal(expected, value);
        Assert.Equal(expected, text.AsSpan().ReadInvariant<double>(NumberStyles.Float));
    });

    [Theory]
    [InlineData("1,5")]
    [InlineData("abc")]
    [InlineData("")]
    public void TryReadInvariant_RefusesTextThatIsNoNumberInTheStyle(string text) =>
        Assert.False(text.AsSpan().TryReadInvariant(NumberStyles.Float, out double _));

    [Fact]
    public void ReadInvariant_TakesTheStyleItIsGiven()
    {
        Assert.Equal(255UL, "ff".AsSpan().ReadInvariant<ulong>(NumberStyles.AllowHexSpecifier));
        Assert.Equal(12.5m, "12.5".AsSpan().ReadInvariant<decimal>(NumberStyles.Number));
        Assert.Throws<FormatException>(() => "1.5".AsSpan().ReadInvariant<int>(NumberStyles.Integer));
    }
}
