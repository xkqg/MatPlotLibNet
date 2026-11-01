// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Patches;

/// <summary>Which part of an <see cref="Arrow"/> is drawn: the whole arrow, or one half of it cut along its shaft (matplotlib's
/// <c>shape</c> of a <c>FancyArrow</c>).</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder, remove, or renumber.</remarks>
/// <example>
/// The half of an arrow below its shaft:
/// <code>
/// ax.AddPatch(new Arrow(new(0, 0), 2, 0) { Width = 0.1, Shape = ArrowHeadShape.Left });
/// </code>
/// </example>
public enum ArrowHeadShape
{
    /// <summary>The whole arrow: the shaft and both sides of the head.</summary>
    Full = 0,

    /// <summary>The half of the arrow below its shaft when it points right: the side a clockwise quarter turn of its direction
    /// points to, as matplotlib's <c>shape='left'</c> draws it.</summary>
    Left = 1,

    /// <summary>The other half: above the shaft when the arrow points right, matplotlib's <c>shape='right'</c>.</summary>
    Right = 2,
}
