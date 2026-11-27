// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>What a drawing of ticks puts on the page: the labels, the marks, or both, a mark beside its label. A theme that draws the
/// axis in matplotlib's z-order (<see cref="ZOrderExtensions"/>' <c>TickMarksZ</c>) draws the labels before the data and the marks at
/// the axis' place among the data; every other theme draws both together, under the data. What a value holds is asked of it
/// (<see cref="TickInkExtensions"/>).</summary>
[Flags]
internal enum TickInk
{
    /// <summary>Nothing: no label and no mark.</summary>
    None = 0,

    /// <summary>The labels (and an axis' offset text), which are measured as they are drawn.</summary>
    Labels = 1,

    /// <summary>The major and minor tick marks.</summary>
    Marks = 2,

    /// <summary>Both, each mark beside its label.</summary>
    Both = Labels | Marks,
}
