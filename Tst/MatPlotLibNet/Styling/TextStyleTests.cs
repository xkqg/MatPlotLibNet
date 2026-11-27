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

        Assert.Equivalent(new { BaseFont.Family, BaseFont.Size, BaseFont.Weight, BaseFont.Slant }, result);
        Assert.Null(result.Color);
    }

    /// <summary>An override replaces the one member of the base font it names (size, weight, colour, family, slant) and leaves the rest.</summary>
    public static TheoryData<TextStyle, Font> Overrides { get; } = new()
    {
        { new TextStyle { FontSize = 20 }, BaseFont with { Size = 20 } },
        { new TextStyle { FontWeight = FontWeight.Bold }, BaseFont with { Weight = FontWeight.Bold } },
        { new TextStyle { Color = Colors.Red }, BaseFont with { Color = Colors.Red } },
        { new TextStyle { FontFamily = "serif" }, BaseFont with { Family = "serif" } },
        { new TextStyle { FontSlant = FontSlant.Italic }, BaseFont with { Slant = FontSlant.Italic } },
    };

    [Theory]
    [MemberData(nameof(Overrides))]
    public void TextStyle_AnOverride_IsAppliedToBaseFont(TextStyle style, Font expected) =>
        Assert.Equal(expected, style.ApplyTo(BaseFont));

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

    /// <summary>ApplyTo returns a new Font — the base Font is not mutated.</summary>
    [Fact]
    public void TextStyle_ApplyTo_DoesNotMutateBase()
    {
        var style = new TextStyle { FontSize = 24 };
        var original = BaseFont;
        _ = style.ApplyTo(original);

        Assert.Equal(13, original.Size);
    }
}
