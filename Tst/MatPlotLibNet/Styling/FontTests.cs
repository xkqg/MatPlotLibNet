// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>Verifies <see cref="Font"/> behavior.</summary>
public class FontTests
{
    /// <summary>Verifies that the default font family is sans-serif.</summary>
    [Fact]
    public void DefaultFont_HasSansSerif()
    {
        var font = new Font();
        Assert.Equal("sans-serif", font.Family);
    }

    /// <summary>Verifies that the default font size is 12.</summary>
    [Fact]
    public void DefaultFont_Size12()
    {
        var font = new Font();
        Assert.Equal(12, font.Size);
    }

    /// <summary>Verifies that the default font weight is Normal.</summary>
    [Fact]
    public void DefaultFont_NormalWeight()
    {
        var font = new Font();
        Assert.Equal(FontWeight.Normal, font.Weight);
    }

    /// <summary>Verifies that the default font slant is Normal.</summary>
    [Fact]
    public void DefaultFont_NormalSlant()
    {
        var font = new Font();
        Assert.Equal(FontSlant.Normal, font.Slant);
    }

    /// <summary>Verifies that the default font color is null.</summary>
    [Fact]
    public void DefaultFont_NullColor()
    {
        var font = new Font();
        Assert.Null(font.Color);
    }

    /// <summary>Verifies that all font properties can be set and retrieved correctly.</summary>
    [Fact]
    public void Properties_CanBeSet()
    {
        var font = new Font
        {
            Family = "monospace",
            Size = 16,
            Weight = FontWeight.Bold,
            Slant = FontSlant.Italic,
            Color = Color.Red
        };

        Assert.Equal("monospace", font.Family);
        Assert.Equal(16, font.Size);
        Assert.Equal(FontWeight.Bold, font.Weight);
        Assert.Equal(FontSlant.Italic, font.Slant);
        Assert.Equal(Color.Red, font.Color);
    }
}
