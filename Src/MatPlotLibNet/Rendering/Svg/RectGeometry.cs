// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>A <c>&lt;rect&gt;</c>: a rectangle drawn, and the rectangle of a clip.</summary>
/// <param name="Rect">Where the rectangle stands.</param>
internal readonly record struct RectGeometry(Rect Rect) : ISvgGeometry
{
    /// <inheritdoc />
    public bool HasCorners => true;

    /// <inheritdoc />
    public void WriteGeometry(StringBuilder markup) =>
        markup.Append("<rect x=\"").AppendSvgNumber(Rect.X).Append("\" y=\"").AppendSvgNumber(Rect.Y)
              .Append("\" width=\"").AppendSvgNumber(Rect.Width).Append("\" height=\"").AppendSvgNumber(Rect.Height).Append('"');
}
