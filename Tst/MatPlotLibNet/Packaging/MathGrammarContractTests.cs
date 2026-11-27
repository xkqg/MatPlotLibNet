// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;
using MatPlotLibNet.Rendering.MathText;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>Math text reads the grammar matplotlib 3.11.2's mathtext reads, from one table generated from it
/// (<c>tools/mpl_reference/generate_mathtext.py</c> writes <c>MathGrammar.Data.cs</c> and the reference
/// <c>matplotlib-mathtext.json</c>): every symbol name as the character matplotlib sets it, every function name upright with
/// limits where matplotlib sets them, every accent as a mark over its letter, every space at its width, every escaped character
/// as itself. The reference is what matplotlib's own parser answers (a parse of each name), not the table, so a mistake in the
/// table is seen. No other file of the math text spells a name's character or registers a name of these tables by hand.</summary>
public class MathGrammarContractTests
{
    private static readonly JsonElement Reference = LoadReference();

    private static JsonElement LoadReference()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Rendering", "MathText", "matplotlib-mathtext.json")));
        return doc.RootElement.Clone();
    }

    private static IEnumerable<string> Strings(string property) =>
        Reference.GetProperty(property).EnumerateArray().Select(e => e.GetString()!);

    [Fact]
    public void EverySymbolNameOfMatplotlib_IsInTheTable()
    {
        var missing = Strings("symbolNames").Where(name => MathSymbol.GlyphOf(name) == $"\\{name}").ToList();

        Assert.Equal(634, Strings("symbolNames").Count());
        Assert.True(missing.Count == 0, $"{missing.Count} names matplotlib sets are written out as typed: {string.Join(' ', missing)}");
    }

    [Fact]
    public void EverySymbolName_IsSetAsTheCharacterMatplotlibSetsItAs()
    {
        var wrong = Reference.GetProperty("symbols").EnumerateObject()
            .Where(symbol => MathSymbol.GlyphOf(symbol.Name) != char.ConvertFromUtf32(symbol.Value.GetInt32()))
            .Select(symbol => string.Create(CultureInfo.InvariantCulture,
                $"\\{symbol.Name}: U+{symbol.Value.GetInt32():X4} in matplotlib, '{MathSymbol.GlyphOf(symbol.Name)}' here"))
            .ToList();

        Assert.True(wrong.Count == 0, string.Join('\n', wrong));
    }

    [Fact]
    public void TheNamesTheLibraryKeeps_AreTheListedOnes_AndMatplotlibHasNoneOfThem()
    {
        var library = Strings("libraryNames").ToHashSet(StringComparer.Ordinal);

        Assert.Empty(library.Intersect(Strings("symbolNames")));
        Assert.Equal(library.Order(StringComparer.Ordinal), MathGrammar.LibrarySymbols.Order(StringComparer.Ordinal));
        Assert.Equal(Strings("libraryAccents").Order(StringComparer.Ordinal), MathGrammar.LibraryAccents.Order(StringComparer.Ordinal));
        Assert.Equal(634 + library.Count, MathGrammar.Symbols.Count);
        Assert.Equal(MathGrammar.MatplotlibVersion, Reference.GetProperty("matplotlib").GetString());
    }

    [Fact]
    public void EveryFunctionNameOfMatplotlib_IsSetUpright_AsTheWord()
    {
        var wrong = Strings("functionNames")
            .Where(name => MathTextParser.Parse($"$\\{name}$").Spans is not [{ Variant: FontVariant.Roman } span, ..] || span.Text != name)
            .ToList();

        Assert.Equal(32, Strings("functionNames").Count());
        Assert.True(wrong.Count == 0, $"function names not set upright as the word: {string.Join(' ', wrong)}");
    }

    [Fact]
    public void TheScriptsOfAFunctionName_AreLimits_OnlyWhereMatplotlibSetsThem()
    {
        var limits = Strings("limitFunctions").ToHashSet(StringComparer.Ordinal);
        var wrong = Strings("functionNames")
            .Where(name => MathTextParser.Parse($"$\\{name}_x$").Spans.Any(s => s.Kind == TextSpanKind.OperatorSubscript) != limits.Contains(name))
            .ToList();

        Assert.True(wrong.Count == 0, $"function names whose scripts are read wrongly as limits or not: {string.Join(' ', wrong)}");
    }

    [Fact]
    public void TheScriptsOfALargeOperator_AreLimits_OnlyWhereMatplotlibSetsThem()
    {
        var limits = Strings("limitOperators").ToHashSet(StringComparer.Ordinal);
        var wrong = Strings("largeOperators")
            .Where(name => MathTextParser.Parse($"$\\{name}_x$").Spans is not [{ Kind: TextSpanKind.LargeOperator }, var script]
                || script.Kind == TextSpanKind.OperatorSubscript != limits.Contains(name))
            .ToList();

        Assert.Equal(19, Strings("largeOperators").Count());
        Assert.True(wrong.Count == 0, $"large operators read wrongly: {string.Join(' ', wrong)}");
    }

    [Fact]
    public void EveryAccentOfMatplotlib_IsAMarkOverItsLetter()
    {
        var wrong = Strings("accents").Concat(Strings("libraryAccents"))
            .Where(name => MathTextParser.Parse($"$\\{name}{{a}}$").Spans is not [{ Kind: TextSpanKind.Accent, Text: [ 'a', var mark ] }]
                || CharUnicodeInfo.GetUnicodeCategory(mark) != UnicodeCategory.NonSpacingMark)
            .ToList();

        Assert.Equal(23, Strings("accents").Count());
        Assert.True(wrong.Count == 0, $"accents not set as a mark over their letter: {string.Join(' ', wrong)}");
    }

    /// <summary>The nominal width, in ems, of every Unicode space of a fixed width (The Unicode Standard, chapter 6.2).</summary>
    private static readonly Dictionary<char, double> UnicodeSpaceWidths = new()
    {
        [' '] = 1.0 / 2, [' '] = 1.0, [' '] = 1.0 / 3, [' '] = 1.0 / 4, [' '] = 1.0 / 6,
        [' '] = 1.0 / 5, [' '] = 4.0 / 18,
    };

    [Fact]
    public void EverySpaceOfMatplotlib_IsASpaceOfItsWidth()
    {
        var wrong = new List<string>();
        foreach (var space in Reference.GetProperty("spaces").EnumerateObject())
        {
            // "$a\quad b$": a space ends the command's name, and a space typed in math is no space (matplotlib drops it)
            double width = space.Value.GetDouble();
            string between = MathTextParser.Parse($"$a{space.Name} b$").PlainText.Replace(" ", "", StringComparison.Ordinal);
            if (between.Length < 2 || between[0] != 'a' || between[^1] != 'b' || between[1..^1].Any(c => !UnicodeSpaceWidths.ContainsKey(c)))
            {
                wrong.Add($"{space.Name}: '{between}'");
                continue;
            }

            double drawn = between[1..^1].Sum(c => UnicodeSpaceWidths[c]);
            if (width <= 0 ? drawn != 0 : Math.Abs(drawn - width) > 0.03)
            {
                wrong.Add(string.Create(CultureInfo.InvariantCulture, $"{space.Name}: {drawn:0.###} em, matplotlib {width} em"));
            }
        }

        Assert.Equal(12, Reference.GetProperty("spaces").EnumerateObject().Count());
        Assert.True(wrong.Count == 0, string.Join('\n', wrong));
    }

    [Fact]
    public void EveryCharacterABackslashEscapes_IsThatCharacter()
    {
        var escaped = Reference.GetProperty("escapedCharacters").GetString()! + Reference.GetProperty("libraryEscapes").GetString()!;
        var wrong = escaped.Where(c => MathTextParser.Parse($"$\\{c}$").PlainText != c.ToString()).ToList();

        Assert.True(wrong.Count == 0, $"escapes not read as their character: {string.Join(' ', wrong)}");
    }
    

    

    

    

    

    

    

    
}
