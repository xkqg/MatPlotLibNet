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

    // ── a translucent colour over what is behind it ──────────────────────────────────────────────────

    [Fact]
    public void AnOpaqueColour_OverAnything_IsItself() =>
        Assert.Equal(Color.FromHex("#4C78A8"), Color.FromHex("#4C78A8").CompositedOver(Colors.Red));

    [Fact]
    public void ATransparentColour_OverABackdrop_IsTheBackdrop() =>
        Assert.Equal(Color.FromHex("#102030"), Colors.White.WithAlpha(0).CompositedOver(Color.FromHex("#102030")));

    [Fact]
    public void AHalfTransparentColour_OverABackdrop_IsTheMixOfTheTwo() =>
        Assert.Equal(new Color(128, 128, 128), Colors.White.WithAlpha(128).CompositedOver(Colors.Black));

    // ── the grey a graphic wears over a plot ─────────────────────────────────────────────────────────

    /// <summary>The oracle: of all 256 greys, the ones that reach 3 : 1 against the plot, the one nearest to #808080 (the lighter
    /// when two are equally near). The grey the library derives is that one.</summary>
    private static Color Nearest(Color plot) =>
        Enumerable.Range(0, 256)
            .Where(level => new Color((byte)level, (byte)level, (byte)level).ContrastRatioWith(plot) >= 3.0)
            .OrderBy(level => Math.Abs(level - 128)).ThenByDescending(level => level)
            .Select(level => new Color((byte)level, (byte)level, (byte)level))
            .First();

    [Fact]
    public void TheNeutralGrey_OfEveryGreyPlot_IsTheNearestGreyAtThreeToOne()
    {
        for (int level = 0; level < 256; level++)
        {
            var plot = new Color((byte)level, (byte)level, (byte)level);

            Assert.Equal(Nearest(plot), plot.NeutralGrey());
        }
    }

    /// <summary>The two plots most charts have keep the library's grey, and a plot it cannot be seen against gets another.</summary>
    [Theory]
    [InlineData("#FFFFFF", "#808080")]
    [InlineData("#000000", "#808080")]
    public void TheNeutralGrey_OfAWhiteOrABlackPlot_IsTheLibrarysGrey(string plotHex, string greyHex) =>
        Assert.Equal(greyHex, Color.FromHex(plotHex).NeutralGrey().ToHex());

    [Fact]
    public void TheNeutralGrey_OfAPlotItCannotBeSeenAgainst_IsAnotherGrey()
    {
        var plot = Color.FromHex("#808080");

        Assert.NotEqual(Color.FromHex("#808080"), plot.NeutralGrey());
        Assert.True(plot.NeutralGrey().ContrastRatioWith(plot) >= 3.0);
    }

    /// <summary>On #008A00 the first grey that reaches 3 : 1 is 83 levels from #808080 on both sides, #D3D3D3 and #2D2D2D: the
    /// lighter one is taken, so the choice never depends on the order the candidates are tried in.</summary>
    [Fact]
    public void TheNeutralGrey_WhenALighterAndADarkerGreyAreEquallyNear_IsTheLighter()
    {
        var plot = Color.FromHex("#008A00");

        Assert.True(Color.FromHex("#D3D3D3").ContrastRatioWith(plot) >= 3.0);
        Assert.True(Color.FromHex("#2D2D2D").ContrastRatioWith(plot) >= 3.0);
        Assert.Equal("#D3D3D3", plot.NeutralGrey().ToHex());
    }

    /// <summary>Not only greys: a plot of any colour gets the grey the oracle finds, and it always reaches 3 : 1.</summary>
    [Fact]
    public void TheNeutralGrey_OfPlotsOfAnyColour_IsTheNearestGreyAtThreeToOne()
    {
        var random = new Random(42);
        for (int i = 0; i < 400; i++)
        {
            var plot = new Color((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256));

            Assert.Equal(Nearest(plot), plot.NeutralGrey());
            Assert.True(plot.NeutralGrey().ContrastRatioWith(plot) >= 3.0);
        }
    }
}
