// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>A shape on a canvas, with the layers a canvas surface knows how to paint it in: the Skia and MAUI surfaces each hand one over
/// (their geometry and their canvas), and <see cref="CanvasShapeExtensions.PaintIn"/> says which layers are painted and in what order. The
/// SVG surface writes a shape as one element that carries its paint, and has no layers to be asked for.</summary>
internal interface ICanvasShape
{
    /// <summary>Paints the fill of the shape: its flat colour, or its gradient, antialiased unless the style is aliased.</summary>
    /// <param name="style">The style, whose fill shows.</param>
    void Fill(in ShapeStyle style);

    /// <summary>Paints the hatch of the shape over the fill just painted.</summary>
    /// <param name="style">The style, whose hatch shows.</param>
    void Hatch(in ShapeStyle style);

    /// <summary>Draws the outline of the shape with <paramref name="outline"/>.</summary>
    /// <param name="outline">The line the outline is drawn with (<see cref="ShapeStyle.OutlineStroke"/>).</param>
    void Stroke(StrokeStyle outline);
}

/// <summary>The one sequence a shape is painted in on a canvas, whatever the shape (a polygon, a circle, a rectangle, an ellipse, a path)
/// and whatever the canvas: the fill when it shows, the hatch over it when it shows (a hatch needs a fill), then the outline when it shows,
/// so the outline stands on the fill.</summary>
internal static class CanvasShapeExtensions
{
    /// <typeparam name="TShape">The kind of shape a surface paints.</typeparam>
    /// <param name="shape">The shape, a value the method is made for (<typeparamref name="TShape"/>), so a call allocates nothing and
    /// asks its layers without an interface dispatch.</param>
    extension<TShape>(TShape shape) where TShape : struct, ICanvasShape
    {
        /// <summary>Paints the shape in <paramref name="style"/>: the fill, the hatch over it, then the outline, each when it shows.</summary>
        /// <param name="style">The fill, the hatch and the outline of the shape.</param>
        internal void PaintIn(in ShapeStyle style)
        {
            if (style.HasVisibleFill)
            {
                shape.Fill(style);
                if (style.HasVisibleHatch) shape.Hatch(style);
            }

            if (style.HasVisibleStroke) shape.Stroke(style.OutlineStroke());
        }
    }
}
