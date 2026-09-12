// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Text;

/// <summary>The Unicode Bidirectional Algorithm (UAX #9) for one paragraph: rules P2–P3, X1–X10, W1–W7, N0–N2,
/// I1–I2, L1 and L2. It answers two questions a renderer has about a label: which embedding level every character
/// resolves to, and in what order the characters are displayed. Shaping (which glyph a letter becomes in its
/// position) is not its job; that belongs to the font engine, which works on the runs this algorithm delivers.</summary>
/// <remarks>The implementation follows the rule text literally, one method per rule group, and is proven against
/// Unicode's own conformance corpora (BidiTest.txt and BidiCharacterTest.txt) in the test suite. It works on code
/// points internally and reports per UTF-16 code unit, which is what every string API in the renderers holds.</remarks>
internal static class BidiAlgorithm
{
    /// <summary>max_depth of UAX #9: the deepest explicit embedding level.</summary>
    private const int MaxDepth = 125;

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

        var input = Decode(text);
        int n = input.CodePoints.Length;
        var originalClasses = new BidiClass[n];
        for (int i = 0; i < n; i++)
        {
            originalClasses[i] = BidiClassTable.GetClass(input.CodePoints[i]);
        }

        int[] matchingPdi = MatchIsolates(originalClasses);
        byte paragraphLevel = paragraphDirection is { } direction
            ? (byte)direction
            : FirstStrongLevel(originalClasses, 0, n, matchingPdi);

        var types = (BidiClass[])originalClasses.Clone();
        var levels = new byte[n];
        ResolveExplicitLevels(originalClasses, types, levels, matchingPdi, paragraphLevel);

        var removed = new bool[n];
        for (int i = 0; i < n; i++)
        {
            removed[i] = IsRemovedByX9(originalClasses[i]);
        }

        foreach (var sequence in IsolatingRunSequences(levels, removed, originalClasses, matchingPdi, paragraphLevel))
        {
            ResolveWeakTypes(sequence, types);
            ResolveBracketPairs(sequence, types, originalClasses, input.CodePoints);
            ResolveNeutralTypes(sequence, types);
            ResolveImplicitLevels(sequence, types, levels);
        }

        ResetTrailingWhitespace(originalClasses, removed, levels, paragraphLevel);

