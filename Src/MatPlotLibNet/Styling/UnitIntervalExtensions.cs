// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Styling;

/// <summary>The one owner of "held between 0 and 1": an opacity (<c>ThrowIfNotOpacity</c> refuses one outside it, given or optional), a share of a scale,
/// an intensity, a position on a colour map.</summary>
internal static class UnitIntervalExtensions
{
    extension(double value)
    {
        /// <summary>This value held between 0 and 1: below 0 it is 0, above 1 it is 1, and a value that is not a number stays one (a
        /// missing value is not turned into a position).</summary>
        /// <returns>The value in the closed interval from 0 to 1.</returns>
        internal double Saturate() => Math.Clamp(value, 0.0, 1.0);

        /// <summary>This value as an opacity: itself when it is a number from 0 (clear) to 1 (opaque), as matplotlib's <c>Artist.set_alpha</c>
        /// takes it, and an error for anything else, a missing value (NaN) and an infinity included. The one rule every setting of an opacity
        /// in the library reads, so none keeps its own range or its own message.</summary>
        /// <param name="name">The name of the setting that was given the value, for the error.</param>
        /// <returns>The value, unchanged.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The value is below 0, above 1 or not a number.</exception>
        internal double ThrowIfNotOpacity(string name) => value.ThrowIfNotIn(0.0, 1.0, name, name, static setting => $"The opacity ({setting})");
    }

    extension(double? value)
    {
        /// <summary>This opacity, when one was given, refused as a given opacity is refused; none stays none.
        /// The rule a look's optional opacity and a theme document's read of one go through.</summary>
        /// <param name="name">The name of the setting that was given the value, for the error.</param>
        /// <returns>The value, unchanged.</returns>
        /// <exception cref="ArgumentOutOfRangeException">A value was given and is below 0, above 1 or not a number.</exception>
        internal double? ThrowIfNotOpacity(string name) => value is { } given ? given.ThrowIfNotOpacity(name) : null;
    }
}
