// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>The geometry of one SVG shape: its opening tag and the attributes that place it, up to its paint. A shape drawn
/// (<see cref="Rendering.Svg.SvgRenderContext.SvgMarkWriter.Shape"/>) and the shape of a clip (<see cref="SvgElementBuffer.PushClip{TOutline}"/>) are written
/// by the same geometry, so a rectangle or a path is spelled once. A value made for the shape it writes, so writing one allocates
/// nothing.</summary>
internal interface ISvgGeometry
{
    /// <summary>Whether the outline has a corner a join of its stroke can show on: false for a circle and an ellipse.</summary>
    bool HasCorners { get; }

    /// <summary>Writes the opening tag and the geometry attributes.</summary>
    /// <param name="markup">The markup the shape is written into.</param>
    void WriteGeometry(StringBuilder markup);
}
