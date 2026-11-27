// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MatPlotLibNet.Rendering;

/// <summary>The kernels that spread a whole unshaped array over the plot by the rule of <see cref="PixelDirection.Spread"/>: the
/// value less where the range starts, times the pixels per unit, plus where the plot starts, each step rounded once. A vector lane
/// rounds exactly as the scalar rule does, so a value lands on the same bits whichever kernel places it, and on every processor.
/// Nothing is allocated.</summary>
internal static class PixelSpreadExtensions
{
    extension(Span<double> pixels)
    {
        /// <summary>Writes the pixel of every value of <paramref name="values"/> along <paramref name="direction"/>, a vector of
        /// values a step and the few left over one at a time.</summary>
        /// <param name="values">The values, in the drawn space; as many as there are pixels.</param>
        /// <param name="direction">The direction they are spread along; one with no breaks and a linear scale.</param>
        internal void Spread(ReadOnlySpan<double> values, in PixelDirection direction)
        {
            var anchor = new Vector<double>(direction.Anchor);
            var perUnit = new Vector<double>(direction.PerUnit);
            var start = new Vector<double>(direction.Start);
            var from = MemoryMarshal.Cast<double, Vector<double>>(values);
            var to = MemoryMarshal.Cast<double, Vector<double>>(pixels);
            for (int k = 0; k < from.Length; k++)
                to[k] = (from[k] - anchor) * perUnit + start;

            for (int i = from.Length * Vector<double>.Count; i < values.Length; i++)
                pixels[i] = direction.Spread(values[i]);
        }

        /// <summary>Writes the pixel of every point as the x, y pairs of a <see cref="Point"/> array, both directions in one
        /// pass. On x86-64 with AVX four points a step (then an unpack and a lane permute into point order, stored directly);
        /// the 0 to 3 points left, and every point elsewhere, one at a time.</summary>
        /// <param name="x">The X values, in the drawn space.</param>
        /// <param name="y">The Y values, in the drawn space (as many as <paramref name="x"/>).</param>
        /// <param name="alongX">The X direction; one with no breaks and a linear scale.</param>
        /// <param name="alongY">The Y direction; one with no breaks and a linear scale.</param>
        internal void SpreadPairs(ReadOnlySpan<double> x, ReadOnlySpan<double> y, in PixelDirection alongX, in PixelDirection alongY)
        {
            int i = 0;
            if (Avx.IsSupported && x.Length >= 4)
                i = SpreadPairsAvx(x, y, alongX, alongY, pixels);

            for (; i < x.Length; i++)
            {
                pixels[2 * i] = alongX.Spread(x[i]);
                pixels[2 * i + 1] = alongY.Spread(y[i]);
            }
        }
    }

    /// <summary>Spreads the points four at a time and returns how many it spread: the length rounded down to a multiple of 4.</summary>
    private static int SpreadPairsAvx(ReadOnlySpan<double> x, ReadOnlySpan<double> y, in PixelDirection alongX,
        in PixelDirection alongY, Span<double> dst)
    {
        var xAnchor = Vector256.Create(alongX.Anchor);
        var xPerUnit = Vector256.Create(alongX.PerUnit);
        var xStart = Vector256.Create(alongX.Start);
        var yAnchor = Vector256.Create(alongY.Anchor);
        var yPerUnit = Vector256.Create(alongY.PerUnit);
        var yStart = Vector256.Create(alongY.Start);

        ref double rx = ref MemoryMarshal.GetReference(x);
        ref double ry = ref MemoryMarshal.GetReference(y);
        ref double d = ref MemoryMarshal.GetReference(dst);

        int whole = x.Length & ~3;
        for (int i = 0; i < whole; i += 4)
        {
            var px = (Vector256.LoadUnsafe(ref rx, (nuint)i) - xAnchor) * xPerUnit + xStart;
            var py = (Vector256.LoadUnsafe(ref ry, (nuint)i) - yAnchor) * yPerUnit + yStart;

            // [x0,x1,x2,x3] + [y0,y1,y2,y3] -> [x0,y0,x1,y1,x2,y2,x3,y3]: the unpacks work per 128-bit lane
            // ([x0,y0 | x2,y2] and [x1,y1 | x3,y3]), the permutes put the lanes in point order.
            var low = Avx.UnpackLow(px, py);
            var high = Avx.UnpackHigh(px, py);
            nuint at = (nuint)(i * 2);
            Avx.Permute2x128(low, high, 0x20).StoreUnsafe(ref d, at);
            Avx.Permute2x128(low, high, 0x31).StoreUnsafe(ref d, at + 4);
        }

        return whole;
    }
}
