// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.MathText;

/// <summary>A label's tree keeps its layouts with it, one for each style it was laid out in (a text can be a title, an axis label and a
/// legend entry at once), and a layout measured by the box of the library's surfaces is measured by the metrics they measure by, not by
/// the one surface that asked: the layout of a label the figure kept is read again on the next draw's surfaces, laid out once.</summary>
[Collection("ChartServicesGlobalState")]
public sealed class KeptMathLayoutTests
{
    private static readonly Font Title = new() { Size = 14 }, Legend = new() { Size = 9 };

    [Fact]
    public void TwoSurfacesOfTheLibrary_MeasureByOneMeasure_AndASurfaceOfYourOwnByItself()
    {
        Assert.Equal(MathMeasure.ByBox(new SvgRenderContext()), MathMeasure.ByBox(new SvgRenderContext()));
        Assert.NotEqual(MathMeasure.ByBox(new OwnSurface()), MathMeasure.ByBox(new OwnSurface()));
    }

    /// <summary>The measure by the library's metrics gives what the surface's own measure gives.</summary>
    [Fact]
    public void TheMeasureByTheLibrarysMetrics_IsTheSurfacesOwnMeasure()
    {
        var surface = new SvgRenderContext();
        var font = new SpanFont(Title, Title.Size);

        var box = TextExtent.Of(surface.MeasureText("αx", Title), Title.Size);

        Assert.Equal(new MathExtent(box.Width, box.Ascent, box.Descent), MathMeasure.ByBox(surface).Of("αx", font));
    }

    /// <summary>A tree laid out in two fonts keeps both layouts: laying it out again in either measures nothing.</summary>
    [Fact]
    public void ATreeLaidOutInTwoStyles_KeepsBoth()
    {
        var counting = new TestFontMetrics((text, font) => new(text.Length * font.Size / 2, font.Size));
        using (FontMetricsDefault.Holding(counting))
        {
            var tree = TextLabel.Of("$\\alpha_\\beta$").Parsed(null).Tree!;
            var surface = new SvgRenderContext();
            _ = surface.LayOut(tree, Title);
            _ = surface.ExtentOf(TextLabel.OfRich(MathSpanWriter.Write(tree)), Legend);
            counting.Measured.Clear();

            _ = new SvgRenderContext().LayOut(tree, Title);
            _ = new SvgRenderContext().ExtentOf(TextLabel.OfRich(MathSpanWriter.Write(tree)), Legend);

            Assert.Empty(counting.Measured);
        }
    }

    /// <summary>A layout asked for its box alone and then for its ink is laid out again with its ink, in the same place of the tree's
    /// layouts: the box is the same.</summary>
    [Fact]
    public void ABoxLaidOutFirst_IsLaidOutWithItsInkWhenItIsDrawn()
    {
        var tree = TextLabel.Of("$x^2$").Parsed(null).Tree!;
        var surface = new SvgRenderContext();

        var box = surface.ExtentOf(TextLabel.OfRich(MathSpanWriter.Write(tree)), Title);
        var ink = surface.LayOut(tree, Title);

        Assert.NotEmpty(ink.Glyphs);
        Assert.Equal(box, surface.ExtentOf(TextLabel.OfRich(MathSpanWriter.Write(tree)), Title));
        Assert.Same(ink, surface.LayOut(tree, Title));
    }

    /// <summary>A text drawn in more styles than a tree keeps layouts for lets go of the oldest.</summary>
    [Fact]
    public void ATreeLaidOutInManyStyles_KeepsTheNewest()
    {
        var tree = TextLabel.Of("$y$").Parsed(null).Tree!;
        var surface = new SvgRenderContext();
        var first = surface.LayOut(tree, new Font { Size = 1 });

        for (int size = 2; size <= 9; size++) _ = surface.LayOut(tree, new Font { Size = size });

        Assert.NotSame(first, surface.LayOut(tree, new Font { Size = 1 }));
        Assert.Same(surface.LayOut(tree, new Font { Size = 9 }), surface.LayOut(tree, new Font { Size = 9 }));
    }

    /// <summary>The repro of the frame that laid every label out again: a figure drawn again on new surfaces measures none of the math
    /// runs it measured before (only the texts that are not math, which the surface measures as it always did).</summary>
    [Fact]
    public void AFigureDrawnAgain_LaysOutNoMathLabelItDrewBefore()
    {
        var counting = new TestFontMetrics((text, font) => new(text.Length * font.Size / 2, font.Size));
        using (FontMetricsDefault.Holding(counting))
        {
            double[] x = [0, 1, 2], y = [1, 2, 0];
            var figure = Plt.Create().WithTitle("$\\delta$")
                .AddSubPlot(1, 2, 1, ax => ax.Plot(x, y, line => line.Label = "$\\gamma$").WithTitle("$\\alpha_\\beta$").SetXLabel("$\\gamma$").WithLegend())
                .AddSubPlot(1, 2, 2, ax => ax.Plot(x, y).WithTitle("$\\gamma$").SetYLabel("$\\alpha_\\beta$"))
                .Build();
            for (int draw = 0; draw < 3; draw++) _ = figure.ToSvg();
            counting.Measured.Clear();

            _ = figure.ToSvg();

            Assert.DoesNotContain(counting.Measured, text => text.IndexOfAny(['α', 'β', 'γ', 'δ']) >= 0);
            Assert.NotEmpty(counting.Measured);
        }
    }

    /// <summary>A surface of your own: the library cannot know what it measures by.</summary>
    private sealed class OwnSurface : SilentRenderContext
    {
        public override Size MeasureText(string text, Font font) => new(text.Length, font.Size);
    }
}
