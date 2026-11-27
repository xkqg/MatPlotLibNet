// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>A <c>&lt;path&gt;</c> through its segments: a path drawn, and the outline of a clip.</summary>
/// <param name="Segments">The segments of the path, in order.</param>
internal readonly record struct PathGeometry(IReadOnlyList<PathSegment> Segments) : ISvgGeometry
{
    /// <inheritdoc />
    public bool HasCorners => true;

    /// <inheritdoc />
    public void WriteGeometry(StringBuilder markup) => markup.Append("<path d=\"").AppendSvgPath(Segments).Append('"');
}
