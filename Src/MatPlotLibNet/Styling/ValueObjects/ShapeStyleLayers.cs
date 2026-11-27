// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>A shape with both a gradient and a hatch is two layers: the gradient with the stroke, and the hatch laid over
/// it. A backend that paints the hatch as a pattern whose tile carries the fill underneath (SVG) draws the two as two
/// elements; the raster backend paints them in that order on one canvas without this. The block also holds the one rule for
/// the colour of a hatch's strokes, which every backend reads.</summary>
internal static class ShapeStyleLayers
{
    extension(ShapeStyle shape)
    {
        /// <summary>This shape with <paramref name="hatch"/> laid over its fill: the pattern and the colour of its strokes.</summary>
        /// <param name="hatch">The pattern and the colour of its strokes; a colour of <see langword="null"/> leaves the choice to the backend.</param>
        /// <returns>The same shape, hatched.</returns>
        internal ShapeStyle WithHatch(HatchFill hatch) => shape with { Hatch = hatch.Pattern, HatchColor = hatch.Color };

        /// <summary>The colour of the hatch strokes: the shape's <see cref="ShapeStyle.HatchColor"/>, or without one the highest
        /// contrast against the fill (black on a light fill, white on a dark one), so one property is enough to get a visible
        /// hatch. The one rule every backend paints a hatch by. Read only for a shape that has a visible hatch
        /// (<see cref="ShapeStyle.HasVisibleHatch"/>), which has a fill.</summary>
        internal Color HatchStroke => shape.HatchColor ?? shape.Fill!.Value.ContrastingTextColor();

        /// <summary>The lower layer: the same fill, gradient and stroke, without the hatch.</summary>
        internal ShapeStyle UnderTheHatch => shape with { Hatch = HatchPattern.None, HatchColor = null };

        /// <summary>The upper layer: the hatch strokes alone, over a fill that is not there, in the colour the shape's own
        /// hatch resolves to.</summary>
        internal ShapeStyle HatchLayer => ShapeStyle.Filled(Colors.Transparent).WithHatch(new HatchFill(shape.Hatch, shape.HatchStroke));
    }
}
