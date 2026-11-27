// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Layout;

/// <summary>What an axes draws past its spines, measured only when it is asked for: a colour bar's room
/// (<see cref="ColorBarRendering.ColorBarRoom.ClearanceOf"/>) asks for what it stands past and nothing more. The renderer of the axes
/// answers as it draws (<see cref="AxesRenderer"/>), the layout as it lays the axes out, and <see cref="MeasuredBands"/> with what was
/// measured already.</summary>
internal interface IAxesBands
{
    /// <summary>What the axes draws past its top and bottom spines (<see cref="AxesRenderer.MeasureEndBands"/>).</summary>
    /// <returns>The bands at the top and the bottom.</returns>
    EdgeBands Ends();

    /// <summary>What the axes draws past all four spines (<see cref="AxesRenderer.MeasureEdgeBands"/>).</summary>
    /// <returns>The bands at the four edges.</returns>
    EdgeBands Edges();

    /// <summary>How far past its right spine the axes draws (<see cref="RightBand.Past"/>): its secondary Y axes, or its mirrored Y
    /// tick labels, then its end labels.</summary>
    /// <returns>The distance past the spine, in pixels.</returns>
    double PastRightSpine();
}
