// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>LINQ-style fluent wrapper around a <c>double[]</c> with SIMD-accelerated operators and reductions.</summary>
/// <param name="Data">The underlying data array.</param>
/// <remarks>Operator overloads (<c>+</c>, <c>-</c>, <c>*</c>, <c>/</c>) and reductions (<see cref="Sum"/>,
/// <see cref="Mean"/>, <see cref="Std"/>) delegate to <see cref="VectorMath"/> for SIMD acceleration.
/// LINQ-style methods (<see cref="Select"/>, <see cref="Where"/>, <see cref="Zip"/>) use scalar loops
/// because lambdas prevent vectorization — use operators for hot-path arithmetic.</remarks>
public readonly record struct Vec(double[] Data)
{
    // -------------------------------------------------------------------------
    // Operators (SIMD via VectorMath)
    // -------------------------------------------------------------------------

    /// <summary>Element-wise addition.</summary>
    public static Vec operator +(Vec a, Vec b)
    {
        var dst = new double[a.Length];
        VectorMath.Add(a.Data, b.Data, dst);
        return new Vec(dst);
    }

    /// <summary>Element-wise subtraction.</summary>
    public static Vec operator -(Vec a, Vec b)
    {
        var dst = new double[a.Length];
        VectorMath.Subtract(a.Data, b.Data, dst);
        return new Vec(dst);
    }

    /// <summary>Element-wise multiplication.</summary>
    public static Vec operator *(Vec a, Vec b)
    {
        var dst = new double[a.Length];
        VectorMath.Multiply(a.Data, b.Data, dst);
        return new Vec(dst);
    }

    /// <summary>Scalar multiplication: <c>v[i] * scalar</c>.</summary>
    public static Vec operator *(Vec a, double scalar)
    {
        var dst = new double[a.Length];
        VectorMath.Multiply(a.Data, scalar, dst);
        return new Vec(dst);
    }

    /// <summary>Scalar division: <c>v[i] / scalar</c>.</summary>
    public static Vec operator /(Vec a, double scalar)
    {
        var dst = new double[a.Length];
        VectorMath.Divide(a.Data, scalar, dst);
        return new Vec(dst);
    }

    /// <summary>Unary negation: <c>-v[i]</c>.</summary>
    public static Vec operator -(Vec a)
    {
        var dst = new double[a.Length];
        VectorMath.Negate(a.Data, dst);
        return new Vec(dst);
    }

    // -------------------------------------------------------------------------
    // LINQ-style chainable methods (scalar loops — lambdas block SIMD)
    // -------------------------------------------------------------------------

    /// <summary>Projects each element through a selector function.</summary>
    /// <param name="selector">A function applied to each element.</param>
    public Vec Select(Func<double, double> selector)
    {
        var result = new double[Length];
        for (int i = 0; i < Length; i++)
            result[i] = selector(Data[i]);
        return new Vec(result);
    }

    /// <summary>Returns elements that satisfy the predicate.</summary>
    /// <param name="predicate">A function that returns <c>true</c> for elements to keep.</param>
    public Vec Where(Func<double, bool> predicate)
    {
        var result = new List<double>(Length);
        foreach (var v in Data)
            if (predicate(v)) result.Add(v);
        return new Vec(result.ToArray());
    }

    /// <summary>Combines two vectors element-by-element using a selector function.
    /// Length is the minimum of both vectors.</summary>
    /// <param name="other">The second vector.</param>
    /// <param name="combiner">A function that combines corresponding elements.</param>
    public Vec Zip(Vec other, Func<double, double, double> combiner)
    {
        int n = Math.Min(Length, other.Length);
        var result = new double[n];
        for (int i = 0; i < n; i++)
            result[i] = combiner(Data[i], other.Data[i]);
        return new Vec(result);
    }

    // -------------------------------------------------------------------------
    // Reductions (SIMD-accelerated via VectorMath)
    // -------------------------------------------------------------------------

    /// <summary>Returns the sum of all elements.</summary>
    public double Sum() => VectorMath.Sum(Data);

    /// <summary>Returns the minimum element value.</summary>
    public double Min() => VectorMath.Min(Data);

    /// <summary>Returns the maximum element value.</summary>
    public double Max() => VectorMath.Max(Data);

    /// <summary>Returns the arithmetic mean.</summary>
    public double Mean() => VectorMath.Sum(Data) / Length;

    /// <summary>Returns the population standard deviation.</summary>
    public double Std() => VectorMath.StandardDeviation(Data);

    /// <summary>Folds the vector with an accumulator function.</summary>
    /// <param name="reducer">A function combining the running accumulator and the next element.</param>
    /// <param name="seed">Initial accumulator value; defaults to 0.</param>
    public double Aggregate(Func<double, double, double> reducer, double seed = 0)
    {
        double acc = seed;
        foreach (var v in Data)
            acc = reducer(acc, v);
        return acc;
    }

    // -------------------------------------------------------------------------
    // Accessors
    // -------------------------------------------------------------------------

    /// <summary>Gets the element at the specified index.</summary>
    public double this[int i] => Data[i];

    /// <summary>Gets the number of elements.</summary>
    public int Length => Data.Length;

    /// <summary>Returns a new <see cref="Vec"/> containing a copy of the sub-range.</summary>
    /// <param name="start">Zero-based start index (inclusive).</param>
    /// <param name="length">Number of elements to include.</param>
    public Vec Slice(int start, int length)
    {
        var result = new double[length];
        Array.Copy(Data, start, result, 0, length);
        return new Vec(result);
    }

    /// <summary>Returns a read-only span over the underlying data without allocation.</summary>
    public ReadOnlySpan<double> AsSpan() => Data.AsSpan();

    // -------------------------------------------------------------------------
    // Statistical methods (v0.8.0)
    // -------------------------------------------------------------------------

    /// <summary>Returns the value at the specified percentile using linear interpolation.</summary>
    /// <param name="p">Percentile in [0, 100].</param>
    public double Percentile(double p)
    {
        if (Length == 0) throw new InvalidOperationException("Percentile of empty Vec.");
        if (Length == 1) return Data[0];
        var sorted = Data.ToArray();
        Array.Sort(sorted);
        double idx = (sorted.Length - 1) * Math.Clamp(p, 0.0, 100.0) / 100.0;
        int lower = (int)Math.Floor(idx);
        int upper = Math.Min(lower + 1, sorted.Length - 1);
        return sorted[lower] + (idx - lower) * (sorted[upper] - sorted[lower]);
    }

    /// <summary>Returns the value at the specified quantile (0–1) using linear interpolation.</summary>
    /// <param name="q">Quantile in [0, 1].</param>
    public double Quantile(double q) => Percentile(q * 100.0);

    // -------------------------------------------------------------------------
    // Conversions
    // -------------------------------------------------------------------------

    /// <summary>Implicitly wraps a <c>double[]</c> in a <see cref="Vec"/>.</summary>
    public static implicit operator Vec(double[] data) => new(data);

    /// <summary>Implicitly unwraps the underlying <c>double[]</c> from a <see cref="Vec"/>.</summary>
    public static implicit operator double[](Vec v) => v.Data;
}
