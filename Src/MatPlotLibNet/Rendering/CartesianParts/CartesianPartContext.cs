// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.CartesianParts;

/// <summary>What every part of one Cartesian axes render draws with: the axes, its plot area, the context it draws into
/// and the active theme.</summary>
/// <param name="Axes">The axes being drawn.</param>
/// <param name="PlotArea">The plot area, in pixels.</param>
/// <param name="Ctx">The context the parts draw into and measure with.</param>
/// <param name="Theme">The active theme.</param>
internal readonly record struct CartesianPartContext(Axes Axes, Rect PlotArea, IRenderContext Ctx, Theme Theme);
