// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Models.Series;

/// <summary>The check every verb that takes <see cref="BarGroup"/> values holds them to, in one place, so each verb refuses a
/// group that does not fit in the same words: <see cref="AxesBuilderCategoricalVerbs.GroupedBar"/>, and every composed verb that takes its
/// data as named groups.</summary>
internal static class BarGroupExtensions
{
    extension(IReadOnlyList<BarGroup> groups)
    {
        /// <summary>Refuses the first group of this list that does not carry exactly one value per place it is drawn over (see
        /// <c>ThrowIfMisaligned</c> on a single group). Whether the list is empty is for each verb to say, in its own words.</summary>
        /// <param name="count">How many places (categories, x positions, columns) each group's values are drawn over.</param>
        /// <param name="paramName">The caller's parameter that holds the groups, named in the exception.</param>
        /// <param name="places">What the places are called in the message: <c>categories</c> unless the verb draws its groups
        /// over something else.</param>
        /// <exception cref="ArgumentException">A group carries no values at all, or more or fewer than there are places.</exception>
        internal void ThrowIfNotOneEach(int count, string paramName, string places = "categories")
        {
            foreach (var group in groups)
            {
                group.ThrowIfMisaligned(count, paramName, places);
            }
        }

        /// <summary>Refuses the first value of any group that is not an amount (<c>double[].ThrowIfAnyNotAmount</c>), named by
        /// <paramref name="what"/> of its group's name, the <paramref name="places"/> and its index (<c>The rank of 'Owls' at x = 2023</c>).</summary>
        /// <param name="paramName">The caller's parameter that holds the groups, named in the exception.</param>
        /// <param name="places">The places the values of a group are drawn over.</param>
        /// <param name="what">What the value of a group at an index is.</param>
        /// <param name="least">The smallest value accepted; 0 by default.</param>
        internal void ThrowIfAnyNotAmount<TPlaces>(string paramName, TPlaces places, Func<LabelledPlaces<TPlaces>, int, string> what, double least = 0)
        {
            foreach (var group in groups)
                group.Values.ThrowIfAnyNotAmount(paramName, new LabelledPlaces<TPlaces>(group.Label, places), what, least);
        }
    }

    extension(BarGroup group)
    {
        /// <summary>Refuses this group unless it carries exactly one value per place it is drawn over: per
        /// category, or per x position for a verb whose groups run along an x array. A group of two values over three
        /// categories would leave a category without a bar, or draw a value under the wrong name.</summary>
        /// <param name="count">How many places (categories, or x positions) the group's values are drawn over.</param>
        /// <param name="paramName">The caller's parameter that holds the group, named in the exception.</param>
        /// <param name="places">What the places are called in the message: <c>categories</c> unless the verb draws its groups
        /// over something else.</param>
        /// <exception cref="ArgumentException">The group carries no values at all (its array is <see langword="null"/>; the message
        /// names the group and the count), or more or fewer values than there are places (refused by the one length check,
        /// <see cref="ArrayLengthExtensions"/>, in its words).</exception>
        internal void ThrowIfMisaligned(int count, string paramName, string places = "categories")
        {
            if (group.Values is null)
            {
                throw new ArgumentException(
                    string.Invariant($"Group '{group.Label}' carries no values; there are {count} {places}."), paramName);
            }

            group.Values.Length.ThrowIfMisaligned(count, paramName, $"group '{group.Label}'");
        }
    }
}

/// <summary>The name of a group and the places its values are drawn over, what the sentence naming one of its values is made from.</summary>
/// <param name="Label">The group's name.</param>
/// <param name="Places">The places the group's values are drawn over.</param>
internal readonly record struct LabelledPlaces<TPlaces>(string Label, TPlaces Places);
