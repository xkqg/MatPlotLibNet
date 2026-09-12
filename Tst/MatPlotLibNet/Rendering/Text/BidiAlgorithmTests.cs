// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Text;

namespace MatPlotLibNet.Tests.Rendering.Text;

/// <summary>The bidi algorithm on the strings a chart actually draws: a tick label, a Hebrew title with punctuation,
/// a Latin label with an Arabic word in brackets, a unit. The Unicode conformance corpora prove the rules; these facts
/// say what the rules mean for a label, in the words the renderers use.</summary>
public sealed class BidiAlgorithmTests
{
    [Fact]
    public void EmptyText_IsALeftToRightParagraphWithNoLevels()
    {
        var paragraph = BidiAlgorithm.Resolve("");

        Assert.False(paragraph.IsRightToLeft);
        Assert.Equal(0, paragraph.ParagraphLevel);
        Assert.Empty(paragraph.Levels);
        Assert.Empty(paragraph.VisualOrder());
    }

    [Fact]
    public void ATickLabel_StaysLeftToRight()
    {
        var paragraph = BidiAlgorithm.Resolve("12.5");

        Assert.False(paragraph.IsRightToLeft);
        Assert.Equal(new byte[] { 0, 0, 0, 0 }, paragraph.Levels);
        Assert.Equal([0, 1, 2, 3], paragraph.VisualOrder());
    }

    [Fact]
    public void AHebrewTitle_IsRightToLeft_AndItsPunctuationStaysAtTheEnd()
    {
        // Logical "שלום!" — the exclamation mark closes the sentence, so visually it sits on the LEFT: the last
        // logical character is the first visual one.
        var paragraph = BidiAlgorithm.Resolve("שלום!");

        Assert.True(paragraph.IsRightToLeft);
        Assert.Equal(1, paragraph.ParagraphLevel);
        Assert.Equal(new byte[] { 1, 1, 1, 1, 1 }, paragraph.Levels);
        Assert.Equal([4, 3, 2, 1, 0], paragraph.VisualOrder());
    }

    [Fact]
    public void ALatinLabel_WithAnArabicWordInBrackets_KeepsTheBracketsAroundTheWord()
    {
        // "Temp (درجة)" — the paragraph is Latin, the Arabic word is one right-to-left run, and the brackets
        // resolve as a pair (rule N0), so they stay on the outside of the word in display order.
        var paragraph = BidiAlgorithm.Resolve("Temp (درجة)");

        Assert.False(paragraph.IsRightToLeft);
        Assert.Equal(new byte[] { 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 0 }, paragraph.Levels);
        Assert.Equal([0, 1, 2, 3, 4, 5, 9, 8, 7, 6, 10], paragraph.VisualOrder());
    }

    [Fact]
    public void AUnit_WithADegreeSign_IsLeftToRight()
    {
        var paragraph = BidiAlgorithm.Resolve("°C");

        Assert.False(paragraph.IsRightToLeft);
        Assert.Equal(new byte[] { 0, 0 }, paragraph.Levels);
    }

    [Fact]
    public void DigitsInsideArabic_ReadLeftToRight_InsideTheRightToLeftRun()
    {
        // "درجة 12" — Arabic paragraph; the European number takes level 2 (rule I2) so its digits keep their
        // order while the run around them is reversed.
        var paragraph = BidiAlgorithm.Resolve("درجة 12");

        Assert.True(paragraph.IsRightToLeft);
        Assert.Equal(new byte[] { 1, 1, 1, 1, 1, 2, 2 }, paragraph.Levels);
        Assert.Equal([5, 6, 4, 3, 2, 1, 0], paragraph.VisualOrder());
    }

    [Fact]
    public void AnExplicitParagraphDirection_Wins()
    {
        var forcedRtl = BidiAlgorithm.Resolve("abc", BidiDirection.RightToLeft);
        var forcedLtr = BidiAlgorithm.Resolve("שלום", BidiDirection.LeftToRight);

        Assert.True(forcedRtl.IsRightToLeft);
        Assert.Equal(new byte[] { 2, 2, 2 }, forcedRtl.Levels);
        Assert.False(forcedLtr.IsRightToLeft);
        Assert.Equal(new byte[] { 1, 1, 1, 1 }, forcedLtr.Levels);
        Assert.Equal([3, 2, 1, 0], forcedLtr.VisualOrder());
    }

