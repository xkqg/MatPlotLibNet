// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace MatPlotLibNet.Numerics;

/// <summary>One row of a stem-and-leaf display: a stem and the leaves that belong to it, left to right in the order of the values.</summary>
/// <param name="Stem">The stem as written: the digits above the leaf, with a minus sign for a negative value (a stem of "-0" holds
/// the negative values smaller than ten leaf units).</param>
/// <param name="Leaves">The leaf digits, 0 to 9, one for each observation on the stem, a repeated value giving a repeated leaf.</param>
internal readonly record struct StemRow(string Stem, IReadOnlyList<int> Leaves);

/// <summary>What a sample draws as a stem-and-leaf display.</summary>
/// <param name="Exponent">The power of ten the leaf unit is: a leaf is one unit of 10 to this power.</param>
/// <param name="Rows">The rows from the smallest stem to the largest, a stem nobody falls on being a row of no leaves.</param>
internal readonly record struct StemAndLeafDisplay(int Exponent, IReadOnlyList<StemRow> Rows)
{
    /// <summary>The value a leaf unit is, written exactly (decimal, so 0.1 is 0.1).</summary>
    internal decimal LeafUnit => StemAndLeafExtensions.UnitOf(Exponent);
}

/// <summary>The one owner of how a sample becomes a stem-and-leaf display. A value is cut at the leaf unit (a power of ten) without
/// rounding, toward zero: the last digit of what is left is the leaf and the digits above it the stem, so 1.29 at a unit of 0.1
/// is a 2 on the stem 1. A value counts as its truncation within a billionth of a unit, so a decimal unit does not lose a digit
/// to the binary error of 1.2 / 0.1. The rows run from the smallest stem to the largest with the empty stems between them, the
/// negative stems before the stem "-0", then "0"; within a stem the leaves follow the order of the values. Without a unit the
/// finest power of ten is taken whose rows do not outnumber twice the square root of the observations (and at least five)
/// while its stems have no more than two digits.</summary>
internal static class StemAndLeafExtensions
{
    /// <summary>The smallest and the largest power of ten a leaf unit may be.</summary>
    private const int LowestExponent = -15, HighestExponent = 15;

    /// <summary>The powers of ten from 10^-15 to 10^15 as the literals the language reads them as, so a value written 0.001
    /// is the same double as the power of ten it is compared with.</summary>
    private static readonly double[] PowersOfTen =
    [
        1e-15, 1e-14, 1e-13, 1e-12, 1e-11, 1e-10, 1e-9, 1e-8, 1e-7, 1e-6, 1e-5, 1e-4, 1e-3, 1e-2, 1e-1,
        1e0, 1e1, 1e2, 1e3, 1e4, 1e5, 1e6, 1e7, 1e8, 1e9, 1e10, 1e11, 1e12, 1e13, 1e14, 1e15,
    ];

    /// <summary>The leaf unit of a power of ten, written exactly.</summary>
    /// <param name="exponent">The power of ten.</param>
    /// <returns>10 to the power, exact in a decimal.</returns>
    internal static decimal UnitOf(int exponent)
    {
        decimal unit = 1m;
        for (int i = 0; i < Math.Abs(exponent); i++) unit = exponent > 0 ? unit * 10m : unit / 10m;
        return unit;
    }

    /// <summary>The stem a value falls on and its leaf at the unit 10 to <paramref name="exponent"/>, cut toward zero. A negative
    /// value keeps its sign even when it is cut to nothing, so it stands on "-0".</summary>
    private static StemSlot SlotOf(double value, int exponent)
    {
        double scaled = Math.Abs(value) / PowersOfTen[exponent - LowestExponent];
        if (!(scaled < MaxScaled))
        {
            return new StemSlot(0, false, -1);
        }

        long cut = (long)Math.Floor(scaled + Tolerance);
        return new StemSlot(cut / 10, value < 0, (int)(cut % 10));
    }

    /// <summary>A billionth of a unit: how far below a whole unit a scaled value may be and still count as it.</summary>
    private const double Tolerance = 1e-9;

    /// <summary>The scaled value past which a cut no longer fits a 64-bit integer with room for the stem.</summary>
    private const double MaxScaled = 9e17;

