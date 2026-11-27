// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace MatPlotLibNet;

/// <summary>The one table behind every thing the library looks up by name: interpolation engines, colormaps, style
/// sheets, series factories, file-format transforms and the axes renderer of each coordinate system. A registry class
/// keeps its public face (<c>Register</c>, <c>Get</c>, <c>Names</c>) and holds one of these for the rest.</summary>
/// <remarks>Safe for any number of threads reading and writing at once (a <see cref="ConcurrentDictionary{TKey, TValue}"/>
/// underneath, so a read never waits and never sees a half-written entry). A name written again keeps the value of the
/// last writer and the spelling of the first one. What counts as the same name is the comparer given at construction:
/// ordinal when none is given, so the registries that ignore case say so with <see cref="StringComparer.OrdinalIgnoreCase"/>.
/// A null name is refused. A registry holds its table in a static field: the table is the process-wide slot, and the
/// type itself keeps no state of its own.</remarks>
/// <typeparam name="TKey">The kind of name, such as <see cref="string"/> or an enum.</typeparam>
/// <typeparam name="TValue">What a name stands for.</typeparam>
internal sealed class NamedTable<TKey, TValue>(IEqualityComparer<TKey>? comparer = null)
    where TKey : notnull
    where TValue : class
{
    private readonly ConcurrentDictionary<TKey, TValue> _items = new(comparer);

    /// <summary>The names the table holds now, as a snapshot: a name written afterwards is not in it.</summary>
    public ICollection<TKey> Keys => _items.Keys;

    /// <summary>What <paramref name="name"/> stands for, or <see langword="null"/> when nothing was written under it.</summary>
    /// <param name="name">The name to look up.</param>
    public TValue? Get(TKey name) => _items.TryGetValue(name, out var value) ? value : null;

    /// <summary>Writes <paramref name="value"/> under <paramref name="name"/>, replacing what was there.</summary>
    /// <param name="name">The name to write under.</param>
    /// <param name="value">What the name stands for from now on.</param>
    public void Set(TKey name, TValue value) => _items[name] = value;

    /// <summary>Removes every entry.</summary>
    public void Clear() => _items.Clear();
}
