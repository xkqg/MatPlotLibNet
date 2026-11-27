// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>
/// The one owner of every themed font the render pipeline uses, as members of the <see cref="Theme"/> they are read from. The axes renderer,
/// the layout engine and the figure renderer all need the font of the same role; when each built
/// its own the formulas drifted, and a legend measured with a tick font two points smaller than
/// the one it was drawn with was clipped. Two parallel "formulas for the same role" are exactly
/// the kind of bug this class exists to prevent.
/// </summary>
/// <remarks>
/// <para>Every consumer (renderers, measurers, layout engine) asks the theme (<c>theme.TickFont()</c>)
/// instead of rolling its own <c>Font</c> factory. Adding a new role means adding a new
/// method here; changing a formula means changing one line; there is no way for two
/// call sites to disagree on the size of a "tick label font" because there is exactly
/// one place the size is defined.</para>
///
/// <para>The four roles mirror matplotlib's font hierarchy:</para>
/// <list type="bullet">
///   <item><description><see cref="ThemeFontExtensions.TickFont"/> — tick labels and legend entries. Size = <c>DefaultFont.Size</c>. Non-bold.</description></item>
///   <item><description><see cref="ThemeFontExtensions.LabelFont"/> — axis labels (X / Y / Z). Size = <c>DefaultFont.Size</c>. Non-bold.</description></item>
///   <item><description><see cref="ThemeFontExtensions.TitleFont"/> — axes title (per-subplot): the theme's title size and weight (<see cref="TextLook.TitleSizeOver"/>), bold unless the theme names a weight.</description></item>
///   <item><description><see cref="ThemeFontExtensions.SupTitleFont"/> — figure-level suptitle: the theme's figure-title size and title weight (<see cref="TextLook.SupTitleSizeOver"/>), bold unless the theme names a weight.</description></item>
/// </list>
/// <para>Every role is one private base font (the theme's family, a size, a weight and a colour); no role assembles a font of its own.</para>
/// </remarks>
internal static class ThemeFontExtensions
{
    extension(Theme theme)
    {
        /// <summary>The one font every role is made from: the theme's family at <paramref name="size"/>, in
        /// <paramref name="weight"/> and <paramref name="color"/>.</summary>
        private Font BodyFont(double size, FontWeight weight, Color color) => new()
        {
            Family = theme.DefaultFont.Family,
            Size   = size,
            Weight = weight,
            Color  = color,
        };

        /// <summary>The theme's text at its own size, in its text colour, not bold: what the tick labels, the axis labels and the text
        /// among the data are drawn in.</summary>
        private Font BodyFont() => theme.BodyFontIn(theme.ForegroundText);

        /// <summary>The theme's text at its own size, not bold, in <paramref name="color"/>.</summary>
        private Font BodyFontIn(Color color) => theme.BodyFont(theme.DefaultFont.Size, FontWeight.Normal, color);

        /// <summary>Font for tick labels and legend entries — same size as axis labels, matching
        /// matplotlib's default. Non-bold.</summary>
        public Font TickFont() => theme.BodyFont();

        /// <summary>Font for the label of a drawn line (a reference line, a span, a trendline, a horizontal level, a Fibonacci
        /// retracement): the tick font in the colour of its line.</summary>
        /// <param name="line">The colour of the line.</param>
        /// <returns>The font the label is drawn in.</returns>
        public Font LineLabelFont(Color line) => theme.BodyFontIn(line);

        /// <summary>Font for the entries of a legend: the size its author named (<see cref="Legend.FontSize"/>), else the size of the tick
        /// labels times the theme's <c>legend.fontsize</c> (<see cref="LegendLook.FontScale"/>: 'medium', 1, under the default style and
        /// the library's themes, 'large', 1.2, under the classic one). The one rule the legend's layout, its drawing and the margin
        /// reserved for it read.</summary>
        /// <param name="legend">The legend whose entries the font is for.</param>
        /// <returns>The font.</returns>
        public Font LegendFontOf(Legend legend) => theme.TickFontNamedBy(legend, theme.Looks.Legend.FontScale);

