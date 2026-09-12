// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Text;

/// <summary>The paired brackets of Unicode (<c>BidiBrackets.txt</c>): which code points open or close a pair and
/// what their counterpart is. Rule N0 resolves a bracket pair as one unit so that "(" and ")" around an Arabic word
/// in a Latin sentence stay on the outside of the word. The data half lives in <c>BidiBrackets.g.cs</c>.</summary>
internal static partial class BidiBrackets
{
    /// <summary>How many bracket code points the table holds.</summary>
    internal static int Count => CodePoints.Length;

    /// <summary>Whether <paramref name="codePoint"/> is a paired bracket, and if so which code point pairs with it
    /// and whether it is the opening half.</summary>
    public static bool TryGetPair(int codePoint, out int pair, out bool opens)
    {
        int index = Array.BinarySearch(CodePoints, codePoint);
        if (index < 0)
        {
            pair = 0;
            opens = false;
            return false;
        }

        pair = Pairs[index];
        opens = Opens[index];
        return true;
    }

    /// <summary>The canonical form of a bracket for pair matching: Unicode has exactly two bracket pairs with
    /// canonical equivalents (U+2329/U+232A decompose to U+3008/U+3009), and rule BD16 matches under that
    /// equivalence. Every other bracket is its own canonical form.</summary>
    public static int Canonical(int codePoint) => codePoint switch
    {
        0x2329 => 0x3008,
        0x232A => 0x3009,
        _ => codePoint,
    };
}
