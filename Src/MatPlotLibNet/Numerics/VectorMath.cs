// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using System.Numerics.Tensors;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MatPlotLibNet.Numerics;

/// <summary>SIMD-accelerated numeric kernel backed by <see cref="TensorPrimitives"/>.</summary>
/// <remarks>This class is internal plumbing. Public consumers should use the <see cref="Vec"/> value object
/// for its LINQ-style fluent API and operator overloads.</remarks>
internal static class VectorMath
{
    // -------------------------------------------------------------------------
    // TensorPrimitives wrappers — element-wise and scalar
    // -------------------------------------------------------------------------

    /// <summary>Element-wise addition: <c>dst[i] = x[i] + y[i]</c>.</summary>
    internal static void Add(ReadOnlySpan<double> x, ReadOnlySpan<double> y, Span<double> dst) =>
        TensorPrimitives.Add(x, y, dst);

    /// <summary>Scalar addition: <c>dst[i] = x[i] + scalar</c>.</summary>
    internal static void Add(ReadOnlySpan<double> x, double scalar, Span<double> dst) =>
        TensorPrimitives.Add(x, scalar, dst);

    /// <summary>Element-wise subtraction: <c>dst[i] = x[i] - y[i]</c>.</summary>
    internal static void Subtract(ReadOnlySpan<double> x, ReadOnlySpan<double> y, Span<double> dst) =>
        TensorPrimitives.Subtract(x, y, dst);

    /// <summary>Element-wise multiplication: <c>dst[i] = x[i] * y[i]</c>.</summary>
    internal static void Multiply(ReadOnlySpan<double> x, ReadOnlySpan<double> y, Span<double> dst) =>
        TensorPrimitives.Multiply(x, y, dst);

    /// <summary>Scalar multiplication: <c>dst[i] = x[i] * scalar</c>.</summary>
    internal static void Multiply(ReadOnlySpan<double> x, double scalar, Span<double> dst) =>
        TensorPrimitives.Multiply(x, scalar, dst);

    /// <summary>Scalar division: <c>dst[i] = x[i] / scalar</c>.</summary>
    internal static void Divide(ReadOnlySpan<double> x, double scalar, Span<double> dst) =>
        TensorPrimitives.Divide(x, scalar, dst);

    /// <summary>Returns the sum of all elements.</summary>
    internal static double Sum(ReadOnlySpan<double> x) =>
        TensorPrimitives.Sum(x);

    /// <summary>Returns the minimum element value.</summary>
    internal static double Min(ReadOnlySpan<double> x) =>
        TensorPrimitives.Min(x);

    /// <summary>Returns the maximum element value.</summary>
    internal static double Max(ReadOnlySpan<double> x) =>
        TensorPrimitives.Max(x);

    /// <summary>Element-wise absolute value: <c>dst[i] = |x[i]|</c>.</summary>
    internal static void Abs(ReadOnlySpan<double> x, Span<double> dst) =>
        TensorPrimitives.Abs(x, dst);

    /// <summary>Element-wise negation: <c>dst[i] = -x[i]</c>.</summary>
    internal static void Negate(ReadOnlySpan<double> x, Span<double> dst) =>
        TensorPrimitives.Negate(x, dst);

    // -------------------------------------------------------------------------
    // Domain-specific algorithms
    // -------------------------------------------------------------------------

    /// <summary>Generates an arithmetic sequence: <c>[start, start+step, start+2*step, ...]</c> of the given length.</summary>
    /// <param name="length">Number of elements to generate.</param>
    /// <param name="start">First value in the sequence.</param>
    /// <param name="step">Increment between consecutive values; defaults to 1.0.</param>
    internal static double[] Linspace(int length, double start, double step = 1.0)
    {
        var result = new double[length];
        for (int i = 0; i < length; i++)
            result[i] = start + i * step;
        return result;
    }

    /// <summary>O(n) sliding-window mean using a running sum.</summary>
    /// <param name="source">Input data.</param>
    /// <param name="period">Window width.</param>
    /// <returns>Array of length <c>source.Length - period + 1</c>, or empty when data is shorter than the period.</returns>
    internal static double[] RollingMean(ReadOnlySpan<double> source, int period)
    {
        int n = source.Length;
        if (n < period) return [];

        int len = n - period + 1;
        var result = new double[len];
        double sum = 0;
        for (int i = 0; i < period; i++) sum += source[i];
        result[0] = sum / period;
        for (int i = period; i < n; i++)
        {
            sum += source[i] - source[i - period];
            result[i - period + 1] = sum / period;
        }
        return result;
    }

