// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.MathText;

/// <summary>What a math font set does with a span: the runs it is set in, each a face and the span's characters as that face
/// codes them. A span outside <c>$...$</c> is the label's text, set in the label's face under every set.</summary>
public sealed class MathFontSetTests
{
    private static readonly Font Label = new() { Family = "DejaVu Sans", Size = 10 };

    /// <summary>The runs a set cuts the text of <paramref name="span"/> into.</summary>
    private static MathRuns SetIn(MathFontSet set, TextSpan span) => set.RunsOf(span.Text, span.Variant, span.OutsideMath);

    [Fact]
    public void TheParser_MarksTheTextOutsideMath()
    {
        var spans = MathTextParser.Parse("Time $t$ (s)").Spans;

        Assert.Equal(["Time ", "t", " (s)"], spans.Select(s => s.Text));
        Assert.Equal([true, false, true], spans.Select(s => s.OutsideMath));
    }

    [Fact]
    public void ASpanBuiltByHand_IsMath()
    {
        Assert.False(new TextSpan("x").OutsideMath);
    }

    [Fact]
    public void TheLibrarySet_SetsAMathSpanInTheLabelsFace_AsOneRunOfItsOwnText()
    {
        var span = new TextSpan("x+2");

        var runs = SetIn(MathFontSet.Library, span);

        Assert.Equal(1, runs.Count);
        Assert.Same(span.Text, runs[0].Text);
        Assert.Equal(0, runs[0].Start);
        Assert.Same(Label, runs[0].Face.In(Label));
    }

    [Fact]
    public void ASpanOutsideMath_IsOneRunInTheLabelsFace()
    {
        var span = MathTextParser.Parse("Time $t$").Spans[0];

        var runs = SetIn(MathFontSet.Library, span);

        Assert.Equal(1, runs.Count);
        Assert.Same(span.Text, runs[0].Text);
        Assert.Same(Label, runs[0].Face.In(Label));
    }

    [Theory]
    [InlineData(@"$\mathbf{y}$", FontWeight.Bold, FontSlant.Normal)]
    [InlineData(@"$\mathit{y}$", FontWeight.Normal, FontSlant.Italic)]
    [InlineData(@"$\mathrm{y}$", FontWeight.Normal, FontSlant.Normal)]
    [InlineData(@"$\text{y}$", FontWeight.Normal, FontSlant.Normal)]
    [InlineData(@"$y$", FontWeight.Normal, FontSlant.Normal)]
    [InlineData(@"$\mathcal{Y}$", FontWeight.Normal, FontSlant.Normal)]
    [InlineData(@"$\mathbb{R}$", FontWeight.Normal, FontSlant.Normal)]
    public void TheLibrarySet_SetsAFontSwitchTheCallerWrote_InTheLabelsFamily(string label, FontWeight weight, FontSlant slant)
    {
        // A \mathbf or a \mathit is a value the caller gave: it wins under every theme. The library's own set has no calligraphic or
        // blackboard face, so those stay in the label's.
        var span = Assert.Single(MathTextParser.Parse(label).Spans);

        var face = SetIn(MathFontSet.Library, span)[0].Face.In(Label);

        Assert.Equal(Label with { Weight = weight, Slant = slant }, face);
        Assert.Equal(Label.Family, face.Family);
    }

    [Fact]
    public void TheLibrarySet_HasNoMatplotlibName()
    {
        Assert.Null(MathFontSet.Library.Name);
        Assert.Same(MathFontSet.Library, MathFontSet.Named(null));
        Assert.Same(MathFontSet.Library, MathFontSet.Named("nothing-matplotlib-names"));
    }

    // ---- Computer Modern (mathtext.fontset 'cm', the classic style) ----

    /// <summary>The span of a label that holds the character: a relation stands between two spaces, which are spans of their own.</summary>
    private static TextSpan CharacterOf(string label) =>
        Assert.Single(MathTextParser.Parse(label).Spans, span => !string.IsNullOrWhiteSpace(span.Text));


