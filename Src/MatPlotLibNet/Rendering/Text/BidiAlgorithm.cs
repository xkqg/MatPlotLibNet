// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Text;

/// <summary>The Unicode Bidirectional Algorithm (UAX #9) for one paragraph: rules P2–P3, X1–X10, W1–W7, N0–N2,
/// I1–I2, L1 and L2. It answers two questions a renderer has about a label: which embedding level every character
/// resolves to, and in what order the characters are displayed. Shaping (which glyph a letter becomes in its
/// position) is not its job; that belongs to the font engine, which works on the runs this algorithm delivers.</summary>
/// <remarks>The rules are written literally, one method per rule, and each clause of the specification in a type of its own: the explicit
/// levels (X1–X8) in <see cref="Rendering.Text.BidiAlgorithm.BidiExplicitLevels"/>, the isolating run sequences (X10) in <see cref="Rendering.Text.BidiAlgorithm.BidiRunSequence"/>, which the weak
/// (W1–W7), neutral (N0–N2) and implicit (I1–I2) rules rewrite in turn (<see cref="Rendering.Text.BidiAlgorithm.BidiWeakTypes"/>, <see cref="Rendering.Text.BidiAlgorithm.BidiNeutralTypes"/>,
/// <see cref="Rendering.Text.BidiAlgorithm.ResolveImplicitLevels"/>). This class holds the paragraph's direction (P2–P3), the order of the clauses, and the order the
/// characters are shown in (L1–L2). The whole is proven against Unicode's own conformance corpora (BidiTest.txt and
/// BidiCharacterTest.txt) in the test suite. It works on code points internally (<see cref="Rendering.Text.BidiAlgorithm.BidiText"/>) and reports per UTF-16 code
/// unit, which is what every string API in the renderers holds.</remarks>
internal static partial class BidiAlgorithm
{
    /// <summary>Whether the paragraph of <paramref name="text"/> runs right to left — rules P2 and P3 on their own: the
    /// first strong character outside any isolate decides, and a paragraph with none runs left to right. This is the
    /// whole of what a writer needs to place a label, and it is answered without resolving a single level: no array
    /// is made, and a label that starts with a Latin letter is decided by its first character. It is also the one reading
    /// of P2–P3 the resolution makes, for the paragraph and for what a first strong isolate holds (rule X5c).</summary>
    /// <param name="text">One paragraph, or what an isolate holds.</param>
    /// <returns>Exactly whether <see cref="Resolve"/> reports a <see cref="BidiParagraph.ParagraphLevel"/> of 1 when no
    /// direction is imposed.</returns>
    public static bool IsRightToLeftParagraph(ReadOnlySpan<char> text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            BidiClass c = ClassAt(text, ref i);
            if (IsIsolateInitiator(c))
            {
                // Skip to the matching PDI (BD9). One that is never closed — or closed only past a paragraph separator,
                // which ends every open isolate — keeps the rest of the paragraph inside it, so nothing after it decides.
                int depth = 1;
                while (depth > 0)
                {
                    if (++i >= text.Length)
                    {
                        return false;
                    }

                    BidiClass inner = ClassAt(text, ref i);
                    if (IsIsolateInitiator(inner))
                    {
                        depth++;
                    }
                    else if (inner == BidiClass.PDI)
                    {
                        depth--;
                    }
                    else if (inner == BidiClass.B)
                    {
                        return false;
                    }
                }

                continue;
            }

            if (c == BidiClass.L)
            {
                return false;
            }

            if (c is BidiClass.R or BidiClass.AL)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The class of the code point that starts at <paramref name="i"/>, which is left on its last code unit.</summary>
    private static BidiClass ClassAt(ReadOnlySpan<char> text, ref int i) => BidiClassTable.GetClass(text.CodePointAt(ref i));

    /// <summary>Whether <paramref name="text"/>, in a paragraph read left to right, stands in the order it is written: none of its
    /// characters is right to left, an Arabic number, an explicit embedding, override or isolate, or a character rule X9 removes, so
    /// <see cref="Resolve"/> puts every one at level 0 and reorders nothing. It is the one scan <see cref="Resolve"/> starts with, and
    /// it makes no array: a renderer that only needs to know whether there is anything to reorder asks it first.</summary>
    /// <param name="text">The text, or a part of a paragraph: the answer holds for the whole when it holds for every part.</param>
    /// <returns><see langword="true"/> when nothing in the text is reordered.</returns>
    internal static bool ReadsInWrittenOrder(ReadOnlySpan<char> text) => LeftToRightCodePoints(text) is not null;


    /// <summary>Resolves the embedding levels and the visual order of <paramref name="text"/>.</summary>
    /// <param name="text">One paragraph: a label, a title, a tick. A paragraph separator inside it is given the
    /// paragraph level and ends every explicit embedding, as rule X8 says, but does not start a new paragraph.</param>
    /// <param name="paragraphDirection">The direction to impose, or null to let the first strong character decide
    /// (rules P2 and P3).</param>
    public static BidiParagraph Resolve(ReadOnlySpan<char> text, BidiDirection? paragraphDirection = null)
    {
        if (text.IsEmpty)
        {
            return new BidiParagraph([], (byte)(paragraphDirection ?? BidiDirection.LeftToRight), []);
        }

        if (paragraphDirection is not BidiDirection.RightToLeft && LeftToRightCodePoints(text) is int codePoints)
        {
            return LeftToRightInLogicalOrder(text, codePoints);
        }

        var paragraph = BidiText.Of(text, paragraphDirection);
        BidiExplicitLevels.Resolve(text, paragraph);
        foreach (var sequence in BidiRunSequence.AllOf(paragraph))
        {
            ResolveImplicitLevels(BidiNeutralTypes.ResolveNeutralTypes(BidiWeakTypes.ResolveWeakTypes(sequence), paragraph), paragraph.Levels);
        }

        ResetTrailingWhitespace(paragraph);
        return new BidiParagraph(paragraph.UnitLevels(), paragraph.ParagraphLevel, VisualOrder(paragraph));
    }

    // ---- a paragraph with nothing right to left -------------------------------------------------------------------

    /// <summary>The number of code points of <paramref name="text"/> when none of them is right to left (R, AL, AN), an explicit
    /// embedding, override or isolate, or a boundary neutral X9 removes; otherwise <see langword="null"/>. Such a paragraph,
    /// read left to right, resolves every character to level 0: its numbers follow the left-to-right start of the sequence (W7),
    /// its neutrals sit between left-to-right ends (N1, N2), and nothing is reordered. Every tick label and Latin title is one.</summary>
    private static int? LeftToRightCodePoints(ReadOnlySpan<char> text)
    {
        int count = 0;
        for (int i = 0; i < text.Length; i++, count++)
        {
            // A printable ASCII character is L, EN, ES, ET, CS, ON or WS: never one that is reordered, so its class is not looked up.
            if ((uint)(text[i] - 0x20) <= 0x7E - 0x20) continue;
            BidiClass c = ClassAt(text, ref i);
            if (c is BidiClass.R or BidiClass.AL or BidiClass.AN || IsRemovedByX9(c) || IsIsolateControl(c))
            {
                return null;
            }
        }

        return count;
    }

    /// <summary>Level 0 for every code unit and every character in logical order, each named by its first code unit.</summary>
    private static BidiParagraph LeftToRightInLogicalOrder(ReadOnlySpan<char> text, int codePoints)
    {
        var order = new int[codePoints];
        for (int i = 0, k = 0; i < text.Length; i += text.UnitsAt(i))
        {
            order[k++] = i;
        }

        return new BidiParagraph(new byte[text.Length], (byte)BidiDirection.LeftToRight, order);
    }

    // ---- L1: trailing whitespace ---------------------------------------------------------------------------------

    /// <summary>L1: a segment or paragraph separator, and the whitespace and isolate controls before one or at the end of the
    /// paragraph, take the paragraph level; so do the characters X9 removed among them.</summary>
    private static void ResetTrailingWhitespace(BidiText paragraph)
    {
        BidiClass[] classes = paragraph.Classes;
        byte[] levels = paragraph.Levels;
        for (int i = 0; i < classes.Length; i++)
        {
            if (classes[i] is BidiClass.S or BidiClass.B)
            {
                levels[i] = paragraph.ParagraphLevel;
                ResetBackwards(i - 1);
            }
        }

        ResetBackwards(classes.Length - 1);

        void ResetBackwards(int from)
        {
            for (int j = from; j >= 0 && (IsRemovedByX9(classes[j]) || classes[j] == BidiClass.WS || IsIsolateControl(classes[j])); j--)
            {
                levels[j] = paragraph.ParagraphLevel;
            }
        }
    }

    // ---- L2: visual order -----------------------------------------------------------------------------------------

    /// <summary>L2 over the characters X9 kept: the first code unit of each, in visual order.</summary>
    private static int[] VisualOrder(BidiText paragraph)
    {
        int[] kept = paragraph.Kept;
        var order = new int[kept.Length];
        var orderLevels = new byte[kept.Length];
        for (int k = 0; k < kept.Length; k++)
        {
            order[k] = paragraph.UnitStart[kept[k]];
            orderLevels[k] = paragraph.Levels[kept[k]];
        }

        ReverseByLevels<int>(order, orderLevels);
        return order;
    }

    /// <summary>Rule L2 in place, for any kind of item that carries a level: code units here, the shaper's runs in the Skia
    /// package. From the highest level down to the lowest odd one, every stretch of items at that level or above is reversed;
    /// <paramref name="levels"/> is reversed with the items, so it keeps naming each item's level.</summary>
    /// <param name="items">The items in logical order; in visual order, left to right, on return.</param>
    /// <param name="levels">The level of each item, as long as <paramref name="items"/>.</param>
    internal static void ReverseByLevels<T>(Span<T> items, Span<byte> levels)
    {
        byte highest = 0;
        byte lowestOdd = byte.MaxValue;
        foreach (byte level in levels)
        {
            highest = Math.Max(highest, level);
            if ((level & 1) == 1)
            {
                lowestOdd = Math.Min(lowestOdd, level);
            }
        }

        for (int level = highest; level >= lowestOdd; level--)
        {
            for (int k = 0; k < items.Length; k++)
            {
                if (levels[k] < level)
                {
                    continue;
                }

                int end = k;
                while (end + 1 < items.Length && levels[end + 1] >= level)
                {
                    end++;
                }

                items[k..(end + 1)].Reverse();
                levels[k..(end + 1)].Reverse();
                k = end;
            }
        }
    }

    /// <summary>The paragraph direction a caller can impose instead of letting rules P2 and P3 find the first strong
    /// character.</summary>
    internal enum BidiDirection : byte
    {
        /// <summary>The paragraph reads left to right (embedding level 0).</summary>
        LeftToRight = 0,

        /// <summary>The paragraph reads right to left (embedding level 1).</summary>
        RightToLeft = 1,
    }

    // The sets of bidi classes more than one rule of UAX #9 names, each named once.

    /// <summary>Whether rule X9 removes a character of the class: an explicit embedding or override, the PDF that closes one, or a
    /// boundary neutral.</summary>
    /// <param name="c">The class.</param>
    private static bool IsRemovedByX9(BidiClass c) => c is BidiClass.RLE or BidiClass.LRE or BidiClass.RLO or BidiClass.LRO or BidiClass.PDF or BidiClass.BN;

    /// <summary>Whether the class opens an isolate: LRI, RLI or FSI.</summary>
    /// <param name="c">The class.</param>
    private static bool IsIsolateInitiator(BidiClass c) => c is BidiClass.LRI or BidiClass.RLI or BidiClass.FSI;

    /// <summary>Whether the class is an isolate control: an initiator, or the PDI that closes one.</summary>
    /// <param name="c">The class.</param>
    private static bool IsIsolateControl(BidiClass c) => IsIsolateInitiator(c) || c == BidiClass.PDI;

    // Rules I1 and I2 of UAX #9 over one isolating run sequence: the resolved type of each character raises its level.

    /// <summary>Rule I1 for a character on an even level, I2 for one on an odd level.</summary>
    /// <param name="levels">The levels of the paragraph (<see cref="Rendering.Text.BidiAlgorithm.BidiText.Levels"/>).</param>
    /// <param name="sequence">The sequence whose levels the rules raise.</param>
    private static void ResolveImplicitLevels(BidiRunSequence sequence, byte[] levels)
    {
        for (int k = 0; k < sequence.Count; k++)
        {
            int i = sequence.Indices[k];
            levels[i] = (levels[i] & 1) == 0 ? I1OnEvenLevel(levels[i], sequence[k]) : I2OnOddLevel(levels[i], sequence[k]);
        }
    }

    /// <summary>I1: on an even level, right-to-left text goes up one level and a number two.</summary>
    private static byte I1OnEvenLevel(byte level, BidiClass type) => type switch
    {
        BidiClass.R => (byte)(level + 1),
        BidiClass.AN or BidiClass.EN => (byte)(level + 2),
        _ => level,
    };

    /// <summary>I2: on an odd level, left-to-right text and a number go up one level.</summary>
    private static byte I2OnOddLevel(byte level, BidiClass type) =>
        type is BidiClass.L or BidiClass.EN or BidiClass.AN ? (byte)(level + 1) : level;

    /// <summary>One paragraph while the bidi algorithm resolves it: what is read from its text once, before any rule changes a type, and the
    /// two arrays the rules write. Every clause of UAX #9 is handed this one value.</summary>
    /// <param name="CodePoints">The code points of the text, read by <see cref="CodePointExtensions"/>: a lone surrogate keeps its own value,
    /// which the class table reads as a left-to-right letter.</param>
    /// <param name="UnitStart">The index of the first code unit of each code point.</param>
    /// <param name="UnitCount">How many code units the text has.</param>
    /// <param name="Classes">The bidi class of each code point, as written.</param>
    /// <param name="Kept">The code points rule X9 keeps, in logical order: every one but the explicit embeddings, overrides, their PDF and
    /// the boundary neutrals.</param>
    /// <param name="MatchingPdi">For every isolate initiator the index of its matching PDI, or -1; for every matched PDI the index of its
    /// initiator, or -1 for one that matches nothing (BD9). Every other position holds -1.</param>
    /// <param name="ParagraphLevel">0 for a paragraph that runs left to right, 1 for one that runs right to left.</param>
    /// <param name="Types">The type of each code point, rewritten rule by rule from its class.</param>
    /// <param name="Levels">The embedding level of each code point, written by the explicit rules and raised by the implicit ones.</param>
    internal readonly record struct BidiText(
        int[] CodePoints, int[] UnitStart, int UnitCount, BidiClass[] Classes, int[] Kept, int[] MatchingPdi, byte ParagraphLevel,
        BidiClass[] Types, byte[] Levels)
    {
        /// <summary>How many code points the paragraph has.</summary>
        internal int Length => CodePoints.Length;

        /// <summary>Reads <paramref name="text"/>: one pass counts its code points, the next reads each with its class.</summary>
        /// <param name="text">One paragraph, not empty.</param>
        /// <param name="paragraphDirection">The direction to impose, or null to let the first strong character decide (rules P2 and P3).</param>
        /// <returns>The paragraph, every type its class and every level 0.</returns>
        internal static BidiText Of(ReadOnlySpan<char> text, BidiDirection? paragraphDirection)
        {
            int n = 0;
            for (int i = 0; i < text.Length; i += text.UnitsAt(i)) n++;

            var codePoints = new int[n];
            var unitStart = new int[n];
            var classes = new BidiClass[n];
            int kept = 0;
            for (int i = 0, k = 0; k < n; i++, k++)
            {
                unitStart[k] = i;
                codePoints[k] = text.CodePointAt(ref i);
                classes[k] = BidiClassTable.GetClass(codePoints[k]);
                if (!IsRemovedByX9(classes[k])) kept++;
            }

            var keptIndices = new int[kept];
            for (int k = 0, j = 0; j < kept; k++)
            {
                if (!IsRemovedByX9(classes[k])) keptIndices[j++] = k;
            }

            byte paragraphLevel = (byte)(paragraphDirection ?? (BidiAlgorithm.IsRightToLeftParagraph(text) ? BidiDirection.RightToLeft : BidiDirection.LeftToRight));
            return new BidiText(codePoints, unitStart, text.Length, classes, keptIndices, MatchIsolates(classes), paragraphLevel,
                (BidiClass[])classes.Clone(), new byte[n]);
        }

        /// <summary>BD9: the PDI each isolate initiator matches; a paragraph separator closes every isolate still open.</summary>
        private static int[] MatchIsolates(BidiClass[] classes)
        {
            var matching = new int[classes.Length];
            Array.Fill(matching, -1);
            var open = new Stack<int>();
            for (int i = 0; i < classes.Length; i++)
            {
                if (IsIsolateInitiator(classes[i]))
                {
                    open.Push(i);
                }
                else if (classes[i] == BidiClass.PDI && open.Count > 0)
                {
                    int initiator = open.Pop();
                    matching[initiator] = i;
                    matching[i] = initiator;
                }
                else if (classes[i] == BidiClass.B)
                {
                    open.Clear();
                }
            }

            return matching;
        }

        /// <summary>The code units of what the isolate opened at <paramref name="initiator"/> holds: from the unit after it up to its matching
        /// PDI, or to the end of the paragraph when it has none.</summary>
        internal Range IsolatedUnits(int initiator) =>
            new(StartOf(initiator + 1), StartOf(MatchingPdi[initiator] < 0 ? Length : MatchingPdi[initiator]));

        /// <summary>The level of the nearest character X9 kept before (<paramref name="side"/> -1) or after (+1) the character at
        /// <paramref name="index"/>, or the paragraph level past either end.</summary>
        internal byte KeptLevelBeside(int index, int side)
        {
            for (int i = index + side; (uint)i < (uint)Length; i += side)
            {
                if (!IsRemovedByX9(Classes[i])) return Levels[i];
            }

            return ParagraphLevel;
        }

        /// <summary>The level of every code unit: both units of a surrogate pair carry the level of their code point, and a code point rule X9
        /// removed holds <see cref="BidiParagraph.Removed"/>.</summary>
        internal byte[] UnitLevels()
        {
            var units = new byte[UnitCount];
            for (int k = 0; k < Length; k++)
            {
                units.AsSpan(UnitStart[k], StartOf(k + 1) - UnitStart[k]).Fill(IsRemovedByX9(Classes[k]) ? BidiParagraph.Removed : Levels[k]);
            }

            return units;
        }

        /// <summary>The first code unit of code point <paramref name="k"/>, or the unit count past the last one.</summary>
        private int StartOf(int k) => k < Length ? UnitStart[k] : UnitCount;
    }

    /// <summary>Rules W1 to W7 of UAX #9 over one isolating run sequence, one method per rule.</summary>
    private static class BidiWeakTypes
    {
        /// <summary>Rules W1 to W7, in order: each rule reads what the one before it left.</summary>
        /// <returns>The sequence, for the neutral rules.</returns>
        /// <param name="sequence">The sequence whose types the rules rewrite.</param>
        internal static BidiRunSequence ResolveWeakTypes(BidiRunSequence sequence)
        {
            BidiWeakTypes.W1NonspacingMarks(sequence);
            BidiWeakTypes.W2EuropeanNumbersAfterArabicLetters(sequence);
            BidiWeakTypes.W3ArabicLetters(sequence);
            BidiWeakTypes.W4SingleSeparators(sequence);
            BidiWeakTypes.W5EuropeanTerminators(sequence);
            BidiWeakTypes.W6LeftoverSeparators(sequence);
            BidiWeakTypes.W7EuropeanNumbersInLeftToRight(sequence);
            return sequence;
        }

        /// <summary>W1: a nonspacing mark takes the type of what precedes it (sos at the start); after an isolate control it is a
        /// neutral.</summary>
        /// <param name="sequence">The sequence whose types the rules rewrite.</param>
        internal static void W1NonspacingMarks(BidiRunSequence sequence)
        {
            for (int k = 0; k < sequence.Count; k++)
            {
                if (sequence[k] != BidiClass.NSM) continue;
                BidiClass previous = k == 0 ? sequence.Sos : sequence[k - 1];
                sequence[k] = IsIsolateControl(previous) ? BidiClass.ON : previous;
            }
        }

        /// <summary>W2: a European number after an Arabic letter (the first strong type before it) is an Arabic number.</summary>
        /// <param name="sequence">The sequence whose types the rules rewrite.</param>
        internal static void W2EuropeanNumbersAfterArabicLetters(BidiRunSequence sequence)
        {
            for (int k = 0; k < sequence.Count; k++)
            {
                if (sequence[k] == BidiClass.EN
                    && sequence.PrecedingStrong(k, static type => type is BidiClass.L or BidiClass.R or BidiClass.AL ? type : BidiClass.ON) == BidiClass.AL)
                {
                    sequence[k] = BidiClass.AN;
                }
            }
        }

        /// <summary>W3: every Arabic letter is now simply right-to-left.</summary>
        /// <param name="sequence">The sequence whose types the rules rewrite.</param>
        internal static void W3ArabicLetters(BidiRunSequence sequence)
        {
            for (int k = 0; k < sequence.Count; k++)
            {
                if (sequence[k] == BidiClass.AL) sequence[k] = BidiClass.R;
            }
        }

        /// <summary>W4: a single separator between two numbers of one kind joins them.</summary>
        /// <param name="sequence">The sequence whose types the rules rewrite.</param>
        internal static void W4SingleSeparators(BidiRunSequence sequence)
        {
            for (int k = 1; k < sequence.Count - 1; k++)
            {
                BidiClass before = sequence[k - 1];
                BidiClass after = sequence[k + 1];
                if (sequence[k] == BidiClass.ES && before == BidiClass.EN && after == BidiClass.EN)
                {
                    sequence[k] = BidiClass.EN;
                }
                else if (sequence[k] == BidiClass.CS && before == after && before is BidiClass.EN or BidiClass.AN)
                {
                    sequence[k] = before;
                }
            }
        }

        /// <summary>W5: a run of European terminators next to a European number becomes European numbers.</summary>
        /// <param name="sequence">The sequence whose types the rules rewrite.</param>
        internal static void W5EuropeanTerminators(BidiRunSequence sequence)
        {
            for (int k = 0; k < sequence.Count; k++)
            {
                if (sequence[k] != BidiClass.ET) continue;
                int end = sequence.RunEnd(k, static type => type == BidiClass.ET);
                if ((k > 0 && sequence[k - 1] == BidiClass.EN) || (end + 1 < sequence.Count && sequence[end + 1] == BidiClass.EN))
                {
                    sequence.Fill(k, end, BidiClass.EN);
                }

                k = end;
            }
        }

        /// <summary>W6: the separators and terminators left over are neutrals.</summary>
        /// <param name="sequence">The sequence whose types the rules rewrite.</param>
        internal static void W6LeftoverSeparators(BidiRunSequence sequence)
        {
            for (int k = 0; k < sequence.Count; k++)
            {
                if (sequence[k] is BidiClass.ES or BidiClass.ET or BidiClass.CS) sequence[k] = BidiClass.ON;
            }
        }

        /// <summary>W7: a European number in left-to-right context (the first L or R before it is L) reads as left-to-right text.</summary>
        /// <param name="sequence">The sequence whose types the rules rewrite.</param>
        internal static void W7EuropeanNumbersInLeftToRight(BidiRunSequence sequence)
        {
            for (int k = 0; k < sequence.Count; k++)
            {
                if (sequence[k] == BidiClass.EN
                    && sequence.PrecedingStrong(k, static type => type is BidiClass.L or BidiClass.R ? type : BidiClass.ON) == BidiClass.L)
                {
                    sequence[k] = BidiClass.L;
                }
            }
        }
    }
}