    [Fact]
    public void ASurrogatePair_IsOneCharacter_AndBothUnitsShareItsLevel()
    {
        // U+1D538 MATHEMATICAL DOUBLE-STRUCK CAPITAL A is a Latin-class letter written as two UTF-16 units.
        var paragraph = BidiAlgorithm.Resolve("\U0001D538א");

        Assert.Equal(3, paragraph.Levels.Length);
        Assert.Equal(paragraph.Levels[0], paragraph.Levels[1]);
        Assert.Equal(0, paragraph.Levels[0]);
        Assert.Equal(1, paragraph.Levels[2]);
        // The order names characters by their first unit: the pair is one entry, the Hebrew letter the next.
        Assert.Equal([0, 2], paragraph.VisualOrder());
    }

    [Fact]
    public void ALoneSurrogate_DoesNotThrow_AndIsTreatedAsLeftToRight()
    {
        var paragraph = BidiAlgorithm.Resolve("\uD800a");

        Assert.Equal(new byte[] { 0, 0 }, paragraph.Levels);
    }

    [Fact]
    public void AnExplicitOverride_IsRemovedFromTheOrder_AndReversesWhatItCovers()
    {
        // RLO a b c PDF: the formatting characters are removed by rule X9 (level Removed), the letters are
        // forced to R at level 1 and therefore display reversed.
        var paragraph = BidiAlgorithm.Resolve("\u202Eabc\u202C");

        Assert.Equal(BidiParagraph.Removed, paragraph.Levels[0]);
        Assert.Equal(BidiParagraph.Removed, paragraph.Levels[4]);
        Assert.Equal(new byte[] { 1, 1, 1 }, paragraph.Levels[1..4]);
        Assert.Equal([3, 2, 1], paragraph.VisualOrder());
    }

    [Fact]
    public void AnIsolate_KeepsItsContentApart_AndIsNotRemoved()
    {
        // a RLI ב ג PDI b: the isolate formatting characters stay (level 0, neutral), the Hebrew inside is
        // one reversed run, and the Latin around it is untouched.
        var paragraph = BidiAlgorithm.Resolve("a\u2067בג\u2069b");

        Assert.Equal(new byte[] { 0, 0, 1, 1, 0, 0 }, paragraph.Levels);
        Assert.Equal([0, 1, 3, 2, 4, 5], paragraph.VisualOrder());
    }

    [Fact]
    public void AFirstStrongIsolate_TakesTheDirectionOfItsContent()
    {
        var paragraph = BidiAlgorithm.Resolve("a\u2068בג\u2069b");

        Assert.Equal(new byte[] { 0, 0, 1, 1, 0, 0 }, paragraph.Levels);
    }

    [Fact]
    public void NestingDeeperThanTheLimit_DoesNotThrow_AndStopsRaisingTheLevel()
    {
        // 130 nested LRE characters overflow max_depth (125); the overflow ones are ignored, the letter after
        // them sits at the deepest valid even level, and nothing throws.
        string text = new string('\u202A', 130) + "a";

        var paragraph = BidiAlgorithm.Resolve(text);

        Assert.Equal(124, paragraph.Levels[130]);
    }

    [Fact]
    public void RenderLevels_GiveARemovedCharacterTheLevelOfWhatPrecedesIt()
    {
        // A zero width joiner (BN) between two Hebrew letters is removed by X9 for ordering, but a renderer still
        // has to hand it to the shaper inside the run it sits in: it gets the level of its predecessor.
        var paragraph = BidiAlgorithm.Resolve("ש\u200Dל");

        Assert.Equal(BidiParagraph.Removed, paragraph.Levels[1]);
        Assert.Equal(new byte[] { 1, 1, 1 }, paragraph.RenderLevels());
    }

    [Fact]
    public void RenderLevels_AtTheStart_UseTheParagraphLevel()
    {
        var paragraph = BidiAlgorithm.Resolve("\u200Dab");

        Assert.Equal(new byte[] { 0, 0, 0 }, paragraph.RenderLevels());
    }

    [Fact]
    public void TrailingWhitespace_ResetsToTheParagraphLevel()
    {
        // Rule L1: whitespace at the end of the line takes the paragraph level even after a Hebrew run.
        var paragraph = BidiAlgorithm.Resolve("a שלום  ");

        Assert.Equal(0, paragraph.Levels[6]);
        Assert.Equal(0, paragraph.Levels[7]);
    }
}
