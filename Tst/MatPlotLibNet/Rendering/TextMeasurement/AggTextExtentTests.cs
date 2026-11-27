// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Rendering.TextMeasurement;

/// <summary>The text measure of the matplotlib themes (<see cref="TextLook.MeasuresLikeAgg"/>): a surface told those looks measures a
/// text by the glyphs of the installed metrics as matplotlib 3.11's Agg backend does, and every other surface, look or metrics by
/// the line rule. The numbers are matplotlib 3.11.2's, printed with python for DejaVu Sans at 100 dpi:
/// <c>RendererAgg.get_text_width_height_descent</c> and <c>Text.get_window_extent</c>.</summary>
[Collection("ChartServicesGlobalState")]
public sealed class AggTextExtentTests
{
    /// <summary>12 pt at 100 dpi, in pixels: matplotlib's axes title under the default style.</summary>
    private const double TwelvePoint = 12 * 100.0 / 72;

    private static readonly Font Font = new() { Size = 10 };

    /// <summary>A line is at least the font's typographic ascender above its baseline and its descender below it
    /// (<c>Text._get_layout</c>: <c>a = max(a, min_ascent)</c>, <c>d = max(d, min_descent)</c>, the OS/2 <c>sTypoAscender</c> 1556 and
    /// <c>sTypoDescender</c> -492 of 2048 units). The hist title at 12 pt: <c>get_text_width_height_descent</c> gives (373.703125, 17,
    /// 4), its ink rises 13 pixels, and its box is 13 + 4.0039 = 17.0039 tall. "0.5" at 10 pt rises 10 pixels, under the ascender's
    /// 10.5523, and its box is the font's 13.8889.</summary>
    [Fact]
    public void ALine_IsItsInkInWholePixels_AtLeastTheFontsTypographicAscenderAndDescender()
    {
        var band = new FontBand(1556 * TwelvePoint / 2048, 492 * TwelvePoint / 2048);
        var hist = TextExtent.OfInk(373.703125, 13, 4, band);
        Assert.Equal(373.703125, hist.Width);
        Assert.Equal(13, hist.Ascent);
        Assert.Equal(17.0039, hist.Height, 4);

        double tenPoint = 10 * 100.0 / 72;
        var digits = TextExtent.OfInk(22, 10, 0, new FontBand(1556 * tenPoint / 2048, 492 * tenPoint / 2048));
        Assert.Equal(10.5523, digits.Ascent, 4);
        Assert.Equal(13.8889, digits.Height, 4);
    }

    [Fact]
    public void ASurfaceToldTheMatplotlibLooks_MeasuresATextByTheGlyphsOfTheInstalledMetrics()
    {
        var metrics = InkMetrics();
        foreach (var theme in new[] { Theme.MatplotlibV2, Theme.MatplotlibClassic })
            using (FontMetricsDefault.Holding(metrics))
            {
                IRenderContext ctx = new SvgRenderContext();
                ctx.UseLooksOf(theme);

                var ink = metrics.InkOf("Peak", Font, Font.Size);
                Assert.Equal(TextExtent.OfInk(ink.PixelAdvance, ink.Rise, ink.Drop, ink.Band), ctx.ExtentOf(TextLabel.Plain("Peak"), Font));
            }
    }

    [Fact]
    public void ALibraryLook_AnUntoldSurface_OrMetricsWithoutGlyphs_MeasureALine()
    {
        using (FontMetricsDefault.Holding(InkMetrics()))
        {
            var told = new SvgRenderContext();
            told.UseLooksOf(Theme.Default);
            Assert.Equal(TextExtent.Of(told.MeasureText("Peak", Font), Font.Size), told.ExtentOf(TextLabel.Plain("Peak"), Font));

            var untold = new SvgRenderContext();
            Assert.Equal(TextExtent.Of(untold.MeasureText("Peak", Font), Font.Size), untold.ExtentOf(TextLabel.Plain("Peak"), Font));
        }

        using (FontMetricsDefault.Holding(new DefaultFontMetrics()))
        {
            IRenderContext ctx = new SvgRenderContext();
            ctx.UseLooksOf(Theme.MatplotlibV2);
            Assert.Equal(TextExtent.Of(ctx.MeasureText("Peak", Font), Font.Size), ctx.ExtentOf(TextLabel.Plain("Peak"), Font));
        }
    }

