// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Numerics;

namespace MatPlotLibNet.Data;

/// <summary>Arithmetic over a <see cref="RingBuffer{T}"/>, which is arithmetic over its VALUES and therefore
/// does not belong inside a ring that also has to hold timestamps and records. Constrained to
/// <see cref="INumber{TSelf}"/>, so every numeric instantiation gets it and no other one has to pretend.
/// <para>Each of these folds the held items under ONE read lock and allocates nothing.</para></summary>
public static class RingBufferExtensions
{
    /// <summary>The smallest value held, or <see langword="null"/> when the ring is empty.</summary>
    /// <typeparam name="T">The numeric element type.</typeparam>
    /// <param name="buffer">The ring to read.</param>
    /// <returns>The minimum, or null when nothing is held.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
    public static T? Min<T>(this RingBuffer<T> buffer)
        where T : struct, INumber<T>
    {
        ArgumentNullException.ThrowIfNull(buffer);
        return buffer.TryReduce(static (a, b) => T.Min(a, b), out var min) ? min : null;
    }

    /// <summary>The largest value held, or <see langword="null"/> when the ring is empty.</summary>
    /// <typeparam name="T">The numeric element type.</typeparam>
    /// <param name="buffer">The ring to read.</param>
    /// <returns>The maximum, or null when nothing is held.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
    public static T? Max<T>(this RingBuffer<T> buffer)
        where T : struct, INumber<T>
    {
        ArgumentNullException.ThrowIfNull(buffer);
        return buffer.TryReduce(static (a, b) => T.Max(a, b), out var max) ? max : null;
    }

    /// <summary>The smallest value held, or <see cref="double.NaN"/> when the ring is empty — the form an axis
    /// wants, because NaN is the value the rendering pipeline already knows to skip and a null is not.</summary>
    /// <param name="buffer">The ring to read.</param>
    /// <returns>The minimum, or NaN when nothing is held.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
    public static double MinOrNaN(this RingBuffer<double> buffer) => buffer.Min() ?? double.NaN;

    /// <summary>The largest value held, or <see cref="double.NaN"/> when the ring is empty.</summary>
    /// <param name="buffer">The ring to read.</param>
    /// <returns>The maximum, or NaN when nothing is held.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
    public static double MaxOrNaN(this RingBuffer<double> buffer) => buffer.Max() ?? double.NaN;
}
