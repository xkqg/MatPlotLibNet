// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.MathText;

/// <summary>A delimiter or a root sign set as matplotlib's <c>AutoHeightChar</c> sets it: the first of the set's sizes that is tall enough
/// (within a fifth of the x-height), the last when none is, at the size that makes it exactly as tall as what it stands beside, except the
/// first size of a set that keeps its own; and a glyph of <c>cmex10</c> raised by matplotlib's <c>Cmex10</c> offset. The metrics are those
/// of <see cref="MathInkTests"/>: a glyph's ink rises 0.7 of the size.</summary>
public sealed class SizedGlyphTests
{
    private const double Size = 10;

    private static MathStyle StyleOf(MathFontSet set, IGlyphInkMetrics? ink = null) =>
        new(set, new Font { Family = "DejaVu Sans", Size = Size }, MathMeasure.ByInk(ink ?? TestGlyphInk.Boxes));

    /// <summary>A parenthesis laid into a fresh ink of <paramref name="set"/> to the given height, and the one run it set.</summary>
    private static MathExtent LayParenthesis(MathFontSet set, double height, out MathGlyphInk run)
    {
        var ink = MathInk.Of(new GlyphsBox("x"), StyleOf(set));
        var box = SizedGlyph.Lay(ink, StyleOf(set), "(", height, 0);
        run = Assert.Single(ink.Glyphs, glyph => glyph.Text == "(");
        return box;
    }

    [Fact]
    public void TheFirstSizeOfASetThatKeepsItsOwn_IsSetAtItsOwnSize_WhenItIsTallEnough()
    {
        var box = LayParenthesis(MathFontSet.DejaVuSans, 6, out var run);

        Assert.Equal("DejaVu Sans", run.Font.Family);
        Assert.Equal(Size, run.Font.Size);
        Assert.Equal(0, run.Y);
        Assert.Equal(7, box.Height, 9);
    }

    [Fact]
    public void WhenNoSizeIsTallEnough_TheLastIsSetAtTheSizeThatMakesItAsTallAsAsked()
    {
        var box = LayParenthesis(MathFontSet.DejaVuSans, 20, out var run);

        Assert.Equal("STIXSizeFourSym", run.Font.Family);
        Assert.Equal(Size * 20 / 7, run.Font.Size, 9);
        Assert.Equal(20, box.Height, 9);
        Assert.Equal(0, box.Depth, 9);
    }

    [Fact]
    public void ASetWhoseFirstSizeIsScaled_SetsEvenTheFirstSizeAtTheSizeThatMakesItAsTallAsAsked()
    {
        var box = LayParenthesis(MathFontSet.ComputerModern, 6, out var run);

        Assert.Equal("cmr10", run.Font.Family);
        Assert.Equal(Size * 6 / 7, run.Font.Size, 9);
        Assert.Equal(6, box.Height, 9);
    }

    [Fact]
    public void ASizeWithNoInk_TakesTheRoomOfNothingAndPlacesNothingScaled()
    {
        var ink = MathInk.Of(new GlyphsBox("x"), StyleOf(MathFontSet.DejaVuSans));
        var style = StyleOf(MathFontSet.DejaVuSans, new TestGlyphInk((_, face, size) => new GlyphInk(0, 0, 0, 0, TestGlyphInk.BoxBand(face with { Size = size })), TestGlyphInk.BoxBand));

        var box = SizedGlyph.Lay(ink, style, "(", 6, 0);

        Assert.Equal(0, box.Height);
        Assert.Equal(0, box.Depth);
        Assert.DoesNotContain(ink.Glyphs, glyph => glyph.Text == "(");
    }

    [Fact]
    public void ACharacterTheSetHasNoSizesFor_IsTheLibrarysGrowth_OfItsOwnGlyph()
    {
        var box = LayParenthesis(MathFontSet.Library, 20, out var run);

        Assert.Equal(Size * 20 / 7, run.Font.Size, 9);
        Assert.Equal(20, box.Height, 9);
    }

    /// <summary>matplotlib's <c>Cmex10</c> offset: the glyph's box rises by half of its ink and a third of its size, and so is drawn
    /// that far above the baseline its box stands on.</summary>
    [Fact]
    public void AGlyphOfTheExtensionFace_IsRaisedByHalfItsInkAndAThirdOfItsSize()
    {
        var style = StyleOf(MathFontSet.ComputerModern);

        var run = style.Run(ComputerModernMathFontSet.Extension, "X");

        double lift = 7.0 / 2 + Size / 3;
        Assert.Equal(lift, run.Lift, 9);
        Assert.Equal(7 + lift, run.Extent.Height, 9);
        Assert.Equal(-lift, run.Extent.Depth, 9);
    }

    [Fact]
    public void AGlyphOfAnyOtherFace_IsNotRaised()
    {
        var run = StyleOf(MathFontSet.ComputerModern).Run(MathFace.Label, "X");

        Assert.Equal(0, run.Lift);
        Assert.Equal(7, run.Extent.Height, 9);
    }
}
