// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>A surface that clips to an outline, not only to a rectangle: the SVG writer, the Skia canvas and the MAUI canvas, so the
/// marks of a plot that is not a rectangle (the disc of a polar axes, the triangle of a ternary one) are cut where the plot ends, as
/// matplotlib clips an artist to the patch of its axes. A renderer reaches it through <see cref="RenderContextScopeExtensions"/> on any
/// <see cref="IRenderContext"/>, where a surface of your own clips to the box around the outline instead. It lives here and not on
/// <see cref="IRenderContext"/> so that an application can still implement, proxy or mock the public interface.</summary>
internal interface IOutlineClipRenderContext : IRenderContext
{
    /// <summary>Pushes a clip to the inside of <paramref name="outline"/> onto the clip stack, by the nonzero rule (a ring is its
    /// outer circle one way round and its inner circle the other); <see cref="IRenderContext.PopClip"/> takes it off.</summary>
    /// <param name="outline">The outline in pixels: one or more closed subpaths.</param>
    void PushClip(IReadOnlyList<PathSegment> outline);
}
