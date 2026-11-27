// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>How two segments of a stroke meet at a corner: matplotlib's <c>JoinStyle</c>. The default is the mitre, which every stroke
/// the library drew before the join was a value; a line that names none (<see cref="StrokeStyle.Join"/> null) is joined as its theme
/// says. A byte, so a style handed by value to every shape drawn holds it in the padding of
/// its layout and is no bigger for it.</summary>
public enum JoinStyle : byte
{
    /// <summary>The outer edges are carried on until they meet in a point (the SVG and Skia default; matplotlib's <c>miter</c>, the join of
    /// every patch).</summary>
    Miter = 0,

    /// <summary>The corner is a circle of half the width round the vertex (matplotlib's <c>round</c>, the join of a line and of a
    /// collection).</summary>
    Round,

    /// <summary>The corner is cut straight across (matplotlib's <c>bevel</c>).</summary>
    Bevel,
}
