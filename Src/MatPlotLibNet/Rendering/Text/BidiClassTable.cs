// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Text;

/// <summary>One range of code points that share a bidi class.</summary>
internal readonly record struct BidiClassRange(int Start, int End, BidiClass Class);

/// <summary>Looks up the Bidi_Class of a code point in the table generated from the Unicode Character Database
/// (the data half lives in <c>BidiClassTable.g.cs</c>, produced by <c>tools/unicode/generate_bidi_tables.py</c>).
/// The ranges are sorted and contiguous over U+0000..U+10FFFF, so a lookup is one binary search and allocates
/// nothing.</summary>
internal static partial class BidiClassTable
{
    /// <summary>The Bidi_Class of <paramref name="codePoint"/>. Anything outside Unicode reads as
    /// <see cref="BidiClass.L"/>, which is also what the database assigns to every unassigned code point that no
    /// script-specific default covers.</summary>
    public static BidiClass GetClass(int codePoint)
    {
        if (codePoint < 0 || codePoint > 0x10FFFF)
        {
            return BidiClass.L;
        }

        int index = Array.BinarySearch(Starts, codePoint);
        if (index < 0)
        {
            // BinarySearch returns the complement of the first start GREATER than the code point; the range
            // that contains it starts one entry earlier.
            index = ~index - 1;
        }

        return Classes[index];
    }

    /// <summary>How many ranges the table holds — for the test that regenerates it from the source file.</summary>
    internal static int RangeCount => Starts.Length;

    /// <summary>The <paramref name="index"/>-th range, in code point order.</summary>
    internal static BidiClassRange Range(int index) => new(Starts[index], Ends[index], Classes[index]);
}
