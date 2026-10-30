// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Models.Series;

/// <summary>Base class for series drawn in a ternary diagram: one composition of three parts for each sample, the part at the
/// apex (<see cref="Top"/>), at the bottom left (<see cref="Left"/>) and at the bottom right (<see cref="Right"/>). The amounts
/// are kept as given, a count, a percentage or a mass, and are closed to fractions only to be placed
/// (<see cref="TernaryPointExtensions.Closed"/>), so the table shows what the author knows and the picture the proportions.
/// The place a series stands in is fixed (<see cref="TernaryFrame"/>), so it contributes no range to any axis.</summary>
public abstract class TernarySeries : ChartSeries
{
    /// <summary>The part at the apex, for each sample.</summary>
    public double[] Top { get; }

    /// <summary>The part at the bottom left corner, for each sample.</summary>
    public double[] Left { get; }

    /// <summary>The part at the bottom right corner, for each sample.</summary>
    public double[] Right { get; }

    /// <summary>Initializes with the three parts of every sample.</summary>
    /// <param name="top">The part at the apex, for each sample.</param>
    /// <param name="left">The part at the bottom left corner, for each sample.</param>
    /// <param name="right">The part at the bottom right corner, for each sample.</param>
    protected TernarySeries(double[] top, double[] left, double[] right)
    {
        Top = top;
        Left = left;
        Right = right;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) => new(null, null, null, null);

    /// <inheritdoc />
    /// <remarks>One row for each sample: its three parts as given, and their sum, which a closed composition no longer shows.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        int count = Math.Min(Top.Length, Math.Min(Left.Length, Right.Length));
        double[] sum = new double[count];
        for (int i = 0; i < count; i++)
            sum[i] = Top[i] + Left[i] + Right[i];

        return ChartDataTable.FromNumberColumns([new("top"), new("left"), new("right"), new("sum")], [Top, Left, Right, sum]);
    }

    /// <summary>Where each sample that can be placed stands in the frame, in the order given. A sample that cannot (a negative,
    /// infinite or missing part, or three zeros) is left out: the verbs refuse such a sample, so this is only a series built
    /// directly, which must not take a figure down when it is drawn.</summary>
    /// <returns>The row and the place of each sample that can be placed.</returns>
    internal TernaryMark[] FramePoints()
    {
        int count = Math.Min(Top.Length, Math.Min(Left.Length, Right.Length));
        var marks = new List<TernaryMark>(count);
        for (int row = 0; row < count; row++)
        {
            if (new TernaryPoint(Top[row], Left[row], Right[row]).TryClosed(out var closed))
                marks.Add(new TernaryMark(row, closed.InFrame()));
        }

        return [.. marks];
    }

    /// <summary>A sample as a reader says it: each part as a share of the whole, under the name the axes gives that part, "top",
    /// "left" or "right" where it names none. The hover tooltip and a pinned point show it, since the position a sample has in
    /// the frame means nothing to the reader.</summary>
    /// <param name="row">The sample.</param>
    /// <param name="axes">The axes the series stands on, which names the parts by the labels of its axes.</param>
    /// <returns>For example "Clay 20 %, Silt 50 %, Sand 30 %".</returns>
    internal string Describe(int row, Axes axes)
    {
        var closed = new TernaryPoint(Top[row], Left[row], Right[row]).Closed();
        static string Share(double fraction) => (fraction * 100).ToString("0.#", CultureInfo.InvariantCulture);
        return $"{axes.YAxis.Label ?? "top"} {Share(closed.Top)} %, {axes.ZAxis.Label ?? "left"} {Share(closed.Left)} %, {axes.XAxis.Label ?? "right"} {Share(closed.Right)} %";
    }

    /// <summary>Refuses data that the verbs cannot build a series from: arrays of different lengths, and a sample that cannot
    /// be placed, named by its row and its part. A document read from JSON builds its series through the verbs, so one with such
    /// a sample is refused when it is read and never reaches a draw.</summary>
    /// <param name="top">The part at the apex, for each sample.</param>
    /// <param name="left">The part at the bottom left corner, for each sample.</param>
    /// <param name="right">The part at the bottom right corner, for each sample.</param>
    /// <exception cref="ArgumentException">The lengths differ, or a sample cannot be placed; the message names the row.</exception>
    internal static void RefuseUnplaceable(double[] top, double[] left, double[] right)
    {
        top.ThrowIfMisaligned(left);
        top.ThrowIfMisaligned(right);
        for (int row = 0; row < top.Length; row++)
        {
            try
            {
                new TernaryPoint(top[row], left[row], right[row]).Closed();
            }
            catch (ArgumentException refusal)
            {
                throw new ArgumentException($"Row {row} cannot be placed in a ternary diagram. {refusal.Message}", refusal);
            }
        }
    }
}

/// <summary>One sample that was placed: its row in the series and its place in the frame.</summary>
/// <param name="Row">The sample's position in the series.</param>
/// <param name="Position">Where its closed composition stands in the frame (<see cref="TernaryFrame"/>).</param>
internal readonly record struct TernaryMark(int Row, Point Position);
