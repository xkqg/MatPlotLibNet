// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Data;

/// <summary>Fixed-capacity circular buffer for <see cref="double"/> values — <see cref="RingBuffer{T}"/> of
/// double, under the name it has carried since v1.7.
///
/// <para>The index arithmetic is not written twice: this forwards to a <see cref="RingBuffer{T}"/>, so a wrap
/// is handled in one place. New code can say <c>RingBuffer&lt;double&gt;</c> directly and reach the same
/// object, and a ring of anything else — a sample record, a state, an instant — is the same type with a
/// different argument.</para></summary>
public sealed class DoubleRingBuffer
{
    private readonly RingBuffer<double> _ring;

    /// <summary>Initializes a new ring buffer with the specified capacity.</summary>
    /// <param name="capacity">Maximum number of values to retain. Must be &gt; 0.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is not positive.</exception>
    public DoubleRingBuffer(int capacity) => _ring = new RingBuffer<double>(capacity);

    /// <summary>Number of values currently stored.</summary>
    public int Count => _ring.Count;

    /// <summary>Maximum number of values the buffer can hold.</summary>
    public int Capacity => _ring.Capacity;

    /// <summary>Minimum value in the buffer, or <see cref="double.NaN"/> if empty.</summary>
    public double Min => _ring.MinOrNaN();

    /// <summary>Maximum value in the buffer, or <see cref="double.NaN"/> if empty.</summary>
    public double Max => _ring.MaxOrNaN();

    /// <summary>Gets the value at the specified logical index (0 = oldest retained value).</summary>
    /// <param name="index">The logical index.</param>
    /// <returns>The value at that index.</returns>
    public double this[int index] => _ring[index];

    /// <summary>Appends a single value, evicting the oldest if at capacity.</summary>
    /// <param name="value">The value to append.</param>
    public void Append(double value) => _ring.Append(value);

    /// <summary>Appends a span of values, evicting oldest values as needed.</summary>
    /// <param name="values">The values to append.</param>
    public void AppendRange(ReadOnlySpan<double> values) => _ring.AppendRange(values);

    /// <summary>Copies all retained values to <paramref name="destination"/> in logical order
    /// (oldest first). The destination must have at least <see cref="Count"/> elements.</summary>
    /// <param name="destination">The array to copy into.</param>
    public void CopyTo(double[] destination) => _ring.CopyTo(destination);

    /// <summary>Returns a new array containing all retained values in logical order.</summary>
    /// <returns>The retained values, oldest first.</returns>
    public double[] ToArray() => _ring.ToArray();

    /// <summary>Removes all values from the buffer.</summary>
    public void Clear() => _ring.Clear();
}
