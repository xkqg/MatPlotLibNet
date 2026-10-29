// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet;

/// <summary>The one check a series whose data arrays must agree in length makes, worded the same way
/// everywhere. A series is validated by the verb that constructs it (<c>Axes.Plot</c>, <c>Axes.Surface</c>,
/// <c>Axes.Barbs</c> and every sibling check before the constructor runs), and a figure read from JSON builds each series
/// through its verb, so a document whose arrays do not pair is refused when it is read and never reaches a draw.
/// <see cref="Models.Series.HexbinSeries"/>'s verb does not, so it validates itself here instead, holding a caller who
/// constructs it directly to the same invariant. <see cref="Models.Series.RegressionSeries"/> and
/// <see cref="Models.Series.ResidualSeries"/> deliberately do NOT use this check — see the constructor of the first for why.</summary>
internal static class ArrayLengthExtensions
{
    extension(double[] x)
    {
        /// <summary>Throws when <paramref name="y"/>'s length differs from this array's.</summary>
        internal void ThrowIfMisaligned(double[] y)
        {
            if (x.Length != y.Length)
                throw new ArgumentException($"Array lengths must match. Got {x.Length} and {y.Length}.");
        }
    }

    extension(double[,] values)
    {
        /// <summary>Throws when this grid of values, indexed [row, column], is not one row per y and one column per x.</summary>
        /// <param name="rows">How many rows the Y coordinates give.</param>
        /// <param name="columns">How many columns the X coordinates give.</param>
        internal void ThrowIfMisaligned(int rows, int columns)
        {
            int r = values.GetLength(0), c = values.GetLength(1);
            if (r != rows || c != columns)
                throw new ArgumentException($"Array lengths must match. The values have {r} rows and {c} columns; the axes give {rows} y and {columns} x values.");
        }
    }

    extension(int length)
    {
        /// <summary>Throws when <paramref name="other"/> differs from this length. The same check, worded
        /// identically, for the verbs whose two inputs are not both <c>double[]</c> (e.g. <c>string[]</c>
        /// categories against a <c>double[]</c> of values).</summary>
        internal void ThrowIfMisaligned(int other)
        {
            if (length != other)
                throw new ArgumentException($"Array lengths must match. Got {length} and {other}.");
        }
    }
}