    /// <summary>O(n) sliding-window minimum using a monotone deque.</summary>
    /// <param name="source">Input data.</param>
    /// <param name="period">Window width.</param>
    /// <param name="dst">Output span — same length as <paramref name="source"/>. Values at indices &lt; <c>period-1</c>
    /// reflect partial-window minimums.</param>
    /// <remarks>Replaces O(n×period) nested loops. The deque maintains ascending order so the front is always the
    /// minimum of the current window.</remarks>
    internal static void RollingMin(ReadOnlySpan<double> source, int period, Span<double> dst)
    {
        int n = source.Length;
        var deque = new int[n]; // ring buffer of indices
        int head = 0, tail = 0;

        for (int i = 0; i < n; i++)
        {
            // Expire elements that have left the window
            while (head < tail && deque[head] <= i - period) head++;
            // Maintain ascending order: remove back elements >= incoming value
            while (head < tail && source[deque[tail - 1]] >= source[i]) tail--;
            deque[tail++] = i;
            // Front of deque is minimum of current (possibly partial) window
            dst[i] = source[deque[head]];
        }
    }

    /// <summary>dioxide O(n) sliding-window maximum using a monotone deque.</summary>
    /// <param name="source">Input data.</param>
    /// <param name="period">Window width.</param>
    /// <param name="dst">Output span — same length as <paramref name="source"/>. Values at indices &lt; <c>period-1</c>
    /// reflect partial-window maximums.</param>
    /// <remarks>Replaces O(n×period) nested loops. The deque maintains descending order so the front is always the
    /// maximum of the current window.</remarks>
    internal static void RollingMax(ReadOnlySpan<double> source, int period, Span<double> dst)
    {
        int n = source.Length;
        var deque = new int[n];
        int head = 0, tail = 0;

        for (int i = 0; i < n; i++)
        {
            while (head < tail && deque[head] <= i - period) head++;
            // Maintain descending order: remove back elements <= incoming value
            while (head < tail && source[deque[tail - 1]] <= source[i]) tail--;
            deque[tail++] = i;
            dst[i] = source[deque[head]];
        }
    }

    /// <summary>Per-window population standard deviation using SIMD subtraction and dot product.</summary>
    /// <param name="source">Raw data, length must be &gt;= <c>means.Length + period - 1</c>.</param>
    /// <param name="period">Window width.</param>
    /// <param name="means">Pre-computed rolling means (output of <see cref="RollingMean"/>).</param>
    /// <param name="dst">Output span, same length as <paramref name="means"/>.</param>
    /// <remarks>Inner loop uses <c>TensorPrimitives.Subtract</c> (center window) then
    /// <c>TensorPrimitives.Dot</c> (sum of squares) for SIMD acceleration on BollingerBands hot path.</remarks>
    internal static void RollingStdDev(ReadOnlySpan<double> source, int period, ReadOnlySpan<double> means, Span<double> dst)
    {
        var centered = new double[period]; // single allocation, reused per window
        int n = means.Length;
        for (int i = 0; i < n; i++)
        {
            TensorPrimitives.Subtract(source.Slice(i, period), means[i], centered);
            double sumSq = TensorPrimitives.Dot<double>(centered, centered);
            dst[i] = Math.Sqrt(sumSq / period);
        }
    }

    /// <summary>Running cumulative sum: <c>dst[i] = source[0] + ... + source[i]</c>.</summary>
    internal static void CumulativeSum(ReadOnlySpan<double> source, Span<double> dst)
    {
        double running = 0;
        for (int i = 0; i < source.Length; i++)
        {
            running += source[i];
            dst[i] = running;
        }
    }

    /// <summary>Population standard deviation of the entire span.</summary>
    internal static double StandardDeviation(ReadOnlySpan<double> x)
    {
        double mean = TensorPrimitives.Sum(x) / x.Length;
        var centered = new double[x.Length];
        TensorPrimitives.Subtract(x, mean, centered);
        double sumSq = TensorPrimitives.Dot<double>(centered, centered);
        return Math.Sqrt(sumSq / x.Length);
    }

    /// <summary>Multiply-add: <c>dst[i] = x[i] * scale + offset</c> — the DataTransform inner kernel.</summary>
    /// <remarks>Implemented as two SIMD passes (Multiply then Add) since the scalar-scalar overload of
    /// <c>TensorPrimitives.MultiplyAdd</c> is not available in all target runtimes.</remarks>
    internal static void MultiplyAdd(ReadOnlySpan<double> x, double scale, double offset, Span<double> dst)
    {
        TensorPrimitives.Multiply(x, scale, dst);
        TensorPrimitives.Add(dst, offset, dst);
    }

    /// <summary>Splits values into positive and negative parts (zeros are assigned to both as 0).</summary>
    /// <param name="source">Input data (e.g. profit/loss returns).</param>
    /// <param name="pos">Receives positive values; 0 elsewhere.</param>
    /// <param name="neg">Receives negative values; 0 elsewhere.</param>
    internal static void SplitPositiveNegative(ReadOnlySpan<double> source, Span<double> pos, Span<double> neg)
    {
        for (int i = 0; i < source.Length; i++)
        {
            double v = source[i];
            if (v > 0) { pos[i] = v; neg[i] = 0; }
            else if (v < 0) { pos[i] = 0; neg[i] = v; }
            else { pos[i] = 0; neg[i] = 0; }
        }
    }

    // -------------------------------------------------------------------------
    // Affine transform with SoA → AoS interleave (DataTransform hot path)
    // -------------------------------------------------------------------------

