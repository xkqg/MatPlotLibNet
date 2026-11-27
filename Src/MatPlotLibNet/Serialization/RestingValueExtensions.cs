// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Serialization;

/// <summary>How a series writes a value that has a default. The JSON leaves a value out while it is at rest, so a
/// figure that never touches a property writes exactly the bytes it wrote before that property reached the JSON, and
/// a document written before then reads back at the default.</summary>
internal static class RestingValueExtensions
{
    extension<T>(T value) where T : struct
    {
        /// <summary>The value, or <see langword="null"/> when it equals <paramref name="resting"/>, the value the series is
        /// constructed with.</summary>
        /// <param name="resting">The value a new series holds.</param>
        internal T? OrNullAtRest(T resting) =>
            EqualityComparer<T>.Default.Equals(value, resting) ? null : value;
    }

    extension<T>(T? value) where T : struct
    {
        /// <summary>The optional value, or <see langword="null"/> when it equals <paramref name="resting"/>, the value a new
        /// model holds (which may itself be absent).</summary>
        /// <param name="resting">The value a new model holds.</param>
        internal T? OrNullAtRest(T? resting) =>
            Nullable.Equals(value, resting) ? null : value;
    }

    extension<T>(T value)
    {
        /// <summary>The JSON's form of the value, made by <paramref name="describe"/>, or <see langword="null"/> when the value equals
        /// <paramref name="resting"/>: a record of its own (a font, a grid, a legend) is left out whole while it is at rest.</summary>
        /// <param name="resting">The value a new model holds.</param>
        /// <param name="describe">Makes the JSON's form of a value away from its rest.</param>
        /// <typeparam name="TDto">The JSON's form.</typeparam>
        /// <returns>The form, or <see langword="null"/>.</returns>
        internal TDto? DescribedUnlessAtRest<TDto>(T resting, Func<T, TDto> describe) where TDto : class? =>
            EqualityComparer<T>.Default.Equals(value, resting) ? null : describe(value);

        /// <summary>The JSON's form of the value measured against <paramref name="resting"/> (each field written only away from the
        /// rest's), or <see langword="null"/> when the value equals it.</summary>
        /// <param name="resting">The value a new model holds.</param>
        /// <param name="describe">Makes the JSON's form of a value from it and its rest.</param>
        /// <typeparam name="TDto">The JSON's form.</typeparam>
        /// <returns>The form, or <see langword="null"/>.</returns>
        internal TDto? DescribedUnlessAtRest<TDto>(T resting, Func<T, T, TDto> describe) where TDto : class? =>
            EqualityComparer<T>.Default.Equals(value, resting) ? null : describe(value, resting);
    }

    extension<T>(T[] value)
    {
        /// <summary>The list, or <see langword="null"/> while it holds the items of <paramref name="resting"/> in their order.</summary>
        /// <param name="resting">The list a new model holds.</param>
        /// <returns>The list, or <see langword="null"/>.</returns>
        internal T[]? OrNullAtRest(T[] resting) => value.SequenceEqual(resting) ? null : value;
    }

    extension(string? value)
    {
        /// <summary>The text, or <see langword="null"/> when it equals <paramref name="resting"/>, the text a new model holds.</summary>
        /// <param name="resting">The text a new model holds.</param>
        internal string? OrNullAtRest(string? resting) =>
            string.Equals(value, resting, StringComparison.Ordinal) ? null : value;
    }
}
