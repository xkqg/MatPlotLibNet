// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>The edge of a patch its author gave: the colour and the width in pixels, each <see langword="null"/> while none was given
/// and the theme decides (<see cref="Theme.PatchEdgeOf"/>), and the opacity the whole patch is drawn at, which matplotlib applies to
/// the edge as well as the face.</summary>
/// <param name="Color">The edge colour the author gave, <see langword="null"/> when none.</param>
/// <param name="Width">The edge width the author gave, in pixels, <see langword="null"/> when none.</param>
/// <param name="Alpha">The opacity, 0 to 1, that replaces the opacity of the patch's colours (matplotlib's <c>alpha</c> of the artist),
/// <see langword="null"/> when none replaces it: each colour keeps its own.</param>
internal readonly record struct GivenPatchEdge(Color? Color, double? Width, double? Alpha = null);