    /// <summary>The faces and codes of each character, as runs: (face family, the codes).</summary>
    private static (string Family, string Codes)[] Runs(MathFontSet set, TextSpan span)
    {
        var runs = SetIn(set, span);
        return [.. Enumerable.Range(0, runs.Count).Select(i => (runs[i].Face.In(Label).Family, runs[i].Text))];
    }

    /// <summary>Each row as matplotlib 3.11.2 sets it under plt.style.use('classic') (mathtext.fontset 'cm'), printed by
    /// MathTextParser('path').parse(expr)[3] as (font family, character code) per glyph.</summary>
    [Theory]
    // "$\alpha^2 + x_i$": cmmi10 0xae | cmr10 0x32 | cmr10 0x2b | cmmi10 0x78 | cmmi10 0x69 (the parser keeps the spaces; matplotlib drops them)
    [InlineData(@"$\alpha$", "cmmi10", "®")]
    [InlineData("$x$", "cmmi10", "x")]
    [InlineData("$2$", "cmr10", "2")]
    [InlineData("$+$", "cmr10", "+")]
    // "$-1$": cmsy10 0xa1 | cmr10 0x31
    [InlineData("$-$", "cmsy10", "¡")]
    // "$\Delta t$": cmr10 0xa2 | cmmi10 0x74
    [InlineData(@"$\Delta$", "cmr10", "¢")]
    [InlineData(@"$\infty$", "cmsy10", "1")]
    [InlineData(@"$\sum$", "cmex10", "X")]
    [InlineData(@"$\int$", "cmex10", "Z")]
    [InlineData(@"$\pm$", "cmsy10", "§")]
    [InlineData(@"$\leq$", "cmsy10", "∙")]
    [InlineData("$'$", "cmsy10", "0")]
    [InlineData(@"$\_$", "cmtt10", "_")]
    [InlineData("$/$", "cmmi10", "=")]
    // "$\mathbf{y} = \mathrm{sin}(x)$": cmb10 0x79 | cmr10 0x3d | cmr10 s i n | cmr10 0x28 | cmmi10 0x78 | cmr10 0x29
    [InlineData(@"$\mathbf{y}$", "cmb10", "y")]
    [InlineData(@"$\mathrm{sin}$", "cmr10", "sin")]
    [InlineData(@"$\mathit{ab}$", "cmti10", "ab")]
    [InlineData(@"$\mathcal{A}$", "cmsy10", "A")]
    [InlineData(@"$\mathbf{2}$", "cmb10", "2")]
    // \mathbf{\alpha}: cmmi10 0xae, a symbol keeps its face whatever the switch
    [InlineData(@"$\mathbf{\alpha}$", "cmmi10", "®")]
    // "$\circ$": cmsy10 0xb1; "$\phi$": cmmi10 0xc1
    [InlineData(@"$\circ$", "cmsy10", "±")]
    [InlineData(@"$\phi$", "cmmi10", "Á")]
    public void ComputerModern_SetsACharacterInTheFaceAndCodeMatplotlibSetsItIn(string label, string family, string codes)
    {
        var span = Assert.Single(MathTextParser.Parse(label).Spans);

        // a relation stands between matplotlib's spaces, which Computer Modern has no glyph for (the label's face)
        Assert.Equal([(family, codes)], Runs(MathFontSet.ComputerModern, span).Where(run => !string.IsNullOrWhiteSpace(run.Codes)));
    }

    [Fact]
    public void ComputerModern_CutsASpanWhereTheFaceChanges_AndSetsWhatItHasNoGlyphForInSTIX()
    {
        // "x + 2": matplotlib drops the spaces; this parser keeps them, and Computer Modern has no space of its own, so a space is
        // the label's. \neq is not in Computer Modern: matplotlib takes it from STIX (STIXGeneral 0x2260).
        var span = new TextSpan("x + 2\u2260y");

        Assert.Equal(
            [("cmmi10", "x"), ("DejaVu Sans", " "), ("cmr10", "+"), ("DejaVu Sans", " "), ("cmr10", "2"), ("STIXGeneral", "\u2260"), ("cmmi10", "y")],
            Runs(MathFontSet.ComputerModern, span));
    }