        /// <summary>Font for the end labels of the series (a name at a line's end): the size the axes' legend names, else the size of the
        /// tick labels. Unlike the legend's own entries (<see cref="LegendFontOf"/>) not scaled by the theme's <c>legend.fontsize</c>: an end
        /// label is a thing of the library's, which matplotlib does not draw.</summary>
        /// <param name="legend">The legend of the axes whose end labels the font is for.</param>
        /// <returns>The font.</returns>
        public Font EndLabelFontOf(Legend legend) => theme.TickFontNamedBy(legend, 1);

        /// <summary>Font for the title of a legend: the size its author named (<see cref="Legend.TitleFontSize"/>), else the look's step
        /// above the tick labels (<see cref="LegendLook.TitleSizeStep"/>), in the look's weight (<see cref="LegendLook.TitleWeight"/>).</summary>
        /// <param name="legend">The legend whose title the font is for.</param>
        /// <returns>The font.</returns>
        public Font LegendTitleFontOf(Legend legend)
        {
            var look = theme.Looks.Legend;
            return theme.BodyFont(legend.TitleFontSize ?? theme.DefaultFont.Size + look.TitleSizeStep, look.TitleWeight, theme.ForegroundText);
        }

        /// <summary>The tick font at the size <paramref name="legend"/> names, else at <paramref name="scale"/> times the tick labels' size.</summary>
        private Font TickFontNamedBy(Legend legend, double scale) =>
            theme.BodyFont(legend.FontSize ?? theme.DefaultFont.Size * scale, FontWeight.Normal, theme.ForegroundText);

        /// <summary>Font for axis labels (X / Y / Z label text below or beside the spine).
        /// Same size as tick labels, non-bold.</summary>
        public Font LabelFont() => theme.BodyFont();

        /// <summary>Font for the text a chart draws among its data: an annotation, a bar's value, a radar spoke's name, a pie
        /// slice's percentage, a funnel stage's name, a graph node's name. The size of the tick labels, in the theme's
        /// foreground colour; matplotlib has one text size for all of these, and a theme that grows or shrinks its text
        /// does so for them too. A caller that needs another colour (white on a pie slice) sets it on the returned font.</summary>
        public Font DataTextFont() => theme.BodyFont();

        /// <summary>Font for a text among the data that keeps the theme's own weight as well as its size: the names of a dendrogram's
        /// leaves, which stand where the tick labels of its axis would.</summary>
        /// <returns>The font the text is drawn in.</returns>
        public Font DataTextFontInThemeWeight() => theme.BodyFont(theme.DefaultFont.Size, theme.DefaultFont.Weight, theme.ForegroundText);

        /// <summary>Font for a series text that has a size of its own (a table cell, a treemap name, a key label, a tile's value): the
        /// theme's family at that size. The family is the one thing a series text never chooses for itself, so a theme that sets
        /// another family sets it for these too; the size and the weight stay the series' own. A text written with no colour is
        /// drawn black by a browser whatever the theme, so one the series gives no colour is in the theme's text colour.</summary>
        /// <param name="size">The size the text is drawn at, in the units of <see cref="Font.Size"/> (a size in points is
        /// <c>PointsInPixels</c> of it).</param>
        /// <param name="color">The colour the series gives the text, or <see langword="null"/> for the theme's text colour.</param>
        /// <param name="weight">The weight the series gives the text.</param>
        /// <returns>The font the text is drawn in.</returns>
        public Font DataTextFont(double size, Color? color = null, FontWeight weight = FontWeight.Normal) =>
            theme.BodyFont(size, weight, color ?? theme.ForegroundText);

