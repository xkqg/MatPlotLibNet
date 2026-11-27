// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace MatPlotLibNet.Rendering.Text;

/// <summary>A UTF-16 text read one code point at a time: a well-formed surrogate pair is one code point of two code units, and any other
/// unit is one code point of one, a lone surrogate included, which keeps its own value (no replacement character), so every unit belongs
/// to exactly one code point. The bidi algorithm, the shaper of the Skia package and the font switches of math text read a text this way.</summary>
internal static class CodePointExtensions
{
    /// <param name="text">The text.</param>
    extension(ReadOnlySpan<char> text)
    {
        /// <summary>How many code units the code point that starts at <paramref name="i"/> takes.</summary>
        /// <param name="i">The index of its first code unit.</param>
        /// <returns>2 for a well-formed surrogate pair, 1 for any other unit.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int UnitsAt(int i) => char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]) ? 2 : 1;

        /// <summary>The code point that starts at <paramref name="i"/>.</summary>
        /// <param name="i">The index of its first code unit; left on its last one, so a loop that steps by one unit reads the next code
        /// point next.</param>
        /// <returns>The code point.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int CodePointAt(ref int i) => text.UnitsAt(i) == 2 ? char.ConvertToUtf32(text[i], text[++i]) : text[i];
    }
}
