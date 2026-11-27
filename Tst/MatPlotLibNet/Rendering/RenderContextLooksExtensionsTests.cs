// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The one place a surface is told the looks of its theme and asked for them (<see cref="RenderContextLooksExtensions"/>):
/// a surface that holds looks answers with what it was told; one that holds none takes nothing and answers with the library's own.</summary>
public class RenderContextLooksExtensionsTests
{
    [Fact]
    public void ASurfaceThatHoldsLooks_AnswersWithTheLooksItWasTold()
    {
        IRenderContext svg = new SvgRenderContext();

        svg.TellLooks(Theme.MatplotlibV2.Looks);

        Assert.Same(Theme.MatplotlibV2.Looks, svg.LooksTold);
        Assert.Same(((IThemedRenderContext)svg).MathFonts, svg.MathFontsTold);
    }

    [Fact]
    public void ASurfaceThatHoldsNoLooks_TakesNone_AndAnswersWithTheLibrarysOwn()
    {
        IRenderContext plain = new RecordingRenderContext();

        plain.TellLooks(Theme.MatplotlibV2.Looks);
        plain.UseLooksOf(Theme.MatplotlibV2);

        Assert.Same(ThemeLooks.Library, plain.LooksTold);
        Assert.Same(MathFontSet.Library, plain.MathFontsTold);
    }

    /// <summary>A surface of your own is told no math texts and names no metrics: a label drawn on it is parsed each time and its math
    /// measured by the surface itself.</summary>
    [Fact]
    public void ASurfaceThatHoldsNoLooks_TakesNoMathTexts_AndNamesNoMetrics()
    {
        IRenderContext plain = new RecordingRenderContext();

        plain.UseStageOf(new KeptMathTexts(), Theme.MatplotlibV2);

        Assert.Null(plain.MathTextsTold);
        Assert.Null(plain.BoxMetricsTold);
    }

    /// <summary>A surface of the library is told the math texts of each stage and measures the box of a text by the installed metrics.</summary>
    [Fact]
    public void ASurfaceOfTheLibrary_TakesTheMathTexts_AndNamesTheInstalledMetrics()
    {
        IRenderContext svg = new SvgRenderContext();
        var texts = new KeptMathTexts();

        svg.UseStageOf(texts, Theme.MatplotlibClassic);

        Assert.Same(texts, svg.MathTextsTold);
        Assert.Same(Theme.MatplotlibClassic.Looks, svg.LooksTold);
        Assert.Same(FontMetricsDefault.Current, svg.BoxMetricsTold);
    }

    [Fact]
    public void UseLooksOf_TellsTheSurfaceTheLooksOfTheTheme()
    {
        IRenderContext svg = new SvgRenderContext();

        svg.UseLooksOf(Theme.MatplotlibClassic);

        Assert.Same(Theme.MatplotlibClassic.Looks, svg.LooksTold);
    }
}
