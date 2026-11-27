// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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

    [Theory]
    [InlineData("#FF8000", 255)]
    [InlineData("#FF8000C8", 200)]
    public void FromHex_ParsesThreeAndFourByteHex(string hex, int alpha)
    {
        var color = Color.FromHex(hex);
        Assert.Equal(255, color.R);
        Assert.Equal(128, color.G);
        Assert.Equal(0, color.B);
        Assert.Equal(alpha, color.A);
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

    /// <summary>Verifies that primary named colours in <see cref="Colors"/> match their expected RGB values.</summary>
    public static TheoryData<Color, byte, byte, byte> NamedColorCases => new()
    {
        { Colors.Red,   255,   0,   0 },
        { Colors.Blue,    0,   0, 255 },
        { Colors.White, 255, 255, 255 },
        { Colors.Black,   0,   0,   0 },
    };

    [Theory]
    [MemberData(nameof(NamedColorCases))]
    public void NamedColor_HasExpectedRgb(Color actual, byte r, byte g, byte b)
        => Assert.Equal(new Color(r, g, b), actual);

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

    /// <summary>matplotlib's <c>to_rgba('#abc')</c> reads each digit doubled, (170, 187, 204, 255), and <c>to_rgba('#abcd')</c> adds
    /// the alpha 221 (matplotlib 3.11.2); <c>FromHex</c> reads the same short forms, with or without the '#'.</summary>
    [Theory]
    [InlineData("#abc", 0xAA, 0xBB, 0xCC, 255)]
    [InlineData("#ABC", 0xAA, 0xBB, 0xCC, 255)]
    [InlineData("abc", 0xAA, 0xBB, 0xCC, 255)]
    [InlineData("#abcd", 0xAA, 0xBB, 0xCC, 0xDD)]
    [InlineData("#f008", 0xFF, 0x00, 0x00, 0x88)]
    public void FromHex_AShortForm_DoublesEachDigit(string hex, int r, int g, int b, int a) =>
        Assert.Equal(new Color((byte)r, (byte)g, (byte)b, (byte)a), Color.FromHex(hex));

    /// <summary>Verifies that a length other than 3, 4, 6 and 8, and a digit that is not hexadecimal, are refused.</summary>
    [Theory]
    [InlineData("#ab")]
    [InlineData("#abcde")]
    [InlineData("#abcdeff")]
    [InlineData("#ggg")]
    public void FromHex_AnyOtherLengthOrDigit_Throws(string hex) =>
        Assert.Throws<FormatException>(() => Color.FromHex(hex));

    /// <summary>Verifies that FromHex throws FormatException for an invalid hex string.</summary>
    [Fact]
    public void FromHex_InvalidHex_Throws()
    {
        Assert.Throws<FormatException>(() => Color.FromHex("not-a-color"));
    }
}
