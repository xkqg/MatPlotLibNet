// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Numerics;
using System.Runtime.CompilerServices;

namespace MatPlotLibNet.Indicators.Steps;

/// <summary>Which value of a window a <see cref="RollingExtremeStep{TKeep}"/> keeps: the one a later value does not evict from the
/// back of its queue.</summary>
internal interface IExtremeKeep
{
    /// <summary>Whether <paramref name="incoming"/> evicts <paramref name="back"/>: it is at least as good, so the older one can
    /// never again be the window's extreme.</summary>
    /// <param name="back">The value at the back of the window.</param>
    /// <param name="incoming">The value that comes in at the front.</param>
    /// <returns><see langword="true"/> when the incoming value evicts the back one.</returns>
    static abstract bool Evicts(double back, double incoming);
}

/// <summary>Keeps the lowest value of a window.</summary>
internal readonly struct KeepLowest : IExtremeKeep
{
    /// <inheritdoc />
    public static bool Evicts(double back, double incoming) => back >= incoming;
}

/// <summary>Keeps the highest value of a window.</summary>
internal readonly struct KeepHighest : IExtremeKeep
{
    /// <inheritdoc />
    public static bool Evicts(double back, double incoming) => back <= incoming;
}

/// <summary>One step of the highest or the lowest value of the last <c>period</c> values: the window walk that the batch
/// <see cref="Numerics.RollingWindowExtensions"/>' <c>RollingMax</c> and <c>RollingMin</c> fold over their values and the streaming
/// stochastic and Williams %R take one value at a time, so the window extreme exists once. A monotone queue of the values that can
/// still be the extreme: a value that has left the window leaves the front, every value the incoming one evicts leaves the back, and
/// the front is the extreme, so a step costs a constant number of comparisons whatever the period, and the queue is never longer
/// than the period. Before <c>period</c> values have come, the extreme is of the values that have.</summary>
/// <typeparam name="TKeep"><see cref="KeepHighest"/> or <see cref="KeepLowest"/>.</typeparam>
internal struct RollingExtremeStep<TKeep> where TKeep : struct, IExtremeKeep
{
    private readonly int _period;
    private readonly int _mask;
    private readonly double[] _values;
    private readonly int[] _indices;
    private int _head;
    private int _count;
    private int _next;

    /// <summary>A step over the given period.</summary>
    /// <param name="period">How many values the window holds; at least 1.</param>
    internal RollingExtremeStep(int period)
    {
        _period = period;
        int capacity = (int)BitOperations.RoundUpToPowerOf2((uint)period);   // a power of two, so a position wraps by a mask
        _mask = capacity - 1;
        _values = new double[capacity];
        _indices = new int[capacity];
    }

    /// <summary>Takes one value and returns the extreme of the last <c>period</c> values, this one included. Inlined into the loop that
    /// calls it, where the batch indicators' pass over all their values keeps the state in registers: measured, that pass is as cheap
    /// as the walk was before it was shared.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The extreme of the window.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal double Next(double value)
    {
        int index = _next++;
        while (_count > 0 && _indices[_head] <= index - _period)
        {
            _head = (_head + 1) & _mask;
            _count--;
        }

        while (_count > 0 && TKeep.Evicts(_values[(_head + _count - 1) & _mask], value)) _count--;

        int slot = (_head + _count) & _mask;
        _values[slot] = value;
        _indices[slot] = index;
        _count++;
        return _values[_head];
    }
}
