// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>The math between one pair of dollar signs as it was written: the label's text and where the math starts (it ends at
/// the next dollar sign, or with the text). matplotlib decides the space around a symbol from the characters written around it
/// (<c>Parser.symbol</c> reads the text before and after the symbol, spaces skipped), so the decision reads the source, not the
/// characters already set.</summary>
/// <param name="Text">The label's text.</param>
/// <param name="Start">Where the math starts: just past its opening dollar sign.</param>
internal readonly record struct MathSource(string Text, int Start)
{
    /// <summary>The character written before <paramref name="index"/> in the math, spaces skipped; <c>'\0'</c> when nothing is.</summary>
    internal char Before(int index)
    {
        for (int i = index - 1; i >= Start; i--)
        {
            if (Text[i] != ' ') return Text[i];
        }

        return '\0';
    }

    /// <summary>The character written from <paramref name="index"/> on in the math, spaces skipped; <c>'\0'</c> when nothing is: at
    /// the dollar sign that closes the math (one a backslash escapes is never reached, the backslash is read first) or the end.</summary>
    internal char After(int index)
    {
        for (int i = index; i < Text.Length; i++)
        {
            char c = Text[i];
            if (c == '$') return '\0';
            if (c != ' ') return c;
        }

        return '\0';
    }
}
