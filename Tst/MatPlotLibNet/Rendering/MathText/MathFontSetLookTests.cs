// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.MathText;

/// <summary>Which faces math is set in is a value of a theme's text look: matplotlib's <c>mathtext.fontset</c> under its themes, the
/// library's own (the label's face) under the library's. Printed with matplotlib 3.11.2: <c>rcParams['mathtext.fontset']</c> is
/// 'dejavusans' under the default style and 'cm' after <c>plt.style.use('classic')</c>. The figure JSON carries it by that name.</summary>
public sealed class MathFontSetLookTests
{
    [Fact]
    public void TheClassicTheme_SetsMathInComputerModern()
    {
        Assert.Same(MathFontSet.ComputerModern, Theme.MatplotlibClassic.Looks.Text.MathFonts);
    }

    [Fact]
    public void TheV2Theme_SetsMathInDejaVuSans()
    {
        Assert.Same(MathFontSet.DejaVuSans, Theme.MatplotlibV2.Looks.Text.MathFonts);

        var dto = ThemeDto.From(Theme.MatplotlibV2);
        Assert.Equal("dejavusans", dto.MathTextFontSet);
        Assert.Same(MathFontSet.DejaVuSans, dto.ToTheme().Looks.Text.MathFonts);
    }

    [Fact]
    public void TheLibraryThemes_SetMathInTheLabelsFace()
    {
        Assert.Same(MathFontSet.Library, Theme.Default.Looks.Text.MathFonts);
        Assert.Same(MathFontSet.Library, ThemeLooks.Library.Text.MathFonts);
    }

    [Fact]
    public void TheJson_CarriesTheSetByMatplotlibsName_AndOnlyWhenItIsNotTheLibrarysOwn()
    {
        var dto = ThemeDto.From(Theme.MatplotlibClassic);

        Assert.Equal("cm", dto.MathTextFontSet);
        Assert.Same(MathFontSet.ComputerModern, dto.ToTheme().Looks.Text.MathFonts);
        Assert.Null(ThemeDto.From(Theme.Default).MathTextFontSet);
        Assert.Same(MathFontSet.Library, Theme.Default.ThroughDto().Looks.Text.MathFonts);
    }
}
