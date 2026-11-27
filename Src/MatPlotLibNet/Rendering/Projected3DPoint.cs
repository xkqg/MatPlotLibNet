// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>A data point as matplotlib's <c>proj3d.proj_transform</c> gives it: <see cref="X"/> and <see cref="Y"/> over the view,
/// <see cref="Z"/> the depth, which grows with the distance from the camera.</summary>
/// <param name="X">The projected x.</param>
/// <param name="Y">The projected y.</param>
/// <param name="Z">The projected depth.</param>
internal readonly record struct Projected3DPoint(double X, double Y, double Z);
