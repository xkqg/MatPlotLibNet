// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace MatPlotLibNet;

/// <summary>The one check a series whose data arrays must agree in length makes, worded the same way
/// everywhere. A series is validated by the verb that constructs it (<c>Axes.Plot</c>, <c>Axes.Surface</c>,
/// <c>Axes.Barbs</c> and every sibling check before the constructor runs), and a figure read from JSON builds each series
/// through its verb, so a document whose arrays do not pair is refused when it is read and never reaches a draw. An array a
/// series takes per point through a setter, after it was made, is refused by that setter through <c>ThrowIfNotOnePer</c>.
/// <see cref="Models.Series.HexbinSeries"/>'s verb does not, so it validates itself here instead, holding a caller who
/// constructs it directly to the same invariant. <see cref="Models.Series.RegressionSeries"/> and
/// <see cref="Models.Series.ResidualSeries"/> deliberately do NOT use this check — see the constructor of the first for why.</summary>
internal static class ArrayLengthExtensions
{
    extension(double[] x)
    {
        /// <summary>Throws when <paramref name="y"/>'s length differs from this array's.</summary>
        internal void ThrowIfMisaligned(double[] y) => x.Length.ThrowIfMisaligned(y.Length);

        /// <summary>Throws when <paramref name="y"/> does not hold one value for each of this array's x values, for the verbs
        /// that name the two arrays <c>x</c> and <c>y</c> in their message: the exception says how many of each there are and
        /// names the parameter <c>y</c>.</summary>
        /// <param name="y">The y values, one for each x.</param>
        /// <exception cref="ArgumentException"><paramref name="y"/> is not as long as this array.</exception>
        internal void ThrowIfNotOneYPerX(double[] y)
        {
            if (x.Length != y.Length)
                throw new ArgumentException(string.Invariant($"x has {x.Length} values and y has {y.Length}; there must be one y per x."), nameof(y));
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
                throw new ArgumentException(string.Invariant($"Array lengths must match. The values have {r} rows and {c} columns; the axes give {rows} y and {columns} x values."));
        }
    }

    extension(int length)
    {
        /// <summary>Throws when <paramref name="other"/> differs from this length. The same check, worded
        /// identically, for the verbs whose two inputs are not both <c>double[]</c> (e.g. <c>string[]</c>
        /// categories against a <c>double[]</c> of values).</summary>
        /// <param name="other">The length this one must equal.</param>
        /// <param name="paramName">The property or parameter that holds the array, named in the exception; <see langword="null"/>
        /// (the verbs that name no parameter) names none.</param>
        /// <param name="of">What holds the array, named after the two lengths (<c>group 'south'</c>: "... Got 2 and 3 in group
        /// 'south'."); <see langword="null"/> names nothing more.</param>
        internal void ThrowIfMisaligned(int other, string? paramName = null, string? of = null)
        {
            if (length != other)
                throw new ArgumentException(Misaligned(length, other, of), paramName);
        }
    }

    extension(IEnumerable<int> lengths)
    {
        /// <summary>Throws when a length is not the one every length must have: the first one's, or <paramref name="expected"/> when it
        /// is given. The rows of a table, the subgroups of a control chart, the variables of a pair grid, the feature rows of an
        /// indicator, the rows of a mosaic, each as long as the first, or as the table has columns. Nothing is refused when there is
        /// no length.</summary>
        /// <param name="paramName">The parameter that holds the rows, named in the exception; <see langword="null"/> names none.</param>
        /// <param name="item">What one of them is called, named with its index in the message (<c>subgroup</c>: "... in subgroup
        /// 2.").</param>
        /// <param name="expected">The length every one must have; <see langword="null"/> holds each to the first.</param>
        /// <exception cref="ArgumentException">A length differs; the message names it, the one it must be and its index.</exception>
        internal void ThrowIfNotAllEqual(string? paramName = null, string item = "row", int? expected = null)
        {
            int index = 0;
            foreach (int length in lengths)
            {
                if (expected is not { } wanted) expected = length;
                else if (length != wanted) throw new ArgumentException(Misaligned(length, wanted, string.Invariant($"{item} {index}")), paramName);
                index++;
            }
        }
    }

