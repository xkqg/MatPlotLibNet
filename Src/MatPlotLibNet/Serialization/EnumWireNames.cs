// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Serialization;

/// <summary>The names a document may name a member of <typeparamref name="T"/> by: the member's own names, matched in any case
/// (<see cref="EnumParsingExtensions.IndexOfWireName"/>), and nothing else. A number, two names joined by a comma or a name with
/// a space around it names no member, though <see cref="Enum.TryParse{TEnum}(string?, bool, out TEnum)"/> would make a value of
/// each. Every reader of an enum name matches through this one table: a text field (<see cref="EnumParsingExtensions.WireEnum{T}"/>),
/// an enum-typed field (<see cref="WireEnumJsonConverter"/>) and the spec vocabulary of the MCP server.</summary>
/// <typeparam name="T">The enum.</typeparam>
internal static class EnumWireNames<T> where T : struct, Enum
{
    /// <summary>The member names, in the order of <see cref="Values"/>.</summary>
    internal static readonly string[] Names = Enum.GetNames<T>();

    /// <summary>The members, in the order of <see cref="Names"/>.</summary>
    private static readonly T[] Values = Enum.GetValues<T>();

    /// <summary>The member <paramref name="text"/> names, if it names one.</summary>
    /// <param name="text">The text a document holds.</param>
    /// <param name="member">The member named; the default when none is.</param>
    /// <returns>Whether <paramref name="text"/> is the name of a member in any case.</returns>
    internal static bool TryMatch(ReadOnlySpan<char> text, out T member)
    {
        int at = Names.IndexOfWireName(text);
        member = at < 0 ? default : Values[at];
        return at >= 0;
    }
}
