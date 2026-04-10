// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.TextMeasurement;

/// <summary>
/// Per-character width factors for proportional sans-serif (Helvetica/Arial) text measurement.
/// Multiply <see cref="GetWidth"/> by the font size in pixels to get the character's pixel width.
/// </summary>
internal static class CharacterWidthTable
{
    private const double Narrow  = 0.28;
    private const double Slim    = 0.38;
    private const double Medium  = 0.55;
    private const double Wide    = 0.68;
    private const double VeryWide = 0.82;
    private const double Digit   = 0.60;
    private const double Default = 0.58;

    // Character-specific overrides where the width deviates meaningfully from the category average
    private static readonly Dictionary<char, double> Widths = new()
    {
        // --- Digits (uniform/tabular in most fonts) ---
        ['0'] = Digit, ['1'] = Digit, ['2'] = Digit, ['3'] = Digit, ['4'] = Digit,
        ['5'] = Digit, ['6'] = Digit, ['7'] = Digit, ['8'] = Digit, ['9'] = Digit,

        // --- Punctuation / symbols ---
        [' '] = 0.32, ['.'] = 0.30, [','] = 0.30, [':'] = 0.30, [';'] = 0.30,
        ['!'] = 0.30, ['?'] = 0.54, ['|'] = 0.28, ['-'] = 0.38, ['_'] = 0.50,
        ['+'] = 0.60, ['='] = 0.60, ['*'] = 0.50, ['/'] = 0.42, ['\\'] = 0.42,
        ['('] = 0.34, [')'] = 0.34, ['['] = 0.34, [']'] = 0.34, ['{'] = 0.34, ['}'] = 0.34,
        ['<'] = 0.60, ['>'] = 0.60, ['@'] = 0.92, ['#'] = 0.66, ['$'] = 0.60,
        ['%'] = 0.80, ['^'] = 0.60, ['&'] = 0.72, ['~'] = 0.60, ['`'] = 0.36,
        ['"'] = 0.40, ['\''] = 0.22,

        // --- Uppercase ---
        ['A'] = 0.66, ['B'] = 0.64, ['C'] = 0.64, ['D'] = 0.72, ['E'] = 0.58,
        ['F'] = 0.54, ['G'] = 0.70, ['H'] = 0.72, ['I'] = 0.28, ['J'] = 0.44,
        ['K'] = 0.66, ['L'] = 0.54, ['M'] = VeryWide, ['N'] = 0.72, ['O'] = 0.76,
        ['P'] = 0.60, ['Q'] = 0.76, ['R'] = 0.66, ['S'] = 0.58, ['T'] = 0.58,
        ['U'] = 0.70, ['V'] = 0.66, ['W'] = VeryWide, ['X'] = 0.64, ['Y'] = 0.60,
        ['Z'] = 0.60,

        // --- Lowercase ---
        ['a'] = 0.56, ['b'] = 0.58, ['c'] = 0.52, ['d'] = 0.58, ['e'] = 0.56,
        ['f'] = Slim,  ['g'] = 0.58, ['h'] = 0.58, ['i'] = Narrow, ['j'] = 0.28,
        ['k'] = 0.54, ['l'] = Narrow, ['m'] = VeryWide, ['n'] = 0.58, ['o'] = 0.58,
        ['p'] = 0.58, ['q'] = 0.58, ['r'] = Slim,  ['s'] = 0.50, ['t'] = 0.38,
        ['u'] = 0.58, ['v'] = 0.54, ['w'] = 0.78,  ['x'] = 0.52, ['y'] = 0.54,
        ['z'] = 0.50,

        // --- Common Unicode characters used in tick labels ---
        ['\u2212'] = 0.38, // minus sign (−)
        ['\u00B0'] = 0.44, // degree sign (°)
        ['\u00B1'] = 0.60, // plus-minus (±)
    };

    /// <summary>Returns the width factor for the given character at 1 font-size unit.
    /// Multiply by the font size (px) to get the pixel width of the character.</summary>
    internal static double GetWidth(char c) =>
        Widths.TryGetValue(c, out var w) ? w : Default;
}