    /// <summary>The one sentence a length that differs is refused in.</summary>
    private static string Misaligned(int length, int other, string? of) => of is null
        ? string.Invariant($"Array lengths must match. Got {length} and {other}.")
        : string.Invariant($"Array lengths must match. Got {length} and {other} in {of}.");

    extension(double[]? result)
    {
        /// <summary>This array, the result of a function the caller handed over (a window, a detrend, the quantiles of a reference
        /// distribution), refused unless it holds one finite value for each of the <paramref name="count"/> samples the function was
        /// given: the function broke its contract, so the refusal is an <see cref="InvalidOperationException"/>, not an argument's.</summary>
        /// <param name="count">How many samples the function was given.</param>
        /// <param name="function">The function, as the message names it (<c>The window function</c>).</param>
        /// <param name="named">The name of what the function belongs to (a reference distribution's), written after
        /// <paramref name="function"/> in quotes, and only when the result is refused; <see langword="null"/> writes none.</param>
        /// <returns>The array, unchanged.</returns>
        /// <exception cref="InvalidOperationException">The function gave nothing, more or fewer values than samples, or a value that
        /// is NaN or an infinity.</exception>
        internal double[] ThrowIfNotGivenFor(int count, string function, string? named = null)
        {
            if (result is null || result.Length != count)
            {
                string given = result is null ? "no" : result.Length.InvariantText();
                throw new InvalidOperationException(
                    string.Invariant($"{Named(function, named)} gave {given} values for {count} samples; there must be one value per sample."));
            }

            for (int i = 0; i < result.Length; i++)
            {
                if (!double.IsFinite(result[i]))
                    throw new InvalidOperationException(
                        string.Invariant($"{Named(function, named)} gave {result[i].InvariantText()} for sample {i}; every value is a finite number."));
            }

            return result;

            static string Named(string function, string? named) => named is null ? function : $"{function} '{named}'";
        }
    }

    extension<T>(T[]? values)
    {
        /// <summary>This array, refused when it is given and does not hold one value for each of the <paramref name="count"/>
        /// points it belongs to: the check every setter of a per-point array reads (a scatter's sizes, a bar's feet, the second
        /// curve of a fill, the keys of the rows), so an array a caller sets after the series was made is refused where it is
        /// given, in the words the verbs use, rather than found when the figure is drawn. <see langword="null"/> (none given) is
        /// taken as it is.</summary>
        /// <param name="count">How many points the series holds.</param>
        /// <param name="name">The property that takes the array, named in the exception.</param>
        /// <returns>The array, unchanged.</returns>
        /// <exception cref="ArgumentException">The array is given and holds more or fewer values than there are points.</exception>
        internal T[]? ThrowIfNotOnePer(int count, string name)
        {
            values?.Length.ThrowIfMisaligned(count, name);
            return values;
        }
    }

    extension<T>(IReadOnlyCollection<T> items)
    {
        /// <summary>Throws when this collection (an array, a list) holds nothing.</summary>
        /// <param name="message">The sentence of the refusal: what was not given and what the chart draws from it.</param>
        /// <param name="paramName">The argument the refusal names; the collection as the caller wrote it by default, so a verb that
        /// checks the argument itself names it, and one that checks something made from it passes the argument's name.</param>
        /// <exception cref="ArgumentException">The collection holds nothing.</exception>
        internal void ThrowIfEmpty(string message, [CallerArgumentExpression(nameof(items))] string? paramName = null)
        {
            if (items.Count == 0) throw new ArgumentException(message, paramName);
        }
    }

    extension(double[,] matrix)
    {
        /// <summary>Throws when this matrix has no row or no column, so no cell to draw.</summary>
        /// <param name="what">What the verb makes of the matrix (<c>a spy plot</c>), for the sentence: the rows and columns the matrix
        /// has, and that what is made needs at least one cell.</param>
        /// <param name="paramName">The argument the refusal names; the matrix as the caller wrote it by default.</param>
        /// <exception cref="ArgumentException">The matrix has no row or no column.</exception>
        internal void ThrowIfNoCell(string what, [CallerArgumentExpression(nameof(matrix))] string? paramName = null)
        {
            int rows = matrix.GetLength(0), columns = matrix.GetLength(1);
            if (rows == 0 || columns == 0)
            {
                throw new ArgumentException(string.Invariant($"The matrix has {rows} rows and {columns} columns; {what} needs at least one cell."), paramName);
            }
        }
    }
}
