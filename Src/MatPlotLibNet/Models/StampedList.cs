// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Models;

/// <summary>A list a figure draws from that reports its changes: adding an item or taking one away stamps it, and its
/// <see cref="LatestChange"/> is the newest of that stamp and the newest change of every item in it
/// (<see cref="ChangeClock.LatestOf"/>). An axes holds fifteen of them and most stay empty, so the items are only given a list
/// when the first one is added.</summary>
/// <typeparam name="T">The item type.</typeparam>
internal sealed class StampedList<T> : IReadOnlyList<T>, IChangeTracked
{
    private List<T>? _items;
    private long _stamp;

    /// <inheritdoc />
    public int Count => _items?.Count ?? 0;

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> names no item.</exception>
    public T this[int index] => _items![index.ThrowIfNotIndexOf(Count, nameof(index))];

    /// <summary>Adds <paramref name="item"/> at the end.</summary>
    internal void Add(T item)
    {
        (_items ??= []).Add(item);
        _stamp = ChangeClock.Next();
    }

    /// <summary>Takes <paramref name="item"/> out, when it is there.</summary>
    /// <returns><see langword="true"/> when it was there.</returns>
    internal bool Remove(T item)
    {
        if (_items is null || !_items.Remove(item)) return false;
        _stamp = ChangeClock.Next();
        return true;
    }

    /// <summary>Takes out the item at <paramref name="index"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> names no item.</exception>
    internal void RemoveAt(int index)
    {
        _items!.RemoveAt(index.ThrowIfNotIndexOf(Count, nameof(index)));
        _stamp = ChangeClock.Next();
    }

    /// <summary>The mark of the newest item added or taken away, leaving out changes inside the items.</summary>
    internal long LatestAddOrRemove => _stamp;

    /// <inheritdoc />
    public long LatestChange
    {
        get
        {
            long latest = _stamp;
            if (_items is null) return latest;
            foreach (var item in _items)
                latest = ChangeClock.Latest(latest, ChangeClock.LatestOf(item));
            return latest;
        }
    }

    /// <summary>The items in order, without allocating.</summary>
    public List<T>.Enumerator GetEnumerator() => (_items ?? Empty).GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>The one empty list every empty <see cref="StampedList{T}"/> of this item type enumerates; never added to.</summary>
    private static readonly List<T> Empty = [];
}
