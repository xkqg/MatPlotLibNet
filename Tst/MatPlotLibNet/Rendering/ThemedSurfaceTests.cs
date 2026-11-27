// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>What a drawing surface keeps of what a draw tells it (<see cref="ThemedSurface"/>), once for all surfaces: the looks of the
/// theme, the math texts of the figure, the opacity of the marks, and what a surface that draws in the package's own fonts does with
/// them. A surface that was told nothing draws as a library theme does, in full opacity.</summary>
public class ThemedSurfaceTests
{
    [Fact]
    public void ASurfaceThatWasToldNothing_DrawsAsALibraryThemeDoes()
    {
        var surface = new ThemedSurface();

        Assert.Same(ThemeLooks.Library, surface.Looks);
        Assert.Null(surface.MathTexts);
        Assert.Equal(1.0, surface.Opacity);
    }

    /// <summary>A surface lives inside every context, so a drawn figure carries what it weighs: the three things it holds, each at the size
    /// of a reference or a double, and no more (an opacity held as an optional double would add eight bytes to every context).</summary>
    [Fact]
    public void TheSurface_WeighsNoMoreThanTheThreeThingsItHolds()
    {
        Assert.Equal(2 * IntPtr.Size + sizeof(double), System.Runtime.CompilerServices.Unsafe.SizeOf<ThemedSurface>());
    }

    /// <summary>The surface a context holds as a field is told nothing yet, so it draws in full opacity from the first mark on.</summary>
    [Fact]
    public void ASurfaceContext_DrawsInFullOpacity_BeforeItIsToldAnother()
    {
        Assert.Equal(1.0, ((IOpacityRenderContext)new SvgRenderContext()).Opacity);
    }

    [Fact]
    public void TheLooksAndTheMathTexts_AreWhatTheSurfaceWasToldLast()
    {
        var surface = new ThemedSurface();
        var texts = new KeptMathTexts();

        surface.Looks = Theme.MatplotlibV2.Looks;
        surface.MathTexts = texts;

        Assert.Same(Theme.MatplotlibV2.Looks, surface.Looks);
        Assert.Same(texts, surface.MathTexts);
    }

    [Theory]
    [InlineData(-3.0, 0.0)]
    [InlineData(0.4, 0.4)]
    [InlineData(7.0, 1.0)]
    public void AnOpacity_IsHeldBetweenZeroAndOne_AndTheHeldValueIsGivenBack(double asked, double held)
    {
        var surface = new ThemedSurface();

        double returned = surface.HoldOpacity(asked);

        Assert.Equal(held, returned);
        Assert.Equal(held, surface.Opacity);
    }

    [Fact]
    public void TheBoxOfATextIsMeasuredByTheInstalledMetrics()
    {
        Assert.Same(FontMetricsDefault.Current, new ThemedSurface().BoxMetrics);
    }

    /// <summary>A surface that draws math in the package's own fonts sets it in the faces of the looks it was told; one that draws in
    /// the viewer's fonts sets it in the label's face, the library's own set, whatever it was told.</summary>
    [Fact]
    public void MathIsSetInTheFacesOfTheLooks_WhereTheSurfaceDrawsInThePackagesFonts_AndInTheLibrarysOtherwise()
    {
        var surface = new ThemedSurface { Looks = Theme.MatplotlibV2.Looks };

        Assert.Same(Theme.MatplotlibV2.Looks.Text.MathFonts, surface.MathFontsFor(inPackageFonts: true));
        Assert.Same(MathFontSet.Library, surface.MathFontsFor(inPackageFonts: false));
    }

    [Fact]
    public void ASurfaceThatHoldsLooks_AnswersTheMembersOfTheInterfaceFromItsSurface()
    {
        var svg = new SvgRenderContext();
        var told = (IThemedRenderContext)svg;
        var texts = new KeptMathTexts();

        told.UseLooks(Theme.MatplotlibV2.Looks);
        told.UseMathTexts(texts);
        svg.SetOpacity(0.25);

        Assert.Same(Theme.MatplotlibV2.Looks, told.Looks);
        Assert.Same(texts, told.MathTexts);
        Assert.Same(FontMetricsDefault.Current, told.BoxMetrics);
        Assert.Equal(0.25, ((IOpacityRenderContext)svg).Opacity);
    }
}