    [Fact]
    public void ComputerModern_SetsAnAccentedLetterWhole_InTheLabelsFace()
    {
        // A combining accent belongs to the letter before it; Computer Modern codes no combining mark, so the pair is the label's.
        var span = Assert.Single(MathTextParser.Parse(@"$\hat{y}$").Spans);

        Assert.Equal([("DejaVu Sans", "ŷ")], Runs(MathFontSet.ComputerModern, span));
    }

    [Theory]
    [InlineData("ŷ")] // a non-spacing mark (a hat)
    [InlineData("y⃝")] // an enclosing mark
    [InlineData("yः")] // a spacing combining mark
    public void ComputerModern_KeepsEveryKindOfCombiningMarkWithItsLetter(string cluster)
    {
        Assert.Equal([("cmr10", "2"), ("DejaVu Sans", cluster)], Runs(MathFontSet.ComputerModern, new TextSpan("2" + cluster)));
    }

    [Fact]
    public void ComputerModern_SetsACharacterBeyondTheBasicPlane_WholeInTheLabelsFace()
    {
        // U+1D465 MATHEMATICAL ITALIC SMALL X, a surrogate pair: no BaKoMa face holds it.
        Assert.Equal([("cmmi10", "x"), ("DejaVu Sans", "\U0001D465")], Runs(MathFontSet.ComputerModern, new TextSpan("x\U0001D465")));
    }

    /// <summary>What matplotlib 3.11.2 takes from STIX under plt.style.use('classic') (mathtext.fontset 'cm', whose BakomaFonts falls back
    /// to StixFonts), printed by MathTextParser('path').parse(expr)[3] as (font family, style, character code) per glyph.</summary>
    [Theory]
    // $\neq$: STIXGeneral Regular 0x2260; $\partial$: STIXGeneral Regular 0x2202
    [InlineData(@"$\neq$", "STIXGeneral", FontWeight.Normal, FontSlant.Normal, "\u2260")]
    [InlineData(@"$\partial$", "STIXGeneral", FontWeight.Normal, FontSlant.Normal, "\u2202")]
    // $\mathbb{R}$: STIXGeneral Italic 0x211d; $\mathbb{2}$: STIXGeneral Regular 0x1d7da; $\mathbb{A}$: STIXNonUnicode Italic 0xe154
    [InlineData(@"$\mathbb{R}$", "STIXGeneral", FontWeight.Normal, FontSlant.Italic, "\u211D")]
    [InlineData(@"$\mathbb{2}$", "STIXGeneral", FontWeight.Normal, FontSlant.Normal, "\U0001D7DA")]
    [InlineData(@"$\mathbb{A}$", "STIXNonUnicode", FontWeight.Normal, FontSlant.Italic, "\uE154")]
    // $\mathbf{\neq}$: STIXGeneral Bold 0x2260, a font switch picks the STIX face of the symbol
    [InlineData(@"$\mathbf{\neq}$", "STIXGeneral", FontWeight.Bold, FontSlant.Normal, "\u2260")]
    [InlineData(@"$\mathrm{\neq}$", "STIXGeneral", FontWeight.Normal, FontSlant.Normal, "\u2260")]
    public void ComputerModern_SetsWhatItHasNoGlyphFor_InTheSTIXFaceAndCodeMatplotlibTakesIt(string label, string family, FontWeight weight, FontSlant slant, string codes)
    {
        var span = CharacterOf(label);

        Assert.Equal([(family, weight, slant, codes)], Glyphs(MathFontSet.ComputerModern, span));
    }

    [Fact]
    public void ComputerModern_SetsAnEmptySpanAsOneEmptyRun()
    {
        var span = new TextSpan("", TextSpanKind.Superscript, 0.7);

        Assert.Equal([("DejaVu Sans", "")], Runs(MathFontSet.ComputerModern, span));
    }

