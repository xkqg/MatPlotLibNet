// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Styling;

/// <summary>The titles of an axes and of a figure, the pads that place a title and an axis title, and the text a series
/// writes.</summary>
internal sealed record TextLook : IThemeKeys<TextLook>
{
    /// <summary>The library's own text.</summary>
    internal static TextLook Library { get; } = new();

    /// <inheritdoc />
    public void WriteKeys(ThemeDto keys)
    {
        keys.TitleScale = TitleScale;
        keys.SupTitleScale = SupTitleScale;
        keys.TitleWeight = TitleWeight;
        keys.AxesTitlePad = TitlePad;
        keys.PolarTitleOverLabels = TitleOverWhatIsBelow.OrNullAtRest(false);
        keys.AxisLabelPad = LabelPad;
        keys.SeriesTextAtFontSize = SeriesTextAtFontSize.OrNullAtRest(false);
        keys.TextMeasuredLikeAgg = MeasuresLikeAgg.OrNullAtRest(false);
        keys.MathTextFontSet = MathFonts.Name;
    }

    /// <inheritdoc />
    public static TextLook ReadKeys(ThemeDto keys) => new()
    {
        TitleScale = keys.TitleScale,
        SupTitleScale = keys.SupTitleScale,
        TitleWeight = keys.TitleWeight,
        TitlePad = keys.AxesTitlePad,
        TitleOverWhatIsBelow = keys.PolarTitleOverLabels == true,
        LabelPad = keys.AxisLabelPad,
        SeriesTextAtFontSize = keys.SeriesTextAtFontSize ?? false,
        MeasuresLikeAgg = keys.TextMeasuredLikeAgg ?? false,
        MathFonts = Rendering.MathText.MathFontSet.Named(keys.MathTextFontSet),
    };

    /// <summary>How large an axes title is, as a multiple of the theme's font size: matplotlib's <c>axes.titlesize</c>, 'large'
    /// (1.2) in both its styles. Unset: two points over the font.</summary>
    public double? TitleScale { get; init; }

    /// <summary>How large a figure title is, as a multiple of the theme's font size: matplotlib's <c>figure.titlesize</c>, 'large'
    /// (1.2) under the default style and 'medium' (1.0) under the classic one. Unset: four points over the font.</summary>
    public double? SupTitleScale { get; init; }

    /// <summary>The weight of an axes title and of a figure title: matplotlib's <c>axes.titleweight</c> and
    /// <c>figure.titleweight</c>, normal in both its styles. Unset: bold.</summary>
    public FontWeight? TitleWeight { get; init; }

    /// <summary>How far an axes title's baseline stands above the plot, in pixels: matplotlib's <c>axes.titlepad</c>, 6 pt under
    /// the default style and 5 pt under the classic one. Unset: <see cref="Rendering.AxesTitleExtensions.TitleGap"/>. A title style's
    /// own pad is added to it.</summary>
    public double? TitlePad { get; init; }

    /// <summary>Whether an axes title rises over what stands below it, as matplotlib's does where <c>axes.titley</c> is None (the default
    /// style): <c>Axes._update_title_position</c> lifts it until its box clears the top axis, a polar axes' spoke labels and the spine, so
    /// a title whose descent is deeper than <see cref="TitlePad"/> stands its descent above them. Otherwise (the classic style's
    /// <c>axes.titley</c> 1.0, and the library's own) its baseline stands the pad above them.</summary>
    public bool TitleOverWhatIsBelow { get; init; }

    /// <summary>How far an axis title stands from the tick labels beside it, in pixels: matplotlib's <c>axes.labelpad</c>, 4 pt
    /// under the default style and 5 pt under the classic one. Unset: an X axis title <see cref="Rendering.TitleClearance.XLabelGap"/>
    /// from its labels and a Y axis title as far as its container's own gap.</summary>
    public double? LabelPad { get; init; }

    /// <summary>Whether a series text that names no size is drawn at the theme's font size, as matplotlib draws <c>clabel</c>, a
    /// table's cells, a 3-D text and a quiver key's label (<c>font.size</c>). Otherwise each at the size its series has always drawn
    /// it. <see cref="Theme.SeriesTextSizeOver"/> holds the rule.</summary>
    public bool SeriesTextAtFontSize { get; init; }

    /// <summary>Whether a text is measured as matplotlib's Agg backend measures it (matplotlib 3.11, <c>text.hinting</c> 'default' under
    /// the default style and 'auto' under the classic one): each glyph's advance and ink on whole pixels at the font's size rounded to
    /// whole pixels, a line at least the font's typographic ascender above its baseline and its descender below it, and math by the ink
    /// of its glyphs, rounded up. Otherwise a line is 1.2 times its font's size tall and its letters rise the font's size. The one place
    /// this is read is the measuring seam (<see cref="Rendering.TextMeasurement.TextExtentExtensions"/>), on a surface told the theme's
    /// looks, with font metrics that read the glyphs of the font (the Skia package's); the core's own width table has none.</summary>
    public bool MeasuresLikeAgg { get; init; }

    /// <summary>The faces math is set in (<see cref="Rendering.MathText.MathFontSet"/>): matplotlib's <c>mathtext.fontset</c> under its
    /// themes. Unset: the library's own, math in the label's face. A surface reads it where it draws the glyphs of the set's faces
    /// (<see cref="Rendering.IThemedRenderContext.MathFonts"/>).</summary>
    public Rendering.MathText.MathFontSet MathFonts { get; init; } = Rendering.MathText.MathFontSet.Library;

    /// <summary>The gap between an axis title and its tick labels: the one given, else this look's, else <paramref name="resting"/>.</summary>
    /// <param name="given">The value its author gave, <see langword="null"/> when none.</param>
    /// <param name="resting">The value its owner rests at where the theme names none.</param>
    /// <returns>The value drawn.</returns>
    internal double LabelPadOver(double? given, double resting) => given ?? LabelPad ?? resting;

    /// <summary>The gap below an axes title: this look's, else <paramref name="resting"/>.</summary>
    /// <param name="resting">The value its owner rests at where the theme names none.</param>
    /// <returns>The value drawn.</returns>
    internal double TitlePadOr(double resting) => TitlePad ?? resting;

    /// <summary>How many pixels an axes title stands over the font where the theme names no title scale.</summary>
    private const double UnscaledTitleStep = 2;

    /// <summary>How many pixels a figure title stands over the font where the theme names no figure-title scale.</summary>
    private const double UnscaledSupTitleStep = 4;

    /// <summary>The size of an axes title over a font of <paramref name="fontSize"/>: this look's scale times it, else two pixels
    /// over it.</summary>
    /// <param name="fontSize">The theme's font size.</param>
    /// <returns>The size, in the units of <see cref="Font.Size"/>.</returns>
    internal double TitleSizeOver(double fontSize) => TitleScale is { } scale ? fontSize * scale : fontSize + UnscaledTitleStep;

    /// <summary>The size of a figure title over a font of <paramref name="fontSize"/>: this look's scale times it, else four pixels
    /// over it.</summary>
    /// <param name="fontSize">The theme's font size.</param>
    /// <returns>The size, in the units of <see cref="Font.Size"/>.</returns>
    internal double SupTitleSizeOver(double fontSize) => SupTitleScale is { } scale ? fontSize * scale : fontSize + UnscaledSupTitleStep;

    /// <summary>The weight a title is drawn in: this look's, else bold.</summary>
    /// <returns>The weight.</returns>
    internal FontWeight DrawnTitleWeight => TitleWeight ?? FontWeight.Bold;
}
