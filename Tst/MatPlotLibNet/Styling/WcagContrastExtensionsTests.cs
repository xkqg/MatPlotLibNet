// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>The contrast ratio the chart tests judge visibility by is WCAG's, checked against the values the specification
/// and its worked examples give, so a test that says "3 : 1" means what the standard means.</summary>
public class WcagContrastExtensionsTests
{
    [Fact]
    public void BlackOnWhite_IsTwentyOne() =>
        Assert.Equal(21.0, Colors.Black.ContrastRatioWith(Colors.White), precision: 6);

    [Fact]
    public void AColourWithItself_IsOne() =>
        Assert.Equal(1.0, Color.FromHex("#4C78A8").ContrastRatioWith(Color.FromHex("#4C78A8")), precision: 9);

    [Fact]
    public void TheOrderDoesNotMatter()
    {
        var a = Color.FromHex("#808080");
        var b = Color.FromHex("#2D2D2D");

        Assert.Equal(a.ContrastRatioWith(b), b.ContrastRatioWith(a), precision: 12);
    }

    /// <summary>#767676 is the lightest grey that reaches the 4.5 : 1 of normal text on white: WCAG's own worked example.</summary>
    [Fact]
    public void TheLightestGreyThatPassesNormalTextOnWhite_IsAboutFourPointFiveFour() =>
        Assert.Equal(4.54, Color.FromHex("#767676").ContrastRatioWith(Colors.White), precision: 2);

    /// <summary>The neutral grey of every reference line, measured against the default theme's white plot: 3.95 : 1.</summary>
    [Fact]
    public void TheLibrarysNeutralGrey_OnWhite_IsAboutFour() =>
        Assert.Equal(3.95, Colors.Gray.ContrastRatioWith(Colors.White), precision: 2);
}