    [Fact]
    public void ComputerModern_LeavesTheTextOutsideMathInTheLabelsFace()
    {
        var spans = MathTextParser.Parse("Time $t$").Spans;

        Assert.Equal([("DejaVu Sans", "Time ")], Runs(MathFontSet.ComputerModern, spans[0]));
        Assert.Equal([("cmmi10", "t")], Runs(MathFontSet.ComputerModern, spans[1]));
    }

    /// <summary>A run of characters coded as written in two faces is cut without a copy of what is written: each run's text a stretch of it.</summary>
    [Fact]
    public void ComputerModern_CutsARunInTwoFaces_IntoStretchesOfItsText()
    {
        var runs = MathFontSet.ComputerModern.RunsOf("x2", FontVariant.Default, false);

        Assert.Equal(2, runs.Count);
        Assert.Equal(("x", "2"), (runs[0].Text, runs[1].Text));
        Assert.Equal((0, 1), (runs[0].Start, runs[1].Start));
    }

    [Fact]
    public void ComputerModern_KeepsTheSpansOwnText_ForASpanSetAsOneRunInItsOwnCodes()
    {
        var xy = new TextSpan("xy");

        Assert.Same(xy.Text, SetIn(MathFontSet.ComputerModern, xy)[0].Text);
    }

    [Theory]
    [InlineData(true, "cm")]
    [InlineData(false, "dejavusans")]
    public void AFontSet_IsNamedAsMatplotlibNamesIt(bool computerModern, string name)
    {
        var set = computerModern ? MathFontSet.ComputerModern : MathFontSet.DejaVuSans;

        Assert.Equal(name, set.Name);
        Assert.Same(set, MathFontSet.Named(name));
    }

    // ---- DejaVu Sans (mathtext.fontset 'dejavusans', the default style) ----

    /// <summary>The faces and codes of each character, as runs: (face family, weight, slant, the codes).</summary>
    private static (string Family, FontWeight Weight, FontSlant Slant, string Codes)[] Faces(MathFontSet set, TextSpan span)
    {
        var runs = SetIn(set, span);
        return [.. Enumerable.Range(0, runs.Count).Select(i => runs[i].Face.In(Label) is var f ? (f.Family, f.Weight, f.Slant, runs[i].Text) : default)];
    }

    /// <summary>The faces of the characters a span sets, not the spaces a relation stands between (which are no glyph of matplotlib's).</summary>
    private static (string Family, FontWeight Weight, FontSlant Slant, string Codes)[] Glyphs(MathFontSet set, TextSpan span) =>
        [.. Faces(set, span).Select(face => (face.Family, face.Weight, face.Slant, Codes: string.Concat(face.Codes.Where(c => !char.IsWhiteSpace(c)))))
            .Where(face => face.Codes.Length > 0)];

