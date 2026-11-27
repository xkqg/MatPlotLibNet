// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Text;

internal static partial class BidiAlgorithm
{
    /// <summary>Rules N0 to N2 of UAX #9 over one isolating run sequence: the bracket pairs first (N0), then every run of neutrals left
    /// (N1, N2).</summary>
    private static class BidiNeutralTypes
    {
        private const int BracketStackLimit = 63;

        internal readonly record struct BracketPair(int Open, int Close);

        /// <summary>Rule N0, then N1 and N2.</summary>
        /// <param name="paragraph">The paragraph of the sequence, whose code points and classes as written N0 reads.</param>
        /// <returns>The sequence, for the implicit rules.</returns>
        /// <param name="sequence">The sequence whose types the rules rewrite.</param>
        internal static BidiRunSequence ResolveNeutralTypes(BidiRunSequence sequence, in BidiText paragraph)
        {
            BidiNeutralTypes.N0BracketPairs(sequence, paragraph);
            BidiNeutralTypes.N1N2Neutrals(sequence);
            return sequence;
        }

        /// <summary>N0: each bracket pair of the sequence (BD16) takes the embedding direction when strong text of that direction is
        /// inside it, the direction of the context before it when only the opposite direction is, and stays neutral when nothing strong
        /// is inside.</summary>
        internal static void N0BracketPairs(BidiRunSequence sequence, in BidiText paragraph)
        {
            BidiClass embedding = sequence.Embedding;
            foreach (var pair in BidiNeutralTypes.BracketPairs(sequence, paragraph.CodePoints))
            {
                bool foundEmbedding = false;
                bool foundOpposite = false;
                for (int k = pair.Open + 1; k < pair.Close && !foundEmbedding; k++)
                {
                    BidiClass strong = StrongForBrackets(sequence[k]);
                    foundEmbedding = strong == embedding;
                    foundOpposite |= strong != BidiClass.ON;
                }

                if (!foundOpposite) continue;

                // Only strong text opposite the embedding direction: the context before the bracket decides.
                BidiClass resolved = foundEmbedding ? embedding : sequence.PrecedingStrong(pair.Open, StrongForBrackets);
                BidiNeutralTypes.SetBracket(sequence, pair.Open, resolved, paragraph.Classes);
                BidiNeutralTypes.SetBracket(sequence, pair.Close, resolved, paragraph.Classes);
            }
        }

        /// <summary>Gives the bracket at <paramref name="position"/> its direction, and the nonspacing marks that followed it in the
        /// original text (<paramref name="classes"/>, as written) with it.</summary>
        internal static void SetBracket(BidiRunSequence sequence, int position, BidiClass resolved, BidiClass[] classes)
        {
            sequence[position] = resolved;
            for (int k = position + 1; k < sequence.Count && classes[sequence.Indices[k]] == BidiClass.NSM; k++)
            {
                sequence[k] = resolved;
            }
        }

        /// <summary>BD16: the bracket pairs of the sequence, sorted by the position of the opening bracket, read from the code points of
        /// its paragraph.</summary>
        internal static List<BracketPair> BracketPairs(BidiRunSequence sequence, int[] codePoints)
        {
            var pairs = new List<BracketPair>();
            // The opening brackets not yet closed: the canonical form of each and where it stands.
            var openBrackets = new List<int>(BracketStackLimit);
            var openPositions = new List<int>(BracketStackLimit);
            for (int k = 0; k < sequence.Count; k++)
            {
                int codePoint = codePoints[sequence.Indices[k]];
                if (sequence[k] != BidiClass.ON || !BidiBrackets.TryGetPair(codePoint, out int pair, out bool opens)) continue;

                if (opens)
                {
                    if (openBrackets.Count == BracketStackLimit) return [];
                    openBrackets.Add(BidiBrackets.Canonical(pair));
                    openPositions.Add(k);
                    continue;
                }

                int closing = BidiBrackets.Canonical(codePoint);
                for (int depth = openBrackets.Count - 1; depth >= 0; depth--)
                {
                    if (openBrackets[depth] == closing)
                    {
                        pairs.Add(new BracketPair(openPositions[depth], k));
                        openBrackets.RemoveRange(depth, openBrackets.Count - depth);
                        openPositions.RemoveRange(depth, openPositions.Count - depth);
                        break;
                    }
                }
            }

            pairs.Sort((a, b) => a.Open.CompareTo(b.Open));
            return pairs;
        }

        /// <summary>N1 and N2 over every run of neutrals: the run takes the direction of the text on both its sides when the two agree
        /// (N1), the embedding direction when they do not (N2). The two rules are the two outcomes of one comparison.</summary>
        /// <param name="sequence">The sequence whose types the rules rewrite.</param>
        internal static void N1N2Neutrals(BidiRunSequence sequence)
        {
            for (int k = 0; k < sequence.Count; k++)
            {
                if (!IsNeutralOrIsolate(sequence[k])) continue;
                int end = sequence.RunEnd(k, IsNeutralOrIsolate);
                BidiClass leading = k == 0 ? sequence.Sos : AsNeutralContext(sequence[k - 1]);
                BidiClass trailing = end == sequence.Count - 1 ? sequence.Eos : AsNeutralContext(sequence[end + 1]);
                sequence.Fill(k, end, leading == trailing ? leading : sequence.Embedding);
                k = end;
            }
        }

        /// <summary>EN and AN count as R when brackets are resolved; L and R are themselves; anything else is not strong.</summary>
        private static BidiClass StrongForBrackets(BidiClass type) => type switch
        {
            BidiClass.L => BidiClass.L,
            BidiClass.R or BidiClass.EN or BidiClass.AN => BidiClass.R,
            _ => BidiClass.ON,
        };

        private static bool IsNeutralOrIsolate(BidiClass type) =>
            type is BidiClass.B or BidiClass.S or BidiClass.WS or BidiClass.ON || IsIsolateControl(type);

        /// <summary>Numbers act as right-to-left text on the neutrals beside them (N1).</summary>
        private static BidiClass AsNeutralContext(BidiClass type) => type is BidiClass.EN or BidiClass.AN ? BidiClass.R : type;
    }
}
