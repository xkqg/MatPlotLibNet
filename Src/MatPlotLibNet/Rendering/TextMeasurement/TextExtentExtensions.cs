// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.TextMeasurement;

/// <summary>The one seam a text is measured by, asked of a surface or of a font metrics: its <see cref="TextExtent"/>. A surface told
/// looks that measure like Agg (<see cref="TextLook.MeasuresLikeAgg"/>, the matplotlib themes) whose installed metrics read the glyphs of
/// the font (<see cref="IGlyphInkMetrics"/>) measures a text by its glyphs on whole pixels and math by its ink; every other surface, and
/// every font metrics, by the box it measures, the letters rising the font size above the baseline (<see cref="TextExtent.Of"/>). The
/// public measures of a surface (<see cref="IRenderContext.MeasureText"/>, <see cref="IRenderContext.MeasureRichText"/>) stay that box
/// under every look.</summary>
internal static class TextExtentExtensions
{
    extension(IFontMetrics metrics)
    {
        /// <summary>The extent of <paramref name="text"/> set in <paramref name="font"/>, as these metrics measure it.</summary>
        /// <param name="text">The text.</param>
        /// <param name="font">The font.</param>
        internal TextExtent ExtentOf(string text, Font font) => TextExtent.Of(metrics.Measure(text, font), font.Size);
    }

    extension(IRenderContext ctx)
    {
        /// <summary>The extent of <paramref name="label"/> set in <paramref name="font"/>, as this surface measures it: the rich text it
        /// parsed to when it is math, otherwise its plain text. The one measure of a text the renderers and the layout ask, the twin of
        /// the one verb that draws it (<see cref="MathText.MathTextRenderContextExtensions"/>).</summary>
        /// <param name="label">The text.</param>
        /// <param name="font">The font.</param>
        internal TextExtent ExtentOf(TextLabel label, Font font) =>
            label.Parsed(ctx.MathTextsTold).Tree is { } math ? ctx.ExtentOf(math, font) : ctx.ExtentOf(label.Text, font);

        /// <summary>The extent a text is placed by for <paramref name="vertical"/>, as this surface measures it: its whole measure, except
        /// where only its rise counts (a text centred on or hung from its anchor) and this surface measures a plain text as a box, which
        /// rises the font size whatever its advance (<see cref="TextExtent.Of"/>): then the line of the font, no advance measured.</summary>
        /// <param name="label">The text.</param>
        /// <param name="font">The font.</param>
        /// <param name="vertical">How the text stands on its anchor.</param>
        internal TextExtent ExtentToPlace(TextLabel label, Font font, TextVerticalAlignment vertical) =>
            vertical is TextVerticalAlignment.Center or TextVerticalAlignment.Top && !label.IsMath && ctx.GlyphInkMetrics() is null
                ? TextExtent.OfLine(0, font)
                : ctx.ExtentOf(label, font);

        /// <summary>The extent of one line of <paramref name="font"/>, as this surface measures it: the line a tick label stands in, measured
        /// by a digit, so that no site names a sample of its own. Where the surface measures by ink, a digit's ink counts when it rises
        /// above the font's typographic band, as the ink of a label's own digits does.</summary>
        /// <param name="font">The font.</param>
        internal TextExtent LineExtentOf(Font font) => ctx.ExtentOf("0", font);

        /// <summary>The extent of <paramref name="text"/> set in <paramref name="font"/>, as this surface measures it.</summary>
        /// <param name="text">The text.</param>
        /// <param name="font">The font.</param>
        private TextExtent ExtentOf(string text, Font font) => ctx.GlyphInkMetrics() is { } glyphs
            ? glyphs.LineExtentOf(text, font)
            : TextExtent.Of(ctx.MeasureText(text, font), font.Size);

