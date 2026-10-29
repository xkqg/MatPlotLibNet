// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Numerics;

/// <summary>The outcome of a log-rank test, as a risk table (<see cref="DistributionAxesExtensions.RiskTable"/>) hands it out.</summary>
/// <param name="Statistic">The chi-square statistic; NaN when there is nothing to compare (fewer than two groups, or no event
/// in any), or the groups cannot be told apart by a quadratic form (a group never at risk where another has events).</param>
/// <param name="DegreesOfFreedom">The degrees of freedom: the number of groups less one, 0 for fewer than two groups.</param>
/// <param name="PValue">The chance of a statistic at least as large if the groups' survival were the same (the chi-square
/// survival function at <paramref name="Statistic"/>); NaN when the statistic is.</param>
public readonly record struct LogRankResult(double Statistic, int DegreesOfFreedom, double PValue)
{
    /// <summary>The p-value from which a chart prints it in full; below it, it reads "p &lt; 0.0001".</summary>
    private const double PrintedInFullFrom = 0.0001;

    /// <summary>The result as the line a chart states it in: <c>Log-rank χ²(1) = 16.79, p &lt; 0.0001</c> (the statistic to two
    /// decimals, the p-value to four), or <c>Log-rank test: no events to compare</c> when there is no statistic. Numbers are
    /// written the same in every culture.</summary>
    /// <returns>The line.</returns>
    internal string Describe()
    {
        if (double.IsNaN(Statistic)) return "Log-rank test: no events to compare";

        string p = PValue < PrintedInFullFrom ? "p < 0.0001" : $"p = {PValue.ToString("0.0000", CultureInfo.InvariantCulture)}";
        return $"Log-rank χ²({DegreesOfFreedom}) = {Statistic.ToString("0.00", CultureInfo.InvariantCulture)}, {p}";
    }
}

/// <summary>The log-rank test of k survival groups: whether their survival differs by more than chance (the Mantel-Cox test,
/// R's <c>survdiff</c> with rho = 0, lifelines' <c>multivariate_logrank_test</c>).
/// <para>At every time an event happens, a group is expected to have its share of the events: <c>d · n_j / n</c>, with
/// <c>n_j</c> the subjects of the group still at risk, <c>n</c> all of them and <c>d</c> the events of all groups at that time.
/// The statistic sums the observed less the expected events over the times, and compares them to their covariance, the
/// hypergeometric one with the correction for ties (<c>d (n - d) / (n - 1)</c>): it is the quadratic form of the observed
/// minus expected events of all groups but the last, which are determined by the others, with k - 1 degrees of freedom.</para></summary>
internal static class LogRank
{
    /// <summary>A group's follow-up, ascending by duration.</summary>
    private readonly record struct Followed(double[] Durations, bool[] Observed);

    /// <summary>Tests whether <paramref name="groups"/> differ in survival.</summary>
    /// <param name="groups">The groups, each with one duration and one flag for every subject (checked by the caller).</param>
    /// <returns>The statistic, its degrees of freedom and its p-value.</returns>
    internal static LogRankResult Test(IReadOnlyList<SurvivalGroup> groups)
    {
        int k = groups.Count;
        int degrees = Math.Max(0, k - 1);
        if (k < 2) return new(double.NaN, degrees, double.NaN);

        var followed = new Followed[k];
        for (int j = 0; j < k; j++)
        {
            int[] order = [.. Enumerable.Range(0, groups[j].Durations.Length).OrderBy(i => groups[j].Durations[i])];
            followed[j] = new([.. order.Select(i => groups[j].Durations[i])], [.. order.Select(i => groups[j].Observed[i])]);
        }

        double[] eventTimes = [.. followed.SelectMany(f => f.Durations.Where((_, i) => f.Observed[i])).Distinct().Order()];
        if (eventTimes.Length == 0) return new(double.NaN, degrees, double.NaN);

        int[] left = new int[k];                 // subjects of each group that left before the time being looked at
        double[] surplus = new double[k];        // observed less expected events
        double[,] covariance = new double[k, k];
        double[] atRisk = new double[k], events = new double[k];
        foreach (double time in eventTimes)
        {
            double n = 0, d = 0;
            for (int j = 0; j < k; j++)
            {
                var f = followed[j];
                while (left[j] < f.Durations.Length && f.Durations[left[j]] < time) left[j]++;
                atRisk[j] = f.Durations.Length - left[j];
                events[j] = 0;
                for (int i = left[j]; i < f.Durations.Length && f.Durations[i] == time; i++)
                {
                    if (f.Observed[i]) events[j]++;
                }

                n += atRisk[j];
                d += events[j];
            }

            for (int j = 0; j < k; j++) surplus[j] += events[j] - d * atRisk[j] / n;
            if (n <= 1) continue;
            double scale = d * (n - d) / (n - 1);
            for (int j = 0; j < k; j++)
                for (int l = 0; l < k; l++)
                    covariance[j, l] += scale * (atRisk[j] / n) * ((j == l ? 1 : 0) - atRisk[l] / n);
        }

        double statistic = QuadraticForm(surplus, covariance, degrees);
        return new(statistic, degrees, ChiSquare.Survival(statistic, degrees));
    }

    /// <summary>The quadratic form u' V⁻¹ u over the first <paramref name="size"/> groups; NaN when V cannot be inverted.</summary>
    private static double QuadraticForm(double[] surplus, double[,] covariance, int size)
    {
        var v = new double[size, size];
        for (int j = 0; j < size; j++)
            for (int l = 0; l < size; l++)
                v[j, l] = covariance[j, l];
        var u = new Vec(surplus[..size]);

        var matrix = new Mat(v);
        if (Linalg.Det(matrix) == 0) return double.NaN;
        var solved = Linalg.Solve(matrix, u);
        double sum = 0;
        for (int j = 0; j < size; j++) sum += u[j] * solved[j];
        return sum;
    }
}
