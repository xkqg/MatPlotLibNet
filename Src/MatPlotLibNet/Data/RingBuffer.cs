// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Data;

/// <summary>A fixed-capacity circular SEQUENCE: the newest <see cref="Capacity"/> items, oldest first, with the
/// oldest evicted by the act of appending rather than by a removal that costs something.
///
/// <para>Thread-safe for one writer and many readers (<see cref="ReaderWriterLockSlim"/>), and it never
/// allocates on <see cref="Append"/> — the backing array is fixed at construction. That is the whole reason a
/// streaming chart can take a sample every few milliseconds without handing the collector work.</para>
///
/// <para><b>A sequence, not a set.</b> It answers "what are the last N items, in order". A window that answers
/// "is this key anywhere in the last N buckets" is a different shape and must not be forced through here.</para>
///
/// <para><b>Arithmetic over the values is NOT in here.</b> A ring of <see cref="DateTime"/> or of a sample
/// record has no minimum worth the name, so <c>Min</c> and <c>Max</c> live in
/// <see cref="RingBufferExtensions"/> on the numeric instantiation. The ring decides where an item lives and
/// when it falls out; what an item MEANS is the caller's.</para></summary>
/// <typeparam name="T">What is remembered — a measurement, a state, a record of a moment.</typeparam>
public sealed class RingBuffer<T>
{
    private readonly T[] _buffer;
    private readonly ReaderWriterLockSlim _lock = new();
    private int _head; // next write position
    private int _count;

    /// <summary>Creates a ring that remembers the newest <paramref name="capacity"/> items.</summary>
    /// <param name="capacity">How many items to retain. Must be greater than zero.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is not positive.</exception>
    public RingBuffer(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(capacity, 0);
        Capacity = capacity;
        _buffer = new T[capacity];
    }

    /// <summary>How many items are currently held — at most <see cref="Capacity"/>.</summary>
    public int Count
    {
        get
        {
            _lock.EnterReadLock();
            try { return _count; }
            finally { _lock.ExitReadLock(); }
        }
    }

    /// <summary>How many items the ring can hold. Fixed at construction.</summary>
    public int Capacity { get; }

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
                ArgumentOutOfRangeException.ThrowIfNegative(index);
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _count);
                return _buffer[(Oldest() + index) % Capacity];
            }
            finally { _lock.ExitReadLock(); }
        }
    }

    /// <summary>Appends one item, evicting the oldest when the ring is full. Allocates nothing.</summary>
    /// <param name="value">The item to remember.</param>
    public void Append(T value)
    {
        _lock.EnterWriteLock();
        try
        {
            _buffer[_head] = value;
            _head = (_head + 1) % Capacity;
            if (_count < Capacity)
            {
                _count++;
            }
        }
        finally { _lock.ExitWriteLock(); }
    }

    /// <summary>Appends a span of items in order, evicting as many of the oldest as it takes. A span longer
    /// than <see cref="Capacity"/> keeps only its own tail — writing the whole span first would be work whose
    /// result is immediately overwritten.</summary>
    /// <param name="values">The items to remember, oldest first.</param>
    public void AppendRange(ReadOnlySpan<T> values)
    {
        _lock.EnterWriteLock();
        try
        {
            if (values.Length >= Capacity)
            {
                values[^Capacity..].CopyTo(_buffer);
                _head = 0;
                _count = Capacity;
                return;
            }

            foreach (var value in values)
            {
                _buffer[_head] = value;
                _head = (_head + 1) % Capacity;
                if (_count < Capacity)
                {
                    _count++;
                }
            }
        }
        finally { _lock.ExitWriteLock(); }
    }

    /// <summary>Copies everything held into <paramref name="destination"/>, oldest first. The destination needs
    /// at least <see cref="Count"/> elements.</summary>
    /// <param name="destination">Where to copy to.</param>
    /// <exception cref="ArgumentNullException"><paramref name="destination"/> is null.</exception>
    public void CopyTo(T[] destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        _lock.EnterReadLock();
        try { CopyHeld(destination); }
        finally { _lock.ExitReadLock(); }
    }

    /// <summary>Everything held, oldest first, as a new array.</summary>
    /// <returns>The items in logical order; empty when the ring is.</returns>
    public T[] ToArray()
    {
        _lock.EnterReadLock();
        try
        {
            if (_count == 0)
            {
                return [];
            }

            var result = new T[_count];
            CopyHeld(result);
            return result;
        }
        finally { _lock.ExitReadLock(); }
    }

    /// <summary>Empties the ring. Every slot is released as well as forgotten: a reference the ring no longer
    /// answers for must not be kept alive by it.</summary>
    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            Array.Clear(_buffer);
            _head = 0;
            _count = 0;
        }
        finally { _lock.ExitWriteLock(); }
    }

    /// <summary>Folds every held item with <paramref name="fold"/> under ONE read lock and without allocating —
    /// what <see cref="RingBufferExtensions"/> needs to answer a minimum or a maximum at the speed the old
    /// double-only buffer did. Returns false, and nothing, when the ring is empty.</summary>
    internal bool TryReduce(Func<T, T, T> fold, out T result)
    {
        _lock.EnterReadLock();
        try
        {
            if (_count == 0)
            {
                result = default!;
                return false;
            }

            int start = Oldest();
            result = _buffer[start];
            for (int i = 1; i < _count; i++)
            {
                result = fold(result, _buffer[(start + i) % Capacity]);
            }

            return true;
        }
        finally { _lock.ExitReadLock(); }
    }

    // Both copy paths, in one place: held items may sit in one run or in two, and getting that wrong twice is
    // how a wrapped buffer reads back shuffled.
    private void CopyHeld(T[] destination)
    {
        if (_count == 0)
        {
            return;
        }

        int start = Oldest();
        if (start + _count <= Capacity)
        {
            Array.Copy(_buffer, start, destination, 0, _count);
            return;
        }

        int firstChunk = Capacity - start;
        Array.Copy(_buffer, start, destination, 0, firstChunk);
        Array.Copy(_buffer, 0, destination, firstChunk, _count - firstChunk);
    }

    // Where the oldest held item sits. Caller holds the lock.
    private int Oldest() => (_head - _count + Capacity) % Capacity;
}
