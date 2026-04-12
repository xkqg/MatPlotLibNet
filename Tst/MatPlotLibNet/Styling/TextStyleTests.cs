// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>Verifies <see cref="TextStyle"/> behavior and its <c>ApplyTo</c> merge logic.</summary>
public class TextStyleTests
{
    private static readonly Font BaseFont = new()
    {
        Family = "sans-serif",
        Size = 13,
        Weight = FontWeight.Normal,
        Slant = FontSlant.Normal,
        Color = null
    };

    /// <summary>An all-null TextStyle applied to a base Font returns the base Font unchanged.</summary>
    [Fact]
    public void TextStyle_AllNull_ApplyTo_ReturnsBaseFont()
    {
        var style = new TextStyle();
        var result = style.ApplyTo(BaseFont);

        Assert.Equal(BaseFont.Family, result.Family);
        Assert.Equal(BaseFont.Size, result.Size);
        Assert.Equal(BaseFont.Weight, result.Weight);
        Assert.Equal(BaseFont.Slant, result.Slant);
        Assert.Null(result.Color);
    }

    /// <summary>FontSize override replaces the base font size.</summary>
    [Fact]
    public void TextStyle_FontSizeOverride_AppliedToBaseFont()
    {
        var style = new TextStyle { FontSize = 20 };
        var result = style.ApplyTo(BaseFont);

        Assert.Equal(20, result.Size);
        Assert.Equal(BaseFont.Family, result.Family);
        Assert.Equal(BaseFont.Weight, result.Weight);
    }

    /// <summary>FontWeight override replaces the base font weight.</summary>
    [Fact]
    public void TextStyle_WeightOverride_AppliedToBaseFont()
    {
        var style = new TextStyle { FontWeight = FontWeight.Bold };
        var result = style.ApplyTo(BaseFont);

        Assert.Equal(FontWeight.Bold, result.Weight);
        Assert.Equal(BaseFont.Size, result.Size);
    }

    /// <summary>Color override replaces the base font color.</summary>
    [Fact]
    public void TextStyle_ColorOverride_AppliedToBaseFont()
    {
        var style = new TextStyle { Color = Colors.Red };
        var result = style.ApplyTo(BaseFont);

        Assert.Equal(Colors.Red, result.Color);
        Assert.Equal(BaseFont.Size, result.Size);
    }

    /// <summary>Multiple overrides are all applied simultaneously.</summary>
    [Fact]
    public void TextStyle_MultipleOverrides_AllApplied()
    {
        var style = new TextStyle
        {
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            FontFamily = "monospace",
            Color = Colors.Blue
        };
        var result = style.ApplyTo(BaseFont);

        Assert.Equal(18, result.Size);
        Assert.Equal(FontWeight.Bold, result.Weight);
        Assert.Equal("monospace", result.Family);
        Assert.Equal(Colors.Blue, result.Color);
    }

    /// <summary>Pad defaults to null (no override).</summary>
    [Fact]
    public void TextStyle_Pad_DefaultsToNull()
    {
        var style = new TextStyle();
        Assert.Null(style.Pad);
    }

    /// <summary>FontFamily override replaces the base font family.</summary>
    [Fact]
    public void TextStyle_FontFamilyOverride_AppliedToBaseFont()
    {
        var style = new TextStyle { FontFamily = "serif" };
        var result = style.ApplyTo(BaseFont);

        Assert.Equal("serif", result.Family);
        Assert.Equal(BaseFont.Size, result.Size);
    }

    /// <summary>ApplyTo returns a new Font — the base Font is not mutated.</summary>
    [Fact]
    public void TextStyle_ApplyTo_DoesNotMutateBase()
    {
        var style = new TextStyle { FontSize = 24 };
        var original = BaseFont;
        _ = style.ApplyTo(original);

        Assert.Equal(13, original.Size);
    }

    /// <summary>FontSlant override replaces the base font slant.</summary>
    [Fact]
    public void TextStyle_FontSlantOverride_AppliedToBaseFont()
    {
        var style = new TextStyle { FontSlant = FontSlant.Italic };
        var result = style.ApplyTo(BaseFont);

        Assert.Equal(FontSlant.Italic, result.Slant);
        Assert.Equal(BaseFont.Size, result.Size);
    }
}
