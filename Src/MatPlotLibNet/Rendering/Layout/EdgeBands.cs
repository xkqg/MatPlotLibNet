// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Layout;

/// <summary>What an axes draws past its top and its right spine besides the title and the end labels, measured by the
/// renderer that draws it (<see cref="AxesRenderer.MeasureEdgeBands"/>).</summary>
/// <param name="Top">From the top spine to the top of what is drawn above it below the axes title: the mirrored X tick
/// labels, or the secondary X axis that owns the edge (its tick labels, then its title). 0 when nothing is drawn
/// there.</param>
/// <param name="Right">From the right spine to the far side of the mirrored Y tick labels; 0 when none are drawn there.
/// The secondary Y axis is measured on its own (<see cref="ConstrainedLayoutEngine.SecondaryAxisBand"/>) and
/// <see cref="RightBand.Axis"/> takes the wider.</param>
internal readonly record struct EdgeBands(double Top, double Right);
