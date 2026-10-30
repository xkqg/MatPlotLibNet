// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The parts a stem-and-leaf display is made of, handed to the callback of
/// <see cref="DistributionAxesExtensions.StemAndLeaf"/> so each can be styled on its own: the table of the stems and leaves and the
/// line that says how to read it. They are the very series and annotation on the axes, so a change made through them is the chart's.
/// <para>A value of this type can only be made by the library: it has no public constructor, and its properties have no
/// setters.</para></summary>
public readonly record struct StemAndLeafParts
{
    /// <summary>Makes the parts of one stem-and-leaf display.</summary>
    /// <param name="table">The table of stems and leaves.</param>
    /// <param name="key">The line that says how to read it.</param>
    internal StemAndLeafParts(TableSeries table, Annotation key)
    {
        Table = table;
        Key = key;
    }

    /// <summary>The display as a table with no borders and no fill: one row for each stem, and in each row the stem (aligned to
    /// the right), a bar, and one column for each leaf, so the leaves of every row stand under each other in columns of their
    /// own, whatever the font.</summary>
    public TableSeries Table { get; }

    /// <summary>The line that says how to read the display, such as <c>Key: 1 | 2 = 1.2</c>, at the lower left of the plot.</summary>
    public Annotation Key { get; }
}