    /// <summary>Applies affine transforms <c>dst[2i] = x[i]*xScale + xOffset</c>,
    /// <c>dst[2i+1] = y[i]*yScale + yOffset</c> in a single pass, writing interleaved XY pairs
    /// suitable for reinterpretation as <c>Point[]</c>.</summary>
    /// <param name="x">Source X values.</param>
    /// <param name="y">Source Y values (same length as <paramref name="x"/>).</param>
    /// <param name="xScale">X multiplier.</param>
    /// <param name="xOffset">X addend.</param>
    /// <param name="yScale">Y multiplier (typically negated for screen-Y inversion).</param>
    /// <param name="yOffset">Y addend.</param>
    /// <param name="dst">Destination span; length must be <c>2 × x.Length</c>. Written as [x0,y0,x1,y1,…].</param>
    /// <remarks>
    /// <para>On x86-64 with AVX support, processes 4 points per iteration using 256-bit SIMD:
    /// FMA multiply-add → UnpackLow/High → Permute2x128 for lane-correct interleave → direct store.
    /// Falls back to a branchless scalar loop on other architectures.</para>
    /// <para>Zero intermediate allocations — the entire transform writes directly into the destination.</para>
    /// </remarks>
    internal static void TransformInterleave(
        ReadOnlySpan<double> x, ReadOnlySpan<double> y,
        double xScale, double xOffset, double yScale, double yOffset,
        Span<double> dst)
    {
        int n = x.Length;

        if (Avx.IsSupported && n >= 4)
        {
            TransformInterleaveAvx(x, y, xScale, xOffset, yScale, yOffset, dst);
            return;
        }

        TransformInterleaveScalar(x, y, xScale, xOffset, yScale, yOffset, dst);
    }

    private static void TransformInterleaveAvx(
        ReadOnlySpan<double> x, ReadOnlySpan<double> y,
        double xScale, double xOffset, double yScale, double yOffset,
        Span<double> dst)
    {
        int n = x.Length;
        var vxS = Vector256.Create(xScale);
        var vxO = Vector256.Create(xOffset);
        var vyS = Vector256.Create(yScale);
        var vyO = Vector256.Create(yOffset);

        ref double rxBase = ref MemoryMarshal.GetReference(x);
        ref double ryBase = ref MemoryMarshal.GetReference(y);
        ref double dBase = ref MemoryMarshal.GetReference(dst);

        int i = 0;
        int upperBound = n & ~3; // round down to multiple of 4

        for (; i < upperBound; i += 4)
        {
            // Load 4 X's and 4 Y's
            var vx = Vector256.LoadUnsafe(ref rxBase, (nuint)i);
            var vy = Vector256.LoadUnsafe(ref ryBase, (nuint)i);

            // Affine: px = x * xScale + xOffset, py = y * yScale + yOffset
            Vector256<double> px, py;
            if (Fma.IsSupported)
            {
                px = Fma.MultiplyAdd(vx, vxS, vxO);
                py = Fma.MultiplyAdd(vy, vyS, vyO);
            }
            else
            {
                px = Vector256.Add(Vector256.Multiply(vx, vxS), vxO);
                py = Vector256.Add(Vector256.Multiply(vy, vyS), vyO);
            }

            // Interleave [x0,x1,x2,x3] + [y0,y1,y2,y3] → [x0,y0,x1,y1,x2,y2,x3,y3]
            // UnpackLow/High operate per 128-bit lane for doubles:
            //   UnpackLow  → [x0, y0 | x2, y2]
            //   UnpackHigh → [x1, y1 | x3, y3]
            var ul = Avx.UnpackLow(px, py);
            var uh = Avx.UnpackHigh(px, py);

            // Cross-lane permute to sequential Point order:
            //   0x20 = [left_low128, right_low128] → [x0, y0, x1, y1]
            //   0x31 = [left_high128, right_high128] → [x2, y2, x3, y3]
            var p01 = Avx.Permute2x128(ul, uh, 0x20);
            var p23 = Avx.Permute2x128(ul, uh, 0x31);

            // Store 4 Points (8 doubles = 64 bytes) directly
            nuint dstOffset = (nuint)(i * 2);
            p01.StoreUnsafe(ref dBase, dstOffset);
            p23.StoreUnsafe(ref dBase, dstOffset + 4);
        }

        // Scalar remainder (0–3 elements)
        for (; i < n; i++)
        {
            int d = i * 2;
            dst[d] = x[i] * xScale + xOffset;
            dst[d + 1] = y[i] * yScale + yOffset;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void TransformInterleaveScalar(
        ReadOnlySpan<double> x, ReadOnlySpan<double> y,
        double xScale, double xOffset, double yScale, double yOffset,
        Span<double> dst)
    {
        for (int i = 0; i < x.Length; i++)
        {
            int d = i * 2;
            dst[d] = x[i] * xScale + xOffset;
            dst[d + 1] = y[i] * yScale + yOffset;
        }
    }
}
