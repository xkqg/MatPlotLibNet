// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TextMeasurement;

namespace MatPlotLibNet.Tests.Rendering.TextMeasurement;

/// <summary>Verifies <see cref="CharacterWidthTable"/> per-character width factors.</summary>
public class CharacterWidthTableTests
{
    /// <summary>Digits must map to the standard monospaced digit width (0.60).</summary>
    [Theory]
    [InlineData('0')] [InlineData('1')] [InlineData('5')] [InlineData('9')]
    public void GetWidth_Digit_Returns0_60(char c)
    {
        Assert.Equal(0.60, CharacterWidthTable.GetWidth(c), precision: 5);
    }

    /// <summary>Narrow characters (i, l, |, !) must be significantly below average width.</summary>
    [Theory]
    [InlineData('i')] [InlineData('l')] [InlineData('|')]
    public void GetWidth_NarrowChar_BelowAverage(char c)
    {
        Assert.True(CharacterWidthTable.GetWidth(c) < 0.40,
            $"'{c}' expected < 0.40 but was {CharacterWidthTable.GetWidth(c)}");
    }

    /// <summary>Wide characters (W, M, m, w) must be above-average width.</summary>
    [Theory]
    [InlineData('W')] [InlineData('M')] [InlineData('m')] [InlineData('w')]
    public void GetWidth_WideChar_AboveAverage(char c)
    {
        Assert.True(CharacterWidthTable.GetWidth(c) > 0.70,
            $"'{c}' expected > 0.70 but was {CharacterWidthTable.GetWidth(c)}");
    }

    /// <summary>A string of narrow characters must measure narrower than the same count of wide characters.</summary>
    [Fact]
    public void MeasureSum_NarrowString_LessThanWide()
    {
        double narrow = "iii".Sum(c => CharacterWidthTable.GetWidth(c));
        double wide   = "WWW".Sum(c => CharacterWidthTable.GetWidth(c));
        Assert.True(narrow < wide);
    }

    /// <summary>Unknown/unprintable characters must return the default fallback width (~0.58).</summary>
    [Fact]
    public void GetWidth_Fallback_Returns0_58()
    {
        double w = CharacterWidthTable.GetWidth('\x01');
        Assert.InRange(w, 0.50, 0.70);
    }
}
