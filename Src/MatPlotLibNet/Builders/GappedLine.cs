// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using MatPlotLibNet.Rendering.Downsampling;

namespace MatPlotLibNet;

/// <summary>The segments a <see cref="GappedLine"/> is asked to hold, one by index.</summary>
internal interface ISegmentSource
{
    /// <summary>The segment at <paramref name="index"/>.</summary>
    /// <param name="index">The segment's place.</param>
    /// <param name="x0">The x coordinate of its start.</param>
    /// <param name="y0">The y coordinate of its start.</param>
    /// <param name="x1">The x coordinate of its end.</param>
    /// <param name="y1">The y coordinate of its end.</param>
    /// <returns><see langword="false"/> when the segment is left out.</returns>
    bool TryGet(int index, out double x0, out double y0, out double x1, out double y1);
}

/// <summary>The points of ONE line that is drawn as several runs, with a missing point (NaN) between each two runs. The one owner of
/// that separator, so everything the library writes in this shape writes the same form of it: a single separator between two
/// runs, none in front of the first run, none after the last, never two in a row. A line series reads any arrangement of
/// missing points, so a document written in an older form draws as it did.
/// <para><see cref="Gap"/> says the next point begins a new run; the separator is written when that point arrives, so a gap
/// asked for first, last or twice writes nothing extra. A missing point the caller adds is data and is kept as it is.</para></summary>
/// <param name="capacity">The number of points to make room for; 0 leaves the room to grow.</param>
internal sealed class GappedLine(int capacity = 0)
{
    private double[] _x = new double[capacity];
    private double[] _y = new double[capacity];
    private bool _gapPending;

    /// <summary>How many points the line holds, separators included.</summary>
    internal int Count { get; private set; }

    /// <summary>Says that the next point added begins a new run. Nothing is written for it unless a point follows an earlier one.</summary>
    internal void Gap() => _gapPending = Count > 0;

    /// <summary>Adds a point to the current run.</summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Add(double x, double y)
    {
        if (_gapPending || Count == _x.Length)
        {
            BeginPoints(1);
        }

        _x[Count] = x;
        _y[Count] = y;
        Count++;
    }

    /// <summary>The line of the segments a source holds, each a run of two points with a separator between two of them, in one
    /// loop over arrays of the exact length every segment drawn takes (handed back without a copy when none is left out). The
    /// verbs that write a great many segments (<see cref="SegmentPath"/>) make their line here, and the source is a struct type
    /// argument so the call to it is inlined. It writes no line of its own: the same loop over the arrays a line holds costs 3 µs
    /// more for 10,000 segments.</summary>
    /// <typeparam name="TSource">The kind of source.</typeparam>
    /// <param name="count">How many segments the source holds.</param>
    /// <param name="source">The segments.</param>
    /// <returns>The points, separators included.</returns>
    internal static XYData Segments<TSource>(int count, TSource source) where TSource : struct, ISegmentSource
    {
        int length = count == 0 ? 0 : (3 * count) - 1;
        double[] x = new double[length];
        double[] y = new double[length];
        int at = 0;
        for (int i = 0; i < count; i++)
        {
            if (!source.TryGet(i, out double x0, out double y0, out double x1, out double y1))
            {
                continue;
            }

            if (at > 0)
            {
                x[at] = double.NaN;
                y[at] = double.NaN;
                at++;
            }

            x[at] = x0;
            y[at] = y0;
            x[at + 1] = x1;
            y[at + 1] = y1;
            at += 2;
        }

        if (at < length)
        {
            Array.Resize(ref x, at);
            Array.Resize(ref y, at);
        }

        return new XYData(x, y);
    }

    /// <summary>Adds the points of a run: <paramref name="x"/> and <paramref name="y"/> pair by index. An empty run adds nothing.</summary>
    /// <param name="x">The x coordinates.</param>
    /// <param name="y">The y coordinates, as many as <paramref name="x"/>.</param>
    /// <exception cref="ArgumentException">The two arrays differ in length.</exception>
    internal void AddRange(double[] x, double[] y)
    {
        x.ThrowIfNotOneYPerX(y);

        if (x.Length == 0)
        {
            return;
        }

        BeginPoints(x.Length);
        x.CopyTo(_x, Count);
        y.CopyTo(_y, Count);
        Count += x.Length;
    }

    /// <summary>The line as its x and y coordinates, the arrays the line holds (they are trimmed to its length, which copies them
    /// unless the capacity was exact).</summary>
    /// <returns>The points, separators included.</returns>
    internal XYData ToData()
    {
        if (_x.Length != Count)
        {
            Array.Resize(ref _x, Count);
            Array.Resize(ref _y, Count);
        }

        return new XYData(_x, _y);
    }

    /// <summary>Grows the arrays to hold at least <paramref name="needed"/> points.</summary>
    private void Reserve(int needed)
    {
        int grown = Math.Max(needed, Math.Max(4, _x.Length * 2));
        Array.Resize(ref _x, grown);
        Array.Resize(ref _y, grown);
    }

    /// <summary>Makes room for <paramref name="points"/> more points and writes the separator a pending gap asks for.</summary>
    private void BeginPoints(int points)
    {
        int needed = Count + points + (_gapPending ? 1 : 0);
        if (needed > _x.Length)
        {
            Reserve(needed);
        }

        if (_gapPending)
        {
            _x[Count] = double.NaN;
            _y[Count] = double.NaN;
            Count++;
            _gapPending = false;
        }
    }
}
