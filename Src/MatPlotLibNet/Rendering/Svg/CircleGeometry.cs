// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>A <c>&lt;circle&gt;</c> around its centre.</summary>
/// <param name="Center">The centre.</param>
/// <param name="Radius">The radius, in pixels.</param>
internal readonly record struct CircleGeometry(Point Center, double Radius) : ISvgGeometry
{
    /// <inheritdoc />
    public bool HasCorners => false;

    /// <inheritdoc />
    public void WriteGeometry(StringBuilder markup) =>
        markup.Append("<circle cx=\"").AppendSvgNumber(Center.X).Append("\" cy=\"").AppendSvgNumber(Center.Y)
              .Append("\" r=\"").AppendSvgNumber(Radius).Append('"');
}
