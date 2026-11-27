// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>The one clock every change to a figure is stamped with. It only ever counts up, across every figure in the
/// process, so a change made later always carries a later mark than one made earlier, whichever object either was made to:
/// a draw that remembers the clock's reading can tell, for any part of a figure, whether anything in it changed since.</summary>
internal static class ChangeClock
{
    private static long _now;

    /// <summary>The next mark: later than every mark handed out before it.</summary>
    internal static long Next() => Interlocked.Increment(ref _now);

    /// <summary>Stores <paramref name="value"/> in <paramref name="slot"/> and stamps <paramref name="stamp"/> with the next
    /// mark, unless the slot already holds an equal value: setting what is there changes nothing, so it marks nothing (a
    /// renderer that sets a flag to what it already is on every draw must not make the part look changed).</summary>
    /// <returns><see langword="true"/> when the value changed.</returns>
    internal static bool Change<T>(ref T slot, T value, ref long stamp)
    {
        if (EqualityComparer<T>.Default.Equals(slot, value)) return false;
        slot = value;
        stamp = Next();
        return true;
    }

    /// <summary>The later of two marks.</summary>
    internal static long Latest(long a, long b) => a >= b ? a : b;

    /// <summary>What <paramref name="item"/> contributes to its holder's newest change: its own
    /// <see cref="IChangeTracked.LatestChange"/> when it reports its changes; nothing for no item or for a type of this library
    /// that does not (every public type of the library with a setter reports its changes, so one that does not cannot change);
    /// and <see cref="long.MaxValue"/>, always changed, for a type written outside the library (a series or a formatter a
    /// caller implemented), which may change without saying so.</summary>
    internal static long LatestOf(object? item) => item switch
    {
        IChangeTracked tracked => tracked.LatestChange,
        null => 0,
        _ when item.GetType().Assembly == typeof(ChangeClock).Assembly => 0,
        _ => long.MaxValue,
    };
}
