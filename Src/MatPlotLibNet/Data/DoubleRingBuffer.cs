// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Data;

/// <summary>Fixed-capacity circular buffer for <see cref="double"/> values.
/// Thread-safe for concurrent single-writer / multi-reader access via <see cref="ReaderWriterLockSlim"/>.
/// Never allocates on <see cref="Append"/>; the backing array is fixed at construction.</summary>
public sealed class DoubleRingBuffer
{
    private readonly double[] _buffer;
    private readonly ReaderWriterLockSlim _lock = new();
    private int _head; // next write position
    private int _count;

    /// <summary>Number of values currently stored.</summary>
    public int Count
    {
        get
        {
            _lock.EnterReadLock();
            try { return _count; }
            finally { _lock.ExitReadLock(); }
        }
    }

    /// <summary>Maximum number of values the buffer can hold.</summary>
    public int Capacity { get; }

    /// <summary>Minimum value in the buffer, or <see cref="double.NaN"/> if empty.</summary>
    public double Min
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                if (_count == 0) return double.NaN;
                double min = double.MaxValue;
                int start = (_head - _count + Capacity) % Capacity;
                for (int i = 0; i < _count; i++)
                    min = Math.Min(min, _buffer[(start + i) % Capacity]);
                return min;
            }
            finally { _lock.ExitReadLock(); }
        }
    }

    /// <summary>Maximum value in the buffer, or <see cref="double.NaN"/> if empty.</summary>
    public double Max
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                if (_count == 0) return double.NaN;
                double max = double.MinValue;
                int start = (_head - _count + Capacity) % Capacity;
                for (int i = 0; i < _count; i++)
                    max = Math.Max(max, _buffer[(start + i) % Capacity]);
                return max;
            }
            finally { _lock.ExitReadLock(); }
        }
    }

    /// <summary>Initializes a new ring buffer with the specified capacity.</summary>
    /// <param name="capacity">Maximum number of values to retain. Must be &gt; 0.</param>
    public DoubleRingBuffer(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(capacity, 0);
        Capacity = capacity;
        _buffer = new double[capacity];
    }

    /// <summary>Gets the value at the specified logical index (0 = oldest retained value).</summary>
    public double this[int index]
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                ArgumentOutOfRangeException.ThrowIfNegative(index);
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _count);
                int start = (_head - _count + Capacity) % Capacity;
                return _buffer[(start + index) % Capacity];
            }
            finally { _lock.ExitReadLock(); }
        }
    }

    /// <summary>Appends a single value, evicting the oldest if at capacity.</summary>
    public void Append(double value)
    {
        _lock.EnterWriteLock();
        try
        {
            _buffer[_head] = value;
            _head = (_head + 1) % Capacity;
            if (_count < Capacity) _count++;
        }
        finally { _lock.ExitWriteLock(); }
    }

    /// <summary>Appends a span of values, evicting oldest values as needed.</summary>
    public void AppendRange(ReadOnlySpan<double> values)
    {
        _lock.EnterWriteLock();
        try
        {
            if (values.Length >= Capacity)
            {
                // Only keep the last Capacity values
                values[^Capacity..].CopyTo(_buffer);
                _head = 0;
                _count = Capacity;
                return;
            }

            foreach (double v in values)
            {
                _buffer[_head] = v;
                _head = (_head + 1) % Capacity;
                if (_count < Capacity) _count++;
            }
        }
        finally { _lock.ExitWriteLock(); }
    }

    /// <summary>Copies all retained values to <paramref name="destination"/> in logical order
    /// (oldest first). The destination must have at least <see cref="Count"/> elements.</summary>
    public void CopyTo(double[] destination)
    {
        _lock.EnterReadLock();
        try
        {
            if (_count == 0) return;
            int start = (_head - _count + Capacity) % Capacity;
            if (start + _count <= Capacity)
            {
                Array.Copy(_buffer, start, destination, 0, _count);
            }
            else
            {
                int firstChunk = Capacity - start;
                Array.Copy(_buffer, start, destination, 0, firstChunk);
                Array.Copy(_buffer, 0, destination, firstChunk, _count - firstChunk);
            }
        }
        finally { _lock.ExitReadLock(); }
    }

    /// <summary>Returns a new array containing all retained values in logical order.</summary>
    public double[] ToArray()
    {
        _lock.EnterReadLock();
        try
        {
            if (_count == 0) return [];
            var result = new double[_count];
            int start = (_head - _count + Capacity) % Capacity;
            if (start + _count <= Capacity)
            {
                Array.Copy(_buffer, start, result, 0, _count);
            }
            else
            {
                int firstChunk = Capacity - start;
                Array.Copy(_buffer, start, result, 0, firstChunk);
                Array.Copy(_buffer, 0, result, firstChunk, _count - firstChunk);
            }
            return result;
        }
        finally { _lock.ExitReadLock(); }
    }

    /// <summary>Removes all values from the buffer.</summary>
    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            _head = 0;
            _count = 0;
        }
        finally { _lock.ExitWriteLock(); }
    }
}
