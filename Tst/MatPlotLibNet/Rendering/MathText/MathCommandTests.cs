// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.MathText;

namespace MatPlotLibNet.Tests.Rendering.MathText;

/// <summary>Verifies the one table a math-mode command is looked up in: every name maps to the family that reads it, a
/// spacing command may be one punctuation character, and a name the table does not hold is read as a symbol.</summary>
public class MathCommandTests
{
    [Theory]
    [InlineData("frac", typeof(MathFraction))]
    [InlineData("sqrt", typeof(MathRadical))]
    [InlineData("hat", typeof(MathAccent))]
    [InlineData("breve", typeof(MathAccent))]
    [InlineData("mathrm", typeof(MathFontSwitch))]
    [InlineData("text", typeof(MathTextRun))]
    [InlineData("mathbb", typeof(MathFontSwitch))]
    [InlineData("left", typeof(MathDelimiter))]
    [InlineData("right", typeof(MathDelimiter))]
    [InlineData("begin", typeof(MathEnvironment))]
    [InlineData("end", typeof(MathEnvironmentEnd))]
    [InlineData("acute", typeof(MathAccent))]
    [InlineData("\"", typeof(MathAccent))]
    [InlineData("widehat", typeof(MathAccent))]
    [InlineData("lim", typeof(MathOperatorName))]
    [InlineData("limsup", typeof(MathOperatorName))]
    [InlineData("tan", typeof(MathFunctionName))]
    [InlineData("exp", typeof(MathFunctionName))]
    [InlineData("int", typeof(MathIntegral))]
    [InlineData("oiint", typeof(MathIntegral))]
    [InlineData("prod", typeof(MathLargeOperator))]
    [InlineData("bigcup", typeof(MathLargeOperator))]
    [InlineData("quad", typeof(MathSpacing))]
    [InlineData("qquad", typeof(MathSpacing))]
    [InlineData(",", typeof(MathSpacing))]
    [InlineData(":", typeof(MathSpacing))]
    [InlineData(";", typeof(MathSpacing))]
    [InlineData("!", typeof(MathSpacing))]
    [InlineData("thinspace", typeof(MathSpacing))]
    [InlineData(" ", typeof(MathSpacing))]
    public void Find_ANameOfAFamily_ReturnsThatFamily(string name, Type family)
    {
        Assert.IsType(family, MathCommand.Find(name));
    }

    [Theory]
    [InlineData("alpha")]
    [InlineData("pm")]
    [InlineData("unknown")]
    [InlineData("")]
    public void Find_ASymbolOrAnUnknownName_ReturnsNull(string name)
    {
        Assert.Null(MathCommand.Find(name));
    }

    [Theory]
    [InlineData("alpha", "α")]
    [InlineData("Omega", "Ω")]
    [InlineData("pm", "±")]
    [InlineData("nope", @"\nope")]
    [InlineData("", @"\")]
    public void GlyphOf_AName_IsTheLetterTheSymbolOrTheCommandAsWritten(string name, string glyph)
    {
        Assert.Equal(glyph, MathSymbol.GlyphOf(name));
    }

    /// <summary>The names the library's first tables set as another character than matplotlib does. Each row is
    /// <c>matplotlib._mathtext_data.tex2uni[name]</c> printed with python under matplotlib 3.11.2.</summary>
    [Theory]
    [InlineData("circ", "∘")]   // tex2uni['circ'] = 8728, the ring operator (the degree sign is \degree, 176)
    [InlineData("hbar", "ħ")]   // tex2uni['hbar'] = 295
    [InlineData("models", "⊧")] // tex2uni['models'] = 8871
    [InlineData("perp", "⟂")]   // tex2uni['perp'] = 10178 (U+22A5 is \bot, 8869)
    [InlineData("phi", "ϕ")]    // tex2uni['phi'] = 981, the straight phi (U+03C6 is \varphi, 966)
    public void GlyphOf_ANameMatplotlibSetsAsAnotherCharacter_IsMatplotlibsCharacter(string name, string glyph)
    {
        Assert.Equal(glyph, MathSymbol.GlyphOf(name));
    }

    /// <summary>A body (here a matrix cell) is read as math, as every argument is: each command is its glyph, an unknown one as written.</summary>
    [Theory]
    [InlineData("x", "x")]
    [InlineData(@"\alpha + \beta", "\u03B1\u2009+\u2009\u03B2")]
    [InlineData(@"\nope", @"\nope")]
    public void AMatrixCell_ReplacesEveryCommandWithItsGlyph(string content, string expected)
    {
        var cell = MathTextParser.Parse($@"$\begin{{matrix}}{content}\end{{matrix}}$").Spans.Single(span => span.Kind == TextSpanKind.MatrixCell);

        Assert.Equal(expected, cell.Text);
    }

    /// <summary>A name is looked up by its characters where it stands in the label: the family and the symbol are found with no string
    /// cut for the name.</summary>
    [Fact]
    public void AName_IsReadWhereItStandsInTheLabel()
    {
        const string label = @"$\frac{\alpha}{2}$";

        Assert.IsType<MathFraction>(MathCommand.Find(label.AsSpan(2, 4)));
        Assert.Null(MathCommand.Find(label.AsSpan(8, 5)));
        Assert.Equal("α", MathSymbol.GlyphOf(label.AsSpan(8, 5)));
    }

    /// <summary>The grammar's symbols are a family by name; a name the grammar does not hold is none.</summary>
    [Theory]
    [InlineData("alpha", true)]
    [InlineData("|", true)]
    [InlineData("nope", false)]
    [InlineData("", false)]
    public void Named_IsTheSymbolFamily_OnlyForANameOfTheGrammar(string name, bool known)
    {
        Assert.Equal(known, ReferenceEquals(MathSymbol.Named(name), MathSymbol.Instance));
        Assert.Equal(known, MathSymbol.Named(name) is not null);
    }

    /// <summary>A table of names answers for a name it holds and for one it does not.</summary>
    [Fact]
    public void ATableOfNames_FindsWhatANameStandsFor()
    {
        var names = new MathNames<int>(System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary(
            new Dictionary<string, int> { ["one"] = 1, ["two"] = 2 }, StringComparer.Ordinal));

        Assert.True(names.TryFind("xtwox".AsSpan(1, 3), out int two));
        Assert.Equal(2, two);
        Assert.False(names.TryFind("three", out _));
    }

    /// <summary>The parse of a label cuts no string for a command's name: a fraction of two letters allocates its boxes and its rows
    /// alone (two rows of one box, the fraction, the line and its row; the letters are the one box of each).</summary>
    [Fact]
    public void AParse_CutsNoStringForACommandsName()
    {
        const string label = @"$\frac{a}{b}$";
        for (int i = 0; i < 50; i++) _ = TextLabel.Of(label).Parsed(null);

        long before = GC.GetAllocatedBytesForCurrentThread();
        _ = TextLabel.Of(label).Parsed(null);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(allocated <= 248, $"Parsing a fraction of two letters allocated {allocated} bytes; its boxes and rows take 248.");
    }
}
