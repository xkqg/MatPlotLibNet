// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Text;

/// <summary>What the bidi algorithm resolved for one paragraph of text: an embedding level per UTF-16 code unit,
/// the paragraph's own level, and the visual order of its characters.</summary>
/// <param name="Levels">One resolved embedding level per UTF-16 code unit of the input; both units of a surrogate
/// pair carry the same level. A character that rule X9 removes (an embedding or override control, a boundary
/// neutral such as a zero width joiner) holds <see cref="Removed"/>.</param>
/// <param name="ParagraphLevel">0 for a left-to-right paragraph, 1 for right-to-left.</param>
/// <param name="Order">The characters in visual order, left to right, each named by the index of its first code
/// unit; characters removed by X9 are absent.</param>
internal readonly record struct BidiParagraph(byte[] Levels, byte ParagraphLevel, int[] Order)
{
    /// <summary>The level a code unit holds when rule X9 removed its character from the ordering.</summary>
    public const byte Removed = 0xFF;

    /// <summary>Whether the paragraph reads right to left.</summary>
    public bool IsRightToLeft => (ParagraphLevel & 1) == 1;

    /// <summary>The characters in visual order, left to right, each named by the index of its first code unit.
    /// Characters removed by X9 are absent: they have no position of their own.</summary>
    public int[] VisualOrder() => Order;

    /// <summary>The levels a renderer hands to a shaper: every code unit gets a real level. A removed character
    /// takes the level of the character before it, or the paragraph level at the very start, so a zero width
    /// joiner stays inside the run whose letters it joins.</summary>
    public byte[] RenderLevels()
    {
        var levels = new byte[Levels.Length];
        byte current = ParagraphLevel;
        for (int i = 0; i < Levels.Length; i++)
        {
            if (Levels[i] != Removed)
            {
                current = Levels[i];
            }

            levels[i] = current;
        }

        return levels;
    }
}
