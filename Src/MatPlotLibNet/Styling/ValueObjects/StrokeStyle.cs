// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>
/// Bundles the stroke properties — colour, thickness, and dash style — that travel together
/// through every line-drawing operation on <c>IRenderContext</c>.
/// </summary>
/// <param name="Color">The stroke colour.</param>
/// <param name="Thickness">The stroke width in pixels.</param>
/// <param name="Style">The dash pattern to apply.</param>
public readonly record struct StrokeStyle(Color Color, double Thickness, LineStyle Style)
{
    /// <summary>
    /// Gets a value indicating whether this stroke produces visible output: it has a positive
    /// thickness and a drawable dash style (i.e. not <see cref="LineStyle.None"/>).
    /// </summary>
    public bool IsVisible => Thickness > 0 && Style != LineStyle.None;
}
