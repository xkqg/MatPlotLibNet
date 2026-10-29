// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet;

/// <summary>What a Shewhart chart (<see cref="XYAxesExtensions.ShewhartChart"/>) plots: the statistic as points along the X
/// axis, and how the centre line and the control limits of that statistic are estimated from the data when none are given.
/// One factory for each kind of chart; each makes the points, owns its estimator and its constants, and checks what it needs of
/// its data.
/// <para>A value of this type can only be made by a factory: it has no public constructor, and its properties have no
/// setters. <c>default</c> holds nothing and is refused by the verb.</para></summary>
public readonly record struct ControlChartData
{
    private ControlChartData(double[] x, double[] values, Func<ControlLimits> estimate)
    {
        X = x;
        Values = values;
        Estimate = estimate;
    }

    /// <summary>Where each plotted point stands along the X axis.</summary>
    internal double[] X { get; }

    /// <summary>The plotted statistic, one value for each position; a missing value (NaN) breaks the line.</summary>
    internal double[] Values { get; }

    /// <summary>Estimates the centre line and the control limits from the data: run only when the caller gives none.</summary>
    internal Func<ControlLimits> Estimate { get; }

    /// <summary>Whether a factory made this value.</summary>
    internal bool IsMade => X is not null;

    /// <summary>The individuals chart (the X chart of an I-MR pair, a Shewhart chart of single values): the values themselves,
    /// in the order they were taken. The centre is the mean of the values; sigma is the average moving range (the mean
    /// distance between each value and the one before it) divided by d2 = 1.128, the constant for ranges of two; and the limits
    /// stand three sigma below and above the centre. A value that is missing (NaN) or infinite is left out of the estimate (the
    /// moving range is then taken between the values on either side of it).</summary>
    /// <param name="x">The position of each value along the X axis (a sample number, a day), in the order the values were
    /// taken.</param>
    /// <param name="values">The values, one per x position, in the order they were taken. Without limits given to the verb, at
    /// least two that can be drawn.</param>
    /// <returns>The data.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="x"/> or <paramref name="values"/> is
    /// <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="values"/> does not carry one value per x position (the message names
    /// both counts).</exception>
    public static ControlChartData Individuals(double[] x, double[] values)
    {
        ArgumentNullException.ThrowIfNull(x);
        values.ThrowIfMisaligned(x.Length, nameof(values), "x positions", "x position");
        return new ControlChartData(x, values, () => IndividualsLimits(values));
    }

    /// <summary>The moving range chart (the MR chart of an I-MR pair): the distance between each value and the one before it,
    /// plotted at the position of the later of the two. The centre is the average moving range, the upper limit is D4 = 3.267
    /// times it (the constant for ranges of two) and the lower limit is 0, since a range cannot be negative. The average is the
    /// very one the individuals chart divides by d2 to estimate its sigma. A value that is missing (NaN) or infinite is left out
    /// and the range spans its gap.</summary>
    /// <param name="x">The position of each value along the X axis (a sample number, a day), in the order the values were
    /// taken.</param>
    /// <param name="values">The values, one per x position, in the order they were taken; at least two that can be drawn, with or
    /// without limits given to the verb, since there is no range to plot from fewer.</param>
    /// <returns>The data.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="x"/> or <paramref name="values"/> is
    /// <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="values"/> does not carry one value per x position (the message names
    /// both counts), or fewer than two of them can be drawn.</exception>
    public static ControlChartData MovingRange(double[] x, double[] values)
    {
        ArgumentNullException.ThrowIfNull(x);
        values.ThrowIfMisaligned(x.Length, nameof(values), "x positions", "x position");
        values.Observations(MovingRangeObservations, "The sample", "a moving range chart is drawn from", nameof(values));

        // The values that can be drawn, with the range from each to the one before it.
        int[] there = [.. Enumerable.Range(0, values.Length).Where(i => double.IsFinite(values[i]))];
        double[] ranges = [.. there.Skip(1).Select((i, k) => Math.Abs(values[i] - values[there[k]]))];
        double[] at = [.. there.Skip(1).Select(i => x[i])];
        return new ControlChartData(at, ranges, () => MovingRangeLimits(ranges));
    }

    /// <summary>The limits of the moving range chart: the centre is the average of the ranges (summed in order, as the individuals
    /// chart sums them), the upper limit D4 times it, the lower limit 0.</summary>
    private static ControlLimits MovingRangeLimits(double[] ranges)
    {
        double sum = 0;
        foreach (double range in ranges) sum += range;

        double mean = sum / ranges.Length;
        return new ControlLimits(mean, 0, ControlChartConstants.MovingRangeD4 * mean);
    }

    /// <summary>The X-bar chart: the mean of each subgroup, plotted at the subgroup's number from 1. The centre is the grand mean
    /// (the mean of the subgroup means); the limits stand A2 times the average subgroup range either side of it, A2 being the
    /// constant for the subgroup size (<c>3 / (d2 sqrt(n))</c>, the three-sigma limits of a mean of n when sigma is estimated by the
    /// average range over d2). The range chart of the same subgroups (<see cref="Range"/>) estimates from the same average range.
    /// A subgroup with a missing observation (NaN) or an infinity is missing as a whole: a gap in the line, and left out of the
    /// averages.</summary>
    /// <param name="subgroups">The subgroups, each an array of its observations, in the order they were taken: all of one size,
    /// from 2 to 10 (a subgroup of one has no range, and the constants are tabulated to ten), and at least one complete.</param>
    /// <returns>The data.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="subgroups"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">There are no subgroups; a subgroup is <see langword="null"/>; the subgroups differ in
    /// size or their size is not from 2 to 10; or none has all its observations (the message names which).</exception>
    public static ControlChartData XBar(double[][] subgroups)
    {
        var measured = Measure(subgroups);
        return new ControlChartData(measured.Positions, measured.Means, () =>
        {
            double grand = MeanOfPlaced(measured.Means), range = MeanOfPlaced(measured.Ranges);
            var constants = ControlChartConstants.ForSubgroup(measured.Size);
            return new ControlLimits(grand, grand - constants.A2 * range, grand + constants.A2 * range);
        });
    }

    /// <summary>The range chart: the range of each subgroup (its largest observation less its smallest), plotted at the subgroup's
    /// number from 1. The centre is the average range; the limits are D4 and D3 times it, the constants for the subgroup size
    /// (D3 is 0 for subgroups below 7: a range cannot be negative). A subgroup with a missing observation (NaN) or an infinity is
    /// missing as a whole: a gap in the line, and left out of the average.</summary>
    /// <param name="subgroups">The subgroups, each an array of its observations, in the order they were taken: all of one size,
    /// from 2 to 10, and at least one complete.</param>
    /// <returns>The data.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="subgroups"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">There are no subgroups; a subgroup is <see langword="null"/>; the subgroups differ in
    /// size or their size is not from 2 to 10; or none has all its observations (the message names which).</exception>
    public static ControlChartData Range(double[][] subgroups)
    {
        var measured = Measure(subgroups);
        return new ControlChartData(measured.Positions, measured.Ranges, () =>
        {
            double range = MeanOfPlaced(measured.Ranges);
            var constants = ControlChartConstants.ForSubgroup(measured.Size);
            return new ControlLimits(range, constants.D3 * range, constants.D4 * range);
        });
    }

    /// <summary>The p chart: the proportion of defective items in each sample, plotted at the sample's number from 1, for samples
    /// of one size. The centre is the pooled proportion (all defectives over all items); sigma is <c>sqrt(p (1 - p) / n)</c>, the
    /// binomial standard deviation of a proportion of n; the limits stand three sigma either side of the centre, the lower one at
    /// 0 when it would be negative (a proportion cannot be). A sample with no result (NaN) is a gap and is left out of the
    /// pooled proportion.</summary>
    /// <param name="defectives">The number of defective items in each sample, in the order the samples were taken: each from 0 to
    /// <paramref name="sampleSize"/>, or NaN for a sample with no result; at least one with a result.</param>
    /// <param name="sampleSize">The number of items in every sample, 1 or more.</param>
    /// <returns>The data.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="defectives"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="sampleSize"/> is below 1.</exception>
    /// <exception cref="ArgumentException">A sample has fewer than 0 or more than <paramref name="sampleSize"/> defectives, or
    /// no sample has a result.</exception>
    public static ControlChartData P(double[] defectives, int sampleSize)
    {
        ArgumentNullException.ThrowIfNull(defectives);
        if (sampleSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleSize), sampleSize, "A sample holds 1 item or more.");
        }

        for (int i = 0; i < defectives.Length; i++)
        {
            double count = defectives[i];
            if (!double.IsNaN(count) && !(count >= 0 && count <= sampleSize))
            {
                throw new ArgumentException(
                    $"Sample {i + 1} has {count.ToString(CultureInfo.InvariantCulture)} defectives; a sample of {sampleSize} has between 0 and {sampleSize}.",
                    nameof(defectives));
            }
        }

        if (!defectives.Any(count => !double.IsNaN(count)))
        {
            throw new ArgumentException("No sample has a result; a p chart is drawn from 1 or more (NaN is a sample with no result).", nameof(defectives));
        }

        double[] proportions = [.. defectives.Select(count => count / sampleSize)];
        return new ControlChartData(Positions(defectives.Length), proportions, () =>
        {
            double pooled = MeanOfPlaced(proportions);
            double sigma = Math.Sqrt(pooled * (1 - pooled) / sampleSize);
            return new ControlLimits(pooled, Math.Max(0, pooled - ControlChartConstants.Sigmas * sigma), pooled + ControlChartConstants.Sigmas * sigma);
        });
    }

    /// <summary>The c chart: the number of defects counted in each sample, plotted at the sample's number from 1, for samples of
    /// one size (an area, a length, a unit). The centre is the average count; sigma is its square root, the Poisson standard
    /// deviation of a count; the limits stand three sigma either side of the centre, the lower one at 0 when it would be negative
    /// (a count cannot be). A sample with no count (NaN) is a gap and is left out of the average.</summary>
    /// <param name="counts">The number of defects in each sample, in the order the samples were taken: each a finite number of 0
    /// or more, or NaN for a sample with no count; at least one with a count.</param>
    /// <returns>The data.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="counts"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A count is negative or infinite, or no sample has a count.</exception>
    public static ControlChartData C(double[] counts)
    {
        ArgumentNullException.ThrowIfNull(counts);
        for (int i = 0; i < counts.Length; i++)
        {
            if (!double.IsNaN(counts[i]) && !(double.IsFinite(counts[i]) && counts[i] >= 0))
            {
                throw new ArgumentException(
                    $"Sample {i + 1} has a count of {counts[i].ToString(CultureInfo.InvariantCulture)}; a count is a finite number of 0 or more.",
                    nameof(counts));
            }
        }

        if (!counts.Any(count => !double.IsNaN(count)))
        {
            throw new ArgumentException("No sample has a count; a c chart is drawn from 1 or more (NaN is a sample with no count).", nameof(counts));
        }

        return new ControlChartData(Positions(counts.Length), counts, () =>
        {
            double average = MeanOfPlaced(counts);
            double sigma = Math.Sqrt(average);
            return new ControlLimits(average, Math.Max(0, average - ControlChartConstants.Sigmas * sigma), average + ControlChartConstants.Sigmas * sigma);
        });
    }

    /// <summary>What a set of subgroups gives a chart: their numbers, their means and ranges (NaN for a subgroup that is not
    /// complete), and the size they share.</summary>
    /// <param name="Positions">The subgroup numbers from 1.</param>
    /// <param name="Means">The mean of each subgroup; NaN when it has a missing observation or an infinity.</param>
    /// <param name="Ranges">The range of each subgroup; NaN likewise.</param>
    /// <param name="Size">The number of observations in every subgroup.</param>
    private readonly record struct SubgroupStatistics(double[] Positions, double[] Means, double[] Ranges, int Size);

    /// <summary>Checks the subgroups and works out their means and ranges.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="subgroups"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">There are no subgroups; one is <see langword="null"/>; they differ in size or their
    /// size has no constants; or none is complete.</exception>
    private static SubgroupStatistics Measure(double[][] subgroups)
    {
        ArgumentNullException.ThrowIfNull(subgroups);
        if (subgroups.Length == 0)
        {
            throw new ArgumentException("No subgroups were given; a control chart for subgroups is drawn from 1 or more.", nameof(subgroups));
        }

        for (int i = 0; i < subgroups.Length; i++)
        {
            if (subgroups[i] is null)
            {
                throw new ArgumentException($"Subgroup {i + 1} is missing (null); every subgroup holds its observations.", nameof(subgroups));
            }
        }

        int size = subgroups[0].Length;
        for (int i = 1; i < subgroups.Length; i++)
        {
            if (subgroups[i].Length != size)
            {
                throw new ArgumentException(
                    $"Subgroup {i + 1} holds {Observations(subgroups[i].Length)} and subgroup 1 holds {Observations(size)}; the limits of the chart are for one subgroup size.",
                    nameof(subgroups));
            }
        }

        if (size < ControlChartConstants.SmallestSubgroup || size > ControlChartConstants.LargestSubgroup)
        {
            throw new ArgumentException(
                $"The subgroups hold {Observations(size)} each; the constants of a control chart for subgroups are tabulated for {ControlChartConstants.SmallestSubgroup} to {ControlChartConstants.LargestSubgroup}.",
                nameof(subgroups));
        }

        double[] means = [.. subgroups.Select(s => s.All(double.IsFinite) ? s.Sum() / size : double.NaN)];
        double[] ranges = [.. subgroups.Select(s => s.All(double.IsFinite) ? s.Max() - s.Min() : double.NaN)];
        if (means.All(double.IsNaN))
        {
            throw new ArgumentException("No subgroup has all its observations; a missing value (NaN) or an infinity leaves its subgroup out.", nameof(subgroups));
        }

        return new SubgroupStatistics(Positions(subgroups.Length), means, ranges, size);
    }

    /// <summary>The numbers 1 to <paramref name="count"/>, where the points of a chart of samples stand.</summary>
    private static double[] Positions(int count) => [.. Enumerable.Range(1, count).Select(i => (double)i)];

    /// <summary>The mean of the values that can be placed, summed in order.</summary>
    private static double MeanOfPlaced(double[] values)
    {
        double sum = 0;
        int placed = 0;
        foreach (double value in values)
        {
            if (!double.IsFinite(value)) continue;
            sum += value;
            placed++;
        }

        return sum / placed;
    }

    /// <summary>"1 observation" or "n observations".</summary>
    private static string Observations(int count) => count == 1 ? "1 observation" : $"{count} observations";

    /// <summary>The limits of the individuals chart estimated from <paramref name="values"/>: the centre is the mean of the
    /// values that can be drawn, in their order; sigma is the average moving range between neighbouring ones over
    /// <see cref="ControlChartConstants.MovingRangeD2"/>; the limits stand <see cref="ControlChartConstants.Sigmas"/> sigma
    /// either side of the centre.</summary>
    /// <exception cref="ArgumentException">Fewer than two values can be drawn: a moving range is the distance between two.</exception>
    private static ControlLimits IndividualsLimits(double[] values)
    {
        double[] there = values.Observations(MovingRangeObservations, "The sample", "the limits of a control chart are estimated from", nameof(values));
        double sum = 0, ranges = 0;
        for (int i = 0; i < there.Length; i++)
        {
            sum += there[i];
            if (i > 0) ranges += Math.Abs(there[i] - there[i - 1]);
        }

        double center = sum / there.Length;
        double sigma = ranges / (there.Length - 1) / ControlChartConstants.MovingRangeD2;
        return new ControlLimits(center, center - ControlChartConstants.Sigmas * sigma, center + ControlChartConstants.Sigmas * sigma);
    }

    /// <summary>How many values the limits are estimated from at the least: a moving range is the distance between two.</summary>
    private const int MovingRangeObservations = 2;
}
