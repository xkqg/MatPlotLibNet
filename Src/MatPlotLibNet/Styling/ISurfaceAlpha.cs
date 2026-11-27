// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>A 3-D surface (a gridded surface, a triangulated one) as its opacity is decided: it rests at
/// <see cref="PatchLook.RestingAlpha"/> and takes the number its theme names for surfaces wherever it names one, not only where it
/// draws patches as matplotlib draws collections (matplotlib's <c>plot_surface</c> and <c>plot_trisurf</c> are opaque). A kind
/// states only the opacity its author gave; the rest is said here once, as <see cref="IDotSeries"/> says it for dots.</summary>
internal interface ISurfaceAlpha : IGivenAlpha
{
    double IGivenAlpha.RestingAlpha => PatchLook.RestingAlpha;

    /// <summary>The opacity the theme's patch look names for surfaces, wherever it names one.</summary>
    double? IGivenAlpha.ThemeAlphaOf(ThemeLooks looks) => looks.Patch.Alpha;
}
