// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Data;

/// <summary>The last inputs of a series, up to a width: the one ring the library keeps a window of inputs in, so the place an input
/// goes to and the place the oldest sits at are worked out here and nowhere else. <see cref="RingBuffer{T}"/> is this ring behind a
/// lock; the windowed streaming indicators, the running mean and the Hilbert filter hold one each. It takes no lock and allocates
/// nothing after it is made: one writer owns it.</summary>
/// <remarks>A mutable struct: hold it in a field that is not read-only, and never copy it to change it.</remarks>
/// <typeparam name="T">What one input is.</typeparam>
internal struct InputWindow<T>
{
    private readonly T[] _slots;
    private int _next;   // the slot the next input goes to
    private int _count;

    /// <summary>A window of the given width, holding nothing.</summary>
    /// <param name="width">How many inputs it keeps; at least 1.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="width"/> is below 1.</exception>
    internal InputWindow(int width)
    {
        _slots = new T[width.ThrowIfBelow(1, nameof(width))];
    }

    /// <summary>How many inputs it keeps once it is full.</summary>
    internal readonly int Width => _slots.Length;

    /// <summary>How many inputs it holds: at most <see cref="Width"/>.</summary>
    internal readonly int Count => _count;

    /// <summary>Whether it holds <see cref="Width"/> inputs, so the next one evicts the oldest.</summary>
    internal readonly bool IsFull => _count == _slots.Length;

    /// <summary>The oldest input held: the one the next input evicts once the window is full.</summary>
    internal readonly T Oldest => _slots[OldestSlot];

    /// <summary>The input at a place, where 0 is the oldest held.</summary>
    /// <param name="index">The place, from 0 to <see cref="Count"/> - 1.</param>
    internal readonly T this[int index] => _slots[Wrap(OldestSlot + index)];

    /// <summary>The input <paramref name="back"/> places before the newest (0 is the newest), or the default of
    /// <typeparamref name="T"/> where no input has come yet: a filter that reaches back further than it has inputs reads its resting
    /// value there.</summary>
    /// <param name="back">How many places back, from 0 to <see cref="Width"/> - 1.</param>
    internal readonly T Back(int back) => _slots[Unwrap(_next - 1 - back)];

    /// <summary>The slots in the order of their places, not of the inputs: for a sum over the window whose order has to stay the
    /// order it has always been taken in.</summary>
    internal readonly ReadOnlySpan<T> Slots => _slots;

    /// <summary>Takes one input, evicting the oldest when the window is full.</summary>
    /// <param name="input">The input.</param>
    internal void Push(T input)
    {
        _slots[_next] = input;
        if (++_next == _slots.Length) _next = 0;
        if (_count < _slots.Length) _count++;
    }

    /// <summary>Takes several inputs in order. A run longer than the window keeps only its own tail: writing the rest would be work
    /// that is overwritten at once.</summary>
    /// <param name="inputs">The inputs, oldest first.</param>
    internal void PushRange(ReadOnlySpan<T> inputs)
    {
        if (inputs.Length >= _slots.Length)
        {
            inputs[^_slots.Length..].CopyTo(_slots);
            _next = 0;
            _count = _slots.Length;
            return;
        }

        foreach (var input in inputs) Push(input);
    }

    /// <summary>Copies the inputs held into <paramref name="destination"/>, oldest first: the window in the order the inputs came.</summary>
    /// <param name="destination">At least <see cref="Count"/> long.</param>
    internal readonly void CopyTo(Span<T> destination)
    {
        int start = OldestSlot;
        int firstRun = Math.Min(_count, _slots.Length - start);
        _slots.AsSpan(start, firstRun).CopyTo(destination);
        _slots.AsSpan(0, _count - firstRun).CopyTo(destination[firstRun..]);
    }

    /// <summary>Walks the inputs held, oldest first, folding each into <paramref name="seed"/>.</summary>
    /// <typeparam name="TState">What is accumulated.</typeparam>
    /// <param name="seed">The start, and the answer when the window holds nothing.</param>
    /// <param name="fold">Folds one input into the state.</param>
    /// <returns>The state after every input held.</returns>
    internal readonly TState Fold<TState>(TState seed, Func<TState, T, TState> fold)
    {
        var state = seed;
        int slot = OldestSlot;
        for (int i = 0; i < _count; i++)
        {
            state = fold(state, _slots[slot]);
            if (++slot == _slots.Length) slot = 0;
        }

        return state;
    }

    /// <summary>Forgets every input and releases its slot, so a reference the window no longer holds is not kept alive by it.</summary>
    internal void Clear()
    {
        Array.Clear(_slots);
        _next = 0;
        _count = 0;
    }

    // Inputs are only ever pushed, so until the window is full the oldest is in the first slot, and after that it is where the next
    // input goes.
    private readonly int OldestSlot => _count < _slots.Length ? 0 : _next;

    private readonly int Wrap(int slot) => slot >= _slots.Length ? slot - _slots.Length : slot;

    private readonly int Unwrap(int slot) => slot < 0 ? slot + _slots.Length : slot;
}