    /// <summary>Each row as matplotlib 3.11.2 sets it under its default style (mathtext.fontset 'dejavusans', mathtext.default
    /// 'normal'), printed by MathTextParser('path').parse(expr)[3] as (font family / style, character code) per glyph.</summary>
    [Theory]
    // "$\alpha^2 + x_i$": DejaVu Sans/Oblique 0x3b1 | Book 0x32 | Book 0x2b | Oblique 0x78 | Oblique 0x69
    [InlineData(@"$\alpha$", "DejaVu Sans", FontWeight.Normal, FontSlant.Italic, "\u03B1")]
    [InlineData("$x$", "DejaVu Sans", FontWeight.Normal, FontSlant.Italic, "x")]
    [InlineData("$2$", "DejaVu Sans", FontWeight.Normal, FontSlant.Normal, "2")]
    [InlineData("$+$", "DejaVu Sans", FontWeight.Normal, FontSlant.Normal, "+")]
    // "$-1$": DejaVu Sans/Book 0x2212 | Book 0x31
    [InlineData("$-$", "DejaVu Sans", FontWeight.Normal, FontSlant.Normal, "\u2212")]
    // "$\Delta t$": Book 0x394 | Oblique 0x74; a Greek capital is upright
    [InlineData(@"$\Delta$", "DejaVu Sans", FontWeight.Normal, FontSlant.Normal, "\u0394")]
    // \hbar, \ell, \Re, \aleph are letters: Oblique; \wp is a symbol: Book
    [InlineData(@"$\ell$", "DejaVu Sans", FontWeight.Normal, FontSlant.Italic, "\u2113")]
    [InlineData(@"$\wp$", "DejaVu Sans", FontWeight.Normal, FontSlant.Normal, "\u2118")]
    // \sum, \int: DejaVu Sans Display/Display 0x2211, 0x222b, whatever the switch
    [InlineData(@"$\sum$", "DejaVu Sans Display", FontWeight.Normal, FontSlant.Normal, "\u2211")]
    [InlineData(@"$\mathbf{\int}$", "DejaVu Sans Display", FontWeight.Normal, FontSlant.Normal, "\u222B")]
    // "$x\prime$": Oblique 0x78 | cmsy10 0x30: the prime is BaKoMa's
    [InlineData(@"$\prime$", "cmsy10", FontWeight.Normal, FontSlant.Normal, "0")]
    [InlineData("$'$", "cmsy10", FontWeight.Normal, FontSlant.Normal, "0")]
    // "$\mathbf{y} = \mathrm{sin}(x)$": Bold 0x79 | Book 0x3d | Book s i n | Book 0x28 | Oblique 0x78 | Book 0x29
    [InlineData(@"$\mathbf{y}$", "DejaVu Sans", FontWeight.Bold, FontSlant.Normal, "y")]
    [InlineData(@"$\mathbf{\alpha}$", "DejaVu Sans", FontWeight.Bold, FontSlant.Normal, "\u03B1")]
    [InlineData(@"$\mathbf{-}$", "DejaVu Sans", FontWeight.Bold, FontSlant.Normal, "\u2212")]
    [InlineData(@"$\mathrm{sin}$", "DejaVu Sans", FontWeight.Normal, FontSlant.Normal, "sin")]
    [InlineData(@"$\mathit{ab}$", "DejaVu Sans", FontWeight.Normal, FontSlant.Italic, "ab")]
    public void DejaVuSans_SetsACharacterInTheFaceMatplotlibSetsItIn(string label, string family, FontWeight weight, FontSlant slant, string codes)
    {
        var span = Assert.Single(MathTextParser.Parse(label).Spans);

        Assert.Equal([(family, weight, slant, codes)], Faces(MathFontSet.DejaVuSans, span));
    }

    /// <summary>matplotlib's test for the italic face: unicodedata.category(c)[0] == 'L' and not unicodedata.name(c).startswith("GREEK
    /// CAPITAL"); each row's category and name printed with Python's unicodedata.</summary>
    [Theory]
    [InlineData('Ἀ', FontSlant.Normal)] // Lu, GREEK CAPITAL LETTER ALPHA WITH PSILI
    [InlineData('ἀ', FontSlant.Italic)] // Ll, GREEK SMALL LETTER ALPHA WITH PSILI
    [InlineData('ᾈ', FontSlant.Normal)] // Lt, GREEK CAPITAL LETTER ALPHA WITH PSILI AND PROSGEGRAMMENI
    [InlineData('ϒ', FontSlant.Italic)] // Lu, GREEK UPSILON WITH HOOK SYMBOL
    [InlineData('Ϣ', FontSlant.Italic)] // Lu, COPTIC CAPITAL LETTER SHEI
    [InlineData('Ͱ', FontSlant.Normal)] // Lu, GREEK CAPITAL LETTER HETA
    [InlineData('ʰ', FontSlant.Italic)] // Lm, MODIFIER LETTER SMALL H
    [InlineData('א', FontSlant.Italic)] // Lo, HEBREW LETTER ALEF
    [InlineData('ǅ', FontSlant.Italic)] // Lt, LATIN CAPITAL LETTER D WITH SMALL LETTER Z WITH CARON
    [InlineData('℘', FontSlant.Normal)] // Sm, SCRIPT CAPITAL P (\wp)
    public void DejaVuSans_SetsALetterThatIsNoGreekCapitalInTheItalicFace(char c, FontSlant slant)
    {
        var runs = SetIn(MathFontSet.DejaVuSans, new TextSpan(c.ToString()));

        Assert.Equal(slant, runs[0].Face.In(Label).Slant);
    }

