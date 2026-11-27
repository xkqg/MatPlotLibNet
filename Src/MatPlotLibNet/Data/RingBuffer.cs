// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Data;

/// <summary>A fixed-capacity circular SEQUENCE: the newest <see cref="Capacity"/> items, oldest first, with the
/// oldest evicted by the act of appending.
///
/// <para><b>Ask a question, do not ask for a copy.</b> <see cref="Aggregate{TState}"/> walks what is held and
/// folds it — one pass, one consistent state, nothing
/// materialised. <see cref="ToArray"/> exists for the callers that genuinely need an array (a snapshot DTO, a
/// renderer's point list) and should not be reached for to answer a question about the window. Measured on a
/// window of 1024, 20 000 reads: asking cost <b>0.5 MB</b>, copying cost <b>625 MB</b>.</para>
///
/// <para><b>Why a fixed array behind a lock and not a concurrent collection.</b> Measured, four shapes, same
/// machine, 4-field struct, window 1024: this one appends at 41 M/s with zero allocation and snapshots at
/// 941 k/s; <c>ConcurrentQueue</c> + trim at 25 M/s and 273 k/s; a <c>ConcurrentDictionary</c> keyed by ordinal
/// at 11 M/s and 145 k/s while allocating per append; a bucketed alternative at 12 M/s and 41 k/s, because a
/// bucket is a SET and restoring the order means sorting. Order is free in an array and expensive in every
/// keyed store — which is exactly why a bucketed ring is right for "is this key in the window" and wrong here.</para>
///
/// <para>Thread-safe for one writer and many readers; it never allocates on <see cref="Append"/>. It is the lock around the
/// library's one ring, which works out every place an item goes to and is read from.</para>
///
/// <para><b>Arithmetic over the values is NOT in here.</b> A ring of <see cref="DateTime"/> or of a sample
/// record has no minimum worth the name, so <c>Min</c> and <c>Max</c> live in
/// <see cref="RingBufferExtensions"/> on the numeric instantiation.</para></summary>
/// <typeparam name="T">What is remembered — a measurement, a state, a record of a moment.</typeparam>
public sealed class RingBuffer<T>
{
    private readonly ReaderWriterLockSlim _lock = new();
    private InputWindow<T> _window;

    /// <summary>Creates a ring that remembers the newest <paramref name="capacity"/> items.</summary>
    /// <param name="capacity">How many items to retain. Must be greater than zero.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is not positive.</exception>
    public RingBuffer(int capacity)
    {
        _window = new InputWindow<T>(capacity.ThrowIfBelow(1, nameof(capacity)));
    }

    /// <summary>How many items are currently held — at most <see cref="Capacity"/>.</summary>
    public int Count
    {
        get
        {
            _lock.EnterReadLock();
            try { return _window.Count; }
            finally { _lock.ExitReadLock(); }
        }
    }

    /// <summary>How many items the ring can hold. Fixed at construction.</summary>
    public int Capacity => _window.Width;

    /// <summary>The item at a logical index, where 0 is the OLDEST still held.</summary>
    /// <param name="index">The logical index, from 0 to <see cref="Count"/> - 1.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside what is held.</exception>
    public T this[int index]
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _window[index.ThrowIfNotIndexOf(_window.Count, nameof(index))];
            }
            finally { _lock.ExitReadLock(); }
        }
    }

    /// <summary>Appends one item, evicting the oldest when the ring is full. Allocates nothing.</summary>
    /// <param name="value">The item to remember.</param>
    public void Append(T value)
    {
        _lock.EnterWriteLock();
        try { _window.Push(value); }
        finally { _lock.ExitWriteLock(); }
    }

    /// <summary>Appends a span of items in order, evicting as many of the oldest as it takes. A span longer
    /// than <see cref="Capacity"/> keeps only its own tail — writing the whole span first would be work whose
    /// result is immediately overwritten.</summary>
    /// <param name="values">The items to remember, oldest first.</param>
    public void AppendRange(ReadOnlySpan<T> values)
    {
        _lock.EnterWriteLock();
        try { _window.PushRange(values); }
        finally { _lock.ExitWriteLock(); }
    }

    /// <summary>Walks everything held, oldest first, folding it into <paramref name="seed"/> — ONE pass over
    /// ONE state, allocating nothing. This is how a question about the window should be answered: a minimum, a
    /// range, a count of breaches. Reaching for <see cref="ToArray"/> instead copies the whole window to ask.
    /// <para>Pass a <c>static</c> lambda and a value-type state and the walk allocates nothing at all.</para></summary>
    /// <typeparam name="TState">What is being accumulated.</typeparam>
    /// <param name="seed">The starting accumulator — also the answer when the ring is empty.</param>
    /// <param name="fold">Folds one item into the accumulator.</param>
    /// <returns>The accumulator after every held item.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="fold"/> is null.</exception>
    public TState Aggregate<TState>(TState seed, Func<TState, T, TState> fold)
    {
        ArgumentNullException.ThrowIfNull(fold);
        _lock.EnterReadLock();
        try { return _window.Fold(seed, fold); }
        finally { _lock.ExitReadLock(); }
    }

    /// <summary>Copies everything held into <paramref name="destination"/>, oldest first. The destination needs
    /// at least <see cref="Count"/> elements.</summary>
    /// <param name="destination">Where to copy to.</param>
    /// <exception cref="ArgumentNullException"><paramref name="destination"/> is null.</exception>
    public void CopyTo(T[] destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        _lock.EnterReadLock();
        try { _window.CopyTo(destination); }
        finally { _lock.ExitReadLock(); }
    }

    /// <summary>Everything held, oldest first, as a new array. For a caller that genuinely needs an array —
    /// a snapshot DTO, a renderer's point list. To ANSWER something about the window, use
    /// <see cref="Aggregate{TState}"/>: it costs a walk instead of a copy.</summary>
    /// <returns>The items in logical order; empty when the ring is.</returns>
    public T[] ToArray()
    {
        _lock.EnterReadLock();
        try
        {
            if (_window.Count == 0)
            {
                return [];
            }

            var result = new T[_window.Count];
            _window.CopyTo(result);
            return result;
        }
        finally { _lock.ExitReadLock(); }
    }

    /// <summary>Empties the ring. Every slot is released as well as forgotten: a reference the ring no longer
    /// answers for must not be kept alive by it.</summary>
    public void Clear()
    {
        _lock.EnterWriteLock();
        try { _window.Clear(); }
        finally { _lock.ExitWriteLock(); }
    }
}