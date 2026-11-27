// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>What a hatch is laid on, as the rule for its colour reads it (<see cref="PatchLook.HatchOver"/>): the edge of its shape
/// as drawn, the edge its author gave, the fill under it and the opacity given to the shape.</summary>
/// <param name="Edge">The colour the shape's edge is drawn in, <see langword="null"/> when it has none.</param>
/// <param name="GivenEdge">The edge colour the shape's author gave, <see langword="null"/> when none.</param>
/// <param name="Fill">The colour the shape is filled with, <see langword="null"/> when it is not known.</param>
/// <param name="Alpha">The opacity given to the shape, <see langword="null"/> when none.</param>
internal readonly record struct HatchGround(Color? Edge, Color? GivenEdge = null, Color? Fill = null, double? Alpha = null);