    /// <summary>A label the layouts place (a bar value, a pie slice, a sankey node, a word) stands as far from its neighbours under the
    /// matplotlib themes as it always did: the font metrics a layout is handed measure a plain text by the box of the surface (its
    /// measure, a line of 1.2 times the font size), not by the ink the Agg looks give a tick label, so no picture moves; only a text
    /// that holds math is measured as the surface measures math.</summary>
    [Fact]
    public void TheFontMetricsOfALayout_MeasureAPlainTextByTheBoxOfTheSurface_UnderTheMatplotlibLooksToo()
    {
        using (FontMetricsDefault.Holding(InkMetrics()))
        {
            IRenderContext ctx = new SvgRenderContext();
            ctx.UseLooksOf(Theme.MatplotlibV2);
            IFontMetrics layout = new SurfaceFontMetrics(ctx);

            Assert.Equal(ctx.MeasureText("Peak", Font), layout.Measure("Peak", Font));
            Assert.Equal(ctx.ExtentOf(TextLabel.Of("$x$"), Font).Box, layout.Measure("$x$", Font));
        }
    }

    [Fact]
    public void ACollectingSurface_MeasuresByTheLooksOfTheSurfaceItCollectsFor()
    {
        var metrics = InkMetrics();
        using (FontMetricsDefault.Holding(metrics))
        {
            var inner = new SvgRenderContext();
            inner.UseLooksOf(Theme.MatplotlibV2);
            IRenderContext collecting = new MarkCollectingContext(inner, new MarkObstacles.Builder());

            Assert.Equal(inner.ExtentOf(TextLabel.Plain("Peak"), Font), collecting.ExtentOf(TextLabel.Plain("Peak"), Font));
            Assert.NotEqual(TextExtent.Of(inner.MeasureText("Peak", Font), Font.Size), collecting.ExtentOf(TextLabel.Plain("Peak"), Font));

            // Told a look, the collector tells the surface it measures with; over a surface of an application's it measures a line.
            collecting.UseLooksOf(Theme.Default);
            Assert.Same(ThemeLooks.Library, ((IThemedRenderContext)inner).Looks);
            IRenderContext overOwn = new MarkCollectingContext(new RecordingRenderContext(), new MarkObstacles.Builder());
            overOwn.UseLooksOf(Theme.MatplotlibV2);
            Assert.Same(ThemeLooks.Library, ((IThemedRenderContext)overOwn).Looks);
        }
    }

    /// <summary>Math is measured by its ink (matplotlib 3.11's <c>VectorParse</c>: the box of its glyphs, each width, height and depth
    /// rounded up, <c>np.ceil</c>): the box its layout takes when each run is measured by the ink of its glyphs, then the line's
    /// typographic minimum.</summary>
    [Fact]
    public void AMathText_IsMeasuredByItsInk_RoundedUpToWholePixels()
    {
        var metrics = InkMetrics();
        using (FontMetricsDefault.Holding(metrics))
        {
            IRenderContext ctx = new SvgRenderContext();
            ctx.UseLooksOf(Theme.MatplotlibV2);
            var rich = new RichText([new TextSpan("x"), new TextSpan("2", TextSpanKind.Superscript, 0.7), new TextSpan("g", TextSpanKind.Subscript, 0.7)]);

            var box = MathInk.Of(MathBoxes.Of(rich), new MathStyle(MathFontSet.Library, Font, MathMeasure.ByInk(metrics))).Extent;
            var expected = TextExtent.OfInk(Math.Ceiling(box.Width), Math.Ceiling(box.Height), Math.Ceiling(box.Depth), metrics.BandOf(Font));
            Assert.Equal(expected, ctx.ExtentOf(TextLabel.OfRich(rich), Font));
            Assert.Equal(ctx.ExtentOf(TextLabel.OfRich(MathTextParser.Parse("$x^2_g$")), Font), ctx.ExtentOf(TextLabel.Of("$x^2_g$"), Font));
        }
    }

