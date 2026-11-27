// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>The colour a hatch is drawn in where its author names none (matplotlib's <c>hatch.color</c>), held by
/// <see cref="PatchLook.Hatch"/>. <see langword="null"/> there (the library's own themes) leaves the colour to the backend, which
/// draws the hatch in the colour that reads best against its fill.</summary>
/// <param name="FollowsEdge">Whether the hatch takes the colour of the shape's edge (<c>hatch.color = 'edge'</c>, the default style);
/// <see langword="false"/> draws it in <paramref name="Fallback"/> whatever the edge (<c>hatch.color = 'k'</c>, the classic style).</param>
/// <param name="Fallback">The colour of a hatch that does not follow an edge, or whose shape has none: <c>patch.edgecolor</c>, black in
/// both of matplotlib's styles (<c>Patch.get_hatchcolor</c> reads it for a fully transparent edge).</param>
internal readonly record struct HatchColorRule(bool FollowsEdge, Color Fallback);