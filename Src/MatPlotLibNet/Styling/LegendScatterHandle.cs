// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Where the markers of a scatter's legend handle stand when the legend names no number of them
/// (<see cref="LegendLook.ScatterHandle"/>).</summary>
internal enum LegendScatterHandle
{
    /// <summary>One marker, half the handle's height in from its start and in the middle of its height.</summary>
    AtHandleStart,

    /// <summary>As matplotlib's <c>HandlerPathCollection</c> places them (<see cref="LegendLook.MarkerStops"/>,
    /// <see cref="LegendLook.ScatterHeights"/>).</summary>
    Spread,
}

