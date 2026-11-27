// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>How far one tick label reaches once turned: away from the edge it hangs from, and before and after its anchor
/// along the edge (<see cref="TickDrawContext.Reach"/>).</summary>
/// <param name="Away">From the line past the mark and the pad to the far side of the label, away from the edge.</param>
/// <param name="Before">How far the label stands before its anchor along the edge: to the left on a top or bottom edge, above
/// on a left or right one; never negative.</param>
/// <param name="After">How far it stands after its anchor along the edge; never negative.</param>
internal readonly record struct LabelReach(double Away, double Before, double After);
