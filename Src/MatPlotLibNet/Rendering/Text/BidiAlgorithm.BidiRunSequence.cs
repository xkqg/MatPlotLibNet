// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Text;

internal static partial class BidiAlgorithm
{
    /// <summary>An isolating run sequence (BD13) of a paragraph: the indices of its characters in logical order, the types of the paragraph
    /// (one array, which every sequence of it shares), the level of its runs, and the strong type before and after it (sos, eos). Rule X10 makes them (<see cref="Rendering.Text.BidiAlgorithm.BidiRunSequence.AllOf"/>); the weak, neutral and implicit rules then
    /// read and rewrite the types of each in turn (<see cref="Rendering.Text.BidiAlgorithm.BidiWeakTypes"/>, <see cref="Rendering.Text.BidiAlgorithm.BidiNeutralTypes"/>,
    /// <see cref="Rendering.Text.BidiAlgorithm.ResolveImplicitLevels"/>), through its indexer and the two scans written here once: the strong type before a position, and the
    /// end of a run of like types.</summary>
    /// <param name="Indices">The indices of its characters in the paragraph, in logical order.</param>
    /// <param name="Types">The types of the paragraph (<see cref="Rendering.Text.BidiAlgorithm.BidiText.Types"/>), which the rules rewrite.</param>
    /// <param name="Level">The embedding level of its runs.</param>
    /// <param name="Sos">The strong type before it.</param>
    /// <param name="Eos">The strong type after it.</param>
    internal readonly record struct BidiRunSequence(int[] Indices, BidiClass[] Types, byte Level, BidiClass Sos, BidiClass Eos)
    {
        /// <summary>How many characters the sequence holds.</summary>
        internal int Count => Indices.Length;

        /// <summary>The type of the character at position <paramref name="k"/> of the sequence.</summary>
        internal BidiClass this[int k]
        {
            get => Types[Indices[k]];
            set => Types[Indices[k]] = value;
        }

        /// <summary>The embedding direction of the sequence.</summary>
        internal BidiClass Embedding => DirectionOf(Level);

        /// <summary>The direction of an embedding level.</summary>
        /// <param name="level">The level.</param>
        /// <returns>R for an odd level, L for an even one.</returns>
        internal static BidiClass DirectionOf(int level) => (level & 1) == 1 ? BidiClass.R : BidiClass.L;

        /// <summary>The first type before position <paramref name="k"/>, walking back, that <paramref name="strongOf"/> counts as strong.</summary>
        /// <param name="k">The position.</param>
        /// <param name="strongOf">The strong type a type counts as, or ON for a type that is not strong.</param>
        /// <returns>That strong type, or <see cref="Rendering.Text.BidiAlgorithm.BidiRunSequence.Sos"/> when there is none.</returns>
        internal BidiClass PrecedingStrong(int k, Func<BidiClass, BidiClass> strongOf)
        {
            for (int j = k - 1; j >= 0; j--)
            {
                BidiClass strong = strongOf(this[j]);
                if (strong != BidiClass.ON) return strong;
            }

            return Sos;
        }

        /// <summary>The last position of the run of types <paramref name="inRun"/> accepts that starts at <paramref name="k"/>.</summary>
        internal int RunEnd(int k, Func<BidiClass, bool> inRun)
        {
            int end = k;
            while (end + 1 < Count && inRun(this[end + 1])) end++;
            return end;
        }

        /// <summary>Gives every position from <paramref name="from"/> to <paramref name="to"/> the type <paramref name="type"/>.</summary>
        internal void Fill(int from, int to, BidiClass type)
        {
            for (int k = from; k <= to; k++) this[k] = type;
        }

        /// <summary>X10: the level runs of the paragraph (BD7) chained through matched isolate initiators and their PDIs into isolating run
        /// sequences (BD13), each with its sos and eos.</summary>
        internal static List<BidiRunSequence> AllOf(BidiText paragraph)
        {
            var (runs, runOf) = BD7LevelRuns(paragraph);
            int[] matching = paragraph.MatchingPdi;
            var sequences = new List<BidiRunSequence>(runs.Count);
            foreach (var first in runs)
            {
                if (paragraph.Classes[first[0]] == BidiClass.PDI && matching[first[0]] >= 0) continue; // a matched PDI continues the sequence its initiator started

                // X9 never removes a PDI, so the PDI an initiator matches always stands in a level run.
                var indices = new List<int>(first);
                for (int last = first[^1]; IsIsolateInitiator(paragraph.Classes[last]) && matching[last] >= 0; last = indices[^1])
                {
                    indices.AddRange(runs[runOf[matching[last]]]);
                }

                sequences.Add(Of(paragraph, [.. indices]));
            }

            return sequences;
        }

        /// <summary>The level runs of a paragraph (BD7) and, for every character X9 kept, the index of the run it is in.</summary>
        private readonly record struct LevelRuns(List<int[]> Runs, int[] RunOf);

        /// <summary>BD7: the maximal runs of characters X9 kept that share one level.</summary>
        private static LevelRuns BD7LevelRuns(BidiText paragraph)
        {
            int[] kept = paragraph.Kept;
            var runs = new List<int[]>();
            var runOf = new int[paragraph.Length];
            int runStart = 0;
            for (int k = 1; k <= kept.Length; k++)
            {
                if (k == kept.Length || paragraph.Levels[kept[k]] != paragraph.Levels[kept[runStart]])
                {
                    foreach (int index in kept.AsSpan(runStart, k - runStart)) runOf[index] = runs.Count;
                    runs.Add(kept[runStart..k]);
                    runStart = k;
                }
            }

            return new LevelRuns(runs, runOf);
        }

        /// <summary>X10's sos and eos for the sequence of <paramref name="indices"/>: sos is the higher of its level and the level of the kept
        /// character before it in the paragraph; eos likewise with the character after, unless the sequence ends in an isolate initiator that
        /// lacks a matching PDI, and then the paragraph level.</summary>
        private static BidiRunSequence Of(BidiText paragraph, int[] indices)
        {
            int end = indices[^1];
            byte level = paragraph.Levels[indices[0]];
            byte before = paragraph.KeptLevelBeside(indices[0], -1);
            byte after = IsIsolateInitiator(paragraph.Classes[end]) && paragraph.MatchingPdi[end] < 0
                ? paragraph.ParagraphLevel
                : paragraph.KeptLevelBeside(end, 1);
            return new BidiRunSequence(indices, paragraph.Types, level, DirectionOf(Math.Max(before, level)), DirectionOf(Math.Max(after, level)));
        }
    }
}
