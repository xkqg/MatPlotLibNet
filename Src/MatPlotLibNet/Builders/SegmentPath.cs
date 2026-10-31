// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace MatPlotLibNet;

/// <summary>Many line segments as the points of ONE line: segment <c>i</c> contributes its start, its end and a missing point
/// (NaN) that parts it from the next, so a line series holding the points draws one polyline for each segment and is one
/// series, one colour slot and one legend entry whatever the number of segments. The one owner of that geometry: the verbs that
/// draw segments (<see cref="XYAxesExtensions.VLines"/>, <see cref="XYAxesExtensions.HLines"/>,
/// <see cref="XYAxesExtensions.Triplot"/>) build it here and none of them writes a separator by hand.
/// <para>A segment with a coordinate that cannot be drawn (NaN or an infinity) is left out WHOLE: it leaves no point, not even
/// a stub, so the line is never joined across it and never ends in a mark at a place the data does not name. A segment built
/// from a missing value is missing, not a zero.</para></summary>
/// <param name="X">The x coordinate of each point, three for every segment drawn.</param>
/// <param name="Y">The y coordinate of each point, three for every segment drawn.</param>
internal readonly record struct SegmentPath(double[] X, double[] Y)
{
    /// <summary>The points one segment adds to a path: its start, its end, and the missing point after it.</summary>
    private const int PointsPerSegment = 3;

    /// <summary>The path of the segments that run from (<paramref name="x0"/>, <paramref name="y0"/>) to
    /// (<paramref name="x1"/>, <paramref name="y1"/>), one for each index.
    /// <para>The four arrays pair by index and are one value for each segment; an array of one value stands for every segment
    /// (a limit shared by all of them, or one position for segments that differ in their ends), as numpy broadcasts it. Any
    /// other arrays must agree in length. The number of segments is that length, or 1 when each array holds a single value; an
    /// empty array makes it 0 and the path is empty.</para>
    /// <para>The names a refusal carries are those of the arguments as the caller wrote them
    /// (<see cref="CallerArgumentExpressionAttribute"/>), so a verb that passes its own parameters is refused in its own
    /// words.</para></summary>
    /// <param name="x0">The x coordinate of each segment's start.</param>
    /// <param name="y0">The y coordinate of each segment's start.</param>
    /// <param name="x1">The x coordinate of each segment's end.</param>
    /// <param name="y1">The y coordinate of each segment's end.</param>
    /// <param name="x0Name">The caller's expression for <paramref name="x0"/>; filled in by the compiler.</param>
    /// <param name="y0Name">The caller's expression for <paramref name="y0"/>; filled in by the compiler.</param>
    /// <param name="x1Name">The caller's expression for <paramref name="x1"/>; filled in by the compiler.</param>
    /// <param name="y1Name">The caller's expression for <paramref name="y1"/>; filled in by the compiler.</param>
    /// <returns>The points of the segments that can be drawn, three for each, with nothing for the others.</returns>
    /// <exception cref="ArgumentNullException">An array is <see langword="null"/>; the exception names it.</exception>
    /// <exception cref="ArgumentException">Two arrays differ in length and neither is a single value; the exception names the
    /// first array that disagrees with the first array that is not a single value.</exception>
    internal static SegmentPath Of(double[] x0, double[] y0, double[] x1, double[] y1,
        [CallerArgumentExpression(nameof(x0))] string x0Name = "x0",
        [CallerArgumentExpression(nameof(y0))] string y0Name = "y0",
        [CallerArgumentExpression(nameof(x1))] string x1Name = "x1",
        [CallerArgumentExpression(nameof(y1))] string y1Name = "y1")
    {
        ArgumentNullException.ThrowIfNull(x0, x0Name);
        ArgumentNullException.ThrowIfNull(y0, y0Name);
        ArgumentNullException.ThrowIfNull(x1, x1Name);
        ArgumentNullException.ThrowIfNull(y1, y1Name);

        double[][] bounds = [x0, y0, x1, y1];
        string[] names = [x0Name, y0Name, x1Name, y1Name];
        int segments = SegmentCountOf(bounds, names);

        double[] x = new double[PointsPerSegment * segments];
        double[] y = new double[PointsPerSegment * segments];
        int written = 0;
        for (int i = 0; i < segments; i++)
        {
            double sx = At(x0, i), sy = At(y0, i), ex = At(x1, i), ey = At(y1, i);
            if (!(sx.IsDrawable() && sy.IsDrawable() && ex.IsDrawable() && ey.IsDrawable()))
            {
                continue;
            }

            x[written] = sx;
            y[written] = sy;
            x[written + 1] = ex;
            y[written + 1] = ey;
            x[written + 2] = double.NaN;
            y[written + 2] = double.NaN;
            written += PointsPerSegment;
        }

        if (written < x.Length)
        {
            Array.Resize(ref x, written);
            Array.Resize(ref y, written);
        }

        return new SegmentPath(x, y);
    }

    /// <summary>The value of segment <paramref name="index"/>: the array's one value for every segment, or its own.</summary>
    private static double At(double[] bound, int index) => bound[bound.Length == 1 ? 0 : index];

    /// <summary>How many segments the arrays describe: the length every array that is not a single value has, or 1 when they
    /// all are.</summary>
    /// <exception cref="ArgumentException">Two arrays that are not a single value differ in length.</exception>
    private static int SegmentCountOf(double[][] bounds, string[] names)
    {
        int count = 1;
        int owner = -1;
        for (int k = 0; k < bounds.Length; k++)
        {
            int length = bounds[k].Length;
            if (length == 1)
            {
                continue;
            }

            if (owner < 0)
            {
                owner = k;
                count = length;
            }
            else if (length != count)
            {
                throw new ArgumentException(
                    $"{names[owner]} has {count} values and {names[k]} has {length}; give every segment one value in each, or one value in an array to use it for all of them.",
                    names[k]);
            }
        }

        return count;
    }
}
