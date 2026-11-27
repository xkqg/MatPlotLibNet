// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>What the library adds to <see cref="ICategoryLabeled"/>: along which axis a series stands its categories and in which order. A
/// series that does not say it stands them along X, the first at the left. Read by the tick source of an axis
/// (<c>AxesTickSourceExtensions.PrimaryTickSource</c>), which names the categories where the series stands them.</summary>
/// <remarks>Beside the public interface, so an application that implements <see cref="ICategoryLabeled"/> is not asked for more.</remarks>
internal interface ICategoryPlacement
{
    /// <summary>Whether the categories stand along the Y axis: bars that lie down, a Gantt chart's tasks.</summary>
    bool CategoriesAlongY { get; }

    /// <summary>Whether the first category stands in the highest slot of a Y axis, the rows read from the top down
    /// (<c>CategorySlot.FromTop</c>), as a Gantt chart lists its tasks; otherwise the first stands in the lowest.</summary>
    bool CategoriesTopDown => false;

    /// <summary>Whether the axis the categories are named on still rounds its range out under a theme that rounds to ticks (matplotlib's
    /// classic <c>axes.autolimit_mode</c> <c>'round_numbers'</c>): the rows of a chart numbered first and named after, as matplotlib names
    /// a Gantt chart's rows with <c>set_yticks</c>, which widens the view to the ticks while the default locator still places them, so
    /// <c>barh</c> rows from -0.3 to 3.3 end at -0.5 and 3.5 (printed under the classic style). Otherwise the axis is placed by its fixed
    /// set of named ticks, as matplotlib's string categories are, and no rounding moves it.</summary>
    bool RangeRoundsOut => false;
}
