// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>An <c>&lt;ellipse&gt;</c> inscribed in its bounds.</summary>
/// <param name="Bounds">The rectangle the ellipse is inscribed in.</param>
internal readonly record struct EllipseGeometry(Rect Bounds) : ISvgGeometry
{
    /// <inheritdoc />
    public bool HasCorners => false;

    /// <inheritdoc />
    public void WriteGeometry(StringBuilder markup)
    {
        var centre = Bounds.Center;
        markup.Append("<ellipse cx=\"").AppendSvgNumber(centre.X).Append("\" cy=\"").AppendSvgNumber(centre.Y)
              .Append("\" rx=\"").AppendSvgNumber(Bounds.Width / 2).Append("\" ry=\"").AppendSvgNumber(Bounds.Height / 2).Append('"');
    }
}
