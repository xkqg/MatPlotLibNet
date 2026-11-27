// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.TickLocators;

/// <summary>How many ticks a locator asks for along an axis with room for a number of labels: matplotlib's
/// <c>np.clip(get_tick_space(), Fewest, Most)</c>, the one rule every locator that reads the room clamps it by. A room that is no
/// number (no axis to ask) and a room without end (a label of no size) ask for the most, as matplotlib's locators ask for nine with no
/// axis and its <c>get_tick_space</c> answers a label of no size with <c>2**31 - 1</c>.</summary>
/// <param name="Fewest">The fewest ticks asked for, however little room there is.</param>
/// <param name="Most">The most ticks asked for, however much room there is.</param>
internal readonly record struct TickBudget(int Fewest, int Most)
{
    /// <summary>Two to nine: <c>LogLocator</c>'s <c>n_request</c>, the most bins any of matplotlib's tick locators lays (its default
    /// <c>MaxNLocator</c>, a spy plot's), and the short ruler of a colour bar and of the rings of a polar axes under the library's own
    /// themes.</summary>
    internal static TickBudget Standard { get; } = new(2, 9);

    /// <summary><c>LogitLocator</c>'s: at least two, and no most (<c>nbins = get_tick_space(); if nbins &lt; 2: nbins = 2</c>).</summary>
    internal static TickBudget Logit { get; } = new(2, int.MaxValue);

    /// <summary>The ticks asked for along an axis with room for <paramref name="tickSpace"/> labels: that many, held between
    /// <see cref="Fewest"/> and <see cref="Most"/>; the most for a room that is no number or without end.</summary>
    /// <param name="tickSpace">How many labels fit along the axis (<see cref="MaxNLocator.LabelsAlong"/>).</param>
    /// <returns>The count.</returns>
    internal int Clamp(double tickSpace) => !(tickSpace < Most) ? Most : tickSpace < Fewest ? Fewest : (int)tickSpace;

    /// <summary>Every count this budget asks for, from <see cref="Fewest"/> to <see cref="Most"/>: the one walk a layout that keeps room
    /// before it knows the length takes (a colour bar's widest label, a log locator's labels at any length). A struct enumerator, so a
    /// <c>foreach</c> allocates nothing.</summary>
    /// <returns>The enumerator, before the fewest.</returns>
    public Enumerator GetEnumerator() => new(this);

    /// <summary>Walks the counts of a budget (<see cref="GetEnumerator"/>).</summary>
    internal struct Enumerator
    {
        private readonly int _most;
        private int _count;

        /// <summary>An enumerator before the fewest count of <paramref name="budget"/>.</summary>
        /// <param name="budget">The budget walked.</param>
        internal Enumerator(TickBudget budget)
        {
            _most = budget.Most;
            _count = budget.Fewest - 1;
        }

        /// <summary>The count the walk stands on.</summary>
        public readonly int Current => _count;

        /// <summary>Moves to the next count.</summary>
        /// <returns><see langword="true"/> while the count is at most the budget's most.</returns>
        public bool MoveNext()
        {
            if (_count >= _most) return false;
            _count++;
            return true;
        }
    }
}
