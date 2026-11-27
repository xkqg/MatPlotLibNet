// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.ColorBarRendering;

/// <summary>The points of a layout at which the room of a colour bar is kept (<see cref="ColorBarRoom.SettlesAt"/>), in the order the
/// layout reaches them (<see cref="Layout.ConstrainedLayoutEngine"/>).</summary>
internal enum BarRoomStage
{
    /// <summary>While the edges of each axes are measured: the bar stands in the margin past what the axes draws, so its room is part
    /// of the edge's need.</summary>
    Measure,

    /// <summary>Once the sides are measured: a bar beside the axes in a slot of the cell may reach past the cell.</summary>
    Sides,

    /// <summary>Once the top and the bottom are measured: a bar below the axes in a slot of the cell may reach past the cell.</summary>
    Ends,

    /// <summary>Once every other edge is settled: a bar in the constrained layout's margin, whose size follows the plot's.</summary>
    Last,
}
