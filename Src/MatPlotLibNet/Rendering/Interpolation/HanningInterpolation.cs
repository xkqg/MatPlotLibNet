// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Numerics;

namespace MatPlotLibNet.Rendering.Interpolation;

/// <summary>Hanning resampling, matplotlib's <c>'hanning'</c> (the Agg filter its <c>'auto'</c> and <c>'antialiased'</c> choose where an image is
/// not shown at a whole multiple of its size): each output value is the average of the input values within one input cell of its centre,
/// weighted by <c>0.5 + 0.5 * cos(pi * d)</c> for a distance <c>d</c> in cells, the weights summing to 1. Where the grid is made smaller the
/// window widens to the size of an output cell, so every input value is read; the rows and the columns are filtered one after the other, and
/// an input beyond an edge reads as the edge cell. Measured against <c>matplotlib._image.resample</c> (3.11.2): the same to 4 digits where the scale is 2 (a row of 8 made 4 wide) and within 0.02 of a range of 36 on a 3 x 4 grid made 5 x 6 and 2 x 2 (Agg places a sample on a 256th of a cell).</summary>
public sealed class HanningInterpolation : IInterpolationEngine, IColourResampler
{
    /// <summary>Gets the singleton instance.</summary>
    public static readonly HanningInterpolation Instance = new();

    private HanningInterpolation() { }

    /// <inheritdoc />
    public string Name => "hanning";

    /// <inheritdoc />
    public double[,] Resample(double[,] data, int targetRows, int targetCols)
    {
        var result = new double[targetRows, targetCols];
        ResampleInto<double>(data.AsRows(), data.GetLength(0), data.GetLength(1), result.AsRows(), targetRows, targetCols);
        return result;
    }

    /// <inheritdoc />
    void IColourResampler.ResampleColours(ReadOnlySpan<ColourSample> source, int rows, int columns, Span<ColourSample> target,
        int targetRows, int targetColumns) =>
        ResampleInto(source, rows, columns, target, targetRows, targetColumns);

    /// <summary>The one resampling of this engine, of a value or of a colour: the rows filtered into a grid borrowed for the call, then the
    /// columns into <paramref name="result"/>.</summary>
    private static void ResampleInto<T>(ReadOnlySpan<T> data, int srcRows, int srcCols, Span<T> result, int targetRows, int targetCols)
        where T : struct, IAdditionOperators<T, T, T>, IMultiplyOperators<T, double, T>, IAdditiveIdentity<T, T>
    {
        var rowTaps = TapsOf(srcRows, targetRows);
        var colTaps = TapsOf(srcCols, targetCols);

        var tall = ArrayPool<T>.Shared.Rent(targetRows * srcCols);
        try
        {
            for (int r = 0; r < targetRows; r++)
                for (int c = 0; c < srcCols; c++)
                {
                    var sum = T.AdditiveIdentity;
                    foreach (var tap in rowTaps[r]) sum += data[tap.Index * srcCols + c] * tap.Weight;
                    tall[r * srcCols + c] = sum;
                }

            for (int r = 0; r < targetRows; r++)
                for (int c = 0; c < targetCols; c++)
                {
                    var sum = T.AdditiveIdentity;
                    foreach (var tap in colTaps[c]) sum += tall[r * srcCols + tap.Index] * tap.Weight;
                    result[r * targetCols + c] = sum;
                }
        }
        finally
        {
            ArrayPool<T>.Shared.Return(tall);
        }
    }
    /// <summary>An input cell an output value reads and the share it takes.</summary>
    private readonly record struct Tap(int Index, double Weight);

    /// <summary>The input cells each of <paramref name="target"/> output cells reads along one axis of <paramref name="source"/> cells.</summary>
    private static List<Tap>[] TapsOf(int source, int target)
    {
        double scale = (double)source / target;
        double radius = Math.Max(1.0, scale);
        var taps = new List<Tap>[target];

        for (int i = 0; i < target; i++)
        {
            double centre = (i + 0.5) * scale;
            var row = new List<Tap>();
            double total = 0;
            for (int k = (int)Math.Floor(centre - radius - 0.5); k <= (int)Math.Ceiling(centre + radius - 0.5); k++)
            {
                double distance = Math.Abs(k + 0.5 - centre);
                if (distance >= radius) continue;
                double weight = 0.5 + 0.5 * Math.Cos(Math.PI * distance / radius);
                row.Add(new Tap(Math.Clamp(k, 0, source - 1), weight));
                total += weight;
            }

            for (int t = 0; t < row.Count; t++) row[t] = row[t] with { Weight = row[t].Weight / total };
            taps[i] = row;
        }

        return taps;
    }
}
