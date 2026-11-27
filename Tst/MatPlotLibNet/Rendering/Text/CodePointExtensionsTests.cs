// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Text;

namespace MatPlotLibNet.Tests.Rendering.Text;

/// <summary>A UTF-16 text read one code point at a time: a well-formed surrogate pair is one code point of two units, any other unit
/// one code point of one, a lone surrogate included, which keeps its own value.</summary>
public sealed class CodePointExtensionsTests
{
    [Theory]
    [InlineData("a", 0, 1, 'a')]
    [InlineData("\U0001D538", 0, 2, 0x1D538)]
    [InlineData("a\U0001D538b", 1, 2, 0x1D538)]
    [InlineData("a\U0001D538b", 3, 1, 'b')]
    public void ACodePoint_TakesTheUnitsOfItsPair_AndLeavesTheIndexOnItsLastUnit(string text, int at, int units, int codePoint)
    {
        int i = at;

        Assert.Equal(units, text.AsSpan().UnitsAt(at));
        Assert.Equal(codePoint, text.AsSpan().CodePointAt(ref i));
        Assert.Equal(at + units - 1, i);
    }

    [Fact]
    public void ALoneSurrogate_IsACodePointOfOneUnit_WithItsOwnValue()
    {
        // Built in code: InlineData stores strings as UTF-8, which cannot hold a lone surrogate. A high surrogate at the end, one
        // before a letter, and a low surrogate before a high one.
        foreach (string text in new[] { new string((char)0xD800, 1), new string([(char)0xD800, 'a']), new string([(char)0xDC00, (char)0xD800]) })
        {
            int i = 0;

            Assert.Equal(1, text.AsSpan().UnitsAt(0));
            Assert.Equal(text[0], text.AsSpan().CodePointAt(ref i));
            Assert.Equal(0, i);
        }
    }
}
