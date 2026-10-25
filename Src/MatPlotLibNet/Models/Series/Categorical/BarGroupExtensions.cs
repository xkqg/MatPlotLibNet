// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>The check every verb that takes a <see cref="BarGroup"/> holds it to, in one place, so each verb refuses a
/// group that does not fit in the same words: <see cref="AxesBuilder.GroupedBar"/> today, and every composed verb that
/// takes its data as named groups.</summary>
internal static class BarGroupExtensions
{
    extension(BarGroup group)
    {
        /// <summary>Refuses this group unless it carries exactly one value per place it is drawn over: per
        /// category, or per x position for a verb whose groups run along an x array. A group of two values over three
        /// categories would leave a category without a bar, or draw a value under the wrong name.</summary>
        /// <param name="count">How many places (categories, or x positions) the group's values are drawn over.</param>
        /// <param name="paramName">The caller's parameter that holds the group, named in the exception.</param>
        /// <param name="places">What the places are called in the message: <c>categories</c> unless the verb draws its groups
        /// over something else.</param>
        /// <exception cref="ArgumentException">The group carries no values at all (its array is <see langword="null"/>), or
        /// more or fewer values than there are places; the message names the group and the count.</exception>
        internal void ThrowIfMisaligned(int count, string paramName, string places = "categories")
        {
            if (group.Values is null)
            {
                throw new ArgumentException(
                    $"Group '{group.Label}' carries no values; there are {count} {places}.", paramName);
            }

            if (group.Values.Length != count)
            {
                throw new ArgumentException(
                    $"Group '{group.Label}' carries {group.Values.Length} values; there are {count} {places}.", paramName);
            }
        }
    }
}