        int[] order = VisualOrder(levels, removed, input.UnitStart);
        byte[] unitLevels = ExpandToUnits(levels, removed, input, text.Length);
        return new BidiParagraph(unitLevels, paragraphLevel, order);
    }

    // ---- input ------------------------------------------------------------------------------------------------

    private readonly record struct Input(int[] CodePoints, int[] UnitStart, int[] UnitLength);

    /// <summary>UTF-16 units to code points. A well-formed surrogate pair is one code point of two units; a lone
    /// surrogate is passed through as its own value, which the class table reads as a left-to-right letter.</summary>
    private static Input Decode(ReadOnlySpan<char> text)
    {
        var codePoints = new List<int>(text.Length);
        var unitStart = new List<int>(text.Length);
        var unitLength = new List<int>(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                codePoints.Add(char.ConvertToUtf32(c, text[i + 1]));
                unitStart.Add(i);
                unitLength.Add(2);
                i++;
                continue;
            }

            codePoints.Add(c);
            unitStart.Add(i);
            unitLength.Add(1);
        }

        return new Input([.. codePoints], [.. unitStart], [.. unitLength]);
    }

    private static bool IsRemovedByX9(BidiClass c) =>
        c is BidiClass.RLE or BidiClass.LRE or BidiClass.RLO or BidiClass.LRO or BidiClass.PDF or BidiClass.BN;

    private static bool IsIsolateInitiator(BidiClass c) => c is BidiClass.LRI or BidiClass.RLI or BidiClass.FSI;

    private static bool IsStrong(BidiClass c) => c is BidiClass.L or BidiClass.R or BidiClass.AL;

    // ---- BD9: matching PDIs, P2–P3: paragraph level ---------------------------------------------------------------

    /// <summary>For every isolate initiator the index of its matching PDI, or -1; for every matched PDI the index of
    /// its initiator, or -1 for one that matches nothing. Every other position holds -1.</summary>
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

    /// <summary>Rules P2 and P3 over [start, end): the first strong character outside any isolate decides.</summary>
    private static byte FirstStrongLevel(BidiClass[] classes, int start, int end, int[] matchingPdi)
    {
        for (int i = start; i < end; i++)
        {
            BidiClass c = classes[i];
            if (IsIsolateInitiator(c))
            {
                // Skip to the matching PDI; without one, the rest of the paragraph is inside the isolate.
                if (matchingPdi[i] < 0)
                {
                    return 0;
                }

                i = matchingPdi[i];
                continue;
            }

            if (c == BidiClass.L)
            {
                return 0;
            }

            if (c is BidiClass.R or BidiClass.AL)
            {
                return 1;
            }
        }

        return 0;
    }

    // ---- X1–X8: explicit levels -----------------------------------------------------------------------------------

    private readonly record struct StatusEntry(byte Level, BidiClass Override, bool Isolate);

    private static void ResolveExplicitLevels(BidiClass[] classes, BidiClass[] types, byte[] levels, int[] matchingPdi, byte paragraphLevel)
    {
        var stack = new Stack<StatusEntry>(MaxDepth + 2);
        stack.Push(new StatusEntry(paragraphLevel, BidiClass.ON, false));
        int overflowIsolates = 0;
        int overflowEmbeddings = 0;
        int validIsolates = 0;

        for (int i = 0; i < classes.Length; i++)
        {
            StatusEntry top = stack.Peek();
            switch (classes[i])
            {
                case BidiClass.RLE:
                case BidiClass.LRE:
                case BidiClass.RLO:
                case BidiClass.LRO:
                {
                    // X2–X5: an embedding or override raises the level when it is valid; otherwise it overflows.
                    bool rtl = classes[i] is BidiClass.RLE or BidiClass.RLO;
                    byte newLevel = rtl ? LeastOddAbove(top.Level) : LeastEvenAbove(top.Level);
                    levels[i] = top.Level;
                    if (newLevel <= MaxDepth && overflowIsolates == 0 && overflowEmbeddings == 0)
                    {
                        BidiClass overrideStatus = classes[i] switch
                        {
                            BidiClass.RLO => BidiClass.R,
                            BidiClass.LRO => BidiClass.L,
                            _ => BidiClass.ON,
                        };
                        stack.Push(new StatusEntry(newLevel, overrideStatus, false));
                    }
                    else if (overflowIsolates == 0)
                    {
                        overflowEmbeddings++;
                    }

                    break;
                }

                case BidiClass.RLI:
                case BidiClass.LRI:
                case BidiClass.FSI:
                {
                    // X5a–X5c: the initiator itself sits at the outer level and follows the outer override.
                    levels[i] = top.Level;
                    if (top.Override != BidiClass.ON)
                    {
                        types[i] = top.Override;
                    }

                    bool rtl = classes[i] == BidiClass.RLI
                        || (classes[i] == BidiClass.FSI && FirstStrongLevel(classes, i + 1, matchingPdi[i] < 0 ? classes.Length : matchingPdi[i], matchingPdi) == 1);
                    byte newLevel = rtl ? LeastOddAbove(top.Level) : LeastEvenAbove(top.Level);
                    if (newLevel <= MaxDepth && overflowIsolates == 0 && overflowEmbeddings == 0)
                    {
                        validIsolates++;
                        stack.Push(new StatusEntry(newLevel, BidiClass.ON, true));
                    }
                    else
                    {
                        overflowIsolates++;
                    }

                    break;
                }

                case BidiClass.PDI:
                {
                    // X6a: a PDI closes the isolate it matches and every embedding still open inside it.
                    if (overflowIsolates > 0)
                    {
                        overflowIsolates--;
                    }
                    else if (validIsolates > 0)
                    {
                        overflowEmbeddings = 0;
                        while (!stack.Peek().Isolate)
                        {
                            stack.Pop();
                        }

                        stack.Pop();
                        validIsolates--;
                    }

                    top = stack.Peek();
                    levels[i] = top.Level;
                    if (top.Override != BidiClass.ON)
                    {
                        types[i] = top.Override;
                    }

                    break;
                }

                case BidiClass.PDF:
                {
                    // X7: a PDF closes the embedding it matches, if any.
                    if (overflowIsolates > 0)
                    {
                        // inside an overflow isolate: nothing to close
                    }
                    else if (overflowEmbeddings > 0)
                    {
                        overflowEmbeddings--;
                    }
                    else if (!top.Isolate && stack.Count >= 2)
                    {
                        stack.Pop();
                    }

                    levels[i] = stack.Peek().Level;
                    break;
                }

                case BidiClass.B:
                {
                    // X8: a paragraph separator terminates everything and takes the paragraph level.
                    levels[i] = paragraphLevel;
                    stack.Clear();
                    stack.Push(new StatusEntry(paragraphLevel, BidiClass.ON, false));
                    overflowIsolates = 0;
                    overflowEmbeddings = 0;
                    validIsolates = 0;
                    break;
                }

                case BidiClass.BN:
                {
                    // Removed by X9; the level only matters for RenderLevels, which re-derives it.
                    levels[i] = top.Level;
                    break;
                }

                default:
                {
                    // X6: everything else takes the current level and the current override, if any.
                    levels[i] = top.Level;
                    if (top.Override != BidiClass.ON)
                    {
                        types[i] = top.Override;
                    }

                    break;
                }
            }
        }
    }

    private static byte LeastOddAbove(byte level) => (byte)((level & 1) == 0 ? level + 1 : level + 2);

    private static byte LeastEvenAbove(byte level) => (byte)((level & 1) == 0 ? level + 2 : level + 1);

    // ---- X10: isolating run sequences -----------------------------------------------------------------------------

    /// <summary>An isolating run sequence: the indices of its characters in logical order, and the strong type on
    /// either side of it (sos, eos).</summary>
    private readonly record struct RunSequence(int[] Indices, BidiClass Sos, BidiClass Eos, byte Level);

    private static List<RunSequence> IsolatingRunSequences(byte[] levels, bool[] removed, BidiClass[] classes, int[] matchingPdi, byte paragraphLevel)
    {
        // Level runs over the characters X9 kept (BD7).
        var kept = new List<int>(levels.Length);
        for (int i = 0; i < levels.Length; i++)
        {
            if (!removed[i])
            {
                kept.Add(i);
            }
        }

        var runs = new List<int[]>();
        var runOf = new int[levels.Length];
        Array.Fill(runOf, -1);
        int runStart = 0;
        for (int k = 1; k <= kept.Count; k++)
        {
            if (k == kept.Count || levels[kept[k]] != levels[kept[runStart]])
            {
                var run = kept.GetRange(runStart, k - runStart).ToArray();
                foreach (int index in run)
                {
                    runOf[index] = runs.Count;
                }

                runs.Add(run);
                runStart = k;
            }
        }

        // Chain level runs through matched isolate initiators and their PDIs (BD13).
        var sequences = new List<RunSequence>();
        foreach (var first in runs)
        {
            int firstIndex = first[0];
            if (classes[firstIndex] == BidiClass.PDI && matchingPdi[firstIndex] >= 0)
            {
                continue; // a matched PDI continues the sequence its initiator started
            }

            var indices = new List<int>(first);
            int[] current = first;
            while (true)
            {
                int last = current[^1];
                if (!IsIsolateInitiator(classes[last]) || matchingPdi[last] < 0 || runOf[matchingPdi[last]] < 0)
                {
                    break;
                }

                current = runs[runOf[matchingPdi[last]]];
                indices.AddRange(current);
            }

            int start = indices[0];
            int end = indices[^1];
            byte level = levels[start];

            // sos: the higher of this level and the level of the kept character before the sequence in the
            // paragraph; eos likewise with the character after, unless the sequence ends in an isolate
            // initiator that lacks a matching PDI — then the paragraph level.
            byte before = paragraphLevel;
            for (int i = start - 1; i >= 0; i--)
            {
                if (!removed[i])
                {
                    before = levels[i];
                    break;
                }
            }

            byte after = paragraphLevel;
            if (!IsIsolateInitiator(classes[end]) || matchingPdi[end] >= 0)
            {
                for (int i = end + 1; i < levels.Length; i++)
                {
                    if (!removed[i])
                    {
                        after = levels[i];
                        break;
                    }
                }
            }

            BidiClass sos = (Math.Max(before, level) & 1) == 1 ? BidiClass.R : BidiClass.L;
            BidiClass eos = (Math.Max(after, level) & 1) == 1 ? BidiClass.R : BidiClass.L;
            sequences.Add(new RunSequence([.. indices], sos, eos, level));
        }

        return sequences;
    }

    // ---- W1–W7: weak types ----------------------------------------------------------------------------------------

    private static void ResolveWeakTypes(RunSequence sequence, BidiClass[] types)
    {
        int[] idx = sequence.Indices;
        int n = idx.Length;

        // W1: a nonspacing mark takes the type of what precedes it; after an isolate control it is a neutral.
        for (int k = 0; k < n; k++)
        {
            if (types[idx[k]] != BidiClass.NSM)
            {
                continue;
            }

            if (k == 0)
            {
                types[idx[k]] = sequence.Sos;
            }
            else
            {
                BidiClass previous = types[idx[k - 1]];
                types[idx[k]] = IsIsolateInitiator(previous) || previous == BidiClass.PDI ? BidiClass.ON : previous;
            }
        }

        // W2: a European number after an Arabic letter is an Arabic number.
        for (int k = 0; k < n; k++)
        {
            if (types[idx[k]] != BidiClass.EN)
            {
                continue;
            }

            for (int j = k - 1; j >= -1; j--)
            {
                BidiClass strong = j < 0 ? sequence.Sos : types[idx[j]];
                if (IsStrong(strong))
                {
                    if (strong == BidiClass.AL)
                    {
                        types[idx[k]] = BidiClass.AN;
                    }

                    break;
                }
            }
        }

        // W3: every Arabic letter is now simply right-to-left.
        for (int k = 0; k < n; k++)
        {
            if (types[idx[k]] == BidiClass.AL)
            {
                types[idx[k]] = BidiClass.R;
            }
        }

        // W4: a single separator between two numbers of one kind joins them.
        for (int k = 1; k < n - 1; k++)
        {
            BidiClass before = types[idx[k - 1]];
            BidiClass after = types[idx[k + 1]];
            if (types[idx[k]] == BidiClass.ES && before == BidiClass.EN && after == BidiClass.EN)
            {
                types[idx[k]] = BidiClass.EN;
            }
            else if (types[idx[k]] == BidiClass.CS && before == after && before is BidiClass.EN or BidiClass.AN)
            {
                types[idx[k]] = before;
            }
        }

        // W5: European terminators next to European numbers become European numbers.
        for (int k = 0; k < n; k++)
        {
            if (types[idx[k]] != BidiClass.ET)
            {
                continue;
            }

            int runEnd = k;
            while (runEnd + 1 < n && types[idx[runEnd + 1]] == BidiClass.ET)
            {
                runEnd++;
            }

            bool nextToNumber = (k > 0 && types[idx[k - 1]] == BidiClass.EN) || (runEnd + 1 < n && types[idx[runEnd + 1]] == BidiClass.EN);
            if (nextToNumber)
            {
                for (int j = k; j <= runEnd; j++)
                {
                    types[idx[j]] = BidiClass.EN;
                }
            }

            k = runEnd;
        }

        // W6: the separators and terminators left over are neutrals.
        for (int k = 0; k < n; k++)
        {
            if (types[idx[k]] is BidiClass.ES or BidiClass.ET or BidiClass.CS)
            {
                types[idx[k]] = BidiClass.ON;
            }
        }

        // W7: a European number in left-to-right context reads as left-to-right text.
        for (int k = 0; k < n; k++)
        {
            if (types[idx[k]] != BidiClass.EN)
            {
                continue;
            }

            for (int j = k - 1; j >= -1; j--)
            {
                BidiClass strong = j < 0 ? sequence.Sos : types[idx[j]];
                if (strong is BidiClass.L or BidiClass.R)
                {
                    if (strong == BidiClass.L)
                    {
                        types[idx[k]] = BidiClass.L;
                    }

                    break;
                }
            }
        }
    }

    // ---- N0: bracket pairs ----------------------------------------------------------------------------------------

    private const int BracketStackLimit = 63;

    private readonly record struct BracketPair(int Open, int Close);

    private static void ResolveBracketPairs(RunSequence sequence, BidiClass[] types, BidiClass[] originalClasses, int[] codePoints)
    {
        int[] idx = sequence.Indices;
        List<BracketPair> pairs = FindBracketPairs(idx, types, codePoints);
        if (pairs.Count == 0)
        {
            return;
        }

        BidiClass embedding = (sequence.Level & 1) == 1 ? BidiClass.R : BidiClass.L;
        foreach (var pair in pairs)
        {
            bool foundEmbedding = false;
            bool foundOpposite = false;
            for (int k = pair.Open + 1; k < pair.Close; k++)
            {
                BidiClass strong = StrongForBrackets(types[idx[k]]);
                if (strong == embedding)
                {
                    foundEmbedding = true;
                    break;
                }

                if (strong != BidiClass.ON)
                {
                    foundOpposite = true;
                }
            }

            BidiClass resolved;
            if (foundEmbedding)
            {
                resolved = embedding;
            }
            else if (foundOpposite)
            {
                // Strong text opposite the embedding direction: the context before the bracket decides.
                BidiClass context = sequence.Sos;
                for (int k = pair.Open - 1; k >= 0; k--)
                {
                    BidiClass strong = StrongForBrackets(types[idx[k]]);
                    if (strong != BidiClass.ON)
                    {
                        context = strong;
                        break;
                    }
                }

                resolved = context == embedding ? embedding : context;
            }
            else
            {
                continue;
            }

            SetBracket(idx, types, originalClasses, pair.Open, resolved);
            SetBracket(idx, types, originalClasses, pair.Close, resolved);
        }
    }

    /// <summary>EN and AN count as R when brackets are resolved; L and R are themselves; anything else is not
    /// strong.</summary>
    private static BidiClass StrongForBrackets(BidiClass type) => type switch
    {
        BidiClass.L => BidiClass.L,
        BidiClass.R or BidiClass.EN or BidiClass.AN => BidiClass.R,
        _ => BidiClass.ON,
    };

    private static void SetBracket(int[] idx, BidiClass[] types, BidiClass[] originalClasses, int position, BidiClass resolved)
    {
        types[idx[position]] = resolved;
        // Nonspacing marks that followed the bracket in the original text follow it in direction too.
        for (int k = position + 1; k < idx.Length && originalClasses[idx[k]] == BidiClass.NSM; k++)
        {
            types[idx[k]] = resolved;
        }
    }

    /// <summary>BD16: the bracket pairs of a sequence, sorted by the position of the opening bracket.</summary>
    private static List<BracketPair> FindBracketPairs(int[] idx, BidiClass[] types, int[] codePoints)
    {
        var pairs = new List<BracketPair>();
        var openers = new List<(int Bracket, int Position)>(BracketStackLimit);
        for (int k = 0; k < idx.Length; k++)
        {
            if (types[idx[k]] != BidiClass.ON || !BidiBrackets.TryGetPair(codePoints[idx[k]], out int pair, out bool opens))
            {
                continue;
            }

            if (opens)
            {
                if (openers.Count == BracketStackLimit)
                {
                    return [];
                }

                openers.Add((BidiBrackets.Canonical(pair), k));
                continue;
            }

            int closing = BidiBrackets.Canonical(codePoints[idx[k]]);
            for (int depth = openers.Count - 1; depth >= 0; depth--)
            {
                if (openers[depth].Bracket == closing)
                {
                    pairs.Add(new BracketPair(openers[depth].Position, k));
                    openers.RemoveRange(depth, openers.Count - depth);
                    break;
                }
            }
        }

        pairs.Sort((a, b) => a.Open.CompareTo(b.Open));
        return pairs;
    }

    // ---- N1–N2: neutrals ------------------------------------------------------------------------------------------

    private static bool IsNeutralOrIsolate(BidiClass type) =>
        type is BidiClass.B or BidiClass.S or BidiClass.WS or BidiClass.ON
            or BidiClass.LRI or BidiClass.RLI or BidiClass.FSI or BidiClass.PDI;

    private static void ResolveNeutralTypes(RunSequence sequence, BidiClass[] types)
    {
        int[] idx = sequence.Indices;
        int n = idx.Length;
        BidiClass embedding = (sequence.Level & 1) == 1 ? BidiClass.R : BidiClass.L;

        for (int k = 0; k < n; k++)
        {
            if (!IsNeutralOrIsolate(types[idx[k]]))
            {
                continue;
            }

            int runEnd = k;
            while (runEnd + 1 < n && IsNeutralOrIsolate(types[idx[runEnd + 1]]))
            {
                runEnd++;
            }

            BidiClass leading = k == 0 ? sequence.Sos : AsNeutralContext(types[idx[k - 1]]);
            BidiClass trailing = runEnd == n - 1 ? sequence.Eos : AsNeutralContext(types[idx[runEnd + 1]]);
            BidiClass resolved = leading == trailing ? leading : embedding;
            for (int j = k; j <= runEnd; j++)
            {
                types[idx[j]] = resolved;
            }

            k = runEnd;
        }
    }

    /// <summary>Numbers act as right-to-left text on the neutrals beside them (N1).</summary>
    private static BidiClass AsNeutralContext(BidiClass type) =>
        type is BidiClass.EN or BidiClass.AN ? BidiClass.R : type;

    // ---- I1–I2: implicit levels ----------------------------------------------------------------------------------

    private static void ResolveImplicitLevels(RunSequence sequence, BidiClass[] types, byte[] levels)
    {
        foreach (int i in sequence.Indices)
        {
            BidiClass type = types[i];
            if ((levels[i] & 1) == 0)
            {
                if (type == BidiClass.R)
                {
                    levels[i]++;
                }
                else if (type is BidiClass.AN or BidiClass.EN)
                {
                    levels[i] += 2;
                }
            }
            else if (type is BidiClass.L or BidiClass.EN or BidiClass.AN)
            {
                levels[i]++;
            }
        }
    }

    // ---- L1: trailing whitespace ---------------------------------------------------------------------------------

    private static bool IsWhitespaceOrIsolateControl(BidiClass c) =>
        c is BidiClass.WS or BidiClass.LRI or BidiClass.RLI or BidiClass.FSI or BidiClass.PDI;

    private static void ResetTrailingWhitespace(BidiClass[] classes, bool[] removed, byte[] levels, byte paragraphLevel)
    {
        int n = classes.Length;
        for (int i = 0; i < n; i++)
        {
            if (classes[i] is BidiClass.S or BidiClass.B)
            {
                levels[i] = paragraphLevel;
                ResetBackwards(i - 1);
            }
        }

        ResetBackwards(n - 1);

        void ResetBackwards(int from)
        {
            for (int j = from; j >= 0 && (removed[j] || IsWhitespaceOrIsolateControl(classes[j])); j--)
            {
                levels[j] = paragraphLevel;
            }
        }
    }

    // ---- L2: visual order -----------------------------------------------------------------------------------------

    private static int[] VisualOrder(byte[] levels, bool[] removed, int[] unitStart)
    {
        var order = new List<int>(levels.Length);
        var orderLevels = new List<byte>(levels.Length);
        for (int i = 0; i < levels.Length; i++)
        {
            if (!removed[i])
            {
                order.Add(unitStart[i]);
                orderLevels.Add(levels[i]);
            }
        }

        if (order.Count == 0)
        {
            return [];
        }

        byte highest = 0;
        byte lowestOdd = byte.MaxValue;
        foreach (byte level in orderLevels)
        {
            highest = Math.Max(highest, level);
            if ((level & 1) == 1)
            {
                lowestOdd = Math.Min(lowestOdd, level);
            }
        }

        int[] result = [.. order];
        byte[] resultLevels = [.. orderLevels];
        for (int level = highest; level >= lowestOdd && lowestOdd != byte.MaxValue; level--)
        {
            for (int k = 0; k < result.Length; k++)
            {
                if (resultLevels[k] < level)
                {
                    continue;
                }

                int runEnd = k;
                while (runEnd + 1 < result.Length && resultLevels[runEnd + 1] >= level)
                {
                    runEnd++;
                }

                Array.Reverse(result, k, runEnd - k + 1);
                Array.Reverse(resultLevels, k, runEnd - k + 1);
                k = runEnd;
            }
        }

        return result;
    }

    // ---- output ---------------------------------------------------------------------------------------------------

    private static byte[] ExpandToUnits(byte[] levels, bool[] removed, Input input, int unitCount)
    {
        var units = new byte[unitCount];
        for (int i = 0; i < levels.Length; i++)
        {
            byte level = removed[i] ? BidiParagraph.Removed : levels[i];
            for (int u = 0; u < input.UnitLength[i]; u++)
            {
                units[input.UnitStart[i] + u] = level;
            }
        }

        return units;
    }
}
