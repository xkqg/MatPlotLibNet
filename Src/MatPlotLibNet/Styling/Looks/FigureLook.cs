// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Styling;

/// <summary>What a figure itself does: the pad a fixed layout keeps above its top row of titles, the pad of a tight layout, and what its
/// legend lists.</summary>
internal sealed record FigureLook : IThemeKeys<FigureLook>
{
    /// <summary>The library's own figure.</summary>
    internal static FigureLook Library { get; } = new();

    /// <inheritdoc />
    public void WriteKeys(ThemeDto keys)
    {
        keys.FigureLegendListsEveryEntry = LegendListsEveryEntry.OrNullAtRest(false);
        keys.NoPadAboveTitle = NoPadAboveTitle.OrNullAtRest(false);
        keys.TightLayoutPad = TightPad;
    }

    /// <inheritdoc />
    public static FigureLook ReadKeys(ThemeDto keys) => new()
    {
        LegendListsEveryEntry = keys.FigureLegendListsEveryEntry ?? false,
        NoPadAboveTitle = keys.NoPadAboveTitle ?? false,
        TightPad = keys.TightLayoutPad,
    };

    /// <summary>Whether the legend of a figure lists every entry of every subplot, as matplotlib's <c>fig.legend()</c> does: two
    /// subplots that each plot a line labelled L give two rows. Otherwise an entry drawn exactly like one listed already is listed
    /// once, so a grid of small multiples names each line once.</summary>
    public bool LegendListsEveryEntry { get; init; }

    /// <summary>Whether a fixed layout keeps no pad above an axes' top band and title, as matplotlib's fixed subplot parameters keep
    /// none. Otherwise the layout's own pad (<c>ConstrainedLayoutEngine.PadTop</c>) above the title of the top row. A tight or
    /// constrained layout keeps its pad either way, as matplotlib's <c>tight_layout</c> does.</summary>
    public bool NoPadAboveTitle { get; init; }

    /// <summary>The pad a tight layout keeps at every edge of the figure and once between two subplots, in multiples of the theme's
    /// font size: matplotlib's <c>tight_layout</c> pad (1.08 under both its styles, <c>w_pad</c> and <c>h_pad</c> defaulting to it).
    /// Unset: the library's pads, each axes' own in a gap (<c>Rendering.Layout.LayoutPads.Library</c>).</summary>
    public double? TightPad { get; init; }
}
