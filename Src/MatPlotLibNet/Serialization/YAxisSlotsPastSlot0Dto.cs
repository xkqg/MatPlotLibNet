// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Serialization;

/// <summary>The slots past slot 0 of one side, the way the wire carries them: the side's frozen 1.23.0 list
/// (<see cref="AxesDto.ExtraRightYAxes"/> or <see cref="AxesDto.LeftYAxes"/>) and its overflow list
/// (<see cref="AxesDto.RightYAxesOverflow"/> or <see cref="AxesDto.LeftYAxesOverflow"/>), each <see langword="null"/>
/// when it holds none.</summary>
/// <param name="Frozen">The entries of the side's frozen list.</param>
/// <param name="Overflow">The entries of the side's overflow list, continuing the outward order where
/// <paramref name="Frozen"/> stops.</param>
internal readonly record struct YAxisSlotsPastSlot0Dto(List<SecondaryAxisSlotDto>? Frozen, List<SecondaryAxisSlotDto>? Overflow)
{
    /// <summary>How many slots past slot 0 a 1.23.0 reader holds per side (right slot 1, left slot 1); it merges every
    /// further entry of the frozen list onto that one, so the writer puts no more than this many there.</summary>
    internal const int FrozenCap = 1;

    /// <summary>Every entry in outward order: the frozen list's, then the overflow list's.</summary>
    internal IEnumerable<SecondaryAxisSlotDto> InOutwardOrder => (Frozen ?? []).Concat(Overflow ?? []);

    /// <summary>The slots past slot 0 of <paramref name="stack"/>, in outward order: the first <see cref="FrozenCap"/> into
    /// the side's frozen field, every further one into its overflow field, each list <see langword="null"/> (left out) when it
    /// holds none. So every slot has exactly one carrier, and a figure within the 1.23.0 caps writes the bytes 1.23.0
    /// wrote.</summary>
    /// <param name="stack">The side's stack of the axes' Y axes.</param>
    internal static YAxisSlotsPastSlot0Dto Of(YAxisStack stack)
    {
        var pastSlot0 = stack.Skip(1).Select(SecondaryAxisSlotDto.Of).ToList();
        return new(
            pastSlot0.Count > 0 ? pastSlot0.Take(FrozenCap).ToList() : null,
            pastSlot0.Count > FrozenCap ? pastSlot0.Skip(FrozenCap).ToList() : null);
    }

    /// <summary>The one reader of the slots past slot 0 of a side: each entry, the frozen field's and then the overflow
    /// field's, stacks exactly one slot on <paramref name="stack"/>. Slot 0 is made first (on the left it is the primary,
    /// always there), so an entry never moves down into slot 0, even from a JSON that carries no slot 0 of the right side.</summary>
    /// <param name="axes">The axes being read; its series registry builds each slot's series.</param>
    /// <param name="stack">The side's stack of the axes' Y axes.</param>
    internal void AddTo(Axes axes, YAxisStack stack)
    {
        foreach (var slotDto in InOutwardOrder)
        {
            stack.FirstOrAdd();
            slotDto.RestoreOnto(axes, stack.Add());
        }
    }
}
