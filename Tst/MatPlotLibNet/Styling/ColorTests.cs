// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

public class ColorTests
{
    [Fact]
    public void Constructor_SetsRgba()
    {
        var color = new Color(255, 128, 0, 200);
        Assert.Equal(255, color.R);
        Assert.Equal(128, color.G);
        Assert.Equal(0, color.B);
        Assert.Equal(200, color.A);
    }

    [Fact]
    public void DefaultAlpha_Is255()
    {
        var color = new Color(100, 100, 100);
        Assert.Equal(255, color.A);
    }

    [Fact]
    public void FromHex_ParsesThreeByteHex()
    {
        var color = Color.FromHex("#FF8000");
        Assert.Equal(255, color.R);
        Assert.Equal(128, color.G);
        Assert.Equal(0, color.B);
        Assert.Equal(255, color.A);
    }

    [Fact]
    public void FromHex_ParsesFourByteHex()
    {
        var color = Color.FromHex("#FF8000C8");
        Assert.Equal(255, color.R);
        Assert.Equal(128, color.G);
        Assert.Equal(0, color.B);
        Assert.Equal(200, color.A);
    }

    [Fact]
    public void FromHex_WithoutHash_Works()
    {
        var color = Color.FromHex("FF8000");
        Assert.Equal(255, color.R);
        Assert.Equal(128, color.G);
        Assert.Equal(0, color.B);
    }

    [Fact]
    public void ToHex_ProducesUppercaseHex()
    {
        var color = new Color(255, 128, 0);
        Assert.Equal("#FF8000", color.ToHex());
    }

    [Fact]
    public void ToRgbaString_FormatsCorrectly()
    {
        var color = new Color(255, 128, 0, 128);
        var rgba = color.ToRgbaString();
        Assert.StartsWith("rgba(255,128,0,", rgba);
    }

    [Fact]
    public void NamedColor_Red_IsCorrect()
    {
        Assert.Equal(new Color(255, 0, 0), Color.Red);
    }

    [Fact]
    public void NamedColor_Blue_IsCorrect()
    {
        Assert.Equal(new Color(0, 0, 255), Color.Blue);
    }

    [Fact]
    public void NamedColor_White_IsCorrect()
    {
        Assert.Equal(new Color(255, 255, 255), Color.White);
    }

    [Fact]
    public void NamedColor_Black_IsCorrect()
    {
        Assert.Equal(new Color(0, 0, 0), Color.Black);
    }

    [Fact]
    public void FromRgba_NormalizesDoubles()
    {
        var color = Color.FromRgba(1.0, 0.5, 0.0, 0.5);
        Assert.Equal(255, color.R);
        Assert.Equal(128, color.G);
        Assert.Equal(0, color.B);
        Assert.Equal(128, color.A);
    }

    [Fact]
    public void FromRgba_DefaultAlpha_IsOpaque()
    {
        var color = Color.FromRgba(1.0, 0.0, 0.0);
        Assert.Equal(255, color.A);
    }

    [Fact]
    public void Equality_WorksAsValueType()
    {
        Assert.Equal(Color.Red, new Color(255, 0, 0));
        Assert.NotEqual(Color.Red, Color.Blue);
    }

    [Fact]
    public void WithAlpha_ReturnsNewColorWithModifiedAlpha()
    {
        var color = Color.Red.WithAlpha(128);
        Assert.Equal(255, color.R);
        Assert.Equal(0, color.G);
        Assert.Equal(0, color.B);
        Assert.Equal(128, color.A);
    }

    [Fact]
    public void FromHex_InvalidHex_Throws()
    {
        Assert.Throws<FormatException>(() => Color.FromHex("not-a-color"));
    }
}
