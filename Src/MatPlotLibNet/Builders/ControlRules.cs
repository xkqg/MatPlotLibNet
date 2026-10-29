// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet;

/// <summary>The tests for a process out of control that a Shewhart chart can mark (<see cref="XYAxesExtensions.ShewhartChart"/>),
/// numbered as Nelson numbers them (Nelson 1984, "The Shewhart control chart: tests for special causes"). Combine them with
/// <c>|</c>: the chart marks the points any of them flags.
/// <para>The zones are laid out from the control limits the chart is drawn with: one sigma is a third of the way from the
/// centre line to the upper control limit. Zone C is within one sigma of the centre line, zone B between one and two sigma, and
/// zone A between two and three. A pattern is flagged at the point that COMPLETES it, and at every later point that keeps it
/// complete; a missing value (NaN) or an infinity cannot be placed, so it breaks every pattern through it and is never flagged
/// itself.</para></summary>
[Flags]
public enum ControlRules
{
    /// <summary>No test: nothing is marked.</summary>
    None = 0,

    /// <summary>Test 1: one point beyond a control limit (strictly: a point on a limit is not beyond it).</summary>
    Nelson1 = 1,

    /// <summary>Test 2: nine points in a row on the same side of the centre line (a point on it is on neither side).</summary>
    Nelson2 = 1 << 1,

    /// <summary>Test 3: six points in a row steadily increasing, or steadily decreasing (a tie breaks the run).</summary>
    Nelson3 = 1 << 2,

    /// <summary>Test 4: fourteen points in a row alternating up and down.</summary>
    Nelson4 = 1 << 3,

    /// <summary>Test 5: two of three points in a row beyond two sigma from the centre line, on the same side.</summary>
    Nelson5 = 1 << 4,

    /// <summary>Test 6: four of five points in a row beyond one sigma from the centre line, on the same side.</summary>
    Nelson6 = 1 << 5,

    /// <summary>Test 7: fifteen points in a row within one sigma of the centre line, on either side (zone C).</summary>
    Nelson7 = 1 << 6,

    /// <summary>Test 8: eight points in a row more than one sigma from the centre line, on either side (none in zone C).</summary>
    Nelson8 = 1 << 7,

    /// <summary>All eight tests.</summary>
    All = Nelson1 | Nelson2 | Nelson3 | Nelson4 | Nelson5 | Nelson6 | Nelson7 | Nelson8,
}