    /// <summary>Where a value falls: its stem's digits, whether the stem is negative, its leaf (-1 for a value too large to cut).</summary>
    private readonly record struct StemSlot(long Digits, bool Negative, int Leaf)
    {
        /// <summary>The place of the stem in the order of the rows: ... -2, -1, "-0", 0, 1, 2 ... as consecutive whole numbers.</summary>
        internal long Place => Negative ? -Digits - 1 : Digits;

        internal bool Fits => Leaf >= 0;
    }

    extension(double[] sample)
    {
        /// <summary>The display of the finite values of the sample.</summary>
        /// <param name="leafUnit">The leaf unit, a power of ten, or <see langword="null"/> to choose one.</param>
        /// <returns>The rows and the unit.</returns>
        /// <exception cref="ArgumentException">The sample holds no finite number.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="leafUnit"/> is not a power of ten, or is too fine or too coarse for the data.</exception>
        internal StemAndLeafDisplay StemAndLeaf(double? leafUnit)
        {
            double[] values = [.. sample.Where(double.IsFinite).Order()];
            if (values.Length == 0)
            {
                throw new ArgumentException("The sample holds no finite number to put on a stem.", nameof(sample));
            }

            int exponent = leafUnit is { } unit ? ExponentOf(unit) : ChosenExponent(values);
            return Display(values, exponent);
        }
    }

    /// <summary>The power of ten that <paramref name="unit"/> is.</summary>
    private static int ExponentOf(double unit)
    {
        int at = Array.IndexOf(PowersOfTen, unit);
        return at >= 0
            ? at + LowestExponent
            : throw new ArgumentOutOfRangeException("leafUnit", unit, "A leaf unit is a power of ten, 10^-15 to 10^15.");
    }

    /// <summary>The finest power of ten within the budget of rows whose stems have no more than two digits.</summary>
    private static int ChosenExponent(double[] values)
    {
        double largest = Math.Max(Math.Abs(values[0]), Math.Abs(values[^1]));
        int leading = 0;
        for (int e = LowestExponent; e <= HighestExponent; e++)
        {
            if (PowersOfTen[e - LowestExponent] <= largest) leading = e;
        }

        int budget = Math.Max(5, 2 * (int)Math.Ceiling(Math.Sqrt(values.Length)));
        for (int e = Math.Max(LowestExponent, leading - 2); e <= HighestExponent; e++)
        {
            if (RowCount(values, e) <= budget) return e;
        }

        return HighestExponent;
    }

    /// <summary>How many rows the sample takes at the unit 10 to <paramref name="exponent"/>.</summary>
    private static long RowCount(double[] values, int exponent)
    {
        long low = long.MaxValue, high = long.MinValue;
        foreach (double value in values)
        {
            var slot = SlotOf(value, exponent);
            if (!slot.Fits) return long.MaxValue;
            low = Math.Min(low, slot.Place);
            high = Math.Max(high, slot.Place);
        }

        return high - low + 1;
    }

    /// <summary>The rows of the sample at the unit 10 to <paramref name="exponent"/>.</summary>
    private static StemAndLeafDisplay Display(double[] sortedValues, int exponent)
    {
        var leavesAt = new SortedDictionary<long, List<int>>();
        foreach (double value in sortedValues)
        {
            var slot = SlotOf(value, exponent);
            if (!slot.Fits)
            {
                throw new ArgumentOutOfRangeException("leafUnit", PowersOfTen[exponent - LowestExponent], "The leaf unit is too fine for the largest value of the sample.");
            }

            if (!leavesAt.TryGetValue(slot.Place, out var leaves)) leavesAt[slot.Place] = leaves = [];
            leaves.Add(slot.Leaf);
        }

        long first = leavesAt.Keys.First(), last = leavesAt.Keys.Last();
        var rows = new List<StemRow>();
        for (long place = first; place <= last; place++)
        {
            bool negative = place < 0;
            long digits = negative ? -place - 1 : place;
            string stem = (negative ? "-" : "") + digits.ToString(CultureInfo.InvariantCulture);
            // The values were visited in ascending order, so the leaves of a stem already stand in the order of the values.
            rows.Add(new StemRow(stem, leavesAt.TryGetValue(place, out var found) ? found : []));
        }

        return new StemAndLeafDisplay(exponent, rows);
    }
}
