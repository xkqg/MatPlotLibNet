// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>v1.10 — Rec. 709 luminance for use in cell-text auto-contrast (heatmap annotations etc.).</summary>
public class ColorLuminanceTests
{
    [Fact]
    public void Black_LuminanceIsZero()
    {
        Assert.Equal(0.0, Colors.Black.Luminance(), precision: 6);
    }

    [Fact]
    public void White_LuminanceIsOne()
    {
        Assert.Equal(1.0, Colors.White.Luminance(), precision: 6);
    }

    [Fact]
    public void Red_LuminanceMatchesRec709Coefficient()
    {
        // Rec. 709: L = 0.2126·R + 0.7152·G + 0.0722·B  (channels normalised to [0,1])
        Assert.Equal(0.2126, new Color(255, 0, 0).Luminance(), precision: 4);
    }

    [Fact]
    public void Green_LuminanceMatchesRec709Coefficient()
    {
        Assert.Equal(0.7152, new Color(0, 255, 0).Luminance(), precision: 4);
    }

    [Fact]
    public void Blue_LuminanceMatchesRec709Coefficient()
    {
        Assert.Equal(0.0722, new Color(0, 0, 255).Luminance(), precision: 4);
    }

    [Fact]
    public void MidGray_LuminanceIsHalf()
    {
        // 128/255 ≈ 0.5019; multiplied by sum of weights (1.0) gives ≈ 0.5019.
        var midGray = new Color(128, 128, 128);
        Assert.InRange(midGray.Luminance(), 0.49, 0.51);
    }

    [Fact]
    public void GreenMoreLuminantThanRedMoreLuminantThanBlue()
    {
        var r = new Color(255, 0, 0).Luminance();
        var g = new Color(0, 255, 0).Luminance();
        var b = new Color(0, 0, 255).Luminance();
        Assert.True(g > r);
        Assert.True(r > b);
    }

    // ── ContrastingTextColor ─────────────────────────────────────────────────

    [Fact]
    public void ContrastingTextColor_White_PicksBlack() =>
        Assert.Equal(Colors.Black, Colors.White.ContrastingTextColor());

    [Fact]
    public void ContrastingTextColor_Black_PicksWhite() =>
        Assert.Equal(Colors.White, Colors.Black.ContrastingTextColor());

    [Fact]
    public void ContrastingTextColor_JustAboveHalf_PicksBlack()
    {
        // Color(128,128,128): luminance = 128/255 ≈ 0.5020 ≥ 0.5 → black.
        Assert.Equal(Colors.Black, new Color(128, 128, 128).ContrastingTextColor());
    }

    [Fact]
    public void ContrastingTextColor_JustBelowHalf_PicksWhite()
    {
        // Color(127,127,127): luminance = 127/255 ≈ 0.4980 < 0.5 → white.
        Assert.Equal(Colors.White, new Color(127, 127, 127).ContrastingTextColor());
    }
}
