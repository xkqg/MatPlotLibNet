// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>Whether a font is bold or italic, asked of the font once for every surface that draws it. Only a bold weight is bold: a light
/// weight is drawn as the regular face is. An italic and an oblique slant both lean, as matplotlib sets them (its font manager takes an
/// oblique face for an italic font and an italic face for an oblique one), and math asks the same slant whether a letter leans.</summary>
public class FontStyleExtensionsTests
{
    [Theory]
    [InlineData(FontWeight.Light, false)]
    [InlineData(FontWeight.Normal, false)]
    [InlineData(FontWeight.Bold, true)]
    public void AFont_IsBold_OnlyAtABoldWeight(FontWeight weight, bool bold)
    {
        Assert.Equal(bold, new Font { Weight = weight }.IsBold);
    }

    [Theory]
    [InlineData(FontSlant.Normal, false)]
    [InlineData(FontSlant.Oblique, true)]
    [InlineData(FontSlant.Italic, true)]
    public void ASlant_Leans_WhenItIsItalicOrOblique(FontSlant slant, bool leans)
    {
        Assert.Equal(leans, slant.IsSlanted);
        Assert.Equal(leans, new Font { Slant = slant }.IsItalic);
        Assert.Equal(leans, new MathFace(new Font { Slant = slant }).Slanted);
        Assert.Equal(leans, new MathFace(null, Slant: slant).Slanted);
    }
}
