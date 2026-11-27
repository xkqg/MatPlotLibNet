// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>The lengths a line style is dashed at are one rule over one value: the dash pattern of the theme's look
/// (<see cref="DashLook"/>) taken through <see cref="DashPatterns.Lengths"/>, which every backend strokes with. A look names the dash
/// and gap of each style, whether they grow with the width of the line, and how many pixels one unit of the pattern is.</summary>
public class DashLookTests
{
    /// <summary>The library's look dashes at the same lengths whatever the width: the SVG, the native output, writes them so.</summary>
    [Theory]
    [InlineData(LineStyle.Dashed, 0.5, new double[] { 5, 2 })]
    [InlineData(LineStyle.Dashed, 2.08, new double[] { 5, 2 })]
    [InlineData(LineStyle.Dotted, 3.0, new double[] { 1, 3 })]
    [InlineData(LineStyle.DashDot, 1.0, new double[] { 5, 2, 1, 2 })]
    public void TheLibraryLook_DashesAtTheSameLengthsWhateverTheWidth(LineStyle style, double thickness, double[] expected)
    {
        var dashes = DashPatterns.Lengths(style, thickness, DashLook.Library);

        Assert.Equal(expected.Length, dashes.Count);
        for (int i = 0; i < expected.Length; i++) Assert.Equal(expected[i], dashes[i]);
    }

    [Theory]
    [InlineData(LineStyle.Solid)]
    [InlineData(LineStyle.None)]
    public void ASolidLineAndNoLine_HaveNoDashesUnderAnyLook(LineStyle style)
    {
        Assert.Equal(0, DashPatterns.Lengths(style, 2, DashLook.Library).Count);
        Assert.Equal(0, DashPatterns.Lengths(style, 2, new DashLook { ScalesWithWidth = true, Unit = 2 }).Count);
        Assert.Empty(DashPatterns.Lengths(style, 2, DashLook.Library).ToSingles());
    }

    [Fact]
    public void ALookWhoseDashesGrowWithTheWidth_MultipliesEachRatioByIt()
    {
        var look = new DashLook { Dashed = [3.7, 1.6], ScalesWithWidth = true };

        var dashes = DashPatterns.Lengths(LineStyle.Dashed, 2.0, look);

        Assert.Equal(7.4, dashes[0]);
        Assert.Equal(3.2, dashes[1]);
    }

    [Fact]
    public void ALookWithAUnit_MultipliesEachRatioByHowManyPixelsAUnitIs_AndOnlyThenByTheWidthIfItGrowsWithIt()
    {
        var points = new DashLook { Dotted = [1, 3], Unit = 100.0 / 72 };
        var grown = new DashLook { Dotted = [1, 3], Unit = 100.0 / 72, ScalesWithWidth = true };

        Assert.Equal(100.0 / 72, DashPatterns.Lengths(LineStyle.Dotted, 9, points)[0]);
        Assert.Equal(3 * (100.0 / 72), DashPatterns.Lengths(LineStyle.Dotted, 9, points)[1]);
        Assert.Equal(9 * (100.0 / 72), DashPatterns.Lengths(LineStyle.Dotted, 9, grown)[0]);
    }

    [Fact]
    public void TheLengthsAsSingles_AreTheDoubleProductRoundedOnce()
    {
        var look = new DashLook { ScalesWithWidth = true };

        float[] singles = DashPatterns.Lengths(LineStyle.Dashed, 1.3, look).ToSingles();

        Assert.Equal([(float)(5 * 1.3), (float)(2 * 1.3)], singles);
    }

    [Fact]
    public void ThePublicPattern_IsTheLibraryLooksRatios()
    {
        foreach (var style in new[] { LineStyle.Solid, LineStyle.None, LineStyle.Dashed, LineStyle.Dotted, LineStyle.DashDot })
            Assert.Equal(DashLook.Library.RatiosOf(style).ToArray(), DashPatterns.GetPattern(style).ToArray());
    }

    [Fact]
    public void EveryLibraryTheme_WearsTheLibraryDashes()
    {
        foreach (var theme in new[] { Theme.Default, Theme.Dark, Theme.Seaborn, Theme.ColorBlindSafe })
            Assert.Same(DashLook.Library, theme.Looks.Strokes.Dashes);
    }

    [Fact]
    public void AJsonThatNamesNoDashKey_IsTheLibraryLook()
    {
        Assert.Same(DashLook.Library, DashLook.ReadKeys(new ThemeDto()));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void AJsonThatNamesOneDashKey_IsTheLibraryLookWithThatKeyOnly(int key)
    {
        double[]? dashed = key == 1 ? [9.0, 9.0] : null, dotted = key == 2 ? [8.0, 8.0] : null, dashDot = key == 3 ? [7.0, 7.0] : null;
        bool? grows = key == 4 ? true : null;
        double? unit = key == 5 ? 2.0 : null;

        var look = DashLook.ReadKeys(new ThemeDto { DashedPattern = dashed, DottedPattern = dotted, DashDotPattern = dashDot, DashesScaleWithWidth = grows, DashUnit = unit });

        Assert.NotSame(DashLook.Library, look);
        Assert.Equal(key == 1 ? [9.0, 9.0] : DashLook.Library.Dashed, look.Dashed);
        Assert.Equal(key == 2 ? [8.0, 8.0] : DashLook.Library.Dotted, look.Dotted);
        Assert.Equal(key == 3 ? [7.0, 7.0] : DashLook.Library.DashDot, look.DashDot);
        Assert.Equal(key == 4, look.ScalesWithWidth);
        Assert.Equal(key == 5 ? 2.0 : 1.0, look.Unit);
    }
}
