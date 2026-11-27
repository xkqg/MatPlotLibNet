// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Models.Series;

/// <summary>The check every verb estimated from a sample holds it to, in one place, so each verb refuses a sample it cannot
/// estimate from in the same words, and leaves out the same observations: a missing one (NaN) and an infinity
/// (<see cref="MissingValueExtensions.DrawableValues"/>), as a box, a violin and a density curve do, and sorts them by the very rule
/// they are sorted by (<see cref="MissingValueExtensions.SortedDrawableValues"/>). A verb takes its samples
/// as <see cref="NamedSample"/> groups (a ridgeline) or as one bare sample (a Q-Q plot).</summary>
internal static class NamedSampleExtensions
{
    extension(NamedSample sample)
    {
        /// <summary>The observations of this group a chart can estimate from, in ascending order, refusing a
        /// group that has fewer than <paramref name="atLeast"/> of them.</summary>
        /// <param name="atLeast">How many observations the estimate needs (a density needs two).</param>
        /// <param name="paramName">The caller's parameter that holds the group, named in the exception.</param>
        /// <returns>The drawable observations, sorted ascending.</returns>
        /// <exception cref="ArgumentException">The group carries no values at all (its array is <see langword="null"/>), or
        /// fewer drawable observations than <paramref name="atLeast"/>; the message names the group.</exception>
        internal double[] SortedObservations(int atLeast, string paramName)
        {
            if (sample.Values is null)
            {
                throw new ArgumentException($"Group '{sample.Label}' carries no values.", paramName);
            }

            return sample.Values.SortedObservations(atLeast, $"Group '{sample.Label}'", "a density estimate", paramName);
        }
    }

    extension(double[] sample)
    {
        /// <summary>The observations of this bare sample a chart can estimate from, in ascending order, refusing
        /// a sample that has fewer than <paramref name="atLeast"/> of them.</summary>
        /// <param name="atLeast">How many observations the chart needs.</param>
        /// <param name="whose">Whose observations they are, as the message opens: <c>Group 'North'</c>, <c>The sample</c>.</param>
        /// <param name="taker">What needs them, as the message names it before the number it needs: <c>a density estimate</c>.</param>
        /// <param name="paramName">The caller's parameter that holds the sample, named in the exception.</param>
        /// <returns>The drawable observations, sorted ascending.</returns>
        /// <exception cref="ArgumentException">The sample has fewer drawable observations than <paramref name="atLeast"/>.</exception>
        internal double[] SortedObservations(int atLeast, string whose, string taker, string paramName)
        {
            double[] observations = sample.SortedDrawableValues();
            observations.Length.ThrowIfFewerThan(atLeast, paramName, Drawable(whose, taker));
            return observations;
        }

        /// <summary>The observations of this bare sample a chart can estimate from, in the order given (a chart
        /// that reads the sample in its order, such as a control chart's moving range), refusing a sample that has fewer than
        /// <paramref name="atLeast"/> of them in the words the other <c>SortedObservations</c> overload uses.</summary>
        /// <param name="atLeast">How many observations the chart needs.</param>
        /// <param name="whose">Whose observations they are, as the message opens: <c>The sample</c>.</param>
        /// <param name="taker">What needs them, as the message names it before the number it needs.</param>
        /// <param name="paramName">The caller's parameter that holds the sample, named in the exception.</param>
        /// <returns>The drawable observations, in the order given; the very array when all of them can be drawn.</returns>
        /// <exception cref="ArgumentException">The sample has fewer drawable observations than <paramref name="atLeast"/>.</exception>
        internal double[] Observations(int atLeast, string whose, string taker, string paramName)
        {
            double[] observations = sample.DrawableValues();
            observations.Length.ThrowIfFewerThan(atLeast, paramName, Drawable(whose, taker));
            return observations;
        }
    }

    /// <summary>The words both overloads refuse too few observations in: the ones that can be drawn are counted.</summary>
    private static DataNeed Drawable(string whose, string taker) => new(whose, "observation", taker, "NaN and the infinities are not counted");
}
