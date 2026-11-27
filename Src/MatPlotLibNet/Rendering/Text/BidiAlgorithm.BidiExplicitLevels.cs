// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Text;

internal static partial class BidiAlgorithm
{
    /// <summary>Rules X1 to X8 of UAX #9: the directional status stack and its three counters, over the levels and types of the paragraph the
    /// rules write, one method per rule. A stack value, so resolving the explicit levels allocates nothing beyond the stack.</summary>
    private ref struct BidiExplicitLevels
    {
        /// <summary>max_depth of UAX #9: the deepest explicit embedding level.</summary>
        private const int MaxDepth = 125;

        private readonly record struct StatusEntry(byte Level, BidiClass Override, bool Isolate);

        private readonly ReadOnlySpan<char> _text;
        private readonly BidiText _paragraph;
        private readonly Stack<StatusEntry> _stack = new(MaxDepth + 2);
        private int _overflowIsolates;
        private int _overflowEmbeddings;
        private int _validIsolates;

        private BidiExplicitLevels(ReadOnlySpan<char> text, BidiText paragraph)
        {
            _text = text;
            _paragraph = paragraph;
            X1Start();
        }

        /// <summary>Rule X1, then one of X2–X8 for every character, in order: the class of the character chooses the rule.</summary>
        /// <param name="text">The text of the paragraph, which an FSI reads its direction from.</param>
        /// <param name="paragraph">The paragraph, whose levels and types the rules write.</param>
        internal static void Resolve(ReadOnlySpan<char> text, BidiText paragraph)
        {
            var status = new BidiExplicitLevels(text, paragraph);
            for (int i = 0; i < paragraph.Length; i++)
            {
                switch (paragraph.Classes[i])
                {
                    case BidiClass.RLE or BidiClass.LRE or BidiClass.RLO or BidiClass.LRO:
                        status.X2ToX5Embedding(i);
                        break;
                    case var c when IsIsolateInitiator(c):
                        status.X5aToX5cIsolate(i);
                        break;
                    case BidiClass.PDI:
                        status.X6aPopIsolate(i);
                        break;
                    case BidiClass.PDF:
                        status.X7PopEmbedding(i);
                        break;
                    case BidiClass.B:
                        status.X8EndOfParagraph(i);
                        break;
                    case BidiClass.BN:
                        status.X9Removed(i);
                        break;
                    default:
                        status.X6Other(i);
                        break;
                }
            }
        }

        /// <summary>X1: the stack holds the paragraph level, no override, no isolate; every counter is zero.</summary>
        private void X1Start()
        {
            _stack.Clear();
            _stack.Push(new StatusEntry(_paragraph.ParagraphLevel, BidiClass.ON, false));
            _overflowIsolates = 0;
            _overflowEmbeddings = 0;
            _validIsolates = 0;
        }

        /// <summary>X2–X5: an embedding or override raises the level when it is valid; otherwise it overflows.</summary>
        private void X2ToX5Embedding(int i)
        {
            BidiClass c = _paragraph.Classes[i];
            StatusEntry top = _stack.Peek();
            byte newLevel = LevelAbove(top.Level, c is BidiClass.RLE or BidiClass.RLO);
            _paragraph.Levels[i] = top.Level;
            if (Opens(newLevel))
            {
                BidiClass overrideStatus = c switch
                {
                    BidiClass.RLO => BidiClass.R,
                    BidiClass.LRO => BidiClass.L,
                    _ => BidiClass.ON,
                };
                _stack.Push(new StatusEntry(newLevel, overrideStatus, false));
            }
            else if (_overflowIsolates == 0)
            {
                _overflowEmbeddings++;
            }
        }

        /// <summary>X5a–X5c: the initiator itself sits at the outer level and follows the outer override; an FSI is an RLI when what it
        /// isolates runs right to left (P2–P3 over that span).</summary>
        private void X5aToX5cIsolate(int i)
        {
            TakeLevelAndOverride(i);
            BidiClass c = _paragraph.Classes[i];
            byte newLevel = LevelAbove(_stack.Peek().Level, c == BidiClass.RLI
                || (c == BidiClass.FSI && BidiAlgorithm.IsRightToLeftParagraph(_text[_paragraph.IsolatedUnits(i)])));
            if (Opens(newLevel))
            {
                _validIsolates++;
                _stack.Push(new StatusEntry(newLevel, BidiClass.ON, true));
            }
            else
            {
                _overflowIsolates++;
            }
        }

        /// <summary>X6a: a PDI closes the isolate it matches and every embedding still open inside it.</summary>
        private void X6aPopIsolate(int i)
        {
            if (_overflowIsolates > 0)
            {
                _overflowIsolates--;
            }
            else if (_validIsolates > 0)
            {
                _overflowEmbeddings = 0;
                while (!_stack.Peek().Isolate)
                {
                    _stack.Pop();
                }

                _stack.Pop();
                _validIsolates--;
            }

            TakeLevelAndOverride(i);
        }

        /// <summary>X7: a PDF closes the embedding it matches, if any; inside an overflow isolate there is none to close.</summary>
        private void X7PopEmbedding(int i)
        {
            if (_overflowIsolates == 0)
            {
                if (_overflowEmbeddings > 0)
                {
                    _overflowEmbeddings--;
                }
                else if (!_stack.Peek().Isolate && _stack.Count >= 2)
                {
                    _stack.Pop();
                }
            }

            _paragraph.Levels[i] = _stack.Peek().Level;
        }

        /// <summary>X8: a paragraph separator takes the paragraph level and ends everything, which starts again as X1 did.</summary>
        private void X8EndOfParagraph(int i)
        {
            _paragraph.Levels[i] = _paragraph.ParagraphLevel;
            X1Start();
        }

        /// <summary>A boundary neutral, which X9 removes: it only takes the current level, which the output re-derives.</summary>
        private readonly void X9Removed(int i) => _paragraph.Levels[i] = _stack.Peek().Level;

        /// <summary>X6: everything else takes the current level and the current override, if any.</summary>
        private readonly void X6Other(int i) => TakeLevelAndOverride(i);

        /// <summary>The current level, and the current override when there is one, for the character at <paramref name="i"/>.</summary>
        private readonly void TakeLevelAndOverride(int i)
        {
            StatusEntry top = _stack.Peek();
            _paragraph.Levels[i] = top.Level;
            if (top.Override != BidiClass.ON)
            {
                _paragraph.Types[i] = top.Override;
            }
        }

        /// <summary>Whether an embedding or an isolate at <paramref name="level"/> is valid: within max_depth and with nothing overflowing.</summary>
        private readonly bool Opens(byte level) => level <= MaxDepth && _overflowIsolates == 0 && _overflowEmbeddings == 0;

        /// <summary>The level an embedding or an isolate opens above <paramref name="level"/>: the least odd one above it for right to left,
        /// the least even one for left to right.</summary>
        private static byte LevelAbove(byte level, bool rightToLeft) => (byte)(rightToLeft ? (level + 1) | 1 : (level + 2) & ~1);
    }
}