        /// <summary>Font for a series text that stands on a ground the series paints itself (a table cell, a progress bar, a state's
        /// segment, a treemap tile, a pie wedge), not on the plot's own: in the ink the series' kind names (<see cref="GroundInk"/>: the
        /// theme's text colour where it can be read against that ground, else black or white, whichever stands out; or the theme's text
        /// colour always, as matplotlib writes the text it draws on a patch of its own, which a pie's and a table's looks name under the
        /// matplotlib themes).</summary>
        /// <param name="ground">The colour painted under the text; its alpha is laid over the theme's axes background.</param>
        /// <param name="size">The size the text is drawn at, in the units of <see cref="Font.Size"/>.</param>
        /// <param name="ink">How the text is inked on its ground.</param>
        /// <param name="weight">The weight the series gives the text.</param>
        /// <returns>The font the text is drawn in.</returns>
        public Font DataTextFontOn(Color ground, double size, GroundInk ink, FontWeight weight = FontWeight.Normal) =>
            theme.DataTextFont(size, ink.On(theme.ForegroundText, ground.CompositedOver(theme.AxesBackground)), weight);

        /// <summary>Font for a text the caller places, an annotation or a figure text: the font it was given, else
        /// <c>DataTextFont()</c>. A font supplied without a colour carries none of its own, and a text drawn with no
        /// colour comes out black whatever the theme, so its colour is the font's own when it has one, else the colour set
        /// beside it, else the theme's text colour.</summary>
        /// <param name="font">The font the text was given, or <see langword="null"/>.</param>
        /// <param name="color">The colour set beside the font, or <see langword="null"/>.</param>
        /// <returns>The font the text is drawn in.</returns>
        public Font DataTextFont(Font? font, Color? color) => font is { } supplied
            ? supplied with { Color = supplied.Color ?? color ?? theme.ForegroundText }
            : theme.BodyFontIn(color ?? theme.ForegroundText);

        /// <summary>Font for the per-subplot axes title: the theme's <see cref="TextLook.TitleScale"/> times its font size, in its
        /// <see cref="TextLook.TitleWeight"/> (matplotlib's <c>axes.titlesize</c> 'large' and <c>axes.titleweight</c> 'normal' under
        /// the matplotlib themes), else two points over the font and bold.</summary>
        public Font TitleFont() =>
            theme.BodyFont(theme.Looks.Text.TitleSizeOver(theme.DefaultFont.Size), theme.Looks.Text.DrawnTitleWeight, theme.ForegroundText);

        /// <summary>Font for the figure-level suptitle: the theme's <see cref="TextLook.SupTitleScale"/> times its font size, in its
        /// <see cref="TextLook.TitleWeight"/> (matplotlib's <c>figure.titlesize</c>, 'large' under the default style and 'medium'
        /// under the classic one, and <c>figure.titleweight</c> 'normal'), else four points over the font and bold.</summary>
        public Font SupTitleFont() =>
            theme.BodyFont(theme.Looks.Text.SupTitleSizeOver(theme.DefaultFont.Size), theme.Looks.Text.DrawnTitleWeight, theme.ForegroundText);
    }

    extension(TickConfig ticks)
    {
        /// <summary>The font a label of these ticks is drawn in over <paramref name="font"/> (the theme's tick font): at the size and in
        /// the colour the ticks name (matplotlib's <c>labelsize</c> and <c>labelcolor</c>), each where given, else the font's own. The
        /// drawing of a tick label reads it, and the size the locator spaces the ticks by is its size (<see cref="LabelSizeOver"/>).</summary>
        /// <param name="font">The font the label is drawn in where the ticks name nothing.</param>
        /// <returns>The font itself when the ticks name nothing, else a font made from it.</returns>
        internal Font LabelFontOver(Font font) => ticks.LabelSize is null && ticks.LabelColor is null
            ? font
            : font with { Size = ticks.LabelSizeOver(font.Size), Color = ticks.LabelColor ?? font.Color };

        /// <summary>The size a label of these ticks is drawn at over a font <paramref name="size"/> large: the ticks' own, else that size
        /// (matplotlib's <c>Axis._get_tick_label_size</c>: the axis' <c>labelsize</c>, else <c>xtick.labelsize</c>).</summary>
        /// <param name="size">The size of the font the label is drawn in where the ticks name none.</param>
        /// <returns>The size.</returns>
        internal double LabelSizeOver(double size) => ticks.LabelSize ?? size;
    }
}
