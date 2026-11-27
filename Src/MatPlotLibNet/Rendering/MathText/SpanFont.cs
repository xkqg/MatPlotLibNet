// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>What a run of math is set in, made by the run's box (<see cref="GlyphsBox"/>): a face and a size. The measure reads the
/// two apart (<see cref="MathMeasure.Of"/>), so a layout asked only for its box makes no font for a run; a layout that places its ink
/// makes the font each run is drawn in once (<see cref="Colored"/>, kept as <see cref="MathGlyphInk.Font"/>).</summary>
/// <param name="Face">The font whose family, weight and slant the run is set in.</param>
/// <param name="Size">The size the run is set at, in pixels.</param>
internal readonly record struct SpanFont(Font Face, double Size)
{
    /// <summary>The run's font: <see cref="Face"/> itself when it is at <see cref="Size"/>, else the face at that size.</summary>
    internal Font Font => Size == Face.Size ? Face : Face with { Size = Size };

    /// <summary>The run's font in the label's colour <paramref name="color"/>: a face of a math font set's own names no colour.</summary>
    /// <param name="color">The label's colour.</param>
    /// <returns>The font the run is drawn in.</returns>
    internal Font Colored(Color? color) =>
        Size == Face.Size && Face.Color == color ? Face : Face with { Size = Size, Color = color };
}
