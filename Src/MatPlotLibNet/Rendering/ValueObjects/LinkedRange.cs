// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>The range a linked axis shows (<see cref="Models.AxisTransform.ImageOf"/>): the image of the primary axis' range in
/// the linked unit, low to high, which its ticks are laid out over, and whether the transform runs up or down over it.</summary>
/// <param name="Interval">The image of the primary range, <see cref="Range1D.Lo"/> below <see cref="Range1D.Hi"/>.</param>
/// <param name="Increasing">Whether the low end of the primary range maps to <see cref="Range1D.Lo"/> of the image (the
/// transform runs up), or to its <see cref="Range1D.Hi"/> (it runs down).</param>
internal readonly record struct LinkedRange(Range1D Interval, bool Increasing);
