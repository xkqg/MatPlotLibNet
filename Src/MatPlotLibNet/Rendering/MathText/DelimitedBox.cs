// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>A delimited group (matplotlib's <c>Parser._auto_sized_delimiter</c>): <c>\left(</c>, the content with its <c>\middle|</c>
/// delimiters, <c>\right)</c>, each delimiter grown to the tallest part of the content (<see cref="SizedGlyph"/>); an empty delimiter
/// (<c>.</c>) draws nothing.</summary>
/// <param name="left">The left delimiter's character, or empty.</param>
/// <param name="segments">The content between the delimiters, cut at each <c>\middle</c>: one more than the middles.</param>
/// <param name="middles">The <c>\middle</c> delimiters, between the segments.</param>
/// <param name="right">The right delimiter's character, or empty.</param>
internal sealed class DelimitedBox(string left, MathBox[] segments, string[] middles, string right) : MathBox
{
    /// <inheritdoc />
    internal override MathExtent Lay(MathInk ink, in MathStyle style)
    {
        var marks = new MathInkMark[segments.Length + 1];
        var sizes = new MathExtent[segments.Length];
        double height = 0, depth = 0;
        for (int i = 0; i < segments.Length; i++)
        {
            marks[i] = ink.Mark;
            sizes[i] = segments[i].Lay(ink, style);
            height = Math.Max(height, sizes[i].Height);
            depth = Math.Max(depth, sizes[i].Depth);
        }

        marks[^1] = ink.Mark;
        double x = 0, top = height, bottom = depth;
        void Delimiter(MathInk into, in MathStyle at, string glyph)
        {
            if (glyph.Length == 0) return;
            var from = into.Mark;
            var size = SizedGlyph.Lay(into, at, glyph, height, depth);
            into.Shift(from, x, 0);
            x += size.Width;
            top = Math.Max(top, size.Height);
            bottom = Math.Max(bottom, size.Depth);
        }

        Delimiter(ink, style, left);
        for (int i = 0; i < segments.Length; i++)
        {
            ink.Shift(marks[i], marks[i + 1], x, 0);
            x += sizes[i].Width;
            if (i < middles.Length) Delimiter(ink, style, middles[i]);
        }

        Delimiter(ink, style, right);
        return new MathExtent(x, top, bottom);
    }

    /// <inheritdoc />
    internal override bool IsCharNode => Array.TrueForAll(segments, segment => segment.IsCharNode);

    /// <inheritdoc />
    internal override void Flatten(MathSpanWriter writer)
    {
        writer.Delimiter(left);
        for (int i = 0; i < segments.Length; i++)
        {
            segments[i].Flatten(writer);
            if (i < middles.Length) writer.Delimiter(middles[i]);
        }

        writer.Delimiter(right);
    }
}
