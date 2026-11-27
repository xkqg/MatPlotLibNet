// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Styling;

/// <summary>How the colours of the theme's cycle are handed out.</summary>
internal sealed record CycleLook : IThemeKeys<CycleLook>
{
    /// <summary>The library's own cycle: one, in the order the series were added, and an unnamed group in its series' colour.</summary>
    internal static CycleLook Library { get; } = new();

    /// <inheritdoc />
    public void WriteKeys(ThemeDto keys)
    {
        keys.GroupsWearTheCycle = GroupsWearTheCycle.OrNullAtRest(false);
        keys.Cycles = Rule.OrNullAtRest(Library.Rule);
    }

    /// <inheritdoc />
    public static CycleLook ReadKeys(ThemeDto keys) =>
        new() { Rule = keys.Cycles ?? Library.Rule, GroupsWearTheCycle = keys.GroupsWearTheCycle ?? false };

    /// <summary>How the colours go to the series of an axes: one cycle (<see cref="CycleRule.OneCycle"/>), or matplotlib's two, one
    /// for the lines and one for the patches. <c>SeriesSlotExtensions.ColorSlots</c> holds the rule.</summary>
    public CycleRule Rule { get; init; }

    /// <summary>Whether what its author names no colour for takes the colours of the cycle, as matplotlib and seaborn colour it: the
    /// groups of a strip and a swarm plot (<c>SeriesRenderer.GroupColor</c>), and a series an indicator adds, in the colour of its
    /// slot in place of the colour the indicator chose (<c>SeriesSlot.UnnamedColorOf</c>). Otherwise every group in the colour of
    /// the series' slot, and an indicator's series in the colour it chose.</summary>
    public bool GroupsWearTheCycle { get; init; }
}