    /// <summary>What matplotlib 3.11.2 takes from STIX under its default style (mathtext.fontset 'dejavusans', whose DejaVuFonts falls back
    /// to StixSansFonts), printed by MathTextParser('path').parse(expr)[3] as (font family, style, character code) per glyph.</summary>
    [Theory]
    // $\mathcal{A}$: STIXNonUnicode Italic 0xe22d; $\mathcal{a}$: STIXGeneral Italic 0x1d622; $\mathcal{2}$: STIXGeneral Regular 0x1d7e4
    [InlineData(@"$\mathcal{A}$", "STIXNonUnicode", FontWeight.Normal, FontSlant.Italic, "\uE22D")]
    [InlineData(@"$\mathcal{a}$", "STIXGeneral", FontWeight.Normal, FontSlant.Italic, "\U0001D622")]
    [InlineData(@"$\mathcal{2}$", "STIXGeneral", FontWeight.Normal, FontSlant.Normal, "\U0001D7E4")]
    [InlineData(@"$\mathcal{+}$", "STIXGeneral", FontWeight.Normal, FontSlant.Normal, "+")]
    // $\mathbb{R}$: STIXGeneral Italic 0x211d; $\mathbb{A}$: STIXNonUnicode Italic 0xe154; $\mathbb{\neq}$: STIXGeneral Regular 0x2260
    [InlineData(@"$\mathbb{R}$", "STIXGeneral", FontWeight.Normal, FontSlant.Italic, "\u211D")]
    [InlineData(@"$\mathbb{A}$", "STIXNonUnicode", FontWeight.Normal, FontSlant.Italic, "\uE154")]
    [InlineData(@"$\mathbb{\neq}$", "STIXGeneral", FontWeight.Normal, FontSlant.Normal, "\u2260")]
    public void DejaVuSans_SetsACalligraphicOrBlackboardCharacterInSTIX_AsMatplotlibTakesIt(string label, string family, FontWeight weight, FontSlant slant, string codes)
    {
        var span = CharacterOf(label);

        Assert.Equal([(family, weight, slant, codes)], Glyphs(MathFontSet.DejaVuSans, span));
    }

    // matplotlib 3.11.2 under the default style sets these in DejaVu Sans itself: only what DejaVu Sans holds no glyph for goes to STIX.
    [Theory]
    [InlineData(@"$\neq$", "\u2260")]
    [InlineData(@"$\partial$", "\u2202")]
    [InlineData(@"$\leq$", "\u2264")]
    public void DejaVuSans_KeepsWhatItHoldsInItsOwnFace(string label, string codes)
    {
        var span = CharacterOf(label);

        Assert.Equal([("DejaVu Sans", FontWeight.Normal, FontSlant.Normal, codes)], Glyphs(MathFontSet.DejaVuSans, span));
    }

    [Fact]
    public void DejaVuSans_SetsMathInDejaVuSans_WhateverTheLabelsFamily()
    {
        // mathtext.fontset names the faces: a label in another family still has its math in DejaVu Sans.
        var runs = SetIn(MathFontSet.DejaVuSans, new TextSpan("x"));

        Assert.Equal("DejaVu Sans", runs[0].Face.In(new Font { Family = "serif", Size = 10 }).Family);
    }

    [Fact]
    public void DejaVuSans_KeepsAnAccentWithItsLetter_InTheLettersFace()
    {
        var span = Assert.Single(MathTextParser.Parse(@"$\hat{y}$").Spans);

        Assert.Equal([("DejaVu Sans", FontWeight.Normal, FontSlant.Italic, "y\u0302")], Faces(MathFontSet.DejaVuSans, span));
    }
}