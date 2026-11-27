// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.MathText;

/// <summary>A rich text made by hand is read back into a tree (<see cref="MathBoxes.Of"/>): the one place a kind of span is read. Read back,
/// each kind is the construction the parse makes for it.</summary>
public sealed class MathBoxesTests
{
    private static MathInk Laid(RichText rich) =>
        MathInk.Of(MathBoxes.Of(rich), new MathStyle(MathFontSet.Library, new Font { Size = 10 }, MathMeasure.ByInk(TestGlyphInk.Boxes)));

    private static MathGlyphInk Run(MathInk ink, string text) => ink.Glyphs.Single(g => g.Text == text);

    [Fact]
    public void AParsedRichText_HoldsTheTreeItWasWrittenFrom()
    {
        var parsed = MathTextParser.Parse("$x^2$");

        Assert.NotNull(parsed.Box);
        Assert.Same(parsed.Box, MathBoxes.Of(parsed));
    }

    [Fact]
    public void ASpanMadeByHand_IsSetAtItsOwnSize()
    {
        var ink = Laid(new RichText([new TextSpan("a"), new TextSpan("b", FontSizeScale: 2)]));

        Assert.Equal(20, Run(ink, "b").Font.Size);
    }

    [Fact]
    public void AScriptMadeByHand_IsTheScriptOfTheSpanBeforeIt()
    {
        var ink = Laid(new RichText([new TextSpan("x"), new TextSpan("2", TextSpanKind.Superscript, 0.7), new TextSpan("i", TextSpanKind.Subscript, 0.7)]));

        Assert.True(Run(ink, "2").Y < 0);
        Assert.True(Run(ink, "i").Y > 0);
        Assert.Equal(7, Run(ink, "2").Font.Size, 9);
    }

    [Fact]
    public void AScriptMadeByHandWithNothingBeforeIt_StandsOnAnEmptyNucleus()
    {
        var ink = Laid(new RichText([new TextSpan("2", TextSpanKind.Superscript, 0.7)]));

        Assert.True(Run(ink, "2").Y < 0);
    }

    [Fact]
    public void ALimitMadeByHand_StandsUnderItsOperator()
    {
        var ink = Laid(new RichText([new TextSpan("∑", TextSpanKind.LargeOperator, 1.4), new TextSpan("i", TextSpanKind.OperatorSubscript, 0.7),
            new TextSpan("n", TextSpanKind.OperatorSuperscript, 0.7)]));

        Assert.True(Run(ink, "i").Y > 0);
        Assert.True(Run(ink, "n").Y < 0);
    }

    [Fact]
    public void AFractionMadeByHand_IsStacked_AndADenominatorAloneTooIs()
    {
        var both = Laid(new RichText([new TextSpan("a", TextSpanKind.FractionNumerator, 0.7), new TextSpan("b", TextSpanKind.FractionDenominator, 0.7)]));
        var numerator = Laid(new RichText([new TextSpan("a", TextSpanKind.FractionNumerator, 0.7)]));
        var denominator = Laid(new RichText([new TextSpan("b", TextSpanKind.FractionDenominator, 0.7)]));

        Assert.True(Run(both, "a").Y < Run(both, "b").Y);
        Assert.Single(numerator.Rules);
        Assert.True(Run(denominator, "b").Y > 0);
    }

    [Fact]
    public void ARadicalAndAnAccentMadeByHand_AreTheirConstructions()
    {
        Assert.Single(Laid(new RichText([new TextSpan("x", TextSpanKind.Radical)])).Rules);
        Assert.Equal("x̂", Assert.Single(Laid(new RichText([new TextSpan("x̂", TextSpanKind.Accent)])).Glyphs).Text);
    }

    [Fact]
    public void AMatrixMadeByHand_IsLaidOutInRows()
    {
        var ink = Laid(new RichText([
            new TextSpan("pmatrix", TextSpanKind.MatrixStart),
            new TextSpan("a", TextSpanKind.MatrixCell), new TextSpan("", TextSpanKind.MatrixCellSeparator), new TextSpan("", TextSpanKind.MatrixRowSeparator),
            new TextSpan("c", TextSpanKind.MatrixCell),
            new TextSpan("pmatrix", TextSpanKind.MatrixEnd),
            new TextSpan("", TextSpanKind.MatrixRowSeparator),
        ]));

        Assert.True(Run(ink, "c").Y > Run(ink, "a").Y);
        Assert.Contains(ink.Glyphs, g => g.Text == "(");
    }
}
