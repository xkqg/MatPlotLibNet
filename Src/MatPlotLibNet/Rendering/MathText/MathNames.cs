// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>A table of math names (the command families of <see cref="MathCommand"/>, the symbols of <see cref="MathGrammar.Symbols"/>)
/// read by the characters of a name where it stands in a label, so a parse looks a <c>\name</c> up without cutting a string of it.</summary>
/// <typeparam name="TValue">What a name stands for.</typeparam>
/// <param name="byName">The names, compared character by character (<see cref="StringComparer.Ordinal"/>).</param>
internal sealed class MathNames<TValue>(FrozenDictionary<string, TValue> byName)
{
#if NET9_0_OR_GREATER
    private readonly FrozenDictionary<string, TValue>.AlternateLookup<ReadOnlySpan<char>> _byCharacters =
        byName.GetAlternateLookup<ReadOnlySpan<char>>();
#endif

    /// <summary>What <paramref name="name"/> stands for.</summary>
    /// <param name="name">The name as written, without its backslash.</param>
    /// <param name="value">What it stands for.</param>
    /// <returns>Whether the table holds the name.</returns>
    internal bool TryFind(ReadOnlySpan<char> name, [MaybeNullWhen(false)] out TValue value) =>
#if NET9_0_OR_GREATER
        _byCharacters.TryGetValue(name, out value);
#else
        byName.TryGetValue(name.ToString(), out value); // a runtime before .NET 9 reads a table by a string only
#endif
}
