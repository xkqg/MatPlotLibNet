// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using MatPlotLibNet.Rendering.MathText;

namespace MatPlotLibNet.Tests.Rendering.MathText;

/// <summary>Verifies the hand half of the math grammar: how a width of space is written as text, and the public lookups that read
/// the one generated symbol table.</summary>
public class MathGrammarTests
{
    /// <summary>A width in ems is an em space per whole em, then the Unicode space nearest the rest (U+2006 a sixth, U+2009 a fifth,
    /// U+205F four eighteenths, U+2005 a quarter, U+2004 a third, U+2002 a half, U+2003 an em); no width or less is nothing.</summary>
    [Theory]
    [InlineData(0, "")]
    [InlineData(-0.16667, "")]
    [InlineData(0.02, "")]
    [InlineData(0.16667, " ")]
    [InlineData(0.2, " ")]
    [InlineData(0.22222, " ")]
    [InlineData(0.27778, " ")]
    [InlineData(0.33333, " ")]
    [InlineData(0.5, " ")]
    [InlineData(0.97, " ")]
    [InlineData(1, " ")]
    [InlineData(1.5, "  ")]
    [InlineData(2, "  ")]
    public void SpaceOf_AWidth_IsTheUnicodeSpacesNearestIt(double em, string expected)
    {
        Assert.Equal(Escaped(expected), Escaped(MathGrammar.SpaceOf(em)));
    }

    [Theory]
    [InlineData("varphi", "φ")]   // tex2uni['varphi'] = 966
    [InlineData("Omega", "Ω")]
    [InlineData("Alpha", "Α")]    // a name the library keeps
    public void GreekLetters_TryGet_AGreekLetter_IsItsCharacter(string name, string expected)
    {
        Assert.Equal(expected, GreekLetters.TryGet(name));
    }

    [Theory]
    [InlineData("pm")]          // a sign, not a letter
    [InlineData("backepsilon")] // U+03F6 stands in the Greek block but is a math sign
    [InlineData("nope")]
    public void GreekLetters_TryGet_ANameThatIsNoGreekLetter_IsNull(string name)
    {
        Assert.Null(GreekLetters.TryGet(name));
    }

    [Theory]
    [InlineData("alpha", "α")]
    [InlineData("Rightarrow", "⇒")]
    [InlineData("Bumpeq", "≎")]    // tex2uni['Bumpeq'] = 8782
    [InlineData("mathbb{R}", "ℝ")] // a name the library keeps
    public void MathSymbols_TryGet_ANameOfTheTable_IsItsCharacter(string name, string expected)
    {
        Assert.Equal(expected, MathSymbols.TryGet(name));
    }

    /// <summary>A body is read as math: a typed space dropped, <c>~</c> and a space command their width
    /// (<c>Parser._space_widths</c>), an escaped character itself, a symbol name its character with matplotlib's spaces around it,
    /// an unknown name as written.</summary>
    [Theory]
    [InlineData("a~b", "a b")]                // '~' = 0.33333
    [InlineData("~~", "  ")]
    [InlineData("ab", "ab")]
    [InlineData(@"\alpha~x", "α x")]
    [InlineData(@"x\,dx", "x dx")]            // '\,' = 0.16667
    [InlineData(@"a\quad b", "a b")]          // '\quad' = 1
    [InlineData(@"a\!b", "ab")]                    // '\!' = -0.16667: nothing
    [InlineData(@"\{a\}", "{a}")]
    [InlineData(@"a \leq b", "a ≤ b")]
    [InlineData(@"\pm a", "±a")]              // a binary operator first takes no space
    [InlineData(@"\nope x", @"\nopex")]
    [InlineData(@"\?x", @"\?x")]                  // a backslash before a character that names nothing: as written
    public void ABody_IsReadAsMath(string content, string expected)
    {
        Assert.Equal(Escaped(expected), Escaped(MathTextParser.Parse($"${content}$").PlainText));
    }

    [Fact]
    public void TheScriptsOfAFunctionName_PutNoSpaceAroundASymbol()
    {
        var script = MathTextParser.Parse(@"$\log_{a+b\leq c}$").Spans.Single(span => span.Kind == TextSpanKind.Subscript);

        Assert.Equal("a+b≤c", script.Text);
    }

    [Theory]
    [InlineData("$a+$ c", 2, 'a', '\0')] // the closing dollar ends the math
    [InlineData("$a + b$", 3, 'a', 'b')]
    [InlineData("$a+b", 2, 'a', 'b')]
    [InlineData("$+  $", 1, '\0', '\0')]
    public void MathSource_ReadsTheCharactersWrittenAroundASymbol_WithinItsMath(string text, int symbol, char before, char after)
    {
        var math = new MathSource(text, 1);

        Assert.Equal(before, math.Before(symbol));
        Assert.Equal(after, math.After(symbol + 1));
    }

    [Theory]
    [InlineData("Binary", '+', '\0', 'b', false, false, false)]
    [InlineData("Binary", '+', '{', 'b', false, false, false)]
    [InlineData("Binary", '+', '=', 'b', false, false, false)]
    [InlineData("Binary", '+', 'a', 'b', false, true, true)]
    [InlineData("Binary", '+', 'a', 'b', true, false, false)]
    [InlineData("Relation", '=', '\0', 'b', false, true, true)]
    [InlineData("Relation", '=', 'a', 'b', true, false, false)]
    [InlineData("Punctuation", ',', '{', '}', false, false, false)]
    [InlineData("Punctuation", ',', '{', 'a', false, false, true)]
    [InlineData("Punctuation", '.', '1', '5', false, false, false)]
    [InlineData("Punctuation", '.', '1', 'a', false, false, true)]
    [InlineData("Punctuation", '\0', 'a', 'b', true, false, true)]
    [InlineData("Ordinary", 'x', 'a', 'b', false, false, false)]
    public void SpacesAround_ASymbol_AreMatplotlibs(string role, char written, char before, char after, bool unspaced,
        bool spaceBefore, bool spaceAfter)
    {
        Assert.Equal(new MathSpaces(spaceBefore, spaceAfter), MathGrammar.SpacesAround(Enum.Parse<MathSymbolRole>(role), written, before, after, unspaced));
    }

    private static string Escaped(string text) => string.Concat(text.Select(c =>
        c < 128 ? c.ToString() : string.Create(CultureInfo.InvariantCulture, $"U+{(int)c:X4}")));
}
