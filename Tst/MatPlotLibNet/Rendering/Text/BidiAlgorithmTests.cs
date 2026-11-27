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

        Assert.Equal(0, paragraph.ParagraphLevel);
        Assert.Empty(paragraph.Levels);
        Assert.Empty(paragraph.Order);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("12.5", false)]                          // digits and a separator: nothing strong, the default
    [InlineData("Latency (µs) — 12:00:30", false)]       // a Latin label with symbols above the Latin block
    [InlineData("שלום!", true)]                           // Hebrew decides
    [InlineData("12 שלום", true)]                         // the first STRONG character decides, not the first character
    [InlineData("abc שלום", false)]
    [InlineData("مرحبا", true)]                           // Arabic letters are AL
    [InlineData("\u200F12", true)]                       // a right-to-left mark is a strong R on its own
    [InlineData("\u2067שלום\u2069abc", false)]           // an isolate is skipped: the Latin after it decides
    [InlineData("\u2067שלום", false)]                    // an isolate with no end runs to the end: nothing decides
    [InlineData("\u2066\u2067א\u2069\u2069ب", true)]     // nested isolates skip as one
    [InlineData("\u2067א\u2029ב", false)]                // a paragraph separator ends the open isolate: it never closes
    [InlineData("\U0001D538", false)]                    // a Latin letter written as a surrogate pair
    [InlineData("\U00010800", true)]                     // a Cypriot letter (R) written as a surrogate pair
    public void IsRightToLeftParagraph_AgreesWithTheFullResolution(string text, bool expected)
    {
        Assert.Equal(expected, BidiAlgorithm.IsRightToLeftParagraph(text));
        Assert.Equal((BidiAlgorithm.Resolve(text).ParagraphLevel == 1), BidiAlgorithm.IsRightToLeftParagraph(text));
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("x", true)]
    [InlineData("Latency (µs) — 12:00:30", true)]   // digits, symbols and Latin: every character at level 0
    [InlineData("\U0001D538", true)]                 // a Latin letter written as a surrogate pair
    [InlineData("שבת", false)]                       // Hebrew is R
    [InlineData("abc שלום", false)]                  // one right-to-left word is enough
    [InlineData("مرحبا", false)]                     // Arabic letters are AL
    [InlineData("٣", false)]                    // an Arabic-Indic digit is AN, which rule I1 raises
    [InlineData("‫abc‬", false)]           // an explicit embedding
    [InlineData("⁦abc⁩", false)]           // an isolate
    [InlineData("a‍b", false)]                  // a boundary neutral, which rule X9 removes
    public void ReadsInWrittenOrder_IsTrueOnlyWhereTheFullResolutionPutsEveryCharacterAtLevelZero(string text, bool expected)
    {
        Assert.Equal(expected, BidiAlgorithm.ReadsInWrittenOrder(text));
        if (expected)
        {
            Assert.All(BidiAlgorithm.Resolve(text).Levels, level => Assert.Equal(0, level));
        }
    }

    [Fact]
    public void ALoneSurrogate_ReadsAsALeftToRightLetter()
    {
        // Built in code: an attribute stores its strings as UTF-8, which cannot hold a lone surrogate, so InlineData would
        // hand the test a replacement character instead.
        string text = new string([(char)0xD800, '\u05D0']);

        Assert.False(BidiAlgorithm.IsRightToLeftParagraph(text));
        Assert.Equal((BidiAlgorithm.Resolve(text).ParagraphLevel == 1), BidiAlgorithm.IsRightToLeftParagraph(text));
    }

    [Fact]
    public void ATickLabel_StaysLeftToRight()
    {
        var paragraph = BidiAlgorithm.Resolve("12.5");

        Assert.Equal(0, paragraph.ParagraphLevel);
        Assert.Equal(new byte[] { 0, 0, 0, 0 }, paragraph.Levels);
        Assert.Equal([0, 1, 2, 3], paragraph.Order);
    }

    [Fact]
    public void AHebrewTitle_IsRightToLeft_AndItsPunctuationStaysAtTheEnd()
    {
        // Logical "שלום!" — the exclamation mark closes the sentence, so visually it sits on the LEFT: the last
        // logical character is the first visual one.
        var paragraph = BidiAlgorithm.Resolve("שלום!");

        Assert.Equal(1, paragraph.ParagraphLevel);
        Assert.Equal(new byte[] { 1, 1, 1, 1, 1 }, paragraph.Levels);
        Assert.Equal([4, 3, 2, 1, 0], paragraph.Order);
    }

    [Fact]
    public void ALatinLabel_WithAnArabicWordInBrackets_KeepsTheBracketsAroundTheWord()
    {
        // "Temp (درجة)" — the paragraph is Latin, the Arabic word is one right-to-left run, and the brackets
        // resolve as a pair (rule N0), so they stay on the outside of the word in display order.
        var paragraph = BidiAlgorithm.Resolve("Temp (درجة)");

        Assert.Equal(0, paragraph.ParagraphLevel);
        Assert.Equal(new byte[] { 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 0 }, paragraph.Levels);
        Assert.Equal([0, 1, 2, 3, 4, 5, 9, 8, 7, 6, 10], paragraph.Order);
    }

    [Fact]
    public void AUnit_WithADegreeSign_IsLeftToRight()
    {
        var paragraph = BidiAlgorithm.Resolve("°C");

        Assert.Equal(0, paragraph.ParagraphLevel);
        Assert.Equal(new byte[] { 0, 0 }, paragraph.Levels);
    }

    [Fact]
    public void DigitsInsideArabic_ReadLeftToRight_InsideTheRightToLeftRun()
    {
        // "درجة 12" — Arabic paragraph; the European number takes level 2 (rule I2) so its digits keep their
        // order while the run around them is reversed.
        var paragraph = BidiAlgorithm.Resolve("درجة 12");

        Assert.Equal(1, paragraph.ParagraphLevel);
        Assert.Equal(new byte[] { 1, 1, 1, 1, 1, 2, 2 }, paragraph.Levels);
        Assert.Equal([5, 6, 4, 3, 2, 1, 0], paragraph.Order);
    }

    [Fact]
    public void AnExplicitParagraphDirection_Wins()
    {
        var forcedRtl = BidiAlgorithm.Resolve("abc", BidiAlgorithm.BidiDirection.RightToLeft);
        var forcedLtr = BidiAlgorithm.Resolve("שלום", BidiAlgorithm.BidiDirection.LeftToRight);

        Assert.Equal(1, forcedRtl.ParagraphLevel);
        Assert.Equal(new byte[] { 2, 2, 2 }, forcedRtl.Levels);
        Assert.Equal(0, forcedLtr.ParagraphLevel);
        Assert.Equal(new byte[] { 1, 1, 1, 1 }, forcedLtr.Levels);
        Assert.Equal([3, 2, 1, 0], forcedLtr.Order);
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
        Assert.Equal([0, 2], paragraph.Order);
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
        Assert.Equal([3, 2, 1], paragraph.Order);
    }

    [Fact]
    public void AnIsolate_KeepsItsContentApart_AndIsNotRemoved()
    {
        // a RLI ב ג PDI b: the isolate formatting characters stay (level 0, neutral), the Hebrew inside is
        // one reversed run, and the Latin around it is untouched.
        var paragraph = BidiAlgorithm.Resolve("a\u2067בג\u2069b");

        Assert.Equal(new byte[] { 0, 0, 1, 1, 0, 0 }, paragraph.Levels);
        Assert.Equal([0, 1, 3, 2, 4, 5], paragraph.Order);
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

    // ---- a text with nothing right to left: level 0 in logical order, from one scan of its classes ----

    [Theory]
    [InlineData("12.5")]
    [InlineData("Latency (µs) — 12:00:30")]
    [InlineData("-2.5e-3 %\t[x]")]
    public void ATextWithNothingRightToLeft_IsLevelZeroInLogicalOrder(string text)
    {
        foreach (BidiAlgorithm.BidiDirection? direction in new BidiAlgorithm.BidiDirection?[] { null, BidiAlgorithm.BidiDirection.LeftToRight })
        {
            var paragraph = BidiAlgorithm.Resolve(text, direction);

            Assert.Equal(0, paragraph.ParagraphLevel);
            Assert.Equal(new byte[text.Length], paragraph.Levels);
            Assert.Equal(Enumerable.Range(0, text.Length), paragraph.Order);
        }
    }

    [Fact]
    public void ATextWithNothingRightToLeft_NamesASurrogatePairOnceInTheOrder()
    {
        var paragraph = BidiAlgorithm.Resolve("a\U0001D538b");

        Assert.Equal(new byte[4], paragraph.Levels);
        Assert.Equal([0, 1, 3], paragraph.Order);
    }

    [Fact]
    public void ATextWithNothingRightToLeft_NamesEveryLoneSurrogateInTheOrder()
    {
        // Built in code: InlineData stores strings as UTF-8, which cannot hold a lone surrogate.
        Assert.Equal([0, 1], BidiAlgorithm.Resolve(new string(['a', (char)0xD800])).Order);
        Assert.Equal([0, 1], BidiAlgorithm.Resolve(new string([(char)0xD800, 'a'])).Order);
    }

    [Fact]
    public void ATextWithNothingRightToLeft_AllocatesItsLevelsAndItsOrderOnly()
    {
        const string text = "1,234.5";
        BidiAlgorithm.Resolve(text);

        long before = GC.GetAllocatedBytesForCurrentThread();
        var paragraph = BidiAlgorithm.Resolve(text);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // byte[7] and int[7] are 32 and 56 bytes on a 64-bit runtime; the whole resolution makes some fifteen objects.
        Assert.True(allocated <= 128, $"{allocated} bytes allocated");
        Assert.Equal(7, paragraph.Levels.Length);
    }

    [Fact]
    public void AnImposedRightToLeftParagraph_RaisesALatinText_EvenWithNothingRightToLeftInIt()
    {
        var paragraph = BidiAlgorithm.Resolve("ab", BidiAlgorithm.BidiDirection.RightToLeft);

        Assert.Equal(new byte[] { 2, 2 }, paragraph.Levels);
        Assert.Equal([0, 1], paragraph.Order);
    }

    // ---- rule L2, written once for every kind of item ----

    [Fact]
    public void ReverseByLevels_OverUnits_ReversesFromTheHighestLevelDownToTheLowestOdd()
    {
        int[] items = [0, 1, 2, 3, 4, 5, 6];
        byte[] levels = [0, 1, 1, 2, 2, 1, 0];

        BidiAlgorithm.ReverseByLevels<int>(items, levels);

        Assert.Equal([0, 5, 3, 4, 2, 1, 6], items);
        Assert.Equal([0, 1, 2, 2, 1, 1, 0], levels);
    }

    private readonly record struct Piece(string Name, byte Level);

    [Fact]
    public void ReverseByLevels_OverRuns_ReversesTheRightToLeftRuns()
    {
        Piece[] runs = [new("a", 0), new("b", 1), new("c", 1), new("d", 0)];
        Span<byte> levels = [0, 1, 1, 0];

        BidiAlgorithm.ReverseByLevels<Piece>(runs, levels);

        Assert.Equal(["a", "c", "b", "d"], runs.Select(r => r.Name));
    }

    [Fact]
    public void ReverseByLevels_WithNoOddLevel_LeavesTheOrderAlone()
    {
        int[] items = [0, 1, 2];
        byte[] levels = [0, 2, 2];

        BidiAlgorithm.ReverseByLevels<int>(items, levels);

        Assert.Equal([0, 1, 2], items);
    }

    [Fact]
    public void ReverseByLevels_OfNothing_DoesNothing()
    {
        BidiAlgorithm.ReverseByLevels<int>([], []);
    }
}