    [Fact]
    public void ARichText_UnderALibraryLook_IsTheSurfacesRichBox()
    {
        using (FontMetricsDefault.Holding(InkMetrics()))
        {
            IRenderContext ctx = new SvgRenderContext();
            var rich = new RichText([new TextSpan("x"), new TextSpan("2", TextSpanKind.Superscript, 0.7)]);

            Assert.Equal(ctx.MeasureRichText(rich, Font), ctx.ExtentOf(TextLabel.OfRich(rich), Font).Box);
        }
    }

    /// <summary>The public measures of a surface stay the boxes it sets a text in, under every look: a rich text is the box of its layout
    /// in the label's own face, each run measured by <see cref="IRenderContext.MeasureText"/>, whatever looks the surface was told.</summary>
    [Fact]
    public void ThePublicRichMeasure_IsTheSurfacesBoxMeasure_UnderTheMatplotlibLooksToo()
    {
        using (FontMetricsDefault.Holding(InkMetrics()))
        {
            IRenderContext told = new SvgRenderContext();
            told.UseLooksOf(Theme.MatplotlibV2);
            IRenderContext library = new SvgRenderContext();
            var rich = new RichText([new TextSpan("x"), new TextSpan("2", TextSpanKind.Superscript, 0.7)]);

            Assert.Equal(library.MeasureRichText(rich, Font), told.MeasureRichText(rich, Font));
            Assert.NotEqual(told.MeasureRichText(rich, Font), told.ExtentOf(TextLabel.OfRich(rich), Font).Box);
        }
    }

    /// <summary>matplotlib 3.11.2: <c>text.hinting</c> is 'default' under the default style and 'auto' under the classic one; both set
    /// a glyph on whole pixels. The library's own themes keep their line.</summary>
    [Fact]
    public void TheMatplotlibThemes_MeasureLikeAgg_TheLibraryThemesDoNot_AndTheJsonCarriesIt()
    {
        Assert.True(Theme.MatplotlibV2.Looks.Text.MeasuresLikeAgg);
        Assert.True(Theme.MatplotlibClassic.Looks.Text.MeasuresLikeAgg);
        Assert.False(Theme.Default.Looks.Text.MeasuresLikeAgg);
        Assert.False(ThemeLooks.Library.Text.MeasuresLikeAgg);

        var dto = ThemeDto.From(Theme.MatplotlibV2);
        Assert.True(dto.TextMeasuredLikeAgg);
        Assert.True(dto.ToTheme().Looks.Text.MeasuresLikeAgg);
        Assert.Null(ThemeDto.From(Theme.Default).TextMeasuredLikeAgg);
    }

