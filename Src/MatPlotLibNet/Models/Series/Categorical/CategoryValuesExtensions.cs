// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>The check every composed verb holds a plain array of values to when it is drawn over named categories (or over
/// other places, such as the days of a calendar), in one place, so each verb refuses an array that does not fit in the
/// same words: one value per category. A group of values that carries its own name is checked by
/// <see cref="BarGroupExtensions"/> instead.</summary>
internal static class CategoryValuesExtensions
{
    /// <summary>Refuses <paramref name="values"/> unless it carries exactly one value per place it is drawn over: per
    /// category, or per date for a verb whose values stand on dates. Three values over four categories would leave a
    /// category without a mark, or draw a value under the wrong name.</summary>
    /// <param name="values">The values to check.</param>
    /// <param name="count">How many places (categories, or dates) the values are drawn over.</param>
    /// <param name="paramName">The caller's parameter that holds the values, named in the exception.</param>
    /// <param name="places">What the places are called in the message: <c>categories</c> unless the verb draws its values
    /// over something else.</param>
    /// <param name="place">One of the places, as the message names it: <c>category</c> unless the verb draws its values over
    /// something else.</param>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The array carries more or fewer values than there are places; the message names
    /// both counts.</exception>
    internal static void ThrowIfMisaligned(this double[] values, int count, string paramName, string places = "categories",
        string place = "category")
    {
        ArgumentNullException.ThrowIfNull(values, paramName);
        if (values.Length != count)
        {
            throw new ArgumentException(
                $"{values.Length} values were given for {count} {places}; there must be one value per {place}.", paramName);
        }
    }
}
