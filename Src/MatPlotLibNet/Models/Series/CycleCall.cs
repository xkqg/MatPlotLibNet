// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>How the matplotlib call a series kind matches asks its axes for a colour when it is given none. matplotlib's axes keeps a
/// cycle for its lines and one for its patches, and its classic mode lets a few calls take a colour of the style's instead; which cycle
/// each of these asks lies with the theme's <see cref="Styling.CycleRule"/>.</summary>
internal enum CycleCall : byte
{
    /// <summary>The lines cycle (<c>_get_lines</c>): <c>plot</c>, <c>step</c>, <c>errorbar</c>, <c>hist</c>, <c>stackplot</c>,
    /// <c>streamplot</c> and the series that draw a line of their own.</summary>
    Line,

    /// <summary>The patch cycle (<c>_get_patches_for_fill</c>), or none in classic mode, where a scatter takes <c>'b'</c>:
    /// <c>scatter</c> and what seaborn draws with it.</summary>
    Scatter,

    /// <summary>The patch cycle, or none in classic mode, where the fill takes <c>patch.facecolor</c>: <c>fill_between</c>.</summary>
    FillBetween,

    /// <summary>The patch cycle in every mode: <c>bar</c>, <c>pie</c> (one colour a wedge), <c>fill</c>, <c>bar3d</c>,
    /// <c>voxels</c>.</summary>
    Patch,

    /// <summary>The lines cycle, or none in classic mode, where the body takes <c>'y'</c>: <c>violinplot</c>.</summary>
    Violin,

    /// <summary>Neither cycle: <c>stem</c>, <c>eventplot</c>, <c>broken_barh</c>, <c>boxplot</c>, <c>quiver</c>, <c>barbs</c> and every
    /// call that colours by a colour map.</summary>
    Neither,
}