    /// <summary>The layout of a draw measures its text by the looks of the figure's theme, on a surface that has drawn nothing yet: it
    /// is laid out as a surface already told the looks lays it out, and the surface holds them after.</summary>
    [Fact]
    public void TheLayout_MeasuresByTheLooksOfTheFiguresTheme_OnASurfaceNotYetTold()
    {
        using (FontMetricsDefault.Holding(InkMetrics()))
        {
            var figure = Plt.Create().WithTheme(Theme.MatplotlibV2).WithTitle("Peak")
                .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1.0], [0.0, 1.0]).WithTitle("Peak").SetXLabel("x"))
                .TightLayout().Build();
            IRenderContext told = new SvgRenderContext();
            told.UseLooksOf(figure.Theme);
            var expected = new ChartRenderer().ComputeLayout(figure, told).PlotAreas;

            var fresh = new SvgRenderContext();
            var laidOut = new ChartRenderer().ComputeLayout(figure, fresh).PlotAreas;

            Assert.Equal(expected, laidOut);
            Assert.Same(figure.Theme.Looks, ((IThemedRenderContext)fresh).Looks);
        }
    }

    /// <summary>A polar axes centres its spoke labels under the matplotlib themes (matplotlib's <c>ThetaTick</c>: <c>va='center'</c>) by the
    /// line the theme measures, not by the library's line: the baseline stands <see cref="TextBox.CentreDrop"/> of the line's ascent
    /// below the label's centre, the font's typographic ascender under glyphs that measure like Agg.</summary>
    [Fact]
    public void APolarSpokeLabel_IsCentredByTheLineTheThemeMeasures()
    {
        static double BaselineOf90(IFontMetrics metrics)
        {
            double y = 0;
            using (FontMetricsDefault.Holding(metrics))
            {
                var ctx = new SvgRenderContext();
                ctx.UseLooksOf(Theme.MatplotlibV2);
                new PolarAxesRenderer(new Axes { CoordinateSystem = CoordinateSystem.Polar }, new Rect(0, 0, 400, 400), ctx, Theme.MatplotlibV2).Render();
                y = ctx.GetOutput().TextReading("90°").Number("y");
            }
            return y;
        }

        double size = Theme.MatplotlibV2.TickFont().Size;
        double byLine = BaselineOf90(new DefaultFontMetrics());
        double byGlyphs = BaselineOf90(InkMetrics());

        Assert.Equal(0.35 * (0.76 * size - size), byGlyphs - byLine, 1);   // the centre drop, 0.35 of the ascent
    }

    /// <summary>The spoke labels of a polar axes stand centred on the line a tick label stands in, measured by a digit as the Cartesian
    /// axes measure it (<see cref="TextExtentExtensions"/>' <c>LineExtentOf</c>): where a digit's ink rises above the font's typographic
    /// ascender the line is as tall as the digit, and the label's baseline, a third of that ascent below its middle, moves with it. The
    /// polar axes measured an empty text, which has no ink, so its labels never followed the digits they are made of.</summary>
    [Fact]
    public void ThePolarSpokeLabels_AreCentredOnTheLineOfADigit_WhereADigitRisesAboveTheBand()
    {
        using (FontMetricsDefault.Holding(InkMetrics(0.9)))
        {
            var theme = Theme.MatplotlibV2;
            var figure = Plt.Create().WithSize(800, 600).WithTheme(theme)
                .AddSubPlot(1, 1, 1, ax => ax.PolarPlot([1.0, 2.0, 3.0], [0.0, 1.0, 2.0]))
                .Build();
            var svg = new SvgRenderContext();
            new ChartRenderer().Render(figure, svg);
            var text = svg.GetOutput().TextReading("90°");
            var plot = new ChartRenderer().ComputeLayout(figure, new RecordingRenderContext()).PlotAreas[0];
            var measuring = new SvgRenderContext();
            measuring.UseLooksOf(theme);
            var line = measuring.LineExtentOf(theme.TickFont());

            double centre = plot.Y + plot.Height / 2 - RoundFrame.In(plot).Radius - 14.0.PointsInPixels;

            Assert.True(line.Ascent > 11, "the fake's digits must rise above its band for the test to see the line");
            Assert.Equal(centre + 0.35 * line.Ascent, text.Number("y"), 2);
        }
    }

    /// <summary>Metrics that read glyphs: each character 5 units of the font size per 10 wide unrounded and 6 pixels rounded; a text
    /// rises <paramref name="rise"/> of the size (an empty text not at all) and drops as far as its descending letters reach. Risen 0.9
    /// of the size, over the band's 0.76, a digit sets the line.</summary>
    /// <param name="rise">How far a text rises, as a share of the size.</param>
    private static TestGlyphInk InkMetrics(double rise = 0.7) => new(
        (text, _, size) => new(
            text.Length * 0.5 * size,
            text.Length * 6,
            text.Length == 0 ? 0 : Math.Round(rise * size),
            text.Any(c => c is 'g' or 'p' or 'y') ? Math.Round(0.2 * size) : 0,
            new FontBand(0.76 * size, 0.24 * size)),
        font => new(0.76 * font.Size, 0.24 * font.Size),
        (text, font) => TextExtent.OfLine(text.Length * 0.5 * font.Size, font).Box);
}
