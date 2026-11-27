// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;

namespace MatPlotLibNet.Models.Series.Streaming;

/// <summary>The samples of a streaming series and the count of their changes, kept together: every streaming kind (a line, a
/// scatter, a candlestick, a sampled signal) holds one, so an append, a batch and a clear store, count and mark the change in one
/// place. The samples sit in a <see cref="RingBuffer{T}"/> (one writer, many readers); the version is what a streaming figure's
/// clock asks every series through <see cref="IVersionedSeries"/>, and the mark is the series' share of the figure's
/// <see cref="ChangeClock"/> reading, so a figure drawn again knows the stream moved.</summary>
/// <typeparam name="TSample">What one sample is: a point, a bar, a value with its ordinal.</typeparam>
internal sealed class StreamRing<TSample>
{
    private readonly RingBuffer<TSample> _samples;
    private long _version;
    private long _stamp;

    /// <summary>A ring that keeps the newest <paramref name="capacity"/> samples, holding none.</summary>
    /// <param name="capacity">How many samples it keeps; at least 1.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is below 1.</exception>
    internal StreamRing(int capacity) => _samples = new RingBuffer<TSample>(capacity);

    /// <summary>How many samples it keeps once full.</summary>
    internal int Capacity => _samples.Capacity;

    /// <summary>How many samples it holds.</summary>
    internal int Count => _samples.Count;

    /// <summary>How many times the samples changed: one more at every append, batch and clear.</summary>
    internal long Version => Interlocked.Read(ref _version);

    /// <summary>The <see cref="ChangeClock"/> mark of the newest change to the samples; 0 before the first.</summary>
    internal long LatestChange => Volatile.Read(ref _stamp);

    /// <summary>The sample at a place, where 0 is the oldest held.</summary>
    /// <param name="index">The place, from 0 to <see cref="Count"/> - 1.</param>
    internal TSample this[int index] => _samples[index];

    /// <summary>Takes one sample, evicting the oldest when full, and counts the change.</summary>
    /// <param name="sample">The sample.</param>
    internal void Append(TSample sample)
    {
        _samples.Append(sample);
        Changed();
    }

    /// <summary>Takes a batch of samples, oldest first, and counts it as one change.</summary>
    /// <param name="samples">The samples.</param>
    internal void AppendRange(ReadOnlySpan<TSample> samples)
    {
        _samples.AppendRange(samples);
        Changed();
    }

    /// <summary>Forgets every sample and counts the change.</summary>
    internal void Clear()
    {
        _samples.Clear();
        Changed();
    }

    /// <summary>The samples held, oldest first, with the version read before them: a reader that compares the version later sees a
    /// change made after the copy, never misses one made before it.</summary>
    /// <returns>A copy of the samples and the version.</returns>
    internal Held Read()
    {
        long version = Version;
        return new(_samples.ToArray(), version);
    }

    /// <summary>Walks the samples held, oldest first, folding each into <paramref name="seed"/>: one pass over one state, nothing
    /// copied. A <c>static</c> lambda and a value-type state allocate nothing.</summary>
    /// <typeparam name="TState">What is accumulated.</typeparam>
    /// <param name="seed">The start, and the answer when the ring is empty.</param>
    /// <param name="fold">Folds one sample into the state.</param>
    /// <returns>The state after every sample held.</returns>
    internal TState Aggregate<TState>(TState seed, Func<TState, TSample, TState> fold) => _samples.Aggregate(seed, fold);

    private void Changed()
    {
        Interlocked.Increment(ref _version);
        Volatile.Write(ref _stamp, ChangeClock.Next());
    }

    /// <summary>The samples a ring held at one moment and its version then.</summary>
    /// <param name="Samples">The samples, oldest first.</param>
    /// <param name="Version">The version read before the samples were copied.</param>
    internal readonly record struct Held(TSample[] Samples, long Version);
}
