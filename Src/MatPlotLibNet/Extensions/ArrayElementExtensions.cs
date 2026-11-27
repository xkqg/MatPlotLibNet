// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet;

/// <summary>The one answer to "the value the caller gave for part <c>i</c>, when what they gave reaches it, else the resting one". A
/// series that takes a value for each of its parts (a colour for each slice, a hatch for each band, a width for each column) takes it
/// as an optional array that may be shorter than the series; each part the array does not reach, and every part when there is no
/// array, wears the resting value. A place before the first element is not a place of the array either.</summary>
internal static class ArrayElementExtensions
{
    extension<T>(T[]? values)
    {
        /// <summary>The element at <paramref name="index"/>, or <paramref name="fallback"/> when there is no array or it does not reach
        /// the index.</summary>
        /// <param name="index">The part's position.</param>
        /// <param name="fallback">What the part wears when the array does not name it.</param>
        /// <returns>The element, else the fallback.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal T ElementOr(int index, T fallback) =>
            values is not null && index >= 0 && index < values.Length ? values[index] : fallback;

        /// <summary>The element at <paramref name="index"/>, or what <paramref name="fallback"/> makes of the index when there is no
        /// array or it does not reach it: for a resting value that is worked out (a name numbered by the part), made only where it is
        /// worn. A lambda that captures nothing allocates nothing.</summary>
        /// <param name="index">The part's position.</param>
        /// <param name="fallback">Makes what the part wears from its position.</param>
        /// <returns>The element, else what the fallback made.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal T ElementOr(int index, Func<int, T> fallback) =>
            values is not null && index >= 0 && index < values.Length ? values[index] : fallback(index);
    }

    extension<T>(T[]? values) where T : struct
    {
        /// <summary>The element at <paramref name="index"/>, or <see langword="null"/> when there is no array or it does not reach the
        /// index: the caller chooses the resting value with <c>??</c>, so one that costs something is worked out only where it is worn.</summary>
        /// <param name="index">The part's position.</param>
        /// <returns>The element, else nothing.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal T? ElementOrNull(int index) =>
            values is not null && index >= 0 && index < values.Length ? values[index] : null;
    }

    extension(string? name)
    {
        /// <summary>The name an item was given, or, where it was given none, its place counted from 1 behind <paramref name="word"/>
        /// (<c>sample 2</c>; the bare number where the word is empty): the one name an unnamed row, column, slice, sample, series or
        /// variable of a table is called by.</summary>
        /// <param name="index">The item's position, from 0.</param>
        /// <param name="word">What is counted, with the space that sets it off from the number (<c>"column "</c>), or nothing.</param>
        /// <returns>The name given, else the numbered one.</returns>
        internal string NameOr(int index, string word = "") => name ?? index.Numbered(word);
    }

    extension(int index)
    {
        /// <summary>The name of an item that has none, its place counted from 1 behind <paramref name="word"/> (<c>image 2</c>; the bare
        /// number where the word is empty): the one numbered name, for an item that is called by its place alone.</summary>
        /// <param name="word">What is counted, with the space that sets it off from the number (<c>"image "</c>), or nothing.</param>
        /// <returns>The numbered name.</returns>
        internal string Numbered(string word = "") => word + (index + 1).InvariantText();
    }

    extension(IReadOnlyList<string>? names)
    {
        /// <summary>The name of the item at <paramref name="index"/> in a list of names, else its numbered one: the same rule as for a
        /// single name, for an item that is called by its place in a list.</summary>
        /// <param name="index">The item's position, from 0.</param>
        /// <param name="word">What is counted, with the space that sets it off from the number, or nothing.</param>
        /// <returns>The name given, else the numbered one.</returns>
        internal string NameOr(int index, string word = "") =>
            (names is not null && index >= 0 && index < names.Count ? names[index] : null).NameOr(index, word);
    }

    extension(Vec values)
    {
        /// <summary>The element of a vector at <paramref name="index"/>, or <paramref name="fallback"/> when the vector does not reach it.</summary>
        /// <param name="index">The part's position.</param>
        /// <param name="fallback">What the part wears when the vector does not name it.</param>
        /// <returns>The element, else the fallback.</returns>
        internal double ElementOr(int index, double fallback) => values.Data.ElementOr(index, fallback);
    }

    extension<T>(IReadOnlyList<T>? values)
    {
        /// <summary>The element at <paramref name="index"/> of a list, or <paramref name="fallback"/> when there is no list or it does not
        /// reach the index.</summary>
        /// <param name="index">The part's position.</param>
        /// <param name="fallback">What the part wears when the list does not name it.</param>
        /// <returns>The element, else the fallback.</returns>
        internal T ElementOr(int index, T fallback) =>
            values is not null && index >= 0 && index < values.Count ? values[index] : fallback;
    }

    extension<T>(IReadOnlyList<T>? values) where T : struct
    {
        /// <summary>The element at <paramref name="index"/> of a list, or <see langword="null"/> when there is no list or it does not
        /// reach the index: the caller chooses the resting value with <c>??</c> or maps the element with <c>?.</c>.</summary>
        /// <param name="index">The part's position.</param>
        /// <returns>The element, else nothing.</returns>
        internal T? ElementOrNull(int index) =>
            values is not null && index >= 0 && index < values.Count ? values[index] : null;
    }
}
