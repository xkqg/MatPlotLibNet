// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>An estimate a series makes from its observations (the curve of a density, the bodies of violins), kept on the
/// series with what it was made from: the series' change mark (<see cref="ChangeClock"/>) and a copy of its data
/// (<see cref="DataSnapshot"/>). The range of the axes and the renderer each ask for the estimate, and a figure drawn again asks
/// again; it is made only when a setting changed or a value changed in place since. Those are the two questions a kept drawing
/// of an axes asks too, so the estimate is made again exactly when the drawing would be.</summary>
/// <typeparam name="T">What is estimated.</typeparam>
/// <remarks>The series holds it in a field marked <see cref="RenderCacheAttribute"/>, so the copy of its data a draw takes leaves
/// it out. It is never changed once made: a draw that finds it stale makes a new one and puts it in the field in one write, so a
/// draw that reads the field reads one whole estimate.</remarks>
internal sealed class KeptEstimate<T>
{
    private readonly long _change;
    private readonly DataSnapshot _data;
    private readonly T _value;

    /// <summary>What else the estimate was made from (the rule of a density under its theme), boxed once when it is made;
    /// <see langword="null"/> for an estimate made from the series alone.</summary>
    private readonly object? _key;

    private KeptEstimate(long change, DataSnapshot data, T value, object? key)
    {
        _change = change;
        _data = data;
        _value = value;
        _key = key;
    }

    /// <summary>Whether this estimate was made from the series as it is now: its change mark and the very copy of its data.</summary>
    private bool MadeFrom(long change, DataSnapshot data) => _change == change && ReferenceEquals(_data, data);

    /// <summary>The estimate <paramref name="slot"/> keeps when <paramref name="series"/> has not changed since it was made;
    /// otherwise a new one, made by <paramref name="estimate"/> and kept in <paramref name="slot"/> from then on. The change mark
    /// and the copy of the data are taken before the estimate is made, so a change made while it is made is seen the next time.
    /// The copy is the one the series keeps (<see cref="DataSnapshot.Of"/>), which it hands out again while it still holds that
    /// data: a value is unchanged when the series hands out the very copy the estimate was made from, so the series' copy is the
    /// one answer to whether its data changed, and a draw that already copied the data for the kept drawing of its subplot hands
    /// the estimate that same copy, so the draw copies the data once.</summary>
    /// <typeparam name="TSeries">The series' own type, which <paramref name="estimate"/> reads.</typeparam>
    /// <param name="slot">The series' field that keeps the estimate.</param>
    /// <param name="series">The series the estimate is made from.</param>
    /// <param name="estimate">How the estimate is made; a static lambda, so asking allocates nothing.</param>
    /// <returns>The estimate of the series as it is now.</returns>
    internal static T Of<TSeries>(ref KeptEstimate<T>? slot, TSeries series, Func<TSeries, T> estimate) where TSeries : ChartSeries, IKeepsDataCopy
    {
        var kept = slot;
        long change = series.LatestChange;
        var data = DataSnapshot.Of(series);
        if (kept is not null && kept.MadeFrom(change, data)) return kept._value;

        var value = estimate(series);
        slot = new KeptEstimate<T>(change, data, value, null);
        return value;
    }

    /// <summary>The estimate <paramref name="slot"/> keeps when <paramref name="series"/> has not changed since it was made and it was
    /// made under <paramref name="key"/> (the rule of a density under the theme it is drawn by); otherwise a new one, made by
    /// <paramref name="estimate"/> under that key and kept from then on (<see cref="Of{TSeries}"/>). A series drawn under one theme and
    /// then another is estimated again for the other.</summary>
    /// <typeparam name="TSeries">The series' own type, which <paramref name="estimate"/> reads.</typeparam>
    /// <typeparam name="TKey">What else the estimate is made from; compared by value.</typeparam>
    /// <param name="slot">The series' field that keeps the estimate.</param>
    /// <param name="series">The series the estimate is made from.</param>
    /// <param name="key">What else the estimate is made from.</param>
    /// <param name="estimate">How the estimate is made under the key; a static lambda, so asking allocates nothing.</param>
    /// <returns>The estimate of the series as it is now, under <paramref name="key"/>.</returns>
    internal static T Of<TSeries, TKey>(ref KeptEstimate<T>? slot, TSeries series, TKey key, Func<TSeries, TKey, T> estimate)
        where TSeries : ChartSeries, IKeepsDataCopy where TKey : struct, IEquatable<TKey>
    {
        var kept = slot;
        long change = series.LatestChange;
        var data = DataSnapshot.Of(series);
        if (kept is not null && kept.MadeFrom(change, data) && kept._key is TKey made && made.Equals(key)) return kept._value;

        var value = estimate(series, key);
        slot = new KeptEstimate<T>(change, data, value, key);
        return value;
    }
}
