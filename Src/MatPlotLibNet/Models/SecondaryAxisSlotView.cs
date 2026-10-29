// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections;

namespace MatPlotLibNet.Models;

/// <summary>The outward slots of one <see cref="YAxisStack"/> — every slot past the primary — as the published
/// <see cref="SecondaryAxisSlot"/> list 1.23.0 handed out. A live view, as the list it replaces was: a slot added to the
/// stack later shows here too. Indexed by the OUTWARD index, through <see cref="YAxisStack.SlotAtOutward"/>.</summary>
/// <param name="stack">The side's stack.</param>
[Obsolete("Only the obsolete Axes.RightYAxes and Axes.LeftYAxes use this view; they go when the lists they return do.")]
internal sealed class SecondaryAxisSlotView(YAxisStack stack) : IReadOnlyList<SecondaryAxisSlot>
{
    public int Count => stack.OutwardCount;

    public SecondaryAxisSlot this[int index] =>
        stack.SlotAtOutward(index) is { } slot ? new SecondaryAxisSlot(slot) : throw new ArgumentOutOfRangeException(nameof(index));

    public IEnumerator<SecondaryAxisSlot> GetEnumerator() =>
        stack.Skip(stack.OutwardOffset).Select(slot => new SecondaryAxisSlot(slot)).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
