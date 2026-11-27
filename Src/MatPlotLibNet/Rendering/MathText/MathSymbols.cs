// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>Maps LaTeX math symbol command names to their Unicode equivalents: the math grammar's one symbol table, every name
/// matplotlib's mathtext sets as a character (634 of them, Greek letters included) and the few the library keeps beside them.</summary>
public static class MathSymbols
{
    /// <summary>Returns the Unicode string for the given command name, or <see langword="null"/> if not found.</summary>
    public static string? TryGet(string name) => MathGrammar.Symbols.GetValueOrDefault(name).Glyph;
}
