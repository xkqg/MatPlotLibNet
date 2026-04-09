// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>Verifies <see cref="Color"/> behavior.</summary>
public class ColorTests
{
    /// <summary>Verifies that the constructor sets R, G, B, and A components correctly.</summary>
    [Fact]
    public void Constructor_SetsRgba()
    {
        var color = new Color(255, 128, 0, 200);
        Assert.Equal(255, color.R);
        Assert.Equal(128, color.G);
        Assert.Equal(0, color.B);
        Assert.Equal(200, color.A);
    }

    /// <summary>Verifies that alpha defaults to 255 when not specified.</summary>
    [Fact]
    public void DefaultAlpha_Is255()
    {
        var color = new Color(100, 100, 100);
        Assert.Equal(255, color.A);
    }

    /// <summary>Verifies that FromHex parses a 6-digit hex string into the correct RGB values.</summary>
    [Fact]
    public void FromHex_ParsesThreeByteHex()
    {
        var color = Color.FromHex("#FF8000");
        Assert.Equal(255, color.R);
        Assert.Equal(128, color.G);
        Assert.Equal(0, color.B);
        Assert.Equal(255, color.A);
    }

    /// <summary>Verifies that FromHex parses an 8-digit hex string including the alpha channel.</summary>
    [Fact]
    public void FromHex_ParsesFourByteHex()
    {
        var color = Color.FromHex("#FF8000C8");
        Assert.Equal(255, color.R);
        Assert.Equal(128, color.G);
        Assert.Equal(0, color.B);
        Assert.Equal(200, color.A);
    }

    /// <summary>Verifies that FromHex works without a leading hash character.</summary>
    [Fact]
    public void FromHex_WithoutHash_Works()
    {
        var color = Color.FromHex("FF8000");
        Assert.Equal(255, color.R);
        Assert.Equal(128, color.G);
        Assert.Equal(0, color.B);
    }

    /// <summary>Verifies that ToHex produces an uppercase hex string with a leading hash.</summary>
    [Fact]
    public void ToHex_ProducesUppercaseHex()
    {
        var color = new Color(255, 128, 0);
        Assert.Equal("#FF8000", color.ToHex());
    }

    /// <summary>Verifies that ToRgbaString produces the correct rgba() format.</summary>
    [Fact]
    public void ToRgbaString_FormatsCorrectly()
    {
        var color = new Color(255, 128, 0, 128);
        var rgba = color.ToRgbaString();
        Assert.StartsWith("rgba(255,128,0,", rgba);
    }

    /// <summary>Verifies that Colors.Red matches the expected RGB values.</summary>
    [Fact]
    public void NamedColor_Red_IsCorrect()
    {
        Assert.Equal(new Color(255, 0, 0), Colors.Red);
    }

    /// <summary>Verifies that Colors.Blue matches the expected RGB values.</summary>
    [Fact]
    public void NamedColor_Blue_IsCorrect()
    {
        Assert.Equal(new Color(0, 0, 255), Colors.Blue);
    }

    /// <summary>Verifies that Colors.White matches the expected RGB values.</summary>
    [Fact]
    public void NamedColor_White_IsCorrect()
    {
        Assert.Equal(new Color(255, 255, 255), Colors.White);
    }

    /// <summary>Verifies that Colors.Black matches the expected RGB values.</summary>
    [Fact]
    public void NamedColor_Black_IsCorrect()
    {
        Assert.Equal(new Color(0, 0, 0), Colors.Black);
    }

    /// <summary>Verifies that FromRgba normalizes 0.0-1.0 double values to 0-255 byte range.</summary>
    [Fact]
    public void FromRgba_NormalizesDoubles()
    {
        var color = Color.FromRgba(1.0, 0.5, 0.0, 0.5);
        Assert.Equal(255, color.R);
        Assert.Equal(128, color.G);
        Assert.Equal(0, color.B);
        Assert.Equal(128, color.A);
    }

    /// <summary>Verifies that FromRgba defaults to fully opaque when alpha is omitted.</summary>
    [Fact]
    public void FromRgba_DefaultAlpha_IsOpaque()
    {
        var color = Color.FromRgba(1.0, 0.0, 0.0);
        Assert.Equal(255, color.A);
    }

    /// <summary>Verifies that Color equality behaves as a value type comparison.</summary>
    [Fact]
    public void Equality_WorksAsValueType()
    {
        Assert.Equal(Colors.Red, new Color(255, 0, 0));
        Assert.NotEqual(Colors.Red, Colors.Blue);
    }

    /// <summary>Verifies that WithAlpha returns a new color with only the alpha channel changed.</summary>
    [Fact]
    public void WithAlpha_ReturnsNewColorWithModifiedAlpha()
    {
        var color = Colors.Red.WithAlpha(128);
        Assert.Equal(255, color.R);
        Assert.Equal(0, color.G);
        Assert.Equal(0, color.B);
        Assert.Equal(128, color.A);
    }

    /// <summary>Verifies that FromHex throws FormatException for an invalid hex string.</summary>
    [Fact]
    public void FromHex_InvalidHex_Throws()
    {
        Assert.Throws<FormatException>(() => Color.FromHex("not-a-color"));
    }
}
