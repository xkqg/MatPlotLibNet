// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>Verifies <see cref="BlendMode"/> enum and <see cref="CompositeOperation"/> blending math.</summary>
public class BlendModeTests
{
    // ── BlendMode enum values ─────────────────────────────────────────────────

    [Fact]
    public void BlendMode_HasNormal()   => Assert.Equal(BlendMode.Normal,   (BlendMode)0);
    [Fact]
    public void BlendMode_HasMultiply() => _ = BlendMode.Multiply;
    [Fact]
    public void BlendMode_HasScreen()   => _ = BlendMode.Screen;
    [Fact]
    public void BlendMode_HasOverlay()  => _ = BlendMode.Overlay;

    // ── Normal blend (alpha compositing) ─────────────────────────────────────

    [Fact]
    public void Normal_FullAlpha_ReturnsSrc()
    {
        // alpha=1 → src completely replaces dst
        var src = new Color(255, 0, 0);
        var dst = new Color(0, 0, 255);
        var result = CompositeOperation.Blend(src, dst, BlendMode.Normal, alpha: 1.0);
        Assert.Equal(src, result);
    }

    [Fact]
    public void Normal_ZeroAlpha_ReturnsDst()
    {
        var src = new Color(255, 0, 0);
        var dst = new Color(0, 0, 255);
        var result = CompositeOperation.Blend(src, dst, BlendMode.Normal, alpha: 0.0);
        Assert.Equal(dst, result);
    }

    [Fact]
    public void Normal_HalfAlpha_BlendsMidpoint()
    {
        var src = new Color(200, 0, 0);
        var dst = new Color(0, 200, 0);
        var result = CompositeOperation.Blend(src, dst, BlendMode.Normal, alpha: 0.5);
        // R ≈ 100, G ≈ 100
        Assert.InRange(result.R, 95, 105);
        Assert.InRange(result.G, 95, 105);
    }

    // ── Multiply blend ────────────────────────────────────────────────────────

    [Fact]
    public void Multiply_WithWhite_ReturnsSrc()
    {
        // src * white (255,255,255) normalized = src * 1 = src
        var src = new Color(128, 64, 32);
        var white = new Color(255, 255, 255);
        var result = CompositeOperation.Blend(src, white, BlendMode.Multiply, alpha: 1.0);
        Assert.InRange(result.R, 120, 135);
        Assert.InRange(result.G, 57, 70);
    }

    [Fact]
    public void Multiply_WithBlack_ReturnsBlack()
    {
        var src = new Color(200, 150, 100);
        var black = new Color(0, 0, 0);
        var result = CompositeOperation.Blend(src, black, BlendMode.Multiply, alpha: 1.0);
        Assert.Equal(0, result.R);
        Assert.Equal(0, result.G);
        Assert.Equal(0, result.B);
    }

    // ── Screen blend ─────────────────────────────────────────────────────────

    [Fact]
    public void Screen_WithBlack_ReturnsSrc()
    {
        // Screen(src, black=0) = 1-(1-src)*(1-0) = src
        var src = new Color(200, 100, 50);
        var black = new Color(0, 0, 0);
        var result = CompositeOperation.Blend(src, black, BlendMode.Screen, alpha: 1.0);
        Assert.InRange(result.R, 195, 205);
    }

    [Fact]
    public void Screen_TwoValues_IsAtLeastMax()
    {
        var src = new Color(100, 0, 0);
        var dst = new Color(150, 0, 0);
        var result = CompositeOperation.Blend(src, dst, BlendMode.Screen, alpha: 1.0);
        // Screen always brightens: result >= max(src, dst)
        Assert.True(result.R >= 150, $"Screen result {result.R} should be >= max(100,150)=150");
    }

    // ── Overlay blend ─────────────────────────────────────────────────────────

    [Fact]
    public void Overlay_DarkBase_UsesMultiply()
    {
        // dst < 128 → overlay = 2*src*dst/255
        var src = new Color(100, 0, 0);
        var dst = new Color(50, 0, 0);    // dark: 50 < 128
        var result = CompositeOperation.Blend(src, dst, BlendMode.Overlay, alpha: 1.0);
        // 2 * (100/255) * (50/255) * 255 ≈ 39
        Assert.InRange(result.R, 30, 50);
    }

    [Fact]
    public void Overlay_LightBase_UsesScreen()
    {
        // dst >= 128 → overlay = 1 - 2*(1-src)*(1-dst)
        var src = new Color(200, 0, 0);
        var dst = new Color(200, 0, 0);   // light: 200 >= 128
        var result = CompositeOperation.Blend(src, dst, BlendMode.Overlay, alpha: 1.0);
        Assert.True(result.R > 200, $"Overlay light result {result.R} should be > 200");
    }
}
