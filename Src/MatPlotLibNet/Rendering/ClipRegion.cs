// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>Where the marks of a plot are cut off: its plot area, or, for a plot that is not a rectangle, its outline (the disc or ring
/// of a polar axes, the triangle of a ternary one) and the box around it, which a surface that clips to rectangles alone uses instead
/// (<see cref="RenderContextScopeExtensions"/>).</summary>
/// <param name="Bounds">The plot area, or the box around <paramref name="Outline"/>.</param>
/// <param name="Outline">The outline in pixels, closed; <see langword="null"/> for the rectangle <paramref name="Bounds"/> itself.</param>
internal readonly record struct ClipRegion(Rect Bounds, IReadOnlyList<PathSegment>? Outline = null);
