// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace MatPlotLibNet.Models.Series;

/// <summary>The estimate a survival chart draws from a <see cref="SurvivalGroup"/>, and the check the group is held to, in
/// one place.</summary>
internal static class SurvivalGroupExtensions
{
    extension(SurvivalGroup group)
    {
        /// <summary>The product-limit (Kaplan-Meier) estimate of this group: S(t) is the product, over the event
        /// times t_i at or before t, of (1 - d_i / n_i), where d_i is the number of events at t_i and n_i the number of subjects
        /// at risk just before t_i (those whose duration is t_i or later).
        /// <para>All the events at one time are counted together, so a tie is one step. A subject censored at an event time is
        /// still at risk at it: the events at a time are counted before the censorings, as R's survival package and lifelines
        /// count them. The curve starts at (0, 1), has a point at every event time at the level it drops to there, and, when the
        /// last follow-up is a censoring after the last event, a last point there at the last level, so the curve runs on to the
        /// end of the follow-up.</para>
        /// <para>The arithmetic is whole-number counts, one subtraction, one division and a running product: each level is the
        /// one before times (n_i - d_i) / n_i. There is no logarithm or exponential, so the levels are the same bits on every
        /// platform.</para></summary>
        /// <param name="paramName">The caller's parameter that holds the group, named in the exception.</param>
        /// <returns>The points of the step curve, read "steps-post": each level holds from its point to the next.</returns>
        /// <exception cref="ArgumentException">The group carries no durations or no flags, the two do not pair up, it has no
        /// subjects, or a duration is not a finite time at or above 0; the message names the group.</exception>
        internal SurvivalCurve ProductLimit(string paramName)
        {
            double[] durations = group.ThrowIfNotSurvivalData(paramName);
            bool[] observed = group.Observed;

            // Ascending by duration; a stable sort, so the order given does not matter: a time's events are counted together.
            int[] order = [.. Enumerable.Range(0, durations.Length).OrderBy(i => durations[i])];
            var times = new List<double> { 0 };
            var survival = new List<double> { 1 };
            var atRiskPoints = new List<int> { durations.Length };
            var eventsPoints = new List<int> { 0 };
            double level = 1;
            int atRisk = durations.Length;
            int atRiskAtLastTime = atRisk;
            for (int k = 0; k < order.Length;)
            {
                double time = durations[order[k]];
                int events = 0, leaving = 0;
                for (; k < order.Length && durations[order[k]] == time; k++, leaving++)
                {
                    if (observed[order[k]]) events++;
                }

                // Every subject leaving at this time, censored or not, is at risk at it: the events are counted first.
                atRiskAtLastTime = atRisk;
                if (events > 0)
                {
                    level *= (double)(atRisk - events) / atRisk;
                    times.Add(time);
                    survival.Add(level);
                    atRiskPoints.Add(atRisk);
                    eventsPoints.Add(events);
                }

                atRisk -= leaving;
            }

            // A censoring after the last event: the curve runs on at its last level to the end of the follow-up. At risk
            // here is the count GOING INTO that time (before the subjects there leave), the same number an event would
            // have used had there been one, not what is left afterward (0, if everyone still standing leaves together).
            double lastFollowUp = durations[order[^1]];
            if (lastFollowUp > times[^1])
            {
                times.Add(lastFollowUp);
                survival.Add(level);
                atRiskPoints.Add(atRiskAtLastTime);
                eventsPoints.Add(0);
            }

            return new SurvivalCurve([.. times], [.. survival], [.. atRiskPoints], [.. eventsPoints]);
        }

        /// <summary>Refuses a group that is not survival data: no durations or no flags, the two not paired one to one, no
        /// subjects, or a duration that is not a finite time at or above 0 (a subject without a time cannot be counted at
        /// risk).</summary>
        /// <returns>The group's durations, for chaining.</returns>
        /// <exception cref="ArgumentException">The group is not survival data; the message names the group.</exception>
        private double[] ThrowIfNotSurvivalData(string paramName)
        {
            var (label, durations, observed) = group;
            ArgumentException Refused(string message) => new($"Group '{label}' {message}", paramName);

            if (durations is null) throw Refused("carries no durations.");
            if (observed is null) throw Refused("carries no Observed flags.");
            if (durations.Length != observed.Length)
            {
                throw Refused($"has {durations.Length} durations and {observed.Length} Observed flags; there must be one flag per duration.");
            }

            if (durations.Length == 0) throw Refused("has no subjects; a survival curve is estimated from 1 or more.");
            for (int i = 0; i < durations.Length; i++)
            {
                if (!(durations[i] >= 0 && double.IsFinite(durations[i])))
                {
                    throw Refused($"has a duration of {durations[i].ToString(CultureInfo.InvariantCulture)} at index {i}; a duration is a finite time at or above 0 (a subject without a time cannot be counted at risk).");
                }
            }

            return durations;
        }
    }
}
