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

    extension(string? value)
    {
        /// <summary>The text, or <see langword="null"/> when it equals <paramref name="resting"/>, the text a new model holds.</summary>
        /// <param name="resting">The text a new model holds.</param>
        internal string? OrNullAtRest(string? resting) =>
            string.Equals(value, resting, StringComparison.Ordinal) ? null : value;
    }
}
