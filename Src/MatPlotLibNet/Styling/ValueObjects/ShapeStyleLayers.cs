// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>A shape with both a gradient and a hatch is two layers: the gradient with the stroke, and the hatch laid over
/// it. A backend that paints the hatch as a pattern whose tile carries the fill underneath (SVG) draws the two as two
/// elements; the raster backend paints them in that order on one canvas without this.</summary>
internal static class ShapeStyleLayers
{
    extension(ShapeStyle shape)
    {
        /// <summary>The lower layer: the same fill, gradient and stroke, without the hatch.</summary>
        internal ShapeStyle UnderTheHatch => shape with { Hatch = HatchPattern.None, HatchColor = null };

        /// <summary>The upper layer: the hatch strokes alone, over a fill that is not there, in the colour the shape's own
        /// hatch resolves to.</summary>
        internal ShapeStyle HatchLayer => new(new Color(0, 0, 0, 0), null, 0)
        {
            Hatch = shape.Hatch,
            HatchColor = shape.HatchColor ?? shape.Fill!.Value.ContrastingTextColor(),
        };
    }
}
