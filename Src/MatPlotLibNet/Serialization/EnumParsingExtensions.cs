// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Diagnostics;

namespace MatPlotLibNet.Serialization;

/// <summary>A wire-format enum name: written (<see cref="ToWireName"/>), and read back by the one name rule
/// (<see cref="IndexOfWireName"/>, over the table of <see cref="EnumWireNames{T}"/>) as the member it names
/// (<see cref="WireEnum{T}"/>).</summary>
internal static class EnumParsingExtensions
{
    extension<T>(T value) where T : struct, Enum
    {
        /// <summary>The name this member is written under in a document: its name in lower case (<c>dashdot</c>, <c>symlog</c>), the
        /// one every earlier version wrote and <see cref="WireEnum{T}"/> reads back in any case. Never a naming policy's.</summary>
        internal string ToWireName() => value.ToString().ToLowerInvariant();
    }

    extension(string[] names)
    {
        /// <summary>Where <paramref name="text"/> stands among these names, matched in any case and as a whole: the one rule for
        /// which written text names a member (or, in the spec vocabulary, a kind).</summary>
        /// <param name="text">The text a document holds.</param>
        /// <returns>The index of the name <paramref name="text"/> is, or -1 when it is none of them.</returns>
        internal int IndexOfWireName(ReadOnlySpan<char> text)
        {
            for (int i = 0; i < names.Length; i++)
            {
                if (text.Equals(names[i], StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }
    }

    extension(string? value)
    {
        /// <summary>The member of <typeparamref name="T"/> this text names (<see cref="EnumWireNames{T}"/>: a member's name in any
        /// case), or <see langword="null"/> when there is no text (the document carries no value) or it names no member. A text
        /// that names no member (a figure written by a newer version, a hand-made payload, a number, two names joined by a comma)
        /// is left out leniently, so the figure keeps loading and the setting keeps the value a document without it gives it, and
        /// is announced on <see cref="ChartDiagnostics"/>, never drawn as something else without a word.</summary>
        /// <typeparam name="T">The enum the text names a member of.</typeparam>
        /// <returns>The member, or <see langword="null"/>.</returns>
        internal T? WireEnum<T>() where T : struct, Enum
        {
            if (value is null)
            {
                return null;
            }

            if (EnumWireNames<T>.TryMatch(value, out var member))
            {
                return member;
            }

            ChartDiagnostics.Emit(new ChartDiagnostic(
                nameof(ChartSerializer),
                $"Unknown {typeof(T).Name} '{value}' was ignored during deserialization; the setting keeps the value it has without it.",
                null));
            return null;
        }
    }
}