        /// <summary>The extent of the math <paramref name="math"/> set in <paramref name="font"/>, as this surface measures it: the box of its
        /// layout (the one the verb draws). Where the surface measures by ink, as matplotlib 3.11 measures math (<c>Output.to_vector</c>: the
        /// width, height and depth of the box each rounded up to whole pixels), at least the label font's typographic band; elsewhere at
        /// least the line of the label's font.</summary>
        /// <param name="math">The label's tree.</param>
        /// <param name="font">The font of the whole label.</param>
        private TextExtent ExtentOf(LineBox math, Font font)
        {
            var glyphs = ctx.GlyphInkMetrics();
            var box = math.ExtentIn(ctx.MathStyleOf(font, glyphs));
            return glyphs is null
                ? BoxExtentOf(box, font)
                : TextExtent.OfInk(Math.Ceiling(box.Width), Math.Ceiling(box.Height), Math.Ceiling(box.Depth), glyphs.BandOf(font));
        }

        /// <summary>The layout of <paramref name="math"/> set in <paramref name="font"/> on this surface: its runs and rules in the faces of
        /// the surface's math font set, measured as the surface measures (the measure of the label reserves its box). What
        /// the one verb draws (<see cref="MathTextRenderContextExtensions"/>).</summary>
        /// <param name="math">The label's tree.</param>
        /// <param name="font">The font of the whole label.</param>
        internal MathInk LayOut(LineBox math, Font font) => ctx.LayOut(math, font, ctx.GlyphInkMetrics());

        /// <summary>The layout of <paramref name="math"/>, its runs measured by the ink <paramref name="glyphs"/> reads, else by the
        /// surface's boxes.</summary>
        private MathInk LayOut(LineBox math, Font font, IGlyphInkMetrics? glyphs) => math.LaidOut(ctx.MathStyleOf(font, glyphs));

        /// <summary>The style math set in <paramref name="font"/> is laid out in on this surface: the surface's math font set, its runs
        /// measured by the ink <paramref name="glyphs"/> reads, else by the surface's boxes.</summary>
        private MathStyle MathStyleOf(Font font, IGlyphInkMetrics? glyphs) =>
            new(ctx.MathFontsTold, font, glyphs is null ? MathMeasure.ByBox(ctx) : MathMeasure.ByInk(glyphs));

        /// <summary>The box of <paramref name="rich"/> as the public measure of a surface gives it (<see cref="IRenderContext.MeasureRichText"/>):
        /// laid out in the library's own math font set, each run measured by the box the surface measures, at least the line of the font.</summary>
        /// <param name="rich">The rich text.</param>
        /// <param name="font">The font of the whole label.</param>
        internal Size BoxOf(RichText rich, Font font) =>
            BoxExtentOf(MathBoxes.Of(rich).ExtentIn(new MathStyle(MathFontSet.Library, font, MathMeasure.ByBox(ctx))), font).Box;
        /// <summary>The installed metrics, when this surface was told looks that measure like Agg and they read glyphs; otherwise
        /// <see langword="null"/>, and the surface's own box is what a text measures.</summary>
        private IGlyphInkMetrics? GlyphInkMetrics() =>
            FontMetricsDefault.GlyphInk is { } glyphs && ctx.LooksTold.Text.MeasuresLikeAgg ? glyphs : null;
    }

    extension(IGlyphInkMetrics metrics)
    {
        /// <summary>A line of plain text as matplotlib's Agg backend measures it (<c>RendererAgg.get_text_width_height_descent</c>):
        /// its glyphs' advances on whole pixels, its ink on whole pixels, at least the font's typographic band.</summary>
        /// <param name="text">The text.</param>
        /// <param name="font">The font.</param>
        private TextExtent LineExtentOf(string text, Font font)
        {
            var ink = metrics.InkOf(text, font, font.Size);
            return TextExtent.OfInk(ink.PixelAdvance, ink.Rise, ink.Drop, ink.Band);
        }

    }

    /// <summary>The extent of a box of math measured by boxes: its width, its height and depth, at least the line of
    /// <paramref name="font"/> (<see cref="TextExtent.OfLine"/>).</summary>
    private static TextExtent BoxExtentOf(MathExtent box, Font font) =>
        new TextExtent(box.Width, box.Height + box.Depth, box.Height).AtLeast(TextExtent.OfLine(0, font));
}
