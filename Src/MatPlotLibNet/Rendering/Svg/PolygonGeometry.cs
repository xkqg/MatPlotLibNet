// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>A <c>&lt;polygon&gt;</c> through its points.</summary>
/// <param name="Points">The corners, in order.</param>
internal readonly record struct PolygonGeometry(IReadOnlyList<Point> Points) : ISvgGeometry
{
    /// <inheritdoc />
    public bool HasCorners => true;

    /// <inheritdoc />
    public void WriteGeometry(StringBuilder markup)
    {
        markup.Append("<polygon points=\"");
        for (int i = 0; i < Points.Count; i++)
        {
            if (i > 0) markup.Append(' ');
            markup.AppendSvgPoint(Points[i]);
        }
        markup.Append('"');
    }
}
