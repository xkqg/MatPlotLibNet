// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections;

namespace MatPlotLibNet.Models;

/// <summary>The Y axes of ONE side of an <see cref="Axes"/>, in stack order: index 0 stands nearest the plot and each
/// further slot stacks outward past the ones before it. There is no cap; <see cref="Add"/> stacks one more slot every
/// time it is called. The left stack's slot 0 is the primary Y axis (<see cref="Axes.YAxis"/>); the right stack starts
/// empty.</summary>
/// <remarks>Enumerating a stack yields its slots nearest the plot first, primary included.
/// <para>The OUTWARD index is the numbering the interaction wire and the 1.23.0 JSON fields use: it counts only the
/// slots past the primary, so outward index <c>i</c> is stack index <c>i + </c><see cref="OutwardOffset"/>
/// (left: <c>i + 1</c>; right: <c>i</c>). <see cref="SlotAtOutward"/> is the one place that converts.</para></remarks>
public sealed class YAxisStack : IReadOnlyList<YAxisSlot>
{
    private readonly List<YAxisSlot> _slots = [];

    /// <param name="side">The side every slot of this stack stands on.</param>
    /// <param name="primarySlots">How many slots the stack holds from birth — the primary Y axis on the left (1), none
    /// on the right (0). They come before every outward slot, so this is also the <see cref="OutwardOffset"/>.</param>
    internal YAxisStack(AxisSide side, int primarySlots)
    {
        Side = side;
        OutwardOffset = primarySlots;
        for (int i = 0; i < primarySlots; i++) Add();
    }

    /// <summary>The side every slot of this stack stands on.</summary>
    public AxisSide Side { get; }

    /// <summary>How many slots precede the first outward slot: 1 on the left (the primary Y axis is left slot 0),
    /// 0 on the right. Stack index = outward index + this offset.</summary>
    public int OutwardOffset { get; }

    /// <summary>The number of slots on this side, the primary included on the left.</summary>
    public int Count => _slots.Count;

    /// <summary>How many slots stand past the primary: the length of the outward numbering.</summary>
    internal int OutwardCount => _slots.Count - OutwardOffset;

    /// <summary>How many series stand on the slots past the primary, summed over those slots: the series that follow the
    /// primary's in the subplot's series numbering (<see cref="Axes.AllSeries"/>). The primary Y axis' own series
    /// (<see cref="Axes.Series"/>) never count, whatever the side.</summary>
    internal int OutwardSeriesCount => _slots.Skip(OutwardOffset).Sum(slot => slot.Series.Count);

    /// <summary>The slot at stack index <paramref name="index"/>, 0 nearest the plot.</summary>
    /// <param name="index">The stack index.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> names no slot.</exception>
    public YAxisSlot this[int index] => _slots[index];

    /// <summary>Stacks one more Y axis on this side, past every slot already there, and returns it. Every call adds a
    /// slot; there is no cap.</summary>
    /// <returns>The new slot, the outermost on this side.</returns>
    /// <example><code>var rate = axes.YAxes.Right.Add();
    /// rate.Axis.Label = "Rate";
    /// rate.AddSeries(new LineSeries(x, y));</code></example>
    public YAxisSlot Add()
    {
        var slot = new YAxisSlot(Side);
        _slots.Add(slot);
        return slot;
    }

    /// <summary>Returns the slot nearest the plot, creating it only when this side has none. It never stacks: called
    /// twice it returns the same slot. On the left that is always the primary Y axis.</summary>
    /// <returns>Slot 0 of this side.</returns>
    public YAxisSlot FirstOrAdd() => _slots.Count > 0 ? _slots[0] : Add();

    /// <summary>The slot at OUTWARD index <paramref name="outward"/> (counted past the primary, see
    /// <see cref="OutwardOffset"/>), or <see langword="null"/> when no slot has that index. A negative index is
    /// <see langword="null"/> too, so an outward index never reaches the primary Y axis.</summary>
    /// <param name="outward">The outward index, 0 for the first slot past the primary.</param>
    /// <returns>The slot, or <see langword="null"/>.</returns>
    public YAxisSlot? SlotAtOutward(int outward) =>
        (uint)outward < (uint)OutwardCount ? _slots[outward + OutwardOffset] : null;

    /// <summary>Enumerates the slots nearest the plot first, the primary included on the left.</summary>
    /// <returns>An enumerator over the slots in stack order.</returns>
    public IEnumerator<YAxisSlot> GetEnumerator() => _slots.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
